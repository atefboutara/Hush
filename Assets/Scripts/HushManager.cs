using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HushManager : MonoBehaviour
{
    public static HushManager Instance;
    public string currentMode = "Solo";
    public string Name = "";
    public string ID = "";
    public string LobbyState = "";
    public int currentLobby = -1;
    public bool Slasher = false;

    #region MonoBehaviour
    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
        //PlayerPrefs.DeleteAll();
        if(!PlayerPrefs.HasKey("Name"))
        {
            LUI_PUBG.Instance.inputNamePanel.Play("Panel Open");
        } else
        {
            string name = PlayerPrefs.GetString("Name");
            Debug.Log("Username : " + name);
            Name = name;
            LUI_PUBG.Instance.PlayerProfile.transform.GetChild(1).GetComponent<Text>().text = name;
            //HushNetwork.Instance.SendNameToServer();
        }
    }

    public void ChooseName()
    {
        string name = LUI_PUBG.Instance.inputNamePanel.transform.GetChild(1).GetChild(0).GetComponent<InputField>().text;
        PlayerPrefs.SetString("Name", name);
        PlayerPrefs.Save();
        Debug.Log("Username : " + name);
        LUI_PUBG.Instance.inputNamePanel.Play("Panel Close");
        Name = name;
        LUI_PUBG.Instance.PlayerProfile.transform.GetChild(1).GetComponent<Text>().text = name;
        HushNetwork.Instance.SendNameToServer();
    }

    public void ChangeLobbyState(string state)
    {
        LobbyState = state;
        LUI_PUBG.Instance.matchMaking.transform.GetChild(2).GetComponent<Text>().text = state;
    }

    public void LoadGame()
    {
        SceneManager.LoadScene("GamePlayMansion");
    }
    #endregion
}
