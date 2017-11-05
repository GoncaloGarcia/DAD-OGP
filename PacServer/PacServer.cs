using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonLibs;

namespace pacman
{
    class PacServer
    {
        static void Main(string[] args)
        {
            RemoteServer server = new RemoteServer(500, 1);
            
            IDictionary channelValues = new Hashtable();
            channelValues["port"] = "8086";
            channelValues["name"] = "server";
            TcpChannel channel = new TcpChannel(channelValues, null, null);
            ChannelServices.RegisterChannel(channel, true);
            RemotingServices.Marshal(server, "Server", typeof(RemoteServer));

            server.Run();

            System.Console.WriteLine("<enter> to exit...");
            System.Console.ReadLine();
        }
    }

    public class RemoteServer : MarshalByRefObject, IServer
    {
        private IClient _client;
        private readonly int NUM_PLAYERS;
        private readonly int MSEC_PER_ROUND;
        private readonly IDictionary<string, PlayerPosition> _gameState;
        private readonly ConcurrentDictionary<string, List<InputMsg>> _clientInputs;
        private readonly List<ClientInfo> _clients;
        private bool _isListening;
        private long _roundTimestamp;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="NUM_PLAYERS"> The number of players to wait for before starting</param>
        /// <param name="MSEC_PER_ROUND"> The number of milliseconds to wait for input</param>
        public RemoteServer(int MSEC_PER_ROUND, int NUM_PLAYERS) : base()
        {
            _clientInputs = new ConcurrentDictionary<string, List<InputMsg>>();
            _clients = new List<ClientInfo>();
            _gameState = new Dictionary<string, PlayerPosition>();
            this.MSEC_PER_ROUND = MSEC_PER_ROUND;
            this.NUM_PLAYERS = NUM_PLAYERS;
            
        }

        /// <summary>
        /// Runs the server loop: 
        ///  - Wait for input
        ///  - Calculate state
        ///  - Return state
        ///  - Update round;
        /// </summary>
        public void Run()
        {
            _roundTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            while (_clients.Count != NUM_PLAYERS);
            while (true)
            {
                AwaitInput();
                UpdateState();
                PropagateState();
                _roundTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
        }

        /// <summary>
        /// Waits for the specified time for user input
        /// </summary>
        private void AwaitInput()
        {
           
            _isListening = true;
            Thread.Sleep(MSEC_PER_ROUND);
            _isListening = false;
        }

        /// <summary>
        /// Sends each client the updated game state
        /// </summary>
        private void PropagateState()
        {
            foreach (ClientInfo clientInfo in _clients)
            { 
                clientInfo.Instance.UpdateState(new GameStateMsg(_gameState.Values.ToList(), _roundTimestamp));
            }
        }

        /// <summary>
        /// Calculates the positon of the players based on the inputs received in the 
        /// current round.
        /// </summary>
        private void UpdateState()
        {
            foreach (string client in _clientInputs.Keys)
            {
                foreach (InputMsg msg in _clientInputs[client])
                {
                    CalculateUpdatedPosition(msg, client);
                }
                _clientInputs[client].Clear();
            }
        }

        /// <summary>
        /// Calculates the new position for a given player based on an input message
        /// </summary>
        /// <param name="msg"> The message containing the player's input</param>
        /// <param name="client">The name of the client which sent the input</param>
        /// TODO: Verify how the updating works best
        private void CalculateUpdatedPosition(InputMsg msg, string client)
        {
            var clientState = _gameState[client];
            clientState.Move = msg.Move;
            switch (msg.Move)
            {
                case Moves.Left:
                {
                    var state = _gameState[client];
                    state.X--;
                    state.Move = msg.Move;
                    break;
                }
                case Moves.Right:
                {
                    var state = _gameState[client];
                    state.X++;
                    break;
                }
                case Moves.Down:
                {
                    var state = _gameState[client];
                    state.Y--;
                    break;
                }
                case Moves.Up:
                {
                    var state = _gameState[client];
                    state.Y++;
                    break;
                }
            }
        }

        public void PutValue(string name, InputMsg msg)
        {
            if (_isListening && msg.Timestamp > _roundTimestamp)
            {
                _clientInputs[name].Add(msg);
            }

        }

        public void RegisterClient(string name, string url)
        {
            Console.WriteLine(name + " has connected at " + url);
            IClient client = (IClient) Activator.GetObject(
                typeof(IClient),
                url);

            _clients.Add(new ClientInfo(name, url, client));
            _clientInputs.TryAdd(name, new List<InputMsg>());
            _gameState.Add(name, new PlayerPosition(name, 8, _clients.Count * 40, Moves.Nothing));
            this._client = client;

        }
               
        public IList<ClientInfo> ConnectedClients()
        {
            return _clients;
        }
    }
}

