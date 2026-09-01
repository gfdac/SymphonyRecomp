using System;
using RecompOne.Runtime.Context;
using RecompOne.Runtime.Host.Window;
using RecompOne.Runtime.Memory;
using Sotn;

namespace Recompiled;

public static class HardcoreMode
{
    public enum Difficulty
    {
        Normal = 0,
        Hard = 1,
        Hardcore = 2 // Hard + Permadeath
    }

    public static Difficulty CurrentDifficulty = Difficulty.Normal;
    public static float DamageMultiplier = 2.0f; // 2x damage received on Hard/Hardcore
    public static bool AntiPotionSpam = true;

    private static int _prevHp = -1;
    private static bool _permadeathTriggered = false;

    public static bool IsHard => CurrentDifficulty >= Difficulty.Hard;
    public static bool IsHardcore => CurrentDifficulty == Difficulty.Hardcore;

    public static void Load()
    {
        var v = RecompOne.Runtime.Runtime.View;
        CurrentDifficulty = (Difficulty)v.GetInt("GameDifficulty", 0);
        DamageMultiplier = v.GetFloat("HardDamageMultiplier", 2.0f);
        AntiPotionSpam = v.GetBool("HardAntiPotionSpam", true);
    }

    public static void Save()
    {
        var v = RecompOne.Runtime.Runtime.View;
        v.SetInt("GameDifficulty", (int)CurrentDifficulty);
        v.SetFloat("HardDamageMultiplier", DamageMultiplier);
        v.SetBool("HardAntiPotionSpam", AntiPotionSpam);
        RecompOne.Runtime.Runtime.SaveView();
    }

    public static void Update(CpuContext c, IMemory m)
    {
        if (!Game.Available || !Game.InGame || Game.IsLoading)
        {
            _prevHp = -1;
            _permadeathTriggered = false;
            return;
        }

        int curHp = Player.Hp;

        if (_prevHp < 0)
        {
            _prevHp = curHp;
            return;
        }

        // Hard Mode Damage Scaling
        if (IsHard && curHp < _prevHp)
        {
            int damageTaken = _prevHp - curHp;
            if (damageTaken > 0 && DamageMultiplier > 1.0f)
            {
                int extraDamage = (int)(damageTaken * (DamageMultiplier - 1.0f));
                int newHp = Math.Max(0, curHp - extraDamage);
                Player.Hp = newHp;
                curHp = newHp;
            }
        }

        // Permadeath Check (Hardcore Mode)
        if (IsHardcore && curHp <= 0 && !_permadeathTriggered && StageManager.CurrentStage != Stage.Prologue)
        {
            _permadeathTriggered = true;
            TriggerPermadeath(m);
        }

        _prevHp = curHp;
    }

    private static void TriggerPermadeath(IMemory m)
    {
        ToastNotifications.ShowText(
            "☠ MODO HARDCORE - PERMADEATH!",
            "Você sucumbiu no castelo. Seu progresso foi permanentemente apagado!",
            null,
            8.0f);

        // Wipe warp rooms, map, and castle flags to enforce permadeath
        Progress.WarpsFirstCastle = (Warp)0;
        Progress.WarpsSecondCastle = (Warp)0;
        Map.HideAll();

        // Wipe inventory relics
        for (int i = 0; i < Inventory.RelicCount; i++)
        {
            m.WriteU8(Game.StatusAddr + (uint)i, 0);
        }
    }
}
