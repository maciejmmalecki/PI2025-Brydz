using TMPro;
using UnityEngine;

/// <summary>
/// Wyświetla nazwę użytkownika i jego ELO na ekranie.
/// Pokazuje też komunikat o braku logowania.
/// </summary>
public class UserDisplay : MonoBehaviour
{
    public TMP_Text usernameText;
    public TMP_Text eloText;

    void Start()
    {
        if (UserSession.Instance != null && !string.IsNullOrEmpty(UserSession.Instance.username))
        {
            usernameText.text = UserSession.Instance.username;
            eloText.text = "Elo: "+UserSession.Instance.elo;
        }else{
            usernameText.text = "You aren't logged in";
            eloText.text ="";
        }
    }
}