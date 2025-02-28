using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PageLogin : MonoBehaviour
{
    
    public TMP_InputField username;
    public TMP_InputField password;

    public TMP_InputField db_IpField;

    public Button registerButton; 

    public Button login;

    public DBConnection connection;

    private void Start()
    {
        login.onClick.AddListener(() =>
        {
            var aya = StartCoroutine(connection.Login(username.text, password.text));
         //   Debug.Log("Attempted login");
        });

        registerButton.onClick.AddListener(() =>
        {
            StartCoroutine(connection.Register(username.text, password.text));
         //   Debug.Log("Attempted registration");
        });

        db_IpField.onEndEdit.AddListener((string editedText) =>
        {
            DBConnection.db_ip = new string($"http://{editedText}");
        });

        DBConnection.OnLoggedIn += StartGame;
    }

    private void StartGame()
    {
     //   Debug.Log("StartGame");
        
        gameObject.SetActive(false);
    }
}
