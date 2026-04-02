using HappyHour.Features;
using MelonLoader;
using Mirror;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;

namespace HappyHour
{
    public class LobbyManagerGUI
    {
        public bool showLobbyManager = false;
        public Rect guiWindowRect;

        private Vector2 playerScrollPos = Vector2.zero;
        private const int WINDOW_ID = 54321;

        public LobbyManagerGUI()
        {
            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            guiWindowRect = new Rect(
                Screen.width - (320f * scale) - 10f,
                25f * scale,
                300f * scale,
                400f * scale
            );
        }

        public void Draw()
        {
            try
            {
                guiWindowRect.height = 0;
                guiWindowRect = GUILayout.Window(WINDOW_ID, guiWindowRect, DrawWindow, "Lobby Manager");
            }
            catch { }
        }

        private void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();

            bool isHost = IsLocalPlayerHost();
            DrawPlayerList(isHost);

            GUILayout.Space(5);
            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void DrawPlayerList(bool isHost)
        {
            var players = GetLobbyPlayers();

            if (players.Count == 0)
            {
                GUILayout.Label("<b>No players in lobby</b>",
                    new GUIStyle(GUI.skin.label) { richText = true });
                return;
            }

            GUILayout.Label($"<b>Players ({players.Count})</b>",
                new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label("────────────────────────────");

            playerScrollPos = GUILayout.BeginScrollView(playerScrollPos, GUILayout.Height(200));

            bool firstDrawn = true;
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player == null || player.isOwned)
                    continue;

                if (!firstDrawn)
                    GUILayout.Label("────────────────────────────");

                DrawPlayerRow(player, isHost);
                firstDrawn = false;
            }

            GUILayout.EndScrollView();
        }

        private void DrawPlayerRow(PlayerObjectController player, bool isHost)
        {
            GUILayout.BeginHorizontal();

            ulong realSteamId = GetRealSteamId(player);

            bool isBanned = LobbyBanSystem.IsBanned(realSteamId);
            string nameColor = player.isOwned ? "cyan" : (isBanned ? "red" : "white");
            string ownerTag = player.isOwned ? " (You)" : "";
            string bannedTag = (!player.isOwned && isBanned && !isHost) ? " [Blacklisted]" : "";

            var nameButtonStyle = new GUIStyle(GUI.skin.button) { richText = true, alignment = TextAnchor.MiddleLeft };
            if (GUILayout.Button($"<color={nameColor}>{player.PlayerName}{ownerTag}{bannedTag}</color>", nameButtonStyle))
            {
                ViewSteamProfile(realSteamId);
            }

            if (!player.isOwned)
            {
                if (isHost)
                {
                    if (GUILayout.Button("Kick", GUILayout.Width(40)))
                    {
                        KickPlayer(player);
                    }

                    if (isBanned)
                    {
                        if (GUILayout.Button("Unban", GUILayout.Width(50)))
                        {
                            LobbyBanSystem.Unban(realSteamId, player.PlayerName);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("<color=red>Ban</color>",
                            new GUIStyle(GUI.skin.button) { richText = true }, GUILayout.Width(40)))
                        {
                            LobbyBanSystem.Ban(realSteamId, player.PlayerName);
                            KickPlayer(player);
                        }
                    }
                }
                else
                {
                    if (isBanned)
                    {
                        if (GUILayout.Button("Unblock", GUILayout.Width(60)))
                        {
                            LobbyBanSystem.Unban(realSteamId, player.PlayerName);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Block", GUILayout.Width(60)))
                        {
                            LobbyBanSystem.Ban(realSteamId, player.PlayerName);
                        }
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        private ulong GetRealSteamId(PlayerObjectController player)
        {
            var steamLobby = SteamLobby.Instance;
            if (steamLobby == null || steamLobby.CurrentLobbyID == 0)
                return player.PlayerSteamID;

            string playerName = player.PlayerName;
            if (string.IsNullOrEmpty(playerName))
                return player.PlayerSteamID;

            CSteamID lobbyId = (CSteamID)steamLobby.CurrentLobbyID;
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);

            for (int i = 0; i < memberCount; i++)
            {
                CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i);
                string memberName = SteamFriends.GetFriendPersonaName(memberId);

                if (memberName == playerName)
                {
                    return memberId.m_SteamID;
                }
            }

            return player.PlayerSteamID;
        }

        private bool IsLocalPlayerHost()
        {
            return NetworkServer.active;
        }

        private List<PlayerObjectController> GetLobbyPlayers()
        {
            var networkManager = NetworkManager.singleton as CustomNetworkManager;
            if (networkManager == null)
                return new List<PlayerObjectController>();

            return networkManager.GamePlayers ?? new List<PlayerObjectController>();
        }

        private void KickPlayer(PlayerObjectController player)
        {
            if (!IsLocalPlayerHost())
                return;

            if (player == null || player.Kicked)
                return;

            player.NetworkKicked = true;
            MelonLogger.Msg($"[LobbyManager] Kicked player: {player.PlayerName}");
        }

        private void ViewSteamProfile(ulong steamId)
        {
            if (steamId == 0)
                return;

            try
            {
                SteamFriends.ActivateGameOverlayToUser("steamid", new CSteamID(steamId));
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning($"[LobbyManager] Failed to open Steam profile: {ex.Message}");
            }
        }
    }
}
