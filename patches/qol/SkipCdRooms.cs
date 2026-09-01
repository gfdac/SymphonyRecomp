using System;
using RecompOne.Runtime.Context;
using RecompOne.Runtime.Host.Window;
using RecompOne.Runtime.Memory;
using Sotn;

namespace Recompiled;

public static class SkipCdRooms
{
    public static bool Enabled = false;

    public static void Load()
    {
        var v = RecompOne.Runtime.Runtime.View;
        Enabled = v.GetBool("QolSkipCdRooms", false);
    }

    public static void Save()
    {
        var v = RecompOne.Runtime.Runtime.View;
        v.SetBool("QolSkipCdRooms", Enabled);
        RecompOne.Runtime.Runtime.SaveView();
    }

    public static void Update(CpuContext c, IMemory m)
    {
        if (!Enabled || !Game.Available || !Game.InGame) return;

        // When in loading or CD transition room, accelerate the corridor timer
        if (Game.IsLoading)
        {
            // Boost game timer during transition
            uint step = m.ReadU32(Game.EngineStepAddr);
            if (step > 0 && step < 10)
            {
                m.WriteU32(Game.EngineStepAddr, step + 1);
            }
        }
    }
}
