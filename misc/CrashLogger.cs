using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RecompOne.Runtime.Diagnostics;
using Sotn;

namespace Recompiled;

// Writes a timestamped report to logs/ whenever the process would otherwise die silently
// (or the console window just closes), so a crash leaves something we can actually read
// afterwards instead of nothing at all.
public static class CrashLogger
{
    static int _writing;

    public static string? Write(Exception? ex, string source)
    {
        if (ex == null) return null;
        if (System.Threading.Interlocked.Exchange(ref _writing, 1) != 0) return null; // avoid re-entrant/double writes

        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"crash-{DateTime.Now:yyyyMMdd-HHmmss}.log");

            var sb = new StringBuilder();
            sb.AppendLine($"SymphonyRecomp crash report - {DateTime.Now:O}");
            sb.AppendLine($"source: {source}");
            sb.AppendLine();
            sb.AppendLine("-- exception --");
            sb.AppendLine(ex.ToString());
            sb.AppendLine();
            sb.AppendLine("-- game state --");
            sb.AppendLine(GameStateSnapshot());
            sb.AppendLine();
            sb.AppendLine("-- alucard effect-pool chain diagnostics (teleport-crash investigation) --");
            sb.AppendLine(PoolChainDiagnostics());
            sb.AppendLine();
            sb.AppendLine("-- recent player telemetry (leading up to this) --");
            sb.Append(TelemetryRecorder.FormatRecent());
            sb.AppendLine();
            sb.AppendLine("-- recent console output --");
            var lines = new List<string>();
            ConsoleMirror.SnapshotInto(lines);
            foreach (var line in lines) sb.AppendLine(line);

            File.WriteAllText(path, sb.ToString());
            Console.Error.WriteLine($"[CrashLogger] wrote {path}");
            return path;
        }
        catch
        {
            return null; // never let the crash logger itself take down the process
        }
        finally
        {
            System.Threading.Interlocked.Exchange(ref _writing, 0);
        }
    }

    // Walks the shared GPU-primitive pool chain that func_801093C4 (called from Alucard's own
    // per-frame update) reads via the index at 0x800734F8. That index is only ever written by
    // FUN_80109594, Alucard's proper entity init, which itself is gated on EngineStepAddr==0.
    // Our custom stage-jump teleport (Stages.Load) drives the same PlayStep/EngineStep state
    // machine the vanilla game uses for area-to-area transitions, so in theory this should get
    // reallocated correctly -- but the crash says otherwise for at least one teleport. Rather
    // than keep guessing from static disassembly, dump the raw index/state and the actual chain
    // here so the next crash shows us directly whether the index is stale garbage, whether
    // EngineStepAddr never made it back to 0, or whether the chain is simply short.
    static string PoolChainDiagnostics()
    {
        try
        {
            var m = RecompOne.Runtime.Runtime.Mem;
            if (m == null) return "(no active memory context)";

            const uint AlucardEffectPoolIndexAddr = 0x800734F8;
            const uint EngineStepAddr = 0x8003C9A4;
            const uint GameStepAddr = 0x80073060;
            const uint WaterPrimBase = 0x80086FEC;
            const uint WaterPrimStride = 0x34;

            var sb = new StringBuilder();
            uint index = m.ReadU32(AlucardEffectPoolIndexAddr);
            sb.AppendLine($"AlucardEffectPoolIndex (0x800734F8): {index} (0x{index:X})");
            sb.AppendLine($"EngineStepAddr (0x8003C9A4): {m.ReadU32(EngineStepAddr)}");
            sb.AppendLine($"GameStepAddr (0x80073060): {m.ReadU32(GameStepAddr)}");

            uint node = WaterPrimBase + index * WaterPrimStride;
            sb.AppendLine($"Walking pool chain from node 0 = 0x{node:X8}:");
            for (int i = 0; i < 8; i++)
            {
                if (node == 0) { sb.AppendLine($"  [{i}] node=0x00000000 (end of list)"); break; }
                try
                {
                    uint next = m.ReadU32(node);
                    sb.AppendLine($"  [{i}] node=0x{node:X8} -> next=0x{next:X8}");
                    node = next;
                }
                catch (Exception e)
                {
                    sb.AppendLine($"  [{i}] node=0x{node:X8} -> READ FAILED: {e.Message}");
                    break;
                }
            }
            return sb.ToString();
        }
        catch (Exception e)
        {
            return $"(failed to read pool chain diagnostics: {e.Message})";
        }
    }

    static string GameStateSnapshot()
    {
        try
        {
            if (RecompOne.Runtime.Runtime.Mem == null) return "(no active memory context)";
            if (!Game.Available) return "(game not available)";

            return string.Join('\n', new[]
            {
                $"InGame: {Game.InGame}",
                $"IsLoading: {Game.IsLoading}",
                $"State: {Game.State}",
                $"Stage: {Game.StageId}",
                $"Character: {Player.Character}",
                $"Player PosX/PosY: {Player.PosX} / {Player.PosY}",
                $"Player RoomX/RoomY: {Player.MapX} / {Player.MapY}",
                $"Player VelocityX/VelocityY: {Player.VelocityX} / {Player.VelocityY}",
                $"Player Step: {Player.Step}",
                $"Player Hp/Mp: {Player.Hp}/{Player.HpMax} - {Player.Mp}/{Player.MpMax}",
            });
        }
        catch (Exception e)
        {
            return $"(failed to read game state: {e.Message})";
        }
    }
}
