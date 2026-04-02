using MelonLoader;
using UnityEngine;

namespace HappyHour.Features
{
    public static class ServerListAutoRefresh
    {
        private const float DefaultRefreshSeconds = 15f;
        private const float MinRefreshSeconds = 5f;

        private static MelonPreferences_Entry<float> RefreshSecondsPref;
        private static float NextRefreshTime;

        public static void Initialize(MelonPreferences_Category category)
        {
            RefreshSecondsPref = category.CreateEntry("LobbyRefreshSeconds", DefaultRefreshSeconds, "Lobby Refresh Seconds");
        }

        public static void ScheduleNextRefresh()
        {
            NextRefreshTime = Time.unscaledTime + GetRefreshSeconds();
        }

        public static void Update()
        {
            var lobby = LobbyListManager.instance;
            if (lobby == null)
                return;

            if (Time.unscaledTime < NextRefreshTime)
                return;

            if (SteamLobby.Instance != null && SteamLobby.Instance.JoinLocked)
            {
                NextRefreshTime = Time.unscaledTime + GetRefreshSeconds();
                return;
            }

            lobby.GetLobbies();
            NextRefreshTime = Time.unscaledTime + GetRefreshSeconds();
        }

        private static float GetRefreshSeconds()
        {
            if (RefreshSecondsPref == null)
                return DefaultRefreshSeconds;

            if (RefreshSecondsPref.Value < MinRefreshSeconds)
            {
                RefreshSecondsPref.Value = MinRefreshSeconds;
                MelonPreferences.Save();
            }

            return RefreshSecondsPref.Value;
        }
    }
}
