using HarmonyLib;
using MelonLoader;
using Mirror;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HappyHour.Features
{
    public static class LobbyBanSystem
    {
        private static readonly string BanListFilePath = Path.Combine(
            "UserData", "HappyHour", "banned_players.json");

        private static readonly Dictionary<ulong, string> BannedPlayers = new();

        private static float LastBanCheckTime;

        public static int Count => BannedPlayers.Count;

        public static void Load()
        {
            BannedPlayers.Clear();

            if (!File.Exists(BanListFilePath))
                return;

            try
            {
                string json = File.ReadAllText(BanListFilePath);
                var matches = Regex.Matches(json,
                    @"\{\s*""SteamId""\s*:\s*(\d+)\s*,\s*""Name""\s*:\s*""([^""]*)""\s*\}");

                foreach (Match match in matches)
                {
                    if (ulong.TryParse(match.Groups[1].Value, out ulong steamId) && steamId != 0)
                    {
                        BannedPlayers[steamId] = match.Groups[2].Value;
                    }
                }

                MelonLogger.Msg($"[LobbyBanSystem] Loaded {BannedPlayers.Count} banned players");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning($"[LobbyBanSystem] Failed to load ban list: {ex.Message}");
            }
        }

        public static void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(BanListFilePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var entries = BannedPlayers.Select(kvp =>
                    $"{{\"SteamId\":{kvp.Key},\"Name\":\"{EscapeJson(kvp.Value)}\"}}");
                string json = $"{{\"Players\":[{string.Join(",", entries)}]}}";

                File.WriteAllText(BanListFilePath, json);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning($"[LobbyBanSystem] Failed to save ban list: {ex.Message}");
            }
        }

        private static string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        public static bool IsBanned(ulong steamId)
        {
            return steamId != 0 && BannedPlayers.ContainsKey(steamId);
        }

        public static void Ban(ulong steamId, string playerName)
        {
            if (steamId == 0 || BannedPlayers.ContainsKey(steamId))
                return;

            BannedPlayers[steamId] = playerName ?? "Unknown";
            Save();
            MelonLogger.Msg($"[LobbyBanSystem] Banned player: {playerName} ({steamId})");
        }

        public static void Unban(ulong steamId, string playerName)
        {
            if (steamId == 0 || !BannedPlayers.ContainsKey(steamId))
                return;

            BannedPlayers.Remove(steamId);
            Save();
            MelonLogger.Msg($"[LobbyBanSystem] Unbanned player: {playerName} ({steamId})");
        }

        public static void SetupHarmonyPatch(HarmonyLib.Harmony harmony)
        {
            harmony.Patch(
                AccessTools.Method(typeof(CustomNetworkManager), "OnServerAddPlayer"),
                postfix: new HarmonyMethod(typeof(LobbyBanSystem), nameof(OnServerAddPlayerPostfix)));
        }

        private static void OnServerAddPlayerPostfix(NetworkConnectionToClient conn)
        {
            if (conn == null || Count == 0)
                return;

            MelonCoroutines.Start(DelayedBanCheck(conn));
        }

        private static IEnumerator DelayedBanCheck(NetworkConnectionToClient conn)
        {
            yield return new WaitForSeconds(0.5f);

            if (conn == null || !NetworkServer.active)
                yield break;

            var networkManager = NetworkManager.singleton as CustomNetworkManager;
            if (networkManager?.GamePlayers == null)
                yield break;

            var steamLobby = SteamLobby.Instance;
            if (steamLobby == null || steamLobby.CurrentLobbyID == 0)
                yield break;

            var lobbyMembersByName = BuildLobbyMembersDictionary();

            foreach (var player in networkManager.GamePlayers)
            {
                if (player == null || player.isOwned)
                    continue;

                if (player.ConnectionID == conn.connectionId)
                {
                    string playerName = player.PlayerName;
                    ulong realSteamId = 0;

                    if (!string.IsNullOrEmpty(playerName) && lobbyMembersByName.TryGetValue(playerName, out ulong foundId))
                        realSteamId = foundId;

                    if (realSteamId != 0 && IsBanned(realSteamId))
                    {
                        MelonLogger.Msg($"[LobbyBanSystem] Kicking banned player on join: {playerName} ({realSteamId})");
                        player.NetworkKicked = true;
                        try { conn.Disconnect(); } catch { }
                    }
                    break;
                }
            }
        }

        public static void Update()
        {
            if (!NetworkServer.active || Count == 0)
                return;

            if (Time.unscaledTime - LastBanCheckTime < 0.5f)
                return;
            LastBanCheckTime = Time.unscaledTime;

            var networkManager = NetworkManager.singleton as CustomNetworkManager;
            if (networkManager?.GamePlayers == null || networkManager.GamePlayers.Count == 0)
                return;

            var steamLobby = SteamLobby.Instance;
            if (steamLobby == null || steamLobby.CurrentLobbyID == 0)
                return;

            var lobbyMembersByName = BuildLobbyMembersDictionary();
            var players = networkManager.GamePlayers.ToList();

            foreach (var player in players)
            {
                if (player == null || player.isOwned)
                    continue;

                string playerName = player.PlayerName;
                ulong realSteamId = 0;

                if (!string.IsNullOrEmpty(playerName) && lobbyMembersByName.TryGetValue(playerName, out ulong foundId))
                    realSteamId = foundId;

                if (realSteamId != 0 && IsBanned(realSteamId))
                {
                    MelonLogger.Msg($"[LobbyBanSystem] Kicking banned player: {playerName} ({realSteamId})");
                    player.NetworkKicked = true;
                    try { player.connectionToClient?.Disconnect(); } catch { }
                }
            }
        }

        public static Dictionary<string, ulong> BuildLobbyMembersDictionary()
        {
            var result = new Dictionary<string, ulong>();

            var steamLobby = SteamLobby.Instance;
            if (steamLobby == null || steamLobby.CurrentLobbyID == 0)
                return result;

            CSteamID lobbyId = (CSteamID)steamLobby.CurrentLobbyID;
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
            ulong mySteamId = SteamUser.GetSteamID().m_SteamID;

            for (int i = 0; i < memberCount; i++)
            {
                CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i);
                string memberName = SteamFriends.GetFriendPersonaName(memberId);
                if (!string.IsNullOrEmpty(memberName) && memberId.m_SteamID != mySteamId)
                {
                    result[memberName] = memberId.m_SteamID;
                }
            }

            return result;
        }
    }
}
