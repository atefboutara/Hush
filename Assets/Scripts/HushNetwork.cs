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
    public bool searchingMatch = false;

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
        socket.On("AllPlayersHaveLoaded", onAllPlayersHaveLoaded);
        socket.On("SpawnOthers", onSpawnOthers);
        socket.On("UpdatePositionForOthers", onUpdatePositionForOthers);
        socket.On("UpdateAnimationForOthers", onUpdateAnimationForOthers);
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
        Debug.Log("My spawn point is (ID: "+HushManager.Instance.ID+")" + HushManager.Instance.SpawnPoint);
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

    public void onAllPlayersHaveLoaded(SocketIOEvent e)
    {
        Debug.Log("All players have loaded");
        Hints.Instance.AddLoadingPercentage();
        Hints.Instance.transform.parent.parent.gameObject.SetActive(false);
        AskToSpawn();
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
        if (playerType == "Hunter")
        {
            EquipItems EI = netplayer.AddComponent<EquipItems>();
        }
    }

    public void onUpdatePositionForOthers(SocketIOEvent e)
    {
        string playerID = RemoveQuotes(e.data["id"].ToString());
        Vector3 pos = GetVectorFromJson("pos", e);
        Vector3 rot = GetVectorFromJson("Rot", e);

        GameObject otherPlayer = GameObject.Find(playerID);
        otherPlayer.transform.position = pos;
        otherPlayer.transform.eulerAngles = rot;
    }

    public void SendMyPositionToOthers()
    {
        JSONObject jSONObject = new JSONObject(JSONObject.Type.OBJECT);
        bool slash = HushManager.Instance.Slasher;
        jSONObject.AddField("type", slash ? "Hunter" : "Survivor");
        jSONObject.AddField("pos", Vector3ToJson(Player.Instance.transform.position));
        jSONObject.AddField("Rot", Vector3ToJson(Player.Instance.transform.eulerAngles));
        socket.Emit("UpdatePosition", jSONObject);
    }

    public void SendMyAnimation(string animname, bool animstate)
    {
        JSONObject jSONObject = new JSONObject(JSONObject.Type.OBJECT);
        bool slash = HushManager.Instance.Slasher;
        jSONObject.AddField("type", slash ? "Hunter" : "Survivor");
        jSONObject.AddField("animation", animname);
        jSONObject.AddField("state", animstate);
        socket.Emit("UpdateAnimation", jSONObject);
    }

    public void onUpdateAnimationForOthers(SocketIOEvent e)
    {
        string playerID = RemoveQuotes(e.data["id"].ToString());
        string animation = RemoveQuotes(e.data["animation"].ToString());
        bool state = bool.Parse(RemoveQuotes(e.data["state"].ToString()));
        GameObject otherPlayer = GameObject.Find(playerID);
        if(animation == "Hiding")
        {
            Rigidbody _rigidbody = otherPlayer.GetComponent<Rigidbody>();
            if (state)
                _rigidbody.isKinematic = true;
            else _rigidbody.isKinematic = false;
        }
        otherPlayer.GetComponent<Animator>().SetBool(animation, state);

    }

    public string RemoveQuotesAndReplacePoint(string word)
    {
        string word2 = "";
        for (int i = 0; i < word.Length; i++)
        {
            if (word[i] == '.')
            {
                word2 += ',';
            }
            else if (word[i] != '"')
            {
                word2 += word[i];
            }
        }
        return word2;
    }

    Vector3 GetVectorFromJson(string key,SocketIOEvent obj)
    {
        string x = RemoveQuotesAndReplacePoint(obj.data[key]["x"].ToString());
        string y = RemoveQuotesAndReplacePoint(obj.data[key]["y"].ToString());
        string z = RemoveQuotesAndReplacePoint(obj.data[key]["z"].ToString());
        return new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
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
        bool slash = HushManager.Instance.Slasher;
        jSONObject.AddField("type", slash ? "Hunter" : "Survivor");
        jSONObject.AddField("pos", Vector3ToJson(player.transform.position));
        jSONObject.AddField("Rot", Vector3ToJson(player.transform.eulerAngles));
        socket.Emit("SpawnRequest", jSONObject);
    }

    public void SearchForMatch()
    {
        if(!searchingMatch)
        {
            HushManager.Instance.Slasher = false;
            Debug.Log("Searching for match");
            HushManager.Instance.ChangeLobbyState("STARTED MATCHMAKING");
            JSONObject jSONObject = new JSONObject(JSONObject.Type.OBJECT);
            jSONObject.AddField("ID", HushManager.Instance.ID);
            jSONObject.AddField("Name", HushManager.Instance.Name);
            jSONObject.AddField("Mode", HushManager.Instance.currentMode);
            jSONObject.AddField("type", "Survivor");
            socket.Emit("searchMatch", jSONObject);
            searchingMatch = true;
        }
    }

    public void QuitMatchSearch()
    {
        socket.Emit("quitSearchMatch");
        HushManager.Instance.currentLobby = -1;
    }
}
