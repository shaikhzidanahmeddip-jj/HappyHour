using MelonLoader;

namespace HappyHour.Features
{
    public static class ServerListFilter
    {
        private static MelonPreferences_Category Category;
        private static MelonPreferences_Entry<bool> DeckFilterPref;
        private static MelonPreferences_Entry<bool> DiceFilterPref;
        private static MelonPreferences_Entry<bool> PokerFilterPref;
        private static MelonPreferences_Entry<bool> SpinFilterPref;

        private static int LastConfiguredLobbyUiId = int.MinValue;

        public static void Initialize(MelonPreferences_Category category)
        {
            Category = category;
            DeckFilterPref = Category.CreateEntry("DeckFilter", true, "Deck Filter");
            DiceFilterPref = Category.CreateEntry("DiceFilter", true, "Dice Filter");
            PokerFilterPref = Category.CreateEntry("PokerFilter", true, "Poker Filter");
            SpinFilterPref = Category.CreateEntry("SpinFilter", true, "Spin Filter");
        }

        public static bool Configure(LobbyListManager lobby)
        {
            if (lobby == null)
            {
                LastConfiguredLobbyUiId = int.MinValue;
                return false;
            }

            int lobbyUiId = lobby.GetInstanceID();
            if (lobbyUiId == LastConfiguredLobbyUiId)
                return false;

            LastConfiguredLobbyUiId = lobbyUiId;

            if (lobby.DeckFilter != null)
            {
                lobby.DeckFilter.isOn = DeckFilterPref.Value;
                lobby.DeckFilter.onValueChanged.AddListener(OnDeckFilterChanged);
            }

            if (lobby.DiceFilter != null)
            {
                lobby.DiceFilter.isOn = DiceFilterPref.Value;
                lobby.DiceFilter.onValueChanged.AddListener(OnDiceFilterChanged);
            }

            if (lobby.PokerFilter != null)
            {
                lobby.PokerFilter.isOn = PokerFilterPref.Value;
                lobby.PokerFilter.onValueChanged.AddListener(OnPokerFilterChanged);
            }

            if (lobby.SpinFilter != null)
            {
                lobby.SpinFilter.isOn = SpinFilterPref.Value;
                lobby.SpinFilter.onValueChanged.AddListener(OnSpinFilterChanged);
            }

            return true;
        }

        private static void OnDeckFilterChanged(bool value)
        {
            SavePreference(DeckFilterPref, value);
        }

        private static void OnDiceFilterChanged(bool value)
        {
            SavePreference(DiceFilterPref, value);
        }

        private static void OnPokerFilterChanged(bool value)
        {
            SavePreference(PokerFilterPref, value);
        }

        private static void OnSpinFilterChanged(bool value)
        {
            SavePreference(SpinFilterPref, value);
        }

        private static void SavePreference(MelonPreferences_Entry<bool> entry, bool value)
        {
            if (entry == null || entry.Value == value)
                return;

            entry.Value = value;
            MelonPreferences.Save();
        }
    }
}
