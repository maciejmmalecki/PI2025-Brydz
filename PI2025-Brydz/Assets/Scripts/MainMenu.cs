using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayWithBots()
    {
        PlayerPrefs.SetString("GameMode", "Bots");
        SceneManager.LoadScene("GameScene");
    }

    public void PlayMultiplayer()
    {
        PlayerPrefs.SetString("GameMode", "Multiplayer");
        SceneManager.LoadScene("MultiplayerGameScene"); // przejdź do logowania przed grą online
    }

    public void GoToLogin()
    {
        SceneManager.LoadScene("LoginScene");
    }
}

