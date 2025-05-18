using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public TMP_Text statusText;

    public void OnLoginButtonPressed()
    {
        StartCoroutine(LoginCoroutine());
    }

    private IEnumerator LoginCoroutine()
    {
        string username = usernameField.text;
        string password = passwordField.text;
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post("http://localhost/bridge_login/bridge_login.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json= www.downloadHandler.text;
                PlayerData data = JsonUtility.FromJson<PlayerData>(json);
                if (data.status == "success")
                {
                    UserSession.Instance.username= data.username;
                    UserSession.Instance.elo= data.elo;
                    SceneManager.LoadScene("StartScene");
                }
                else
                {
                    statusText.text = "Incorrect username or password";
                }
            }
            else
            {
                statusText.text = "Connection failure " + www.error;
            }
        }
    }

    public void OnRegisterButtonPressed()
    {
        StartCoroutine(RegisterCoroutine());
    }

    private IEnumerator RegisterCoroutine()
    {
        string username = usernameField.text;
        string password = passwordField.text;

        if (username.Length < 3 || password.Length < 3)
        {
            statusText.text = "Zbyt krótka nazwa użytkownika lub hasło.";
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post("http://localhost/bridge_login/register.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string response = www.downloadHandler.text;
                if (response == "success")
                {
                    statusText.text = "Rejestracja zakończona sukcesem!";
                }
                else if (response == "exists")
                {
                    statusText.text = "Użytkownik już istnieje.";
                }
                else
                {
                    statusText.text = "Błąd rejestracji.";
                }
            }
            else
            {
                statusText.text = "Błąd połączenia: " + www.error;
            }
        }
    }
    public void ReturnToMainMenu()
    {
        Debug.Log("Kliknieto Main Menu");
        SceneManager.LoadScene("StartScene");
    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "StartScene")
        {
            Destroy(this.gameObject);
        }
    }
}
public class PlayerData
{
    public string status;
    public string username;
    public int elo;
}
