using RecompOne.Runtime.Events;
using RecompOne.Runtime.Host.Window;

namespace Recompiled;

public static class QualityOfLifeMenu
{
    public static void Register()
    {
        Event.AddListener<RuntimeReadyEvent>(_ => QualityOfLife.Load());
        PanelManager.Register(new QualityOfLifePanel());
        PanelManager.Register(new FastTravelPanel());
        PanelManager.Register(new SpellWheelPanel());

        MenuRegistry.Menu("menu.misc").After("menu.mods")
            .Panel<QualityOfLifePanel>("panel.qol")
            .Panel<FastTravelPanel>("panel.fast_travel")
            .Panel<SpellWheelPanel>("panel.spell_wheel");
    }
}
