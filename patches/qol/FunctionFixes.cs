using RecompOne.Runtime.Context;
using RecompOne.Runtime.Dispatch;
using RecompOne.Runtime.Memory;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RecompOne.Runtime.Hle;
using Sotn;

namespace Recompiled;

public static partial class FunctionFixes
{
    // Scylla Softlock Door Fix
    // If the player defeats Scylla and enters the room behind before the water level reaches the top the door below will remain locked.
    // This hooks onto function 801A1BE8 in bo3 and checks if Scylla has been defeated and the door is still locked. It unlocks it if needed.
    public static void ScyllaDoorFix(CpuContext c, IMemory m)
    {
        if (QualityOfLife.BugFixes == true)
        {
            byte doorByte = m.ReadU8(0x80180c66);
            UInt32 scyllaDefeat = m.ReadU32(0x8003CA3C);

            if (doorByte == 1 && scyllaDefeat > 0)
            {
                m.WriteU8(0x80180c66, 0x00);
            }
        }
    }
    // Olrox Extended Death Explosion Fix
    // If the player kills Olrox at a specific time when he attacks with his hands the explosion sequence will continue for 18 minutes due to
    // a timer underflow. This function hooks an entity function to see if Olrox is defeated and the timer has underflowed and corrects it.
    public static void OlroxExploFix(CpuContext c, IMemory m)
    {
        if (QualityOfLife.BugFixes == true)
        {
            UInt16 exploDuration = m.ReadU16(0x80077c64);
            UInt32 olroxDefeat = m.ReadU32(0x8003CA2C);

            if (exploDuration > 0x7000 && olroxDefeat > 0)
            {
                m.WriteU16(0x80077c64, 0x60);
            }
        }
    }
    // Clock Tower Softlock Fix
    // Under certain conditions when leaving the room unlocked by hitting the four gears you can trigger a "Reverse Shiftline" and if you
    // leave the room using the bottom right exit you can become stuck in the floor. This is fixed by moving the entity so the Reverse Shiftline
    // condition does not occur. 
    public static void ClockCollisionFix(CpuContext c, IMemory m)
    {
        if (QualityOfLife.BugFixes == true)
        {
            m.WriteU16(0x80182476, 0x80);
        }
    }

    // Marble Gallery Large Room Scroll Bug Fix
    // In the large room that snakes back and forth if you kill Ctulhu in a specific way moving to the right you can cause an entity that changes
    // screen scrolling parameters to not spawn leaving you unable to go up. This is fixed by setting the spawn priority for this entity.
    public static void ScreenScrollFix(CpuContext c, IMemory m)
    {
        if (QualityOfLife.BugFixes == true)
        {
            // Force screen scroll entity in Marble Gallery near Ctulhu to spawn
            if (m.ReadU8(0x800974a0) == 0x00)
            {
                m.WriteU8(0x80182f9f, 0xa0);
                m.WriteU8(0x80183e51, 0xa0);
            }
        }
    }

    // Fix Minotaur & Werewolf Crash when entering from the right using Bat
    // Hook func_801A6EF8 in bo2
    public static void MinotaurAndWerewolfFix(CpuContext c, IMemory m)
    {
        if (QualityOfLife.BugFixes == false)
            return;

        byte Step = m.ReadU8(0x80077A84);
        UInt16 PlayerStep = m.ReadU16(0x80073404);
        UInt16 PlayerPosX = m.ReadU16(0x800733DA);

        if (Step < 4 && PlayerPosX > 0x40 && (PlayerStep == 5 || PlayerStep == 0x18 || PlayerStep == 0x19))
        {
            m.WriteU16(0x80073404, 0);
            m.WriteU16(0x80073406, 0);
            m.WriteU16(0x800733EE, 0x8100);
            m.WriteU16(0x800733F0, 0);
        }
    }

