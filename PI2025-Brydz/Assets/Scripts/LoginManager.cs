using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text messageText;

    private string savedUsername = "";
    private string savedPassword = "";

    public void OnLoginClick()
    {
        string enteredUsername = usernameInput.text;
        string enteredPassword = passwordInput.text;

        if (enteredUsername == savedUsername && enteredPassword == savedPassword)
        {
            messageText.text = "You are logged in";
            SceneManager.LoadScene("StartScene");
        }
        else
        {
            messageText.text = "Incorrect username or password";
        }
    }

    public void OnRegisterClick()
    {
        savedUsername = usernameInput.text;
        savedPassword = passwordInput.text;
        messageText.text = "You are signed in! You can log in now";
    }
}
