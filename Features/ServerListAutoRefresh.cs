using MelonLoader;
using UnityEngine;

namespace HappyHour.Features
{
    public static class ServerListAutoRefresh
    {
        private const float DefaultRefreshSeconds = 15f;
        private const float MinRefreshSeconds = 5f;
        private const float MaxRefreshSeconds = 300f;
        private const bool DefaultEnabled = true;

        private static MelonPreferences_Entry<float> RefreshSecondsPref;
        private static MelonPreferences_Entry<bool> EnabledPref;
        private static float NextRefreshTime;

        public static void Initialize(MelonPreferences_Category category)
        {
            RefreshSecondsPref = category.CreateEntry("Lobby_RefreshSeconds", DefaultRefreshSeconds, "Lobby Refresh Seconds");
            EnabledPref = category.CreateEntry("Lobby_AutoRefreshEnabled", DefaultEnabled, "Lobby Auto Refresh Enabled");
        }

        public static void ScheduleNextRefresh()
        {
            NextRefreshTime = Time.unscaledTime + GetRefreshSecondsValue();
        }

        public static void Update()
        {
            if (!IsEnabled())
                return;

            var lobby = LobbyListManager.instance;
            if (lobby == null)
                return;

            if (Time.unscaledTime < NextRefreshTime)
                return;

            if (SteamLobby.Instance != null && SteamLobby.Instance.JoinLocked)
            {
                NextRefreshTime = Time.unscaledTime + GetRefreshSecondsValue();
                return;
            }

            lobby.GetLobbies();
            NextRefreshTime = Time.unscaledTime + GetRefreshSecondsValue();
        }

        public static bool IsEnabled()
        {
            if (EnabledPref == null)
                return DefaultEnabled;

            return EnabledPref.Value;
        }

        public static void SetEnabled(bool enabled)
        {
            if (EnabledPref == null || EnabledPref.Value == enabled)
                return;

            EnabledPref.Value = enabled;
            MelonPreferences.Save();

            if (enabled)
                ScheduleNextRefresh();
        }

        public static float GetRefreshSecondsValue()
        {
            if (RefreshSecondsPref == null)
                return DefaultRefreshSeconds;

            float clamped = Mathf.Clamp(RefreshSecondsPref.Value, MinRefreshSeconds, MaxRefreshSeconds);
            if (!Mathf.Approximately(RefreshSecondsPref.Value, clamped))
            {
                RefreshSecondsPref.Value = clamped;
                MelonPreferences.Save();
            }

            return RefreshSecondsPref.Value;
        }

        public static void SetRefreshSecondsValue(float seconds)
        {
            if (RefreshSecondsPref == null)
                return;

            float clamped = Mathf.Clamp(seconds, MinRefreshSeconds, MaxRefreshSeconds);
            if (Mathf.Approximately(RefreshSecondsPref.Value, clamped))
                return;

            RefreshSecondsPref.Value = clamped;
            MelonPreferences.Save();

            if (IsEnabled())
                ScheduleNextRefresh();
        }
    }
}
