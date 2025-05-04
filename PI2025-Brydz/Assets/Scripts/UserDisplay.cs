using TMPro;
using UnityEngine;

public class UserDisplay : MonoBehaviour
{
    public TMP_Text usernameText;
    public TMP_Text eloText;

    void Start()
    {
        if (UserSession.Instance != null)
        {
            usernameText.text = UserSession.Instance.username;
            eloText.text = ""+UserSession.Instance.elo;
        }else{
            usernameText.text = "You aren't logged in";
        }
    }
}