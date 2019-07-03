using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace pacman
{
    /// <summary>
    /// Possible moves performed by the client
    /// </summary>
    public enum Moves
    {
        Nothing,
        Left,
        Right,
        Up,
        Down,
        Die
    }

    /// <summary>
    /// Message format to be sent from the client to the server which 
    /// contains the information about the user's input
    /// </summary>
    [Serializable]
    public struct InputMsg
    {
        public Moves Move;
        public long Timestamp;
    }


    /// <summary>
    /// Represents the game's state which is the position of each player 
    /// on the map.
    /// </summary>
    [Serializable]
    public struct GameStateMsg
    {
        public readonly IList<PlayerPosition> Position;
        public readonly long roundTimestamp;

        public GameStateMsg(IList<PlayerPosition> position, long timestamp)
        {
            Position = position;
            roundTimestamp = timestamp;
        }
    }

    /// <summary>
    /// Represents each player's position on the map as well as the last move
    /// </summary>
    [Serializable]
    public struct PlayerPosition
    {
        public int X, Y;
        public string Name;
        public Moves Move;
        public int points;

        public PlayerPosition(string name, int x, int y, Moves move)
        {
            Name = name;
            X = x;
            Y = y;
            Move = move;
            points = 0;
        }
    }


    /// <summary>
    /// Encapsulates information about the client
    /// The instance field is present so this struct can also be used on the server,
    /// however it will not be serialized/sent to the client.
    /// </summary>
    [Serializable]
    public struct ClientInfo
    {
        public string Name;
        public string Url;
        [XmlIgnore] public IClient Instance;

        public ClientInfo(string Name, string Url, IClient instance)
        {
            this.Name = Name;
            this.Url = Url;
            this.Instance = instance;
        }
    }

    [Serializable]
    public struct ServerInfo
    {
        public string name;
        public string url;

        public ServerInfo(string name, string url)
        {
            this.name = name;
            this.url = url;
        }
    }

    [Serializable]
    public struct NewServerMsg
    {
        public string leaderName;
        public string leaderURL;
        public IList<ServerInfo> peers;

        public NewServerMsg(string leaderName, string leaderUrl, IList<ServerInfo> peers)
        {
            this.leaderName = leaderName;
            this.leaderURL = leaderUrl;
            this.peers = peers;
        }
    }


    [Serializable]
    public struct VoteMsg
    {
        public string name;
        public long roundTimestamp;
        public long challenge;

        public VoteMsg(string name, long roundTimestamp, long challenge)
        {
            this.name = name;
            this.roundTimestamp = roundTimestamp;
            this.challenge = challenge;
        }
    }

    [Serializable]
    public struct ChatMsg
    {
        public string msg;
        public int[] vector;

        public ChatMsg(string msg, int[] vector)
        {
            this.msg = msg;
            this.vector = vector;
        }
    }

    public interface IConnectable
    {
        void Freeze();
        void UnFreeze();
        void InjectDelay(string dst_pid);
        void GlobalState();
    }

    /// <summary>
    /// Enforces the public API of the Client
    /// TODO: Update to support new features
    /// </summary>
    public interface IClient : IConnectable
    {
        /// <summary>
        /// Prints the msg passed as argument
        /// Only for testing purposes
        /// </summary>
        /// <param name="msg"></param>
        void Print(InputMsg msg);

        /// <summary>
        /// Updates the state of the game based on the calculations performed by the server.
        /// </summary>
        /// <param name="state"></param>
        void UpdateState(GameStateMsg state, string serverName);

        void newServer(string url);

        void newClient(string name, string url);

        void sendMessage(string name, ChatMsg msg);
    }

    //Enforces the public API of the Server
    public interface IServer : IConnectable
    {
        /// <summary>
        /// Instanciates a remote client object and adds it to the list of clients
        /// then creates an entry in the Dictionary that stores the inputs
        /// </summary>
        /// <param name="name">The name by which the client should be identified</param>
        /// <param name="url">The url to use in CreateInstance</param>
        void RegisterClient(string name, string url);

        /// <summary>
        /// Stores a move in the client's queue
        /// </summary>
        /// <param name="name"> The name of the client who sent the move </param>
        /// <param name="msg"> The msg containing information about the move</param>
        void PutValue(string name, InputMsg msg);

        /// <summary>
        /// Provides information about the clients currently connected to the server
        /// </summary>
        /// <returns> An instance of ClientInfo with client information </returns>
        IList<ClientInfo> ConnectedClients();

        void replicateState(GameStateMsg msg);
        void connectReplica(string replicaURL, string name);
        void addPlayer(string url, string name);
        void sendVote(VoteMsg vote);
        void addPeer(string url, string name);
        string getName();
        void demote(NewServerMsg msg);
    }

    public interface IPCS
    {
        void StartClient(string PID, string CLIENT_URL, string SERVER_URL, int MSEC_PER_ROUND, int NUM_PLAYERS);

        void StartClient(string PID, string CLIENT_URL, string SERVER_URL, int MSEC_PER_ROUND, int NUM_PLAYERS,
            string filename);

        void StartServer(string PID, string SERVER_URL, int MSEC_PER_ROUND, int NUM_PLAYERS);

        void StartServer(string PID, string SERVER_URL, int MSEC_PER_ROUND, int NUM_PLAYERS, string LEADER_URL);
    }
}