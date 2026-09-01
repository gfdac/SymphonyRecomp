using System;
using System.Collections.Generic;
using RecompOne.Runtime.Context;
using RecompOne.Runtime.Host.Window;
using RecompOne.Runtime.Memory;
using Sotn;

namespace Recompiled;

public static class Achievements
{
    public record Achievement(
        string Id,
        string Title,
        string Description,
        string Category,
        int Points = 10
    );

    public static readonly Achievement[] All =
    [
        new("prologue_dracula", "What is a Man?", "Defeat Count Dracula in the Prologue.", "Story", 10),
        new("flip_castle", "Flip the Script", "Enter the Inverted Castle.", "Story", 20),
        new("save_richter", "True Sight", "Free Richter from Shaft's control without defeating him.", "Story", 25),
        new("true_ending", "Alucard's Symphony", "Defeat Shaft and complete the game with the True Ending.", "Story", 50),
        new("galamoth", "Heir of Chaos", "Defeat Galamoth, the Lord of Lightning, in the Floating Catacombs.", "Combat", 40),
        new("beelzebub", "Lord of the Flies", "Defeat Beelzebub in the Necromancy Laboratory.", "Combat", 30),
        new("crissaegrim", "Sword of the Century", "Obtain the legendary Crissaegrim blade from a Schmoo.", "Items", 30),
        new("all_spells", "Master Sorcerer", "Discover and learn all 8 of Alucard's spells.", "Mastery", 25),
        new("map_100", "Castlevania Explorer", "Uncover at least 100% of the Normal Castle map.", "Exploration", 20),
        new("map_200", "Grand Cartographer", "Uncover 200.6% or more of the entire castle map.", "Exploration", 50),
        new("level_50", "Half-Vampire Might", "Reach Level 50 with Alucard.", "Mastery", 25),
        new("richter_mode", "Belmont Ascendant", "Begin a new game as Richter Belmont.", "Secret", 15),
        new("shield_rod_combo", "Alucard Shield Power", "Combine Shield Rod with Alucard Shield to unleash devastation.", "Combat", 20),
        new("max_hearts", "Heart of Gold", "Accumulate over 300 maximum Hearts.", "Mastery", 15),
    ];

    private static readonly HashSet<string> _unlocked = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _unlockedDates = new(StringComparer.OrdinalIgnoreCase);
    private static uint _frameCounter = 0;

    public static int TotalPoints
    {
        get
        {
            int pts = 0;
            foreach (var a in All) pts += a.Points;
            return pts;
        }
    }

    public static int UnlockedPoints
    {
        get
        {
            int pts = 0;
            foreach (var a in All)
                if (IsUnlocked(a.Id)) pts += a.Points;
            return pts;
        }
    }

    public static int UnlockedCount => _unlocked.Count;
    public static int TotalCount => All.Length;

    public static bool IsUnlocked(string id) => _unlocked.Contains(id);

    public static string GetUnlockDate(string id) =>
        _unlockedDates.TryGetValue(id, out var date) ? date : "";

    public static void Load()
    {
        var v = RecompOne.Runtime.Runtime.View;
        _unlocked.Clear();
        _unlockedDates.Clear();

        foreach (var a in All)
        {
            if (v.GetBool($"Achievement_{a.Id}", false))
            {
                _unlocked.Add(a.Id);
                _unlockedDates[a.Id] = v.GetString($"AchievementDate_{a.Id}", "Unlocked");
            }
        }
    }

    public static void Save()
    {
        var v = RecompOne.Runtime.Runtime.View;
        foreach (var a in All)
        {
            bool u = _unlocked.Contains(a.Id);
            v.SetBool($"Achievement_{a.Id}", u);
            if (u && _unlockedDates.TryGetValue(a.Id, out var date))
                v.SetString($"AchievementDate_{a.Id}", date);
        }
        RecompOne.Runtime.Runtime.SaveView();
    }

    public static void Unlock(string id)
    {
        if (_unlocked.Contains(id)) return;

        Achievement? ach = null;
        foreach (var a in All)
        {
            if (a.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            {
                ach = a;
                break;
            }
        }

        if (ach == null) return;

        _unlocked.Add(ach.Id);
        string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        _unlockedDates[ach.Id] = now;

        Save();

        ToastNotifications.ShowText(
            $"🏆 Conquista Desbloqueada! (+{ach.Points} pts)",
            $"{ach.Title}\n{ach.Description}",
            null,
            5.0f);
    }

    public static void Update(CpuContext c, IMemory m)
    {
        if (!Game.Available || !Game.InGame || Game.IsLoading) return;

        // Check every 30 frames to avoid redundant work
        if (++_frameCounter % 30 != 0) return;

        // Richter mode start
        if (Player.IsRichter)
        {
            Unlock("richter_mode");
        }

        // Alucard checks
        if (Player.IsAlucard)
        {
            // Level 50 check
            // Level is stored at StatusAddr + 0x27C
            uint level = m.ReadU32(Game.StatusAddr + 0x27C);
            if (level >= 50) Unlock("level_50");

            // Max Hearts > 300
            if (Player.HeartsMax >= 300) Unlock("max_hearts");

            // Spells: all 8 spells learned
            if (Inventory.SpellsLearnt >= 0xFF) Unlock("all_spells");

            // Inverted castle check
            if ((Game.StageId & Stage.SecondCastle) != 0)
            {
                Unlock("flip_castle");
            }

            // Map completion check
            int rooms = Map.Rooms;
            if (rooms >= 942) Unlock("map_100"); // 100% of first castle (~942 rooms)
            if (rooms >= 1890) Unlock("map_200"); // 200.6% (~1890 rooms)

            // Crissaegrim possession check
            if (Inventory.HasItem(HandItem.Crissaegrim))
            {
                Unlock("crissaegrim");
            }
        }
    }
}
