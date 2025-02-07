using System;
using System.Collections;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
public class DBConnection : MonoBehaviour
{
    public static string usernameAH = "";

    //DIE IPs MUSS MAN ÄNDERN!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    public static string testingURL = "http://192.168.0.222/api/user.php";
    public static string userRegisterURL = "http://192.168.0.222/api/UserLogin.php";
    
    public bool loggedIn = false;
    public event Action OnLoggedIn;
    private string currentUsername;


    IEnumerator Get(string url)
    {
        var request = new UnityWebRequest(url, "GET");
        request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        Debug.Log("Status Code: " + request.responseCode);
        Debug.Log(request.downloadHandler.text);
        string json = JsonUtility.ToJson(request.downloadHandler.text);

        Debug.Log(json);
    }


    public IEnumerator Login(string username, string password)
    {
        UserLoginData loginData = new UserLoginData
        {
            apikey = "bestpasseuwest",
            name = username,
            password = password,
            Login_Method = "Login"
        };
        string json = JsonUtility.ToJson(loginData);

        using (var request = new UnityWebRequest("http://192.168.0.222/api/UserLogin.php", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (!request.isNetworkError && !request.isHttpError && request.responseCode == 200)
            {
                try
                {
                    LoginResponse response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
                    if (response.Status)
                    {
                    ////////////////////////////////////////////////////////////    Debug.Log($"[DBConnection] Login successful. Username: {username}");

                        // Speichere den Benutzernamen
                        currentUsername = username;
                        usernameAH = username;

                        ulong localClientId = NetworkManager.Singleton.LocalClientId;
                        GameManager.Instance.SetLoggedInUsernameRpc(localClientId, username);

                        OnLoggedIn?.Invoke();
                    }
                    else
                    {
                        Debug.LogError($"Login failed: {response.Message}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"JSON parse error: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Network or HTTP error: {request.error}");
            }
        }
    }

    // Methode, um den aktuellen Benutzernamen zu erhalten
    public string GetUsername()
    {
        return currentUsername;
    }


    public void AddLoginListener(Action listener)
    {
        OnLoggedIn += listener;

        
        if (loggedIn)
        {
            listener?.Invoke();
        }
    }

    public void RemoveLoginListener(Action listener)
    {
        OnLoggedIn -= listener;
    }

    public IEnumerator Register(string username, string password)
    {
        UserLoginData registerData = new UserLoginData
        {
            apikey = "bestpasseuwest",
            name = username,
            password = password,
            Login_Method = "Register"
        };
        string json = JsonUtility.ToJson(registerData);

        using (var request = new UnityWebRequest(userRegisterURL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.responseCode == 202)
            {
                Debug.Log($"[DBConnection] Registration successful: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"[DBConnection] Registration failed: {request.responseCode}");
            }
        }
    }
}
[Serializable]
public class LoginResponse
{
    public string Message;
    public bool Status;
    public string UserName;
}



