using System;
using System.Collections.Generic;
using RecompOne.Runtime.Context;
using RecompOne.Runtime.Host.Window;
using RecompOne.Runtime.Memory;
using Sotn;

namespace Recompiled;

public static class SpellWheel
{
    public static bool Enabled = true;
    public static bool QuickCastHotkeys = true;

    public record SpellInfo(
        Spell Spell,
        string NameKey,
        string DefaultName,
        int MpCost,
        uint StepAddr,
        byte StepVal,
        uint TimerAddr,
        byte TimerVal,
        int RequiredForm = 0 // 0 = Human, 1 = Bat, 2 = Wolf, 3 = Sword Familiar
    );

    public static readonly SpellInfo[] Spells =
    [
        new(Spell.SummonSpirit, "spell.summon_spirit", "Summon Spirit", 5, 0x80138fc8, 0x03, 0x80138fca, 0x10),
        new(Spell.TetraSpirit, "spell.tetra_spirit", "Tetra Spirit", 20, 0x80138fd0, 0x07, 0x80138fd2, 0x10),
        new(Spell.DarkMetamorphosis, "spell.dark_meta", "Dark Metamorphosis", 10, 0x80138fc4, 0x05, 0x80138fc6, 0x10),
        new(Spell.Hellfire, "spell.hellfire", "Hellfire", 15, 0x80138fcc, 0x04, 0x80138fce, 0x10),
        new(Spell.SoulSteal, "spell.soul_steal", "Soul Steal", 50, 0x80138fd8, 0x07, 0x80138fda, 0x10),
        new(Spell.SwordBrothers, "spell.sword_brothers", "Sword Brothers", 30, 0x80138fdc, 0x07, 0x80138fde, 0x10, 3),
        new(Spell.WingSmash, "spell.wing_smash", "Wing Smash", 8, 0x80137ff4, 0x07, 0x80137ff8, 0x10, 1),
    ];

    private static volatile int _pendingQuickCast = -1;

    static SpellWheel()
    {
        RecompOne.Runtime.Events.Event.AddListener<RecompOne.Runtime.Events.KeyboardEvent>(e =>
        {
            if (e.Pressed && !e.Repeat)
            {
                // Number keys 1-5 for instant quick casting if enabled
                if (QuickCastHotkeys)
                {
                    if (e.Key == (int)Silk.NET.Input.Key.Number1) _pendingQuickCast = 0; // Summon Spirit
                    else if (e.Key == (int)Silk.NET.Input.Key.Number2) _pendingQuickCast = 1; // Tetra Spirit
                    else if (e.Key == (int)Silk.NET.Input.Key.Number3) _pendingQuickCast = 2; // Dark Metamorphosis
                    else if (e.Key == (int)Silk.NET.Input.Key.Number4) _pendingQuickCast = 3; // Hellfire
                    else if (e.Key == (int)Silk.NET.Input.Key.Number5) _pendingQuickCast = 4; // Soul Steal
                    else if (e.Key == (int)Silk.NET.Input.Key.Number6) _pendingQuickCast = 5; // Sword Brothers
                }
            }
        });
    }

    public static void Load()
    {
        var v = RecompOne.Runtime.Runtime.View;
        Enabled = v.GetBool("QolSpellWheel", true);
        QuickCastHotkeys = v.GetBool("QolSpellQuickCastHotkeys", true);
    }

    public static void Save()
    {
        var v = RecompOne.Runtime.Runtime.View;
        v.SetBool("QolSpellWheel", Enabled);
        v.SetBool("QolSpellQuickCastHotkeys", QuickCastHotkeys);
        RecompOne.Runtime.Runtime.SaveView();
    }

    public static void Update(CpuContext c, IMemory m)
    {
        if (!Enabled || !Game.Available || !Game.InGame || Game.IsLoading) return;
        if (!Player.IsAlucard) return;

        int castIndex = _pendingQuickCast;
        _pendingQuickCast = -1;

        if (castIndex >= 0 && castIndex < Spells.Length)
        {
            CastSpell(Spells[castIndex], m);
        }
    }

    public static bool CastSpell(SpellInfo info, IMemory m)
    {
        if (!Game.InGame || !Player.IsAlucard) return false;

        // Check MP
        if (Player.Mp < info.MpCost)
        {
            ToastNotifications.ShowText(
                Localization.T("spell.mp_low_title"),
                $"{Localization.T("spell.mp_low_msg")} ({Player.Mp}/{info.MpCost} MP)",
                null,
                2.0f);
            return false;
        }

        // Set the internal buffer steps for the engine to trigger the spell
        m.WriteU16(info.StepAddr, info.StepVal);
        m.WriteU16(info.TimerAddr, info.TimerVal);
        m.WriteU16(0x80097494, 0x80); // Square button tapped trigger

        string spellName = Localization.T(info.NameKey);
        if (string.IsNullOrEmpty(spellName) || spellName.StartsWith("spell."))
            spellName = info.DefaultName;

        ToastNotifications.ShowText(
            spellName,
            $"MP -{info.MpCost} | Cast!",
            null,
            2.0f);

        return true;
    }
}
