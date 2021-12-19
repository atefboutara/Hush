using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketIO;
using UnityEngine.UI;

public class HushNetwork : MonoBehaviour
{
    public static HushNetwork Instance = null;
    public static SocketIOComponent socket;
    public GameObject PlayerPrefab;
    public GameObject NetworkPlayerPrefab;

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
        socket.On("SpawnOthers", onSpawnOthers);
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
        if (PlayerPrefs.HasKey("Name"))
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

    Vector3 getSpawnPosition()
    {
        GameObject SpawnPoint = GameObject.Find(HushManager.Instance.Slasher ? "SlasherSpawn" : "SpawnPoint ("+HushManager.Instance.SpawnPoint+")");
        return SpawnPoint.transform.position;
    }

    Vector3 getNetworkSpawnPosition(int s, bool isSlasher)
    {
        GameObject SpawnPoint = GameObject.Find(isSlasher ? "SlasherSpawn" : "SpawnPoint (" + s + ")");
        return SpawnPoint.transform.position;
    }

    public void onLobbyReady(SocketIOEvent e)
    {
        HushManager.Instance.SpawnPoint = int.Parse(RemoveQuotes(e.data["spawnPoint"].ToString()));
        HushManager.Instance.ChangeLobbyState("MATCH READY");
        HushManager.Instance.LoadGame();
    }

    public void onSetSlasher(SocketIOEvent e)
    {
        bool state = e.data["State"].b;
        HushManager.Instance.Slasher = state;
        socket.Emit("updateProfileToSlasher");
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

    public void onSpawnOthers(SocketIOEvent e)
    {
        Debug.Log(e.data);
        string playerID = RemoveQuotes(e.data["thisPlayerId"].ToString());
        Debug.Log("playerID " + playerID);
        string playerType = RemoveQuotes(e.data["type"].ToString());
        Debug.Log("playerType " + playerType);
        int spawnPoint = int.Parse(RemoveQuotes(e.data["spawnPoint"].ToString()));
        Debug.Log("Spawnpoint " + spawnPoint);
        GameObject netplayer = Instantiate(NetworkPlayerPrefab);
        netplayer.transform.name = playerID;
        netplayer.transform.position = getNetworkSpawnPosition(spawnPoint,playerType == "Hunter");
    }

    JSONObject Vector3ToJson(Vector3 pos)
    {
        JSONObject jSONObject = new JSONObject(JSONObject.Type.OBJECT);
        jSONObject.AddField("x", pos.x.ToString());
        jSONObject.AddField("y", pos.y.ToString());
        jSONObject.AddField("z", pos.z.ToString());
        return jSONObject;
    }

    public void AskToSpawn()
    {
        GameObject player = Instantiate(PlayerPrefab);
        player.transform.position = getSpawnPosition();
        JSONObject jSONObject = new JSONObject(JSONObject.Type.OBJECT);
        jSONObject.AddField("pos", Vector3ToJson(player.transform.position));
        jSONObject.AddField("Rot", Vector3ToJson(player.transform.eulerAngles));
        socket.Emit("SpawnRequest", jSONObject);
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
