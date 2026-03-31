using MelonLoader;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(HappyHour.Core), "Happy Hour", "1.0.0", "w2og")]
[assembly: MelonGame("Curve Animation", "Liar's Bar")]

namespace HappyHour
{
    public class Core : MelonMod
    {
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
    }
}
