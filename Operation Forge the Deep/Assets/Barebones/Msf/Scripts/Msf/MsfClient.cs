using Barebones.Networking;

namespace Barebones.MasterServer
{
    public class MsfClient : MsfBaseClient
    {
        public MsfRoomsClient Rooms { get; private set; }

        public MsfSpawnersClient Spawners { get; private set; }

        public MsfMatchmakerClient Matchmaker { get; private set; }

        public MsfAuthClient Auth { get; private set; }

        public MsfChatClient Chat { get; private set; }

        public MsfLobbiesClient Lobbies { get; private set; }

        public MsfProfilesClient Profiles { get; private set; }

        public SaveLoadClient SaveLoad { get; private set; }

        public RandMatchmakeClient MatchmakeClient { get; private set; }

        public LeaderboardClient Leader { get; private set; }

        public PrivateTeamClient Team { get; private set; }

        public MsfClient(IClientSocket connection) : base(connection)
        {
            Rooms = new MsfRoomsClient(connection);
            Spawners = new MsfSpawnersClient(connection);
            Matchmaker = new MsfMatchmakerClient(connection);
            Auth = new MsfAuthClient(connection);
            Chat = new MsfChatClient(connection);
            Lobbies = new MsfLobbiesClient(connection);
            Profiles = new MsfProfilesClient(connection);
            SaveLoad = new SaveLoadClient(connection);
            MatchmakeClient = new RandMatchmakeClient(connection);
            Leader = new LeaderboardClient(connection);
            Team = new PrivateTeamClient(connection);
        }
    }
}