using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Komponent menu głównego gry.
/// Umożliwia rozpoczęcie gry z botami, trybu multiplayer oraz zalogowanie się.
/// </summary>
public class MainMenu : MonoBehaviour
{
    /// <summary>
    /// Uruchamia tryb gry lokalnej z botami.
    /// </summary>
    public void PlayWithBots()
    {
        PlayerPrefs.SetString("GameMode", "Bots");
        SceneManager.LoadScene("GameScene");
    }

    /// <summary>
    /// Przechodzi do sceny multiplayer.
    /// </summary>
    public void PlayMultiplayer()
    {
        PlayerPrefs.SetString("GameMode", "Multiplayer");
        SceneManager.LoadScene("MultiplayerGameScene");
    }

    /// <summary>
    /// Przechodzi do sceny logowania
    /// </summary>
    public void GoToLogin()
    {
        SceneManager.LoadScene("LoginScene");
    }
}

