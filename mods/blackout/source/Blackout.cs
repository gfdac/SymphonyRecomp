using System;
using RecompOne.Runtime.Modding;
using RecompOne.Runtime;
using RecompOne.Runtime.Context;
using RecompOne.Runtime.Memory;
using RecompOne.Runtime.Hardware;
using Sotn;
using Recompiled;
using RecompOne.Runtime.Events;
using ImGuiNET;

/*
[PreHook("dra", "func_800FE044")]
[PostHook("dra", "func_800FE044")]
[Replace("dra", "func_800FE044")]
*/

public enum CastleMode { None, Dark, Dim }

public class BlackoutMod : IMod
{
    /* Debug */
    public bool forceLightsOn = false;

    /* Internal State Core */
    public bool shouldUpdatePalette = true;

    /* Internal State Primary */
    public CastleMode castleMode = CastleMode.Dark;
    public float dimLevel = 0.05f;
    public bool blackoutEntities = true;
    public bool invisibleEntities = false;

    public bool darkCastle => castleMode != CastleMode.None;

    /* Internal State Secondary */
    public byte currentFamiliar = 0xFF;
    public byte batEchoTimer = 100;

    /* Customizable Options */
    public bool giveBlackoutStartingRelics = true;
    public bool giveMoreStartingMP = true;


    // bat echo progress timer
    private const int ExtProgress = 0x02;
    private const int ExtProgressMax = 0x04;

    private static readonly float[][] DarkRamp =
    {
        new[] {0f,0f,0f},
        new[] {40f, 32f, 64f},
        new[] {56f, 72f, 72f},
        new[] {90f, 90f, 76f},
        new[] {136f, 120f, 114f},
        new[] {255f, 255f, 255f},
    };

    private static readonly float[][] FireRamp =
    {
        new[] {0f, 0f,0f},
        new[] {40f, 32f,64f },
        new[] {80f, 50f, 58f},
        new[] {164f, 72f, 54f},
        new[] {204f, 148f, 64f },
        new[] {255f, 255f, 255f },
    };

    //controls light level
    private const float EchoPeak = 0.80f;
    private const float FireAttack = 0.30f;
    private const float FireDecay = 0.06f;
    private const float FirePeak = 0.70f;

    private bool mpBonusApplied = false;
    private bool dirty = true;
    private static BlackoutMod? instance; //hooks need to be static, since the mod doesnt is static and im too lazy to convert i make this singletron isntance
    private static float echoK;
    private static bool fireActive;
    private static float fireLevel;

    public void DrawSettings()
    {
        //float v = DarkStage.Brightness;
        //if (ImGui.SliderFloat("Brightness", ref v, 0f, 1f, "%.2f"))
        //    DarkStage.Brightness = v;

        bool dark = castleMode == CastleMode.Dark;
        if (ImGui.Checkbox("Dark Castle", ref dark)) SetMode(dark ? CastleMode.Dark : CastleMode.None);

        bool dim = castleMode == CastleMode.Dim;
        if (ImGui.Checkbox("Dim Castle", ref dim)) SetMode(dim ? CastleMode.Dim : CastleMode.None);

        if (castleMode != CastleMode.Dim) ImGui.BeginDisabled();
        float level = dimLevel;
        if (ImGui.SliderFloat("Dim Level", ref level, 0.01f, 0.50f, "%.2f"))
        {
            dimLevel = level;
            Save();
            dirty = true;
        }
        if (castleMode != CastleMode.Dim) ImGui.EndDisabled();

        if (ImGui.Checkbox("Completely Blackout Entities", ref blackoutEntities)) { Save(); dirty = true; }
        if (ImGui.Checkbox("Invisible Enemies", ref invisibleEntities)) { Save(); dirty = true; }
    }

    /* the two castle checkboxes are one setting, picking one clears the other */
    private void SetMode(CastleMode mode)
    {
        castleMode = mode;
        Save();
        dirty = true;
    }

    private const string Prefix = "mods.blackout.";

    private void Save()
    {
        var view = RecompOne.Runtime.Runtime.View;
        view.SetInt(Prefix + "castleMode", (int)castleMode);
        view.SetFloat(Prefix + "dimLevel", dimLevel);
        view.SetBool(Prefix + "blackoutEntities", blackoutEntities);
        view.SetBool(Prefix + "invisibleEntities", invisibleEntities);
        view.SetBool(Prefix + "startingRelics", giveBlackoutStartingRelics);
        view.SetBool(Prefix + "startingMP", giveMoreStartingMP);
        RecompOne.Runtime.Runtime.SaveView();
    }

    private void Load()
    {
        var view = RecompOne.Runtime.Runtime.View;
        castleMode = (CastleMode)view.GetInt(Prefix + "castleMode", (int)CastleMode.Dark);
        dimLevel = view.GetFloat(Prefix + "dimLevel", 0.05f);
        blackoutEntities = view.GetBool(Prefix + "blackoutEntities", true);
        invisibleEntities = view.GetBool(Prefix + "invisibleEntities", false);
        giveBlackoutStartingRelics = view.GetBool(Prefix + "startingRelics", true);
        giveMoreStartingMP = view.GetBool(Prefix + "startingMP", true);
    }

