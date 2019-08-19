using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts
{
    public class PauseUIManager : MonoBehaviour
    {
        public void OnResume()
        {
            //Time.timeScale = 1; //Not allowed by Bolt :(
            gameObject.SetActive(false);
        }

        public void OnExit()
        {
            DisconnectAll();
        }

        void DisconnectAll()
        {
            if (BoltNetwork.IsServer)
            {
                foreach (var connection in BoltNetwork.Connections)
                {
                    connection.Disconnect();
                }
            }
            BoltNetwork.Shutdown();
            SceneManager.LoadSceneAsync("Menu");
        }
    }
}