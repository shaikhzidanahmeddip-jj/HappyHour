using HappyHour.Features;
using Steamworks;
using UnityEngine;

namespace HappyHour
{
    public class SettingsGUI
    {
        public bool showSettings = false;
        public Rect guiWindowRect;

        private const int WINDOW_ID = 54322;

        public SettingsGUI()
        {
            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            guiWindowRect = new Rect(
                10f,
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
                guiWindowRect = GUILayout.Window(WINDOW_ID, guiWindowRect, DrawWindow, "HappyHour Settings");
            }
            catch { }
        }

        private void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(5);

            GUILayout.Label("<b>Mod Updates</b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label(UpdateNotifier.GetSettingsStatusText());
            GUILayout.Label("────────────────────────────");

            if (GUILayout.Button("Game Microphone Settings"))
            {
                SteamFriends.ActivateGameOverlay("settings:voice");
            }
            GUILayout.Label("Click the <b>Voice</b> tab on the settings window to adjust microphone settings for the game.");

            GUILayout.Label("────────────────────────────");

            bool autoRefreshEnabled = ServerListAutoRefresh.IsEnabled();
            if (GUILayout.Button($"Server List Auto Refresh: {(autoRefreshEnabled ? "ON" : "OFF")}"))
            {
                ServerListAutoRefresh.SetEnabled(!autoRefreshEnabled);
            }

            float refreshSeconds = ServerListAutoRefresh.GetRefreshSecondsValue();
            GUILayout.Label($"Server List Refresh Seconds: {refreshSeconds:0}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GUILayout.Width(40f)))
            {
                ServerListAutoRefresh.SetRefreshSecondsValue(refreshSeconds - 1f);
            }

            if (GUILayout.Button("+", GUILayout.Width(40f)))
            {
                ServerListAutoRefresh.SetRefreshSecondsValue(refreshSeconds + 1f);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
    }
}
