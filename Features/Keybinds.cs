using MelonLoader;
using System;
using UnityEngine;

namespace HappyHour.Features
{
    public static class Keybinds
    {
        private static MelonPreferences_Entry<string> SettingsKeyPref;
        private static MelonPreferences_Entry<string> LobbyManagerKeyPref;
        private static MelonPreferences_Entry<string> QuickDisconnectKeyPref;

        private static KeyCode SettingsKey = KeyCode.F9;
        private static KeyCode LobbyManagerKey = KeyCode.F10;
        private static KeyCode QuickDisconnectKey = KeyCode.End;

        public static void Initialize(MelonPreferences_Category category)
        {
            SettingsKeyPref = category.CreateEntry("Keys_Settings", "F9", "Settings Key");
            LobbyManagerKeyPref = category.CreateEntry("Keys_LobbyManager", "F10", "Lobby Manager Key");
            QuickDisconnectKeyPref = category.CreateEntry("Keys_QuickDisconnect", "End", "Quick Disconnect Key");

            SettingsKey = ParseKeyCode(SettingsKeyPref.Value, KeyCode.F9);
            LobbyManagerKey = ParseKeyCode(LobbyManagerKeyPref.Value, KeyCode.F10);
            QuickDisconnectKey = ParseKeyCode(QuickDisconnectKeyPref.Value, KeyCode.End);
        }

        public static bool IsSettingsKeyDown() => Input.GetKeyDown(SettingsKey);
        public static bool IsLobbyManagerKeyDown() => Input.GetKeyDown(LobbyManagerKey);
        public static bool IsQuickDisconnectKeyDown() => Input.GetKeyDown(QuickDisconnectKey);

        private static KeyCode ParseKeyCode(string value, KeyCode defaultKey)
        {
            if (string.IsNullOrEmpty(value))
                return defaultKey;

            if (Enum.TryParse(value, true, out KeyCode key))
                return key;

            return defaultKey;
        }
    }
}
