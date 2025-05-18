using UnityEngine;

public class UserSession : MonoBehaviour
{
    public static UserSession Instance;

    public string username;
    public int elo;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