    public void OnLoad()
    {
        instance = this;
        Load();

        Event.AddListener<PlayerLoadedEvent>(Init);
        Event.AddListener<RoomLayerLoadEvent>(OnRoomLayerLoadEvent);
        Event.AddListener<VSyncEvent>(OnVSync);
    }

    public void OnUnload()
    {
        Event.RemoveListener<PlayerLoadedEvent>(Init);
        Event.RemoveListener<RoomLayerLoadEvent>(OnRoomLayerLoadEvent);
        Event.RemoveListener<VSyncEvent>(OnVSync);

        instance = null;

        Stages.Restore();
        Stages.RestoreEntities();
    }

    public void Init(PlayerLoadedEvent ple)
    {
        if (giveBlackoutStartingRelics) GiveBlackoutRelics();
        if (giveMoreStartingMP && !mpBonusApplied) GivePlayerHigherStartingMP();
    }


    public void OnRoomLayerLoadEvent(RoomLayerLoadEvent rle)
    {
        echoK = 0f;
        fireActive = false;
        fireLevel = 0f;
        dirty = true;
    }

    public void OnVSync(VSyncEvent e)
    {
        float k = echoK;
        bool fire = fireActive;
        echoK = 0f;
        fireActive = false;

        fireLevel += fire ? FireAttack : -FireDecay;
        fireLevel = Math.Clamp(fireLevel, 0f, 1f);

        if (!CanTouchPalettes()) return;

        SampleRamp(DarkRamp, k * EchoPeak, out float er, out float eg, out float eb);
        SampleRamp(FireRamp, fireLevel * FirePeak, out float fr, out float fg, out float fb);

        float lr = Math.Max(er, fr), lg = Math.Max(eg, fg), lb = Math.Max(eb, fb); //im not sure if you can echo and fire at the same time, but in case it does both add up instead of canceling each other

        if (lr + lg + lb > 0f)
        {
            Stages.Shade(lr, lg, lb);
            if (blackoutEntities && !invisibleEntities) Stages.ShadeEntities(lr, lg, lb);
            dirty = true;
            return;
        }

        if (!dirty) return;
        BlackoutMap();
        BlackoutEntities();
        dirty = false;
    }

    private static void SampleRamp(float[][] ramp, float k, out float r, out float g, out float b)
    {
        float pos = Math.Clamp(k, 0f, 1f) * (ramp.Length - 1);
        int i = (int)pos;
        if (i >= ramp.Length - 1)
        {
            r = ramp[ramp.Length - 1][0] / 255f;
            g = ramp[ramp.Length - 1][1] / 255f;
            b = ramp[ramp.Length - 1][2] / 255f;
            return;
        }

        float t = pos - i;
        r = (ramp[i][0] + (ramp[i + 1][0] - ramp[i][0]) * t) / 255f;
        g = (ramp[i][1] + (ramp[i + 1][1] - ramp[i][1]) * t) / 255f;
        b = (ramp[i][2] + (ramp[i + 1][2] - ramp[i][2]) * t) / 255f;
    }

    [PreHook("dra", "EntityBatEcho")]
    public static void OnBatEcho(CpuContext c, IMemory m)
    {
        if (instance == null || instance.forceLightsOn || !instance.darkCastle) return;

        var self = new Entity(c.A0);
        float max = self.ExtU16(ExtProgressMax);
        if (max <= 0f) return;

        float k = self.ExtU16(ExtProgress) / max;
        if (k > echoK) echoK = k;
    }

    [PreHook("dra", "EntityBatFireball")]
    public static void OnBatFireball(CpuContext c, IMemory m)
    {
        if (instance == null || instance.forceLightsOn || !instance.darkCastle) return;

        fireActive = true;
    }


    private bool CanTouchPalettes() //i put this here just because i like stuff being packet nicely :p
    {
        return Game.Available && Game.InGame && !Game.IsLoading;
    }

    private void BlackoutMap()
    {
        // Stages.Shade(0, 0, 0);

        if (!CanTouchPalettes()) return;

        if (forceLightsOn || castleMode == CastleMode.None) Stages.Restore();
        else if (castleMode == CastleMode.Dim) Stages.Shade(dimLevel, dimLevel, dimLevel);
        else Stages.WriteAllPalettes(0x8000);
    }

    private void BlackoutEntities()
    {
        // Stages.ShadeEntities(0, 0, 0);

        if (!CanTouchPalettes()) return;

        if (forceLightsOn) Stages.RestoreEntities();
        else if (invisibleEntities) Stages.HideEntities();
        else if (blackoutEntities) Stages.WriteAllEntityPalettes(0x8000);
        else Stages.RestoreEntities();
    }

    /* Gives Soul of Bat, Echo of Bat, Spirit Orb, and Faerie Scroll */
    private void GiveBlackoutRelics()
    {
        Inventory.GiveRelic(Relic.SoulOfBat, true);
        Inventory.GiveRelic(Relic.EchoOfBat, true);
        Inventory.GiveRelic(Relic.SpiritOrb, true);
        Inventory.GiveRelic(Relic.FaerieScroll, true);
    }

    private void GivePlayerHigherStartingMP()
    {
        Player.MpMax += 200;
        mpBonusApplied = true;
    }

    private uint GetEchoOfBatTimer()
    {
        return 0;
    }
}
