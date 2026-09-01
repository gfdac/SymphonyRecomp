using RecompOne.Runtime.Context;
using RecompOne.Runtime.Events;
using RecompOne.Runtime.Memory;

namespace RecompOne.Runtime.Sdk;

public static class LibEtc
{
    static int _vcount;
    static readonly VSyncEvent _vsyncEvent = new();

    public static void VSync(CpuContext c, IMemory m)
    {
        int mode = (int)c.A0;
        Log.Sdk($"VSync({mode})");
        if (mode < 0) { c.V0 = (uint)_vcount; return; }
        //if (mode == 1) { c.V0 = 0; return; }
        
        Runtime.PresentFrame();
        _vcount++;

        if (Event.HasAnyListeners<VSyncEvent>())
        {
            var e = _vsyncEvent;
            e.Context = c; e.Memory = m;
            e.Frame = _vcount;
            Event.Dispatch(e);
        }

        c.V0 = 0;
    }
}
