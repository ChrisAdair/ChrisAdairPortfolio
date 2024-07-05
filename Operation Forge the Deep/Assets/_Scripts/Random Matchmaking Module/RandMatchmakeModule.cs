using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.MasterServer;
using Barebones.Networking;

public enum RandMatchmakeOpCodes
{
    queueUp = 100,
    cancelQueue = 101
}

public class RandMatchmakeModule : MatchmakerModule {

    public int maxPlayers = 4;
    public float maxWaitTime = 30;

    private Queue<IIncommingMessage> _playerQueue;
    private SpawnersModule spawner;
    private ChatModule chatModule;
    private int maxLevels;
    private ISaveDatabase db;
    private float waitTime;
    private int chatRoomName = 0;


    public override void Initialize(IServer server)
    {
        base.Initialize(server);

        //Get spawner module for game creation, and chat module for game communication
        spawner = server.GetModule<SpawnersModule>();
        AddOptionalDependency<ChatModule>();
        chatModule = server.GetModule<ChatModule>();
        //Handlers for messages from client
        server.SetHandler((short)RandMatchmakeOpCodes.queueUp, HandleQueueUp);
        server.SetHandler((short)RandMatchmakeOpCodes.cancelQueue, HandleCancelQueue);

        //Set up variables for class
        db = Msf.Server.DbAccessors.GetAccessor<ISaveDatabase>();
        db.GetNumberOfLevels((levels) => { maxLevels = levels; });
        waitTime = Time.time;
        _playerQueue = new Queue<IIncommingMessage>();

        //Start the update queue that puts players into rooms
        StartCoroutine(Matchmake());
    }


    private void HandleQueueUp(IIncommingMessage message)
    {
        _playerQueue.Enqueue(message);
    }

    private void HandleCancelQueue(IIncommingMessage message)
    {
        //Find a way to avoid the player joining if they don't want to anymore
        Queue<IIncommingMessage> temp = new Queue<IIncommingMessage>();
        bool foundPlayer = false;
        for(int i =0; i < _playerQueue.Count; i++)
        {
            IIncommingMessage player = _playerQueue.Dequeue();
            if(player.Peer == message.Peer)
            {
                foundPlayer = true;
                break;
            }
            temp.Enqueue(player);
        }

        //If the player is not found in the queue, notify the client that there is an error
        if (!foundPlayer)
        {
            message.Respond("Failed to find player in the queue", ResponseStatus.Failed);
            _playerQueue = temp;
            return;
        }

        //Reform the queue in it's original order
        for (int i = 0; i < _playerQueue.Count; i++)
        {
            temp.Enqueue(_playerQueue.Dequeue());
        }
        _playerQueue = temp;

        //Notify client of success
        message.Respond("", ResponseStatus.Success);
    }

    private IEnumerator Matchmake()
    {
        while(true)
        {
            if (_playerQueue.Count < 1)
            {
                waitTime = Time.time;
                yield return null;
                continue;
            }

            bool foundGame = false;
            foreach(GameInfoPacket game in GetCurrentGames(_playerQueue.Peek().Peer))
            {
                if(game.MaxPlayers - game.OnlinePlayers > 0)
                {
                    //Send message to client about the game
                    IIncommingMessage player = _playerQueue.Dequeue();
                    player.Respond(game.ToBytes(), ResponseStatus.Success);

                    foundGame = true;
                    break;
                }
            }

            if (foundGame)
            {
                yield return new WaitForSeconds(5f);
                continue;
            } 
            else
            {
                if ((_playerQueue.Count >= 4 && maxLevels > 0) || (Time.time - waitTime) > maxWaitTime)
                {
                    //Create new game for players to join
                    CreateNewGame();
                    break;
                }
            }
           

            yield return null;
        }
    }

    public List<GameInfoPacket> GetCurrentGames(IPeer peer)
    {
        var list = new List<GameInfoPacket>();

        var filters = new Dictionary<string, string>();

        foreach (var provider in GameProviders)
        {
            list.AddRange(provider.GetPublicGames(peer, filters));
        }
        return list;
    }


    private void CreateNewGame()
    {
        int levelID = Random.Range(1,maxLevels+1);

        var options = new Dictionary<string, string>();

        db.GetLevel(levelID, LevelData =>
        {
            if (LevelData == null)
                Logs.Logger.Error("Tried to access a non-existent level");
            else
            {

                options.Add(MsfDictKeys.MaxPlayers, 4.ToString());
                options.Add(MsfDictKeys.RoomName, LevelData.LevelName);
                options.Add(MsfDictKeys.MapName, LevelData.LevelName);
                //Differentiate between the optimize and non-optimize scenarios
                if(levelID % 2 == 1)
                    options.Add(MsfDictKeys.SceneName, "In_Game_Puzzle");
                else
                    options.Add(MsfDictKeys.SceneName, "In_Game_Puzzle_No_Opt");
                options.Add("GameStructurePath", LevelData.FileName);
                options.Add("ChatChannel", "Room" + chatRoomName.ToString());
                chatRoomName++;
            }
        });

        var task = spawner.Spawn(options);

        task.WhenDone((spawnTask) =>
        {
            StartCoroutine(Matchmake());
        });
    }
}
