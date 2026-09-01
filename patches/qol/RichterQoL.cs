using System;
using RecompOne.Runtime.Context;
using RecompOne.Runtime.Host.Window;
using RecompOne.Runtime.Memory;
using Sotn;

namespace Recompiled;

public static class RichterQoL
{
    public static bool Enabled = true;

    public static void Load()
    {
        var v = RecompOne.Runtime.Runtime.View;
        Enabled = v.GetBool("QolRichterQoL", true);
    }

    public static void Save()
    {
        var v = RecompOne.Runtime.Runtime.View;
        v.SetBool("QolRichterQoL", Enabled);
        RecompOne.Runtime.Runtime.SaveView();
    }

    public static void Update(CpuContext c, IMemory m)
    {
        if (!Enabled || !Game.Available || !Game.InGame || Game.IsLoading) return;
        if (!Player.IsRichter) return;

        ushort pressed = Game.Pressed;
        ushort tapped = Game.Tapped;

        // Easy Item Crash (Triangle button / Button.Triangle = 0x0010)
        bool tapTriangle = (tapped & (ushort)Button.Triangle) != 0;
        if (tapTriangle)
        {
            // Trigger Richter Item Crash subweapon step
            m.WriteU16(0x80097494, 0x10); // Triangle tapped
        }
    }
}
