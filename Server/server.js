var port = process.env.PORT || 3251; 
var io = require('socket.io')(port);
var shortid = require('shortid');

console.log('Server started on port ' + port);
var MatchLobbys = []
var players = [];
var survivorTotal = 5;
var totalSurvivorSpawnPoints = 4;

function randomInteger(min, max) {
	return Math.floor(Math.random() * (max - min + 1)) + min;
  }

class MatchLobby {
	constructor()
	{
		console.log('Match lobby created');
		this.SurvivorCount = 0;
		this.HunterCount = 0;
		this.SurvivorList = [];
		this.HunterList = [];
		this.SpawnPointsTaken = [];
		this.LobbyID = ++MatchLobby.counter;
		this.MatchStarting = false;
		this.CheckingReady = false;
	}
	
	AddSurvivor(ID)
	{
		this.SurvivorCount += 1;
		players[ID].type = "Survivor";
		//this.SurvivorList[this.SurvivorCount-1] = players[ID];
		this.SurvivorList.push(players[ID]);
		console.log('Added Player ', this.SurvivorCount,'/',survivorTotal,' (PlayerID: ', ID,') to lobby ', this.LobbyID);
		if(this.SurvivorCount == survivorTotal)
		{
			this.FirstHunterSlotID = randomInteger(0, this.SurvivorCount-1);
			this.MatchStarting = true;
			console.log("Match starting on lobby ", this.LobbyID);
			io.to(this.SurvivorList[this.FirstHunterSlotID].socketid).emit('SetSlasher', {State:true});
			this.ChangePlayerToHunter(ID);

			var i;
			for(i = 0; i<this.SurvivorCount; i++)
			{
				var rSpawn = randomInteger(0,totalSurvivorSpawnPoints-1);
				var generatedSpawn = false;
				while(!generatedSpawn)
				{
					if(this.SpawnPointsTaken.includes(rSpawn))
						rSpawn = randomInteger(0,totalSurvivorSpawnPoints-1);
					else {
						this.SpawnPointsTaken.push(rSpawn);
						generatedSpawn = true;
					}
				}
				this.SurvivorList[i].spawnPoint = rSpawn;
			}

			for(i = 0; i<this.SurvivorCount; i++)
			{
				io.to(this.SurvivorList[i].socketid).emit('LobbyReady',{"spawnPoint":rSpawn});
			}
			for(i = 0; i<this.HunterCount; i++)
			{
				io.to(this.HunterList[i].socketid).emit('LobbyReady',{"spawnPoint":0});
			}
		}
	}
	
	RemoveSurvivor(ID, lobbyParam)
	{
		//delete this.SurvivorList[this.GetSurvivorIndex(ID)];
		this.SurvivorList.splice(this.GetSurvivorIndex(ID), 1);
		this.SurvivorCount -= 1;
		console.log("Remaining survivors in lobby ", lobbyParam, " : ", this.SurvivorCount);
	}
	
	RemoveHunter(ID)
	{
		//delete this.HunterList[this.GetHunterIndex(ID)];
		this.HunterList.splice(this.GetHunterIndex(ID), 1);
		this.HunterCount -= 1;
	}

	ChangePlayerToHunter(ID)
	{
		this.SurvivorList.splice(this.GetSurvivorIndex(ID), 1);
		this.SurvivorCount -= 1;
		this.HunterList.push(players[ID]);
		this.HunterCount += 1;
		this.HunterList[0].spawnPoint = 0;
		this.HunterList[0].type = "Hunter";
		this.HunterList[0].id = ID;
		players[ID].type = "Hunter";
		players[ID].spawnPoint = 0;
		console.log("Player " + ID + " chosen as slasher in lobby " + this.LobbyID);
	}
	
	GetSurvivorIndex(ID)
	{
		var i;
		for(i = 0; i< this.SurvivorList.length; i++)
		{
			if(this.SurvivorList[i].id == ID)
			{
				console.log("found ID ", ID, " in survivorlist of lobby ", this.LobbyID);
				return i;
			}
		}
		return -1;
	}
	
	GetHunterIndex(ID)
	{
		var i;
		for(i = 0; i< this.HunterList.length; i++)
		{
			if(this.HunterList[i].id == ID)
			{
				console.log("found ID ", ID, " in hunterlist of lobby ", this.LobbyID);
				return i;
			}
		}
		return -1;
	}
	
