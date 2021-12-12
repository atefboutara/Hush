using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketIO;
using UnityEngine.UI;

public class HushNetwork : MonoBehaviour
{
    public static HushNetwork Instance = null;
    public static SocketIOComponent socket;
    void Awake()
    {
        socket = GetComponent<SocketIOComponent>();
    }

    private void Start()
    {
        if (Instance == null)
            Instance = this;
        
        //socket.Connect();
        socket.On("ReceiveID", onReceiveID);
        socket.On("JoinedLobby", onJoinedLobby);
        socket.On("PlayerJoinedLobby", onPlayerJoinedLobby);
        socket.On("PlayerLeftLobby", onPlayerLeftLobby);
        socket.On("LobbyReady", onLobbyReady);
        socket.On("SetSlasher", onSetSlasher);
        socket.On("OtherPlayerLoaded", onOtherPlayerLoaded);
    }

    public void SendNameToServer()
    {
        Debug.Log("Sending name to server");
        JSONObject jSONObject = new JSONObject(JSONObject.Type.OBJECT);
        jSONObject.AddField("Name", HushManager.Instance.Name);
        socket.Emit("SendName", jSONObject);
    }

    public void onReceiveID(SocketIOEvent e)
    {
        string ID = e.data["ID"].str;
        HushManager.Instance.ID = ID;
        Debug.Log("Received ID from server, ID = " + ID + " sending name.");
        SendNameToServer();
    }

    public void onJoinedLobby(SocketIOEvent e)
    {
        int id = int.Parse(RemoveQuotes(e.data["LobbyID"].ToString()));
        int remaining = int.Parse(RemoveQuotes(e.data["Remaining"].ToString()));
        HushManager.Instance.currentLobby = id;
        if (remaining > 1 && remaining < 5)
        {
            HushManager.Instance.ChangeLobbyState(((5 - remaining) == 1 ? ("WAITING FOR 1 PLAYER") : ("WAITING FOR " + (5 - remaining) + " PLAYERS")));
        }
        else if (remaining == 1)
        {
            HushManager.Instance.ChangeLobbyState("STARTED MATCHMAKING");
        }
    }

    public string RemoveQuotes(string word)
    {
        string word2 = "";
        for (int i = 0; i < word.Length; i++)
        {
            if (word[i] != '"')
            {
                word2 += word[i];
            }
        }
        return word2;
    }

    public void onPlayerJoinedLobby(SocketIOEvent e)
    {
        int remaining = int.Parse(RemoveQuotes(e.data["Remaining"].ToString()));
        int id = int.Parse(RemoveQuotes(e.data["LobbyID"].ToString()));

        Debug.Log("A player joined our current lobby");
        if (remaining > 1 && remaining < 5)
        {
            HushManager.Instance.ChangeLobbyState(((5 - remaining) == 1 ? ("WAITING FOR 1 PLAYER") : ("WAITING FOR " + (5 - remaining) + " PLAYERS")));
        }
        else if (remaining == 1)
        {
            HushManager.Instance.ChangeLobbyState("STARTED MATCHMAKING");
        }
        /*JSONObject jSONObject = new JSONObject(JSONObject.Type.OBJECT);
        jSONObject.AddField("lobbyID", id);
        socket.Emit("onPlayerJoinedLobby", jSONObject);*/

    }

    public void onPlayerLeftLobby(SocketIOEvent e)
    {
        int remaining = int.Parse(RemoveQuotes(e.data["Remaining"].ToString()));
        int id = int.Parse(RemoveQuotes(e.data["LobbyID"].ToString()));
        Debug.Log("A player left our lobby");
        if (remaining > 1 && remaining < 5)
        {
            HushManager.Instance.ChangeLobbyState("WAITING FOR " + (5 - remaining) + " PLAYER(S)");
        }
        else if (remaining == 1)
        {
            HushManager.Instance.ChangeLobbyState("STARTED MATCHMAKING");
        }
    }

    public void onLobbyReady(SocketIOEvent e)
    {
        HushManager.Instance.ChangeLobbyState("MATCH READY");
        HushManager.Instance.LoadGame();
    }

    public void onSetSlasher(SocketIOEvent e)
    {
        bool state = e.data["State"].b;
        HushManager.Instance.Slasher = state;
    }

    public void SetReadyLoading()
    {
        JSONObject jSONObject = new JSONObject(JSONObject.Type.OBJECT);
        jSONObject.AddField("ID", HushManager.Instance.ID);
        socket.Emit("ReadyLoading", jSONObject);
    }

    public void onOtherPlayerLoaded(SocketIOEvent e)
    {
        Hints.Instance.AddLoadingPercentage();
    }

    public void AskToSpawn()
    {
        socket.Emit("SpawnMe");
    }

    public void SearchForMatch()
    {
        Debug.Log("Searching for match");
        HushManager.Instance.ChangeLobbyState("STARTED MATCHMAKING");
        JSONObject jSONObject = new JSONObject(JSONObject.Type.OBJECT);
        jSONObject.AddField("ID", HushManager.Instance.ID);
        jSONObject.AddField("Name", HushManager.Instance.Name);
        jSONObject.AddField("Mode", HushManager.Instance.currentMode);
        jSONObject.AddField("type", "Survivor");
        socket.Emit("searchMatch", jSONObject);
    }

    public void QuitMatchSearch()
    {
        socket.Emit("quitSearchMatch");
        HushManager.Instance.currentLobby = -1;
    }
}