    // Alucard Effect-Pool Never-Allocated Crash Fix
    //
    // Alucard's per-frame update (EntityAlucard -> func_801093C4) walks a 6-node chain out of the
    // shared GPU-primitive pool (base 0x80086FEC, stride 0x34) using an index stored at 0x800734F8.
    // That index is written in exactly one place in the whole game: FUN_80109594, part of Alucard's
    // full entity init, which itself only runs when EngineStepAddr (0x8003C9A4) is 0 AND the current
    // stage is neither the Prologue nor Richter mode. Since every normal playthrough starts in the
    // Prologue, that condition is never true there, and (confirmed via a crash dump with pool-chain
    // diagnostics) it never becomes true again for the rest of the session either -- so 0x800734F8
    // stays 0 for the whole game. Index 0 isn't "unallocated", it's the raw base of the pool array,
    // so func_801093C4 ends up walking whatever happens to be linked there instead of a chain it
    // actually owns. That's fine as long as those first slots stay coincidentally chained, but once
    // anything else consumes/frees nodes near the front of the pool, the chain it walks can end
    // early (confirmed: chain from index 0 terminated after 4 nodes instead of 6) or point at
    // memory that isn't part of the pool at all, and func_801093C4's loop doesn't check for that --
    // it just crashes with "unmapped address" a couple reads later.
    //
    // Fix: once, the first time we see Alucard in active gameplay with a still-zero index, allocate
    // a real 8-node chain ourselves via GameApi.AllocPrimitives -- the same allocator FUN_80109594
    // itself calls (through the same 0x8003C7B8 API slot the widescreen water code uses) -- and mark
    // the first 6 nodes exactly the way FUN_80109594 does. We deliberately don't call FUN_80109594
    // itself: it also zeroes Alucard's entire stat block (HP/MP/stats/etc), which would be correct
    // only at a true fresh entity spawn, not mid-playthrough.
    const uint AlucardEffectPoolIndexAddr = 0x800734F8;
    const uint AlucardEffectPoolFlagsAddr = 0x800734C8; // Entity[1].Flags (Entity 0 = Alucard, stride 0xBC)
    const uint PrimitivePoolBase = 0x80086FEC;
    const uint PrimitivePoolStride = 0x34;
    const int PrimitivePoolCount = 0x400; // matches FUN_800edc80's own search bound

    static int _alucardEffectPoolRepairs;
    const int MaxRepairsPerSession = 20; // give up logging/retrying past this so a persistent conflict doesn't spam-reallocate forever

    // First pass here (see the long comment above) assumed a one-time allocation would be
    // enough, like FUN_80109594's own single call. It ran, and correctly got index 0 back --
    // that's a legitimate allocation, not "still unallocated" (0 just happens to double as
    // both). But the crash still recurred later, and confirmed via xref there is NO other
    // writer of 0x800734F8 anywhere in the DRA overlay -- so the index itself can't have
    // reverted. What must be happening instead is the *chain* getting shortened/corrupted by
    // something else in the shared pool later in the session, while the index we wrote stays
    // exactly as we left it. A one-shot fix can't defend against that. This now re-verifies the
    // chain every frame and re-allocates whenever it's broken, instead of trusting it forever
    // after the first repair.
    public static void AlucardEffectPoolFix(CpuContext c, IMemory m)
    {
        if (QualityOfLife.BugFixes == false) return;
        if (!Game.Available || !Game.InGame || Game.IsLoading || !Player.IsAlucard) return;
        if (Game.StageId == Stage.Prologue) return; // mirrors FUN_80109594's own exclusion, entity 1 may serve a different purpose there
        if (!Player.HasControl) return; // don't call into the engine reentrantly during a cutscene/demo -- suspected of leaving the player stuck with no control right after the Prologue ends
        if (_alucardEffectPoolRepairs >= MaxRepairsPerSession) return;

        if (ChainLooksValid(m, m.ReadU32(AlucardEffectPoolIndexAddr))) return;

        int index = GameApi.AllocPrimitives(1, 8);
        _alucardEffectPoolRepairs++;
        Console.WriteLine($"[AlucardEffectPoolFix] repair #{_alucardEffectPoolRepairs}: stage={Game.StageId} AllocPrimitives(1,8) returned {index}");
        if (index < 0) return; // pool exhausted; nothing safe to do, try again next frame

        m.WriteU32(AlucardEffectPoolIndexAddr, (uint)index);
        m.WriteU32(AlucardEffectPoolFlagsAddr, m.ReadU32(AlucardEffectPoolFlagsAddr) | 0x00800000u);

        uint node = PrimitivePoolBase + (uint)index * PrimitivePoolStride;
        var visited = new List<uint>();
        for (int i = 0; i < 6 && node != 0; i++)
        {
            visited.Add(node);
            m.WriteU16(node + 0x32, 0x10A);
            node = m.ReadU32(node);
        }
        Console.WriteLine($"[AlucardEffectPoolFix] repair #{_alucardEffectPoolRepairs}: chain = {string.Join(" -> ", visited.ConvertAll(n => $"0x{n:X8}"))} -> 0x{node:X8}");
    }

    // Walks up to 6 nodes (what func_801093C4 itself walks) and confirms each hop stays inside
    // the pool's actual address range -- a chain that goes null early, or wanders outside the
    // pool entirely, is exactly what made func_801093C4 read unmapped memory.
    static bool ChainLooksValid(IMemory m, uint index)
    {
        uint node = PrimitivePoolBase + index * PrimitivePoolStride;
        uint poolEnd = PrimitivePoolBase + (uint)PrimitivePoolCount * PrimitivePoolStride;
        for (int i = 0; i < 6; i++)
        {
            if (node < PrimitivePoolBase || node >= poolEnd) return false;
            try { node = m.ReadU32(node); }
            catch { return false; }
        }
        return true;
    }
}

