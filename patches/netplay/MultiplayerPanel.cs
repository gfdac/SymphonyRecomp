using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using RecompOne.Runtime.Host.Window;
using Sotn;

namespace Recompiled.Netplay;

public sealed class MultiplayerPanel : IPanel
{
    public string Name => "Multiplayer Co-op & PvP";
    public string TitleKey => "panel.multiplayer";
    public bool IsOpen { get; set; }

    private string _inputIp = "127.0.0.1";
    private int _inputPort = 7777;
    private string _inputName = "Alucard";
    private string _chatInput = "";
    private readonly List<string> _chatHistory = [];

    public void Draw()
    {
        ImGui.SetNextWindowSize(new Vector2(520, 480), ImGuiCond.FirstUseEver);
        bool open = IsOpen;
        if (!ImGui.Begin(this.Title(), ref open))
        {
            IsOpen = open;
            ImGui.End();
            return;
        }

        if (ImGui.BeginTabBar("multiplayer_tabs"))
        {
            // TAB 1: CONEXÃO & LOBBY
            if (ImGui.BeginTabItem("Lobby & Conexão"))
            {
                DrawLobbyTab();
                ImGui.EndTabItem();
            }

            // TAB 2: JOGADORES & BUDDY WARP
            if (ImGui.BeginTabItem("Jogadores & Co-op"))
            {
                DrawPlayersTab();
                ImGui.EndTabItem();
            }

            // TAB 3: CHAT & AVISOS
            if (ImGui.BeginTabItem("Chat & Avisos Rápidos"))
            {
                DrawChatTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        IsOpen = open;
        ImGui.End();
    }

    private void DrawLobbyTab()
    {
        ImGui.TextColored(new Vector4(1f, 0.85f, 0.2f, 1f), "Castlevania Netplay P2P 60Hz:");
        ImGui.Separator();

        ImGui.InputText("Seu Nome", ref _inputName, 32);
        NetworkManager.LocalPlayerName = _inputName;

        ImGui.Spacing();

        if (!NetworkManager.IsConnected)
        {
            ImGui.BeginChild("host_join_child", new Vector2(-1, 160), ImGuiChildFlags.Border);
            ImGui.TextColored(new Vector4(0.3f, 0.8f, 1f, 1f), "Opções de Conexão:");

            ImGui.InputInt("Porta", ref _inputPort);
            if (ImGui.Button("🛡️ Hospedar Sessão (Host)"))
            {
                NetworkManager.StartHost(_inputPort);
            }

            ImGui.Separator();
            ImGui.InputText("IP do Servidor / Amigo", ref _inputIp, 64);
            if (ImGui.Button("⚔️ Conectar a um Amigo (Client)"))
            {
                NetworkManager.ConnectTo(_inputIp, _inputPort);
            }
            ImGui.EndChild();
        }
        else
        {
            ImGui.BeginChild("connected_status_child", new Vector2(-1, 130), ImGuiChildFlags.Border);
            ImGui.TextColored(new Vector4(0.2f, 1f, 0.3f, 1f), "✔ STATUS: CONECTADO");
            ImGui.Text($"Modo: {NetworkManager.Role}");
            ImGui.Text($"Parceiro: {NetworkManager.RemotePlayerName}");

            Vector4 pingColor = NetworkManager.PingMs < 50 ? new Vector4(0.2f, 1f, 0.3f, 1f) :
                                NetworkManager.PingMs < 120 ? new Vector4(1f, 0.85f, 0.2f, 1f) :
                                new Vector4(1f, 0.3f, 0.3f, 1f);
            ImGui.TextColored(pingColor, $"Latência (Ping): {NetworkManager.PingMs} ms");

            ImGui.Spacing();
            if (ImGui.Button("Desconectar da Sessão"))
            {
                NetworkManager.Stop();
            }
            ImGui.EndChild();
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Dica: Para jogar via internet, você pode usar seu IP público ou uma rede virtual (ex: Radmin / ZeroTier).");
    }

    private void DrawPlayersTab()
    {
        if (!NetworkManager.IsConnected)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.2f, 1f), "Você precisa estar conectado a uma sessão multiplayer para ver os jogadores.");
            return;
        }

        ImGui.TextColored(new Vector4(0.3f, 0.8f, 1f, 1f), "👥 Jogadores no Castelo:");
        ImGui.Separator();

        // Local Player
        ImGui.Text($"👑 {NetworkManager.LocalPlayerName} (Você):");
        ImGui.ProgressBar((float)Player.Hp / Math.Max(1, Player.HpMax), new Vector2(240, 18), $"HP: {Player.Hp}/{Player.HpMax}");
        ImGui.TextDisabled($"Área Atual: {StageManager.CurrentStage} (Sala: {Map.CurrentRoomX}, {Map.CurrentRoomY})");

        ImGui.Separator();

        // Remote Player
        ImGui.Text($"🗡️ {NetworkManager.RemotePlayerName} (Parceiro):");
        ImGui.ProgressBar((float)RemotePuppet.CurHp / Math.Max(1, RemotePuppet.MaxHp), new Vector2(240, 18), $"HP: {RemotePuppet.CurHp}/{RemotePuppet.MaxHp}");
        ImGui.TextDisabled($"Área Atual: {(Stage)RemotePuppet.StageId} (Sala: {RemotePuppet.RoomX}, {RemotePuppet.RoomY})");

        ImGui.Spacing();

        if (ImGui.Button("✨ Teleportar até o Amigo (Buddy Warp)", new Vector2(-1, 35)))
        {
            NetworkManager.RequestBuddyWarp();
        }

        if (PvPManager.IsPvPActive)
        {
            ImGui.Separator();
            ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), "⚔️ DUELO NO COLISEU ATIVO!");
            ImGui.Text($"Placar: {NetworkManager.LocalPlayerName} {PvPManager.LocalScore} x {PvPManager.RemoteScore} {NetworkManager.RemotePlayerName}");
        }
    }

    private void DrawChatTab()
    {
        ImGui.TextColored(new Vector4(0.3f, 0.8f, 1f, 1f), "💬 Chat da Sessão:");
        ImGui.Separator();

        // Quick Chat buttons
        ImGui.Text("Avisos Rápidos:");
        string[] quicks = ["Preciso de ajuda!", "Chefe encontrado!", "Achei uma passagem secreta!", "Teleporte até mim!"];
        foreach (var q in quicks)
        {
            if (ImGui.Button(q))
            {
                SendChatMessage(q);
            }
            ImGui.SameLine();
        }
        ImGui.NewLine();

        ImGui.Separator();

        ImGui.InputText("Mensagem", ref _chatInput, 128);
        ImGui.SameLine();
        if (ImGui.Button("Enviar") && !string.IsNullOrWhiteSpace(_chatInput))
        {
            SendChatMessage(_chatInput);
            _chatInput = "";
        }
    }

    private void SendChatMessage(string msg)
    {
        if (!NetworkManager.IsConnected) return;
        byte[] payload = NetworkPacket.CreateChat(NetworkManager.LocalPlayerName, msg);
        NetworkManager.SendPacket(new NetworkPacket(PacketOpCode.ChatMessage, payload));
        ToastNotifications.ShowText($"💬 Você", msg, null, 4.0f);
    }
}
