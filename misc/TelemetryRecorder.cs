using System;
using System.IO;
using System.Text;
using RecompOne.Runtime.Context;
using RecompOne.Runtime.Host.Window;
using RecompOne.Runtime.Memory;
using Sotn;

namespace Recompiled;

// Rolling "flight recorder" of player/room state, sampled every frame. CrashLogger only captures a
// single snapshot at the moment something throws, which is useless for bugs that don't crash (a
// position snap, a wrong room, a one-frame speed spike) -- this keeps the last ~15s of state so we
// have a trail to look at either way. Press F9 in-game to dump it on demand without needing a crash;
// CrashLogger also pulls the same trail into its report automatically.
public static class TelemetryRecorder
{
    readonly record struct Sample(
        long Frame, Stage Stage, PlayableCharacter Character, int RoomX, int RoomY,
        int PosX, int PosY, int VelX, int VelY, PlayerStep Step, GameState State);

    const int Capacity = 900; // ~15s at 60fps

    static readonly Sample[] _ring = new Sample[Capacity];
    static int _head;
    static int _count;
    static long _frame;
    static volatile bool _dumpRequested;

    static TelemetryRecorder()
    {
        RecompOne.Runtime.Events.Event.AddListener<RecompOne.Runtime.Events.KeyboardEvent>(e =>
        {
            if (e.Pressed && !e.Repeat && e.Key == (int)Silk.NET.Input.Key.F9)
                _dumpRequested = true;
        });
    }

    public static void Update(CpuContext c, IMemory m)
    {
        if (!Game.Available) return;

        _frame++;
        if (Game.InGame && !Game.IsLoading)
        {
            _ring[_head] = new Sample(_frame, Game.StageId, Player.Character, Player.MapX, Player.MapY,
                Player.PosX, Player.PosY, Player.VelocityX, Player.VelocityY, Player.Step, Game.State);
            _head = (_head + 1) % Capacity;
            if (_count < Capacity) _count++;
        }

        if (_dumpRequested)
        {
            _dumpRequested = false;
            var path = DumpToFile("manual (F9)");
            if (path != null)
                ToastNotifications.ShowText("Telemetry", $"Saved {Path.GetFileName(path)}", null, 2.5f);
        }
    }

    public static string FormatRecent()
    {
        var sb = new StringBuilder();
        int n = _count;
        int idx = (_head - n + Capacity) % Capacity;
        for (int i = 0; i < n; i++)
        {
            var s = _ring[idx];
            sb.AppendLine(
                $"[{s.Frame}] stage={s.Stage} chr={s.Character} room=({s.RoomX},{s.RoomY}) " +
                $"pos=({s.PosX},{s.PosY}) vel=({s.VelX},{s.VelY}) step={s.Step} state={s.State}");
            idx = (idx + 1) % Capacity;
        }
        return sb.ToString();
    }

    public static string? DumpToFile(string reason)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"telemetry-{DateTime.Now:yyyyMMdd-HHmmss}.log");

            var sb = new StringBuilder();
            sb.AppendLine($"SymphonyRecomp telemetry dump - {DateTime.Now:O}");
            sb.AppendLine($"reason: {reason}");
            sb.AppendLine();
            sb.Append(FormatRecent());

            File.WriteAllText(path, sb.ToString());
            return path;
        }
        catch
        {
            return null;
        }
    }
}
