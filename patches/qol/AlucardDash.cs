using System;
using RecompOne.Runtime.Context;
using RecompOne.Runtime.Host.Window;
using RecompOne.Runtime.Memory;
using Sotn;

namespace Recompiled;

public static class AlucardDash
{
    public static bool Enabled = true;
    public static float DashSpeedMultiplier = 1.75f;

    private static int _doubleTapTimer = 0;
    private static int _lastTapDirection = 0; // -1: Left, 1: Right
    private static bool _isDashing = false;

    public static void Load()
    {
        var v = RecompOne.Runtime.Runtime.View;
        Enabled = v.GetBool("QolAlucardDash", true);
    }

    public static void Save()
    {
        var v = RecompOne.Runtime.Runtime.View;
        v.SetBool("QolAlucardDash", Enabled);
        RecompOne.Runtime.Runtime.SaveView();
    }

    public static void Update(CpuContext c, IMemory m)
    {
        if (!Enabled || !Game.Available || !Game.InGame || Game.IsLoading) return;
        if (!Player.IsAlucard || !Player.HasControl) return;

        ushort pressed = Game.Pressed;
        ushort tapped = Game.Tapped;

        bool tapRight = (tapped & (ushort)Button.Right) != 0;
        bool tapLeft = (tapped & (ushort)Button.Left) != 0;
        bool holdRight = (pressed & (ushort)Button.Right) != 0;
        bool holdLeft = (pressed & (ushort)Button.Left) != 0;
        bool holdL1 = (pressed & (ushort)Button.L1) != 0;
        bool holdR1 = (pressed & (ushort)Button.R1) != 0;

        if (_doubleTapTimer > 0) _doubleTapTimer--;

        // Double tap detection
        if (tapRight)
        {
            if (_lastTapDirection == 1 && _doubleTapTimer > 0) _isDashing = true;
            else { _lastTapDirection = 1; _doubleTapTimer = 18; }
        }
        else if (tapLeft)
        {
            if (_lastTapDirection == -1 && _doubleTapTimer > 0) _isDashing = true;
            else { _lastTapDirection = -1; _doubleTapTimer = 18; }
        }

        // Hold L1/R1 trigger dash
        if ((holdL1 || holdR1) && (holdRight || holdLeft))
        {
            _isDashing = true;
        }

        // Stop dashing if no movement direction is held
        if (!holdRight && !holdLeft)
        {
            _isDashing = false;
        }

        // Only apply the boost while actually Walking. That's the one step confirmed (via
        // telemetry) to have VelocityX reset from scratch by the engine every frame before
        // this runs, so re-multiplying the fresh base value each frame is safe and is what
        // keeps the dash sustained (98304 -> 172032 -> 98304 -> 172032 ... each frame).
        // Standing does NOT reset it -- VelocityX just carries over frame to frame there
        // (e.g. residual momentum), same as Aerial -- so multiplying it repeatedly compounds
        // an already-boosted value instead of boosting it once. Confirmed via telemetry: with
        // the previous Standing-included gate, VelocityX ran away from 86016 to over 1.7
        // billion in about 18 frames while the player wasn't even moving.
        if (Player.Step == PlayerStep.Walking && _isDashing && (holdRight || holdLeft))
        {
            int curVelX = Player.VelocityX;
            if (curVelX != 0)
            {
                int targetVel = (int)(curVelX * DashSpeedMultiplier);
                Player.VelocityX = targetVel;
            }
        }
    }
}
