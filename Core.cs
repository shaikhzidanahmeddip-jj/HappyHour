using HarmonyLib;
using MelonLoader;
using Mirror;
using Steamworks;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(HappyHour.Core), "HappyHour", "0.1", "w2og")]
[assembly: MelonGame("Curve Animation", "Liar's Bar")]

namespace HappyHour
{
    public class Core : MelonMod
    {
        private static readonly HashSet<int> BlockedEmoteFrame = new();
        private static readonly HashSet<int> BlockedDeadEmoteFrame = new();

        private const int MaxChatMessageCharacters = 128;
        private const float DefaultLobbyAutoRefreshSeconds = 15f;
        private const float MinLobbyAutoRefreshSeconds = 5f;

        private static int LastConfiguredChatUiId = int.MinValue;
        private static int LastConfiguredLobbyUiId = int.MinValue;
        private static float NextLobbyRefreshTime;

        private static MelonPreferences_Category LobbyCategory;
        private static MelonPreferences_Entry<bool> DeckFilterPreference;
        private static MelonPreferences_Entry<bool> DiceFilterPreference;
        private static MelonPreferences_Entry<bool> PokerFilterPreference;
        private static MelonPreferences_Entry<bool> SpinFilterPreference;
        private static MelonPreferences_Entry<float> LobbyRefreshSecondsPreference;

        private static readonly FieldInfo EmoteReadyField = AccessTools.Field(typeof(CharController), "EmoteReadyy");
        private static readonly FieldInfo EmoteCooldownField = AccessTools.Field(typeof(CharController), "EmoteCooldown");
        private static readonly FieldInfo DeadEmotePlayedField = AccessTools.Field(typeof(CharController), "DeadEmotePlayed");

        public override void OnInitializeMelon()
        {
            LobbyCategory = MelonPreferences.CreateCategory("HappyHour.Lobby", "Happy Hour Lobby");
            DeckFilterPreference = LobbyCategory.CreateEntry("DeckFilter", true, "Deck Filter");
            DiceFilterPreference = LobbyCategory.CreateEntry("DiceFilter", true, "Dice Filter");
            PokerFilterPreference = LobbyCategory.CreateEntry("PokerFilter", true, "Poker Filter");
            SpinFilterPreference = LobbyCategory.CreateEntry("SpinFilter", true, "Spin Filter");
            LobbyRefreshSecondsPreference = LobbyCategory.CreateEntry("LobbyRefreshSeconds", DefaultLobbyAutoRefreshSeconds, "Lobby Refresh Seconds");

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
            AutoRefreshLobbyList();

            if (Input.GetKeyDown(KeyCode.End))
            {
                if (HostMigration.Instance != null)
                    HostMigration.Instance.leavednormal = true;

                var steamLobby = SteamLobby.Instance;
                if (steamLobby != null && steamLobby.CurrentLobbyID != 0)
                    SteamMatchmaking.LeaveLobby((CSteamID)steamLobby.CurrentLobbyID);

                if (steamLobby != null)
                    steamLobby.JoinLocked = false;

                var networkManager = NetworkManager.singleton as CustomNetworkManager;
                if (networkManager != null)
                    networkManager.StopClient();

                SceneManager.LoadScene("SteamTest");
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
            {
                LastConfiguredLobbyUiId = int.MinValue;
                return;
            }

            int lobbyUiId = lobby.GetInstanceID();
            if (lobbyUiId == LastConfiguredLobbyUiId)
                return;

            LastConfiguredLobbyUiId = lobbyUiId;

            if (lobby.DeckFilter != null)
            {
                lobby.DeckFilter.isOn = DeckFilterPreference.Value;
                lobby.DeckFilter.onValueChanged.AddListener(OnDeckFilterChanged);
            }

            if (lobby.DiceFilter != null)
            {
                lobby.DiceFilter.isOn = DiceFilterPreference.Value;
                lobby.DiceFilter.onValueChanged.AddListener(OnDiceFilterChanged);
            }

            if (lobby.PokerFilter != null)
            {
                lobby.PokerFilter.isOn = PokerFilterPreference.Value;
                lobby.PokerFilter.onValueChanged.AddListener(OnPokerFilterChanged);
            }

            if (lobby.SpinFilter != null)
            {
                lobby.SpinFilter.isOn = SpinFilterPreference.Value;
                lobby.SpinFilter.onValueChanged.AddListener(OnSpinFilterChanged);
            }

            lobby.GetLobbies();
            NextLobbyRefreshTime = Time.unscaledTime + GetLobbyRefreshSeconds();
        }

        private static void AutoRefreshLobbyList()
        {
            var lobby = LobbyListManager.instance;
            if (lobby == null)
                return;

            if (Time.unscaledTime < NextLobbyRefreshTime)
                return;

            if (SteamLobby.Instance != null && SteamLobby.Instance.JoinLocked)
            {
                NextLobbyRefreshTime = Time.unscaledTime + GetLobbyRefreshSeconds();
                return;
            }

            lobby.GetLobbies();
            NextLobbyRefreshTime = Time.unscaledTime + GetLobbyRefreshSeconds();
        }

        private static float GetLobbyRefreshSeconds()
        {
            if (LobbyRefreshSecondsPreference == null)
                return DefaultLobbyAutoRefreshSeconds;

            if (LobbyRefreshSecondsPreference.Value < MinLobbyAutoRefreshSeconds)
            {
                LobbyRefreshSecondsPreference.Value = MinLobbyAutoRefreshSeconds;
                MelonPreferences.Save();
            }

            return LobbyRefreshSecondsPreference.Value;
        }

        private static void OnDeckFilterChanged(bool value)
        {
            SaveBoolPreference(DeckFilterPreference, value);
        }

        private static void OnDiceFilterChanged(bool value)
        {
            SaveBoolPreference(DiceFilterPreference, value);
        }

        private static void OnPokerFilterChanged(bool value)
        {
            SaveBoolPreference(PokerFilterPreference, value);
        }

        private static void OnSpinFilterChanged(bool value)
        {
            SaveBoolPreference(SpinFilterPreference, value);
        }

        private static void SaveBoolPreference(MelonPreferences_Entry<bool> entry, bool value)
        {
            if (entry == null)
                return;

            if (entry.Value == value)
                return;

            entry.Value = value;
            MelonPreferences.Save();
        }
    }
}