	GetAllPlayers()
	{
		var AllPlayers = [];
		var i;
		
		for(i = 0; i< this.HunterList.length; i++)
		{
			AllPlayers.push(this.HunterList[i]);
		}
		
		for(i = 0; i< this.SurvivorList.length; i++)
		{
			AllPlayers.push(this.SurvivorList[i]);
		}
		return AllPlayers;
	}

	LoadedOneMorePlayer(ID)
	{
		var i = 0;
		for(i = 0; i<this.SurvivorCount; i++)
		{
			if(this.SurvivorList[i].id == ID)
				continue;
			io.to(this.SurvivorList[i].socketid).emit('OtherPlayerLoaded');
		}
		for(i = 0; i<this.HunterCount; i++)
		{
			if(this.HunterList[i].id == ID)
				continue;
			io.to(this.HunterList[i].socketid).emit('OtherPlayerLoaded');
		}
	}
}

MatchLobby.counter = -1;

function CreateLobby()
{
	var L = new MatchLobby();
	MatchLobbys.push(L);
	return L;
}

io.on('connection', function(socket){
	var thisPlayerId = shortid.generate();
	var searchingMatch = false;
	var CurrentLobby;
	var inLobby = false;
    var player = {
        id: thisPlayerId,
		socketid: socket.id,
		name:"",
		type: "",
		currentLobby: null,
		pos:{
			x:0,
			y:0,
			z:0
		},
		Rot:{
			x:0,
			y:0,
			z:0
		},
		spawnPoint:-1
    }
	players[thisPlayerId] = player;
	console.log("Client connected with ID: " + thisPlayerId + " waiting for name..");
	socket.emit('ReceiveID', {ID: thisPlayerId});
	
	socket.on('SendName', function(data) {
		console.log("Received name " + data.Name + " for client ID : " + thisPlayerId);
		player.name = data.Name;
	});

	socket.on('updateProfileToSlasher', function(data) {
		player.type = "Hunter";
	});
	
	socket.on('SpawnRequest', function(data) {
		console.log('Spawn request from PlayerID: ', thisPlayerId);
		if(player.type == "Hunter")
		{
			data.type = "Hunter";
			lobbyIndex = CurrentLobby.GetHunterIndex(thisPlayerId);
			CurrentLobby.HunterList[0].pos = data.pos;
			CurrentLobby.HunterList[0].Rot = data.Rot;
			var i = 0;
			for(i = 0; i<CurrentLobby.SurvivorCount; i++)
			{
				io.to(CurrentLobby.SurvivorList[i].socketid).emit('SpawnOthers', {"thisPlayerId":CurrentLobby.HunterList[lobbyIndex].thisPlayerId,"type":CurrentLobby.HunterList[lobbyIndex].type,"spawnPoint":CurrentLobby.HunterList[lobbyIndex].spawnPoint});
			}
			//console.log('PlayerID: ', thisPlayerId, ' informations: ', CurrentLobby.HunterList[lobbyIndex]);
			console.log("Spawned " + thisPlayerId);
		}
		else
		{
			data.type = "Survivor";
			lobbyIndex = CurrentLobby.GetSurvivorIndex(thisPlayerId);	
			CurrentLobby.SurvivorList[lobbyIndex].pos = data.pos;
			CurrentLobby.SurvivorList[lobbyIndex].Rot = data.Rot;
			console.log("Set pos and rotation for " + thisPlayerId + " sending to other players");
			var i = 0;
			for(i = 0; i<CurrentLobby.SurvivorCount; i++)
			{
				if(CurrentLobby.SurvivorList[i].id == thisPlayerId)
					continue;
				io.to(CurrentLobby.SurvivorList[i].socketid).emit('SpawnOthers', {"thisPlayerId":CurrentLobby.SurvivorList[lobbyIndex].thisPlayerId,"type":CurrentLobby.SurvivorList[lobbyIndex].type,"spawnPoint":CurrentLobby.SurvivorList[lobbyIndex].spawnPoint});
			}
			console.log("Spawned " + thisPlayerId);
			//console.log('PlayerID: ', thisPlayerId, ' informations: ', CurrentLobby.SurvivorList[lobbyIndex]);
		}

		var PlayersInLobby = CurrentLobby.GetAllPlayers();
		var i;
		for(i = 0; i< PlayersInLobby.length; i++)
		{
			if(PlayersInLobby[i].id == thisPlayerId)
				continue;
			socket.emit('SpawnOthers', {"thisPlayerId":PlayersInLobby[i].thisPlayerId, "type":PlayersInLobby[i].type, "spawnPoint":PlayersInLobby[i].spawnPoint});
		}
	});
	
	socket.on('UpdatePosition', function(data) {
		console.log("Position data update recieved (PlayerID: ", thisPlayerId, ")", data);
		var lobbyIndex = -1;
		if(player.type == "Hunter")
		{
			data.type = "Hunter";
			lobbyIndex = CurrentLobby.GetHunterIndex(thisPlayerId);
			CurrentLobby.HunterList[lobbyIndex].pos = data.pos;
			CurrentLobby.HunterList[lobbyIndex].Rot = data.Rot;
		}
		else
		{
			data.type = "Survivor";
			lobbyIndex = CurrentLobby.GetSurvivorIndex(thisPlayerId);	
			CurrentLobby.SurvivorList[lobbyIndex].pos = data.pos;
			CurrentLobby.SurvivorList[lobbyIndex].Rot = data.Rot;
		}
		data.id=thisPlayerId;
		socket.broadcast.emit('UpdatePositionForOthers',data);
	});
	
	socket.on('quitSearchMatch', function(data) {
		if(inLobby == false)
		{
			searchingMatch = false;
			console.log('PlayerID: ', thisPlayerId, ' stopped searching for a lobby');
		}
		else {
			console.log('PlayerID: ', thisPlayerId, ' left current lobby : ', CurrentLobby.LobbyID);
			if(player.type == "Hunter")
			{
				CurrentLobby.RemoveHunter(thisPlayerId);
				inLobby = false;
				if(CurrentLobby.SurvivorCount == 0 && CurrentLobby.HunterCount == 0)
				{
					console.log("Destroying lobby : ", CurrentLobby.LobbyID, " because everyone left it");
					MatchLobbys.splice(CurrentLobby.LobbyID, 1);
					MatchLobbys.counter--;
					if(MatchLobbys.length == 0)
						MatchLobby.counter = -1;					
				}
			}
			else {
				CurrentLobby.RemoveSurvivor(thisPlayerId, CurrentLobby.LobbyID);
				inLobby = false;
				if(CurrentLobby.SurvivorCount == 0 && CurrentLobby.HunterCount == 0)
				{
					console.log("Destroying lobby : ", CurrentLobby.LobbyID, " because everyone left it");
					MatchLobbys.splice(CurrentLobby.LobbyID, 1);
					MatchLobbys.counter--;
					if(MatchLobbys.length == 0)
						MatchLobby.counter = -1;					
				} else {
					for(i = 0; i<CurrentLobby.SurvivorCount; i++)
					{
						io.to(CurrentLobby.SurvivorList[i].socketid).emit('PlayerLeftLobby', {LobbyID:CurrentLobby.LobbyID, Remaining:CurrentLobby.SurvivorCount});
					}
				}
			}
			CurrentLobby = null;
		}
	});
	
	socket.on('searchMatch', function(data) {
		console.log("Player ", thisPlayerId , " searching for lobby");
		player.type = data.type;
		searchingMatch = true;
		
		while(searchingMatch && inLobby == false) {
			console.log("Player ID : ", thisPlayerId, " searching for lobby loop ");
			if(MatchLobbys.length > 0 && (AllLobbysFull(player.type) == false))
			{
				console.log("PlayerID: ", thisPlayerId, " there are existing lobbys");
				var i;
				for(i = 0; i < MatchLobbys.length; i++)
				{
					/*if(player.type == "Hunter")
					{
						if(MatchLobbys[i].HunterCount == 0)
						{
							MatchLobbys[i].AddHunter(thisPlayerId);
							searchingMatch = false;
							inLobby = true;
							CurrentLobby = MatchLobbys[i];
							socket.emit('JoinedLobby');
							socket.broadcast.emit('PlayerJoinedLobby', {lobbyID:MatchLobbys[i].LobbyID});
						}
					}
					else {*/
						if((MatchLobbys[i].SurvivorCount + MatchLobbys[i].HunterCount) < survivorTotal)
						{
							MatchLobbys[i].AddSurvivor(thisPlayerId);
							searchingMatch = false;
							inLobby = true;
							CurrentLobby = MatchLobbys[i];
							player.currentLobby = CurrentLobby;
							console.log("PlayerID: ", thisPlayerId, " added to lobby ", i);
							socket.emit('JoinedLobby', {LobbyID:i, Remaining:CurrentLobby.SurvivorCount});
							//socket.broadcast.emit('PlayerJoinedLobby', {LobbyID:i, Remaining:CurrentLobby.SurvivorCount});
							var i = 0;
							for(i = 0; i<CurrentLobby.SurvivorCount; i++)
							{
								if(CurrentLobby.SurvivorList[i].id == thisPlayerId)
									continue;
								io.to(CurrentLobby.SurvivorList[i].socketid).emit('PlayerJoinedLobby', {LobbyID:i, Remaining:CurrentLobby.SurvivorCount});
							}
							/*if(CurrentLobby.MatchStarting)
							{
								socket.emit('LobbyReady');
							}*/
							/*if(CurrentLobby.MatchStarting)
							{
								var i;
								for(i = 0; i<CurrentLobby.SurvivorCount; i++)
								{
									io.to(CurrentLobby.SurvivorList[i].socketid).emit('LobbyReady');
								}
							}*/
							break;
						}
					//}
				}
			}
			else {
				console.log("PlayerID: ", thisPlayerId, " creating lobby since no lobby was found (either full or inexistant)");
				var L = CreateLobby();
				CurrentLobby = L;
				player.currentLobby = CurrentLobby;
				L.AddSurvivor(thisPlayerId);
				searchingMatch = false;
				inLobby = true;
				socket.emit('JoinedLobby', {LobbyID:CurrentLobby.LobbyID, Remaining:CurrentLobby.SurvivorCount});
				break;
			}
		}
	});

	socket.on("ReadyLoading", function(data) {
		CurrentLobby.LoadedOneMorePlayer(thisPlayerId);
	});
	
	socket.on('disconnect', function() {
		console.log('Client disconnected');
		if(searchingMatch)
			searchingMatch = false;
		else if(inLobby)
		{
			if(player.type == "Hunter")
			{
				CurrentLobby.RemoveHunter(thisPlayerId);
				inLobby = false;
				if(CurrentLobby.SurvivorCount == 0 && CurrentLobby.HunterCount == 0)
				{
					console.log("Destroying lobby : ", CurrentLobby.LobbyID, " because everyone left it");
					MatchLobbys.splice(CurrentLobby.LobbyID, 1);
					MatchLobbys.counter--;
					if(MatchLobbys.length == 0)
						MatchLobby.counter = -1;
					var i = 0;
					for(i = 0; i<CurrentLobby.SurvivorCount; i++)
					{
						io.to(CurrentLobby.SurvivorList[i].socketid).emit('disconnected', {id: thisPlayerId});
					}
				}
			}
			else {
				CurrentLobby.RemoveSurvivor(thisPlayerId, CurrentLobby.LobbyID);
				inLobby = false;
				if(CurrentLobby.SurvivorCount == 0 && CurrentLobby.HunterCount == 0)
				{
					console.log("Destroying lobby : ", CurrentLobby.LobbyID, " because everyone left it");
					MatchLobbys.splice(CurrentLobby.LobbyID, 1);
					MatchLobbys.counter--;
					if(MatchLobbys.length == 0)
						MatchLobby.counter = -1;
					var i = 0;
					for(i = 0; i<CurrentLobby.SurvivorCount; i++)
					{
						io.to(CurrentLobby.SurvivorList[i].socketid).emit('disconnected', {id: thisPlayerId});
					}
				}
			}
		}
		CurrentLobby = null;
		delete players[thisPlayerId];
	});		
	
	function AllLobbysFull(type)
	{
		var bool = true;
		var i;
		for(i = 0; i < MatchLobbys.length; i++)
		{
			/*if(type == "Hunter")
			{
				if(MatchLobbys[i].HunterCount == 0)
				{
					bool = false;
					break;
				}
			}
			else 
			{*/
				if((MatchLobbys[i].SurvivorCount + MatchLobbys[i].HunterCount) < survivorTotal)
				{
					bool = false;
					break;
				}
			//}
		}
		return bool;
	}
})

