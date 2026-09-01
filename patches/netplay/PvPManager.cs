using System;
using RecompOne.Runtime.Host.Window;
using Sotn;

namespace Recompiled.Netplay;

public static class PvPManager
{
    public static bool IsPvPActive { get; private set; } = false;
    public static int LocalScore { get; set; } = 0;
    public static int RemoteScore { get; set; } = 0;

    private static bool _wasInColosseum = false;

    public static void Update()
    {
        if (!NetworkManager.IsConnected || !RemotePuppet.IsActive)
        {
            IsPvPActive = false;
            _wasInColosseum = false;
            return;
        }

        bool localInColosseum = StageManager.CurrentStage == Stage.Colosseum || StageManager.CurrentStage == Stage.ReverseColosseum;
        bool remoteInColosseum = (Stage)RemotePuppet.StageId == Stage.Colosseum || (Stage)RemotePuppet.StageId == Stage.ReverseColosseum;
        bool sameRoom = RemotePuppet.RoomX == Map.CurrentRoomX && RemotePuppet.RoomY == Map.CurrentRoomY;

        IsPvPActive = localInColosseum && remoteInColosseum && sameRoom;

        if (IsPvPActive && !_wasInColosseum)
        {
            ToastNotifications.ShowText("⚔️ ARENA DO COLISEU!", "Duelo PvP Ativado! 3.. 2.. 1.. LUTEM!", null, 5.0f);
        }

        _wasInColosseum = IsPvPActive;
    }
}
