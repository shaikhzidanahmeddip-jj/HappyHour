using HappyHour.Features;
using HarmonyLib;
using MelonLoader;
using Steamworks;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(HappyHour.Core), "HappyHour", "0.3", "w2log")]
[assembly: MelonGame("Curve Animation", "Liar's Bar")]

namespace HappyHour
{
    public class Core : MelonMod
    {
        private static readonly HashSet<int> BlockedEmoteFrame = new();
        private static readonly HashSet<int> BlockedDeadEmoteFrame = new();

        private const int MaxChatMessageCharacters = 128;

        private static int LastConfiguredChatUiId = int.MinValue;

        private static LobbyManagerGUI lobbyManager;
        private static bool lobbyManagerDrawn = false;

        private static readonly FieldInfo EmoteReadyField = AccessTools.Field(typeof(CharController), "EmoteReadyy");
        private static readonly FieldInfo EmoteCooldownField = AccessTools.Field(typeof(CharController), "EmoteCooldown");
        private static readonly FieldInfo DeadEmotePlayedField = AccessTools.Field(typeof(CharController), "DeadEmotePlayed");

        public override void OnInitializeMelon()
        {
            var lobbyCategory = MelonPreferences.CreateCategory("HappyHour.Lobby", "Happy Hour Lobby");
            ServerListFilter.Initialize(lobbyCategory);
            ServerListAutoRefresh.Initialize(lobbyCategory);
            LobbyBanSystem.Load();
            LobbyBanSystem.SetupHarmonyPatch(HarmonyInstance);

            lobbyManager = new LobbyManagerGUI();

            HarmonyInstance.Patch(
                AccessTools.Method(typeof(CharController), "PlayEmote1Sfx"),
                prefix: new HarmonyMethod(typeof(Core), nameof(BlockEmoteWhileChatting)));
            HarmonyInstance.Patch(
                AccessTools.Method(typeof(CharController), "PlayEmote2Sfx"),
                prefix: new HarmonyMethod(typeof(Core), nameof(BlockEmoteWhileChatting)));
            HarmonyInstance.Patch(
                AccessTools.Method(typeof(CharController), "PlayEmote3Sfx"),
                prefix: new HarmonyMethod(typeof(Core), nameof(BlockEmoteWhileChatting)));
            HarmonyInstance.Patch(
                AccessTools.Method(typeof(CharController), "PlayEmoteDeadSfx"),
                prefix: new HarmonyMethod(typeof(Core), nameof(BlockEmoteWhileChatting)));
            HarmonyInstance.Patch(
                AccessTools.Method(typeof(CharController), "Update"),
                postfix: new HarmonyMethod(typeof(Core), nameof(RepairBlockedEmoteState)));
        }

        public override void OnLateUpdate()
        {
            ConfigureChatUi();
            ConfigureLobbyUi();
            ServerListAutoRefresh.Update();
            LobbyBanSystem.Update();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                var steamLobby = SteamLobby.Instance;
                if (steamLobby != null)
                    steamLobby.JoinLocked = false;
            }

            if (Input.GetKeyDown(KeyCode.End))
            {
                if (HostMigration.Instance != null)
                    HostMigration.Instance.leavednormal = true;

                var steamLobby = SteamLobby.Instance;
                if (steamLobby != null && steamLobby.CurrentLobbyID != 0)
                    SteamMatchmaking.LeaveLobby((CSteamID)steamLobby.CurrentLobbyID);

                if (steamLobby != null)
                    steamLobby.JoinLocked = false;

                var networkManager = Mirror.NetworkManager.singleton as CustomNetworkManager;
                if (networkManager != null)
                    networkManager.StopClient();

                SceneManager.LoadScene("SteamTest");
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                lobbyManager.showLobbyManager = !lobbyManager.showLobbyManager;

                if (lobbyManager.showLobbyManager && !lobbyManagerDrawn)
                {
                    MelonEvents.OnGUI.Subscribe(lobbyManager.Draw);
                    lobbyManagerDrawn = true;
                }
                else if (!lobbyManager.showLobbyManager && lobbyManagerDrawn)
                {
                    MelonEvents.OnGUI.Unsubscribe(lobbyManager.Draw);
                    lobbyManagerDrawn = false;
                }
            }
        }

        public override void OnGUI()
        {
            if (!IsChatActive())
                return;

            const string chatMessage = "CHAT ACTIVE - GAMEPLAY INPUTS DISABLED";

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 20,
                normal = { textColor = Color.red }
            };

            Vector2 textSize = labelStyle.CalcSize(new GUIContent(chatMessage));
            float rectWidth = textSize.x + 32f;
            float rectHeight = Mathf.Max(42f, textSize.y + 14f);
            var chatModeIndicatorRect = new Rect((Screen.width - rectWidth) / 2f, 24f, rectWidth, rectHeight);

            var oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.Box(chatModeIndicatorRect, GUIContent.none);
            GUI.color = oldColor;

            GUI.Label(chatModeIndicatorRect, chatMessage, labelStyle);
        }

        private static bool BlockEmoteWhileChatting(CharController __instance)
        {
            bool chatActive = IsChatActive();
            if (__instance != null && chatActive)
            {
                int id = __instance.GetInstanceID();
                BlockedEmoteFrame.Add(id);
                BlockedDeadEmoteFrame.Add(id);
                return false;
            }

            return !chatActive;
        }

        private static void RepairBlockedEmoteState(CharController __instance)
        {
            if (__instance == null)
                return;

            int id = __instance.GetInstanceID();
            bool blockedEmote = BlockedEmoteFrame.Remove(id);
            bool blockedDeadEmote = BlockedDeadEmoteFrame.Remove(id);

            if (!blockedEmote)
                return;

            EmoteReadyField?.SetValue(__instance, true);
            EmoteCooldownField?.SetValue(__instance, 0f);

            if (blockedDeadEmote)
                DeadEmotePlayedField?.SetValue(__instance, false);
        }

        private static bool IsChatActive()
        {
            return Manager.Instance != null && Manager.Instance.Chatting;
        }

        private static void ConfigureChatUi()
        {
            var chatUi = ChatArayuz.instance;
            if (chatUi == null)
            {
                LastConfiguredChatUiId = int.MinValue;
                return;
            }

            int chatUiId = chatUi.GetInstanceID();
            if (chatUiId == LastConfiguredChatUiId)
                return;

            LastConfiguredChatUiId = chatUiId;

            if (chatUi.inputField != null)
                chatUi.inputField.characterLimit = MaxChatMessageCharacters;

            if (chatUi.chatText != null)
            {
                chatUi.chatText.horizontalOverflow = HorizontalWrapMode.Wrap;
                chatUi.chatText.verticalOverflow = VerticalWrapMode.Overflow;
            }
        }

        private static void ConfigureLobbyUi()
        {
            var lobby = LobbyListManager.instance;
            if (lobby == null)
                return;

            if (ServerListFilter.Configure(lobby))
            {
                lobby.GetLobbies();
                ServerListAutoRefresh.ScheduleNextRefresh();
            }
        }
    }
}
