using HarmonyLib;
using MelonLoader;
using Mirror;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(HappyHour.Core), "HappyHour", "0.1", "w2og")]
[assembly: MelonGame("Curve Animation", "Liar's Bar")]

namespace HappyHour
{
    public class Core : MelonMod
    {
        private static readonly HashSet<int> BlockedEmoteFrame = new();
        private static readonly HashSet<int> BlockedDeadEmoteFrame = new();

        private const float TransitionSuppressSeconds = 10f;

        public override void OnInitializeMelon()
        {
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
            // Quick Disconnect - Leave if you're stuck on loading or encounter general gameplay bugs in a lobby.
            // Lobbies can get bugged for many reasons. There's also times where you are unable to press "Esc" and leave.
            // This mod fixes it so you don't have to Alt + F4.
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
            if (__instance != null && IsChatActive())
            {
                int id = __instance.GetInstanceID();
                BlockedEmoteFrame.Add(id);
                BlockedDeadEmoteFrame.Add(id);
                return false;
            }

            return !IsChatActive();
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

            var emoteReadyField = AccessTools.Field(typeof(CharController), "EmoteReadyy");
            var emoteCooldownField = AccessTools.Field(typeof(CharController), "EmoteCooldown");
            var deadEmotePlayedField = AccessTools.Field(typeof(CharController), "DeadEmotePlayed");

            emoteReadyField?.SetValue(__instance, true);
            emoteCooldownField?.SetValue(__instance, 0f);

            if (blockedDeadEmote)
                deadEmotePlayedField?.SetValue(__instance, false);
        }

        private static bool IsChatActive()
        {
            return Manager.Instance != null && Manager.Instance.Chatting;
        }
    }
}
