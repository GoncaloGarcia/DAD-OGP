using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using pacman;
using Timer = System.Timers.Timer;


namespace pacman
{
    class PacServer
    {
        static void Main(string[] args)
        {
            Console.WriteLine(args[1]);
            if (args.Length < 5)
            {
                Console.WriteLine("LEADER");
                RemoteServer server = new RemoteServer(args[0], args[1], Int32.Parse(args[2]), Int32.Parse(args[3]));
            }
            else
            {
                Console.WriteLine("REPlICA");
                RemoteServer server =
                    new RemoteServer(args[0], args[1], Int32.Parse(args[2]), Int32.Parse(args[3]), args[4]);
            }
        }
    }


    public class RemoteServer : MarshalByRefObject, IServer
    {
        private int NUM_PLAYERS;
        private int MSEC_PER_ROUND;
        private IDictionary<string, PlayerPosition> _gameState;
        private ConcurrentDictionary<string, InputMsg> _clientInputs;
        private List<string> _clientDelays;
        private List<ClientInfo> _clients;
        private string PID;
        private string SERVER_URL;
        private string LEADER_URL;
        private IServer leader;
        private bool _isListening;
        private long _roundTimestamp;
        private int SPEED = 5;
        private int _redGhostSpeed = 5;
        private int _yellowGhostSpeed = 5;
        private int _pinkGhostXSpeed = 5;
        private int _pinkGhostYSpeed = 5;
        private List<PlayerPosition> _stars;
        private IDictionary<string, IServer> peerServers;
        private IDictionary<string, string> peerURLs;
        private List<VoteMsg> peervVoteMsgs;
        private bool isFrozen;
        private volatile bool isLeader;
        private Timer LeaderAliveTimer;
        private Timer PeersAliveTimer;
        private volatile bool hasVoted;
        private int started = 0;
        private int stopped = 0;
        private static EventWaitHandle run;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="NUM_PLAYERS"> The number of players to wait for before starting</param>
        /// <param name="MSEC_PER_ROUND"> The number of milliseconds to wait for input</param>
        public RemoteServer(string PID, string SERVER_URL, int MSEC_PER_ROUND, int NUM_PLAYERS) : base()
        {
            IDictionary channelValues = new Hashtable();
            channelValues["port"] = SERVER_URL.Split(':')[2].Split('/')[0];
            channelValues["name"] = "server";
            channelValues["secure"] = "false";
            TcpChannel channel = new TcpChannel(channelValues, null, null);
            ChannelServices.RegisterChannel(channel, false);
            RemotingServices.Marshal(this, "Server", typeof(RemoteServer));
            isLeader = true;
            init(PID, SERVER_URL, MSEC_PER_ROUND, NUM_PLAYERS);
            Run();
        }

        public RemoteServer(string PID, string SERVER_URL, int MSEC_PER_ROUND, int NUM_PLAYERS, string LEADER_URL)
        {
            this.LEADER_URL = LEADER_URL;
            IDictionary channelValues = new Hashtable();
            channelValues["port"] = SERVER_URL.Split(':')[2].Split('/')[0];
            channelValues["name"] = "server";
            channelValues["secure"] = "false";
            TcpChannel channel = new TcpChannel(channelValues, null, null);
            ChannelServices.RegisterChannel(channel, false);
            RemotingServices.Marshal(this, "Server", typeof(RemoteServer));

            IServer server = (IServer) Activator.GetObject(
                typeof(IServer),
                LEADER_URL);
            leader = server;
            init(PID, SERVER_URL, MSEC_PER_ROUND, NUM_PLAYERS);
            server.connectReplica(SERVER_URL, PID);
            while (true) ;
        }

        private void init(string PID, string SERVER_URL, int MSEC_PER_ROUND, int NUM_PLAYERS)
        {
            peerServers = new Dictionary<string, IServer>();
            peerURLs = new Dictionary<string, string>();
            peervVoteMsgs = new List<VoteMsg>();
            this.PID = PID;
            this.SERVER_URL = SERVER_URL;
            _clientInputs = new ConcurrentDictionary<string, InputMsg>();
            _clients = new List<ClientInfo>();
            _gameState = new Dictionary<string, PlayerPosition>();
            this.MSEC_PER_ROUND = MSEC_PER_ROUND;
            this.NUM_PLAYERS = NUM_PLAYERS;
            _stars = new List<PlayerPosition>();
            _clientDelays = new List<string>();

            int i = 5;
            for (int x = 8; x <= 328; x += 40)
            {
                for (int y = 40; y <= 320; y += 40)
                {
                    if (y <= 145 && (x == 88 || x == 248)) ;
                    else if (y > 220 && (x == 128 || x == 288)) ;
                    else
                    {
                        _stars.Add(new PlayerPosition("picturebox" + i, x, y, Moves.Nothing));
                        i++;
                    }
                }
            }
            foreach (var star in _stars)
            {
                _gameState.Add(star.Name, star);
            }

            _gameState.Add("yellowGhost", new PlayerPosition("yellowGhost", 221, 273, Moves.Nothing));
            _gameState.Add("redGhost", new PlayerPosition("redGhost", 180, 73, Moves.Nothing));
            _gameState.Add("pinkGhost", new PlayerPosition("pinkGhost", 301, 72, Moves.Nothing));

            LeaderAliveTimer = new Timer(MSEC_PER_ROUND * 5);
            LeaderAliveTimer.AutoReset = false;
            LeaderAliveTimer.Elapsed += (sender, e) => vote();

            PeersAliveTimer = new Timer(MSEC_PER_ROUND * 3);
            PeersAliveTimer.AutoReset = false;
            PeersAliveTimer.Elapsed += (sender, e) => ElectLeader();

            run = new ManualResetEvent(initialState: true);
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
            Console.WriteLine("RUNNING");
            //_roundTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            _roundTimestamp = 1;
            _roundTimestamp = 1;
            while (_clients.Count != NUM_PLAYERS) ;
            while (true)
            {
                AwaitInput();
                if (!isFrozen && isLeader)
                {
                    //Console.WriteLine("THREAD ID ----- " + Thread.CurrentThread.ManagedThreadId);
                    Console.WriteLine(isLeader);
                    UpdateState();

                        List<string> toDelete = new List<string>();
                        foreach (var server in peerServers)
                        {
                            try
                            {
                                server.Value.replicateState(new GameStateMsg(_gameState.Values.ToList(), _roundTimestamp));
                            }
                            catch (Exception e)
                            {
                               
                                toDelete.Add(server.Key);
                                Console.WriteLine("Replica has disconnected");
                            }
                        }
                    foreach (var deletable in toDelete)
                    {
                        peerServers.Remove(deletable);
                    }
                        PropagateState();

                    
                    


                    //_roundTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    _roundTimestamp++;
                }
                else
                {
                    run.Reset();
                    run.WaitOne();
                }
                
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
            List<Task> tasks = new List<Task>();
            foreach (ClientInfo clientInfo in _clients)
            {
                var task = Task.Run(() =>
                {
                    if (_clientDelays.Contains(clientInfo.Name))
                    {
                        Console.WriteLine("Sleep");
                        Thread.Sleep(MSEC_PER_ROUND * 10);
                    }
                    //Console.WriteLine("Sending state to " + clientInfo.Name);
                    clientInfo.Instance.UpdateState(new GameStateMsg(_gameState.Values.ToList(), _roundTimestamp), PID);
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
        }

        public void replicateState(GameStateMsg msg)
        {
            if (LeaderAliveTimer != null)
            {
                //Console.WriteLine("STOPPED TIMER");
                LeaderAliveTimer.Stop();
            }

            _roundTimestamp = msg.roundTimestamp;
            foreach (PlayerPosition pos in msg.Position)
            {
                _gameState[pos.Name] = pos;
            }

            //Console.WriteLine($"STARTED TIMER " + ++started);
           
            LeaderAliveTimer.Start();
        }

        /// <summary>Fs
        /// Calculates the positon of the players based on the inputs received in the 
        /// current round.
        /// </summary>
        private void UpdateState()
        {
            UpdateGhosts();
            foreach (string client in _clientInputs.Keys)
            {
                CalculateUpdatedPosition(_clientInputs[client], client);
                //Console.WriteLine(_gameState.Values.First().Y);


                //_clientInputs[client].Clear();
            }
        }

        

        public void connectReplica(string replicaURL, string name)
        {
            if (!name.Equals(PID))
            {
                IServer server = (IServer) Activator.GetObject(
                    typeof(IServer),
                    replicaURL);
                if (isLeader)
                {
                    foreach (string peer in peerServers.Keys)
                    {
                        peerServers[peer].addPeer(replicaURL, name);
                        Console.WriteLine(peer);
                        server.addPeer(peerURLs[peer], peer);
                        Console.WriteLine("Added peer");
                    }
                }
                Console.WriteLine("Added server " + peerServers.Count + " " + name);
                peerURLs.Add(name, replicaURL);
                peerServers.Add(name, server);
            }
        }

        public void addPeer(string url, string name)
        {
            if (!name.Equals(PID) && !name.Equals(leader.getName()))
            {
                IServer server = (IServer) Activator.GetObject(
                    typeof(IServer),
                    url);
                Console.WriteLine("ADDING PEER ---- " + name);
                peerServers[name] = server;
                peerURLs[name] = url;
            }
        }

        public string getName()
        {
            return PID;
        }

        public void demote(NewServerMsg msg)
        {
            if (isLeader)
            {
                Console.WriteLine("NO LONGER LEADER");
                isLeader = false;
            }
            leader = (IServer) Activator.GetObject(
                typeof(IServer),
                msg.leaderURL);
            LEADER_URL = msg.leaderURL;
            peerServers.Clear();
            peerURLs.Clear();
            peervVoteMsgs.Clear();
            _gameState.Clear();
            _clientInputs.Clear();
            foreach (ServerInfo info in msg.peers)
            {
                if (!info.name.Equals(PID))
                {
                    var server = (IServer) Activator.GetObject(
                        typeof(IServer),
                        info.url);
                    peerServers.Add(info.name, server);
                    peerURLs.Add(info.name, info.url);
                }
            }
            Console.WriteLine("Peers " + string.Join("; ", peerServers.Keys));
            Console.WriteLine("Leader " + leader.getName());
        }

        public void addPlayer(string url, string name)
        {
            Console.WriteLine(url);
            var client = (IClient) Activator.GetObject(
                typeof(IClient),
                url);
            ClientInfo info_new = new ClientInfo(name, url, client);
            _clients.Add(info_new);

            Console.WriteLine("Added new player");
        }

        public void sendVote(VoteMsg vote)
        {
            if (!hasVoted) this.vote();
            
            Console.WriteLine($"Received vote from {vote.name}");
            peervVoteMsgs.Add(vote);
            Console.WriteLine(peervVoteMsgs.Count);
            Console.WriteLine(peerServers.Count + 1);
            //Console.WriteLine("CURRENT PEERS IN VOTE " + string.Join(";", peerServers));
            Console.WriteLine("LALALA");
            if (peervVoteMsgs.Count == peerServers.Count + 1)
            {
                PeersAliveTimer.Stop();
                ElectLeader();
            }
            Console.WriteLine("CENAS");
        }

        private void ElectLeader()
        {
            Console.WriteLine("Electing");
            string leaderName = null;
            long leaderTimestamp = -1;
            long leaderChallenge = -1;
            foreach (VoteMsg voteMsg in peervVoteMsgs)
            {
                Console.WriteLine($"{voteMsg.name} {voteMsg.challenge} {voteMsg.roundTimestamp}");
                if (leaderName == null)
                {
                    leaderName = voteMsg.name;
                    leaderTimestamp = voteMsg.roundTimestamp;
                    leaderChallenge = voteMsg.challenge;
                }
                else if (leaderTimestamp < voteMsg.roundTimestamp)
                {
                    leaderName = voteMsg.name;
                    leaderTimestamp = voteMsg.roundTimestamp;
                    leaderChallenge = voteMsg.challenge;
                }
                else if (leaderTimestamp == voteMsg.roundTimestamp && leaderChallenge < voteMsg.challenge)
                {
                    leaderName = voteMsg.name;
                    leaderTimestamp = voteMsg.roundTimestamp;
                    leaderChallenge = voteMsg.challenge;
                }
                else if (leaderTimestamp == voteMsg.roundTimestamp && leaderChallenge == voteMsg.challenge)
                {
                    if (leaderName.CompareTo(voteMsg.name) < 0)
                    {
                        leaderName = voteMsg.name;
                        leaderTimestamp = voteMsg.roundTimestamp;
                        leaderChallenge = voteMsg.challenge;
                    }
                }
            }
            peervVoteMsgs.Clear();
            Console.WriteLine($"The new leader is {leaderName}");
            if (leaderName.Equals(PID))
            {
                isLeader = true;
                Console.WriteLine("try leader");
                Console.WriteLine("talk to peers");

                List<ServerInfo> peerInfos = new List<ServerInfo>();
                foreach (var server in peerServers)
                {
                    peerInfos.Add(new ServerInfo(server.Key, peerURLs[server.Key]));
                }
                var msg = new NewServerMsg(PID, SERVER_URL, peerInfos);

                foreach (string peer in peerServers.Keys)
                {
                    peerServers[peer].demote(msg);
                }
                Console.WriteLine("BEFORE ADDING LEADER TO PEERS " + string.Join(";", peerServers));
                try
                {
                    leader.demote(msg);
                    peerServers.Add(leader.getName(), leader);
                    Console.WriteLine("AFTER ADDING LEADER TO PEERS " + string.Join(";", peerServers));
                    peerURLs.Add(leader.getName(), LEADER_URL);
                }
                catch (Exception e)
                {
                    Console.WriteLine("HE DED");
                }

                leader = null;
                Console.WriteLine("talk to clients");
                foreach (ClientInfo client in _clients)
                {
                    client.Instance.newServer(SERVER_URL);
                }
                Console.WriteLine("CAN RUN");
                this.Run();
                //run.Set();
            }
            else
            {
                leader = peerServers[leaderName];
                peerServers.Remove(leaderName);
            }
        }


        private void vote()
        {
            if (!isFrozen)
            {
                hasVoted = true;
                Console.WriteLine($"Leader is dead, USURP");
                ThreadLocal<Random> rand = new ThreadLocal<Random>(() =>
                    new Random(Environment.TickCount * Thread.CurrentThread.ManagedThreadId));
                VoteMsg msg = new VoteMsg(PID, _roundTimestamp, rand.Value.Next(0, 999999999));
                Console.WriteLine(string.Join((" - "), peerServers.Keys));
                List<string> toDelete = new List<string>();
                foreach (var server in peerServers)
                {
                    Console.WriteLine("Sending vote");
                    try
                    {
                        server.Value.sendVote(msg);
                        Console.WriteLine("sent vote");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("UPS");
                        toDelete.Add(server.Key);
                        continue;
                    }
                    foreach (var deletable in toDelete)
                    {
                        peerServers.Remove(deletable);
                    }
                    Console.WriteLine(server.Value.ConnectedClients());
                }
                Console.WriteLine("BLA");
                sendVote(msg);
                Console.WriteLine("BLABLA");
                PeersAliveTimer.Start();
            }
        }

        private void UpdateGhosts()
        {
            var redState = _gameState["redGhost"];
            redState.X += _redGhostSpeed;
            if (redState.X <= 88 + 15 || redState.X + 30 >= 248)
            {
                _redGhostSpeed *= -1;
            }
            _gameState["redGhost"] = redState;

            var yellowState = _gameState["yellowGhost"];
            yellowState.X += _yellowGhostSpeed;
            if (yellowState.X <= 128 + 15 || yellowState.X + 30 >= 288)
            {
                _yellowGhostSpeed *= -1;
            }
            _gameState["yellowGhost"] = yellowState;

            var pinkState = _gameState["pinkGhost"];
            pinkState.X += _pinkGhostXSpeed;
            pinkState.Y += _pinkGhostYSpeed;
            if (Intersects(pinkState.X, pinkState.Y, 25, 25, 88, 40, 15, 95)
                || Intersects(pinkState.X, pinkState.Y, 25, 25, 248, 40, 15, 95)
                || Intersects(pinkState.X, pinkState.Y, 25, 25, 128, 240, 15, 95)
                || Intersects(pinkState.X, pinkState.Y, 25, 25, 288, 240, 15, 95)
                || pinkState.X < 0
                || pinkState.X > 320)
            {
                _pinkGhostXSpeed *= -1;
            }
            if (pinkState.Y < 0 || pinkState.Y + 30 > 320 - 2)
            {
                _pinkGhostYSpeed *= -1;
            }
            _gameState["pinkGhost"] = pinkState;
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
                    state.X -= SPEED;
                    _gameState[client] = state;
                    break;
                }
                case Moves.Right:
                {
                    var state = _gameState[client];
                    state.X += SPEED;
                    _gameState[client] = state;
                    break;
                }
                case Moves.Down:
                {
                    var state = _gameState[client];
                    state.Y += SPEED;
                    _gameState[client] = state;
                    break;
                }
                case Moves.Up:
                {
                    var state = _gameState[client];
                    state.Y -= SPEED;
                    _gameState[client] = state;
                    break;
                }
            }
            clientState = CheckPlayerPoint(client);
            clientState = CheckPlayerDead(client);

            

            msg.Move = Moves.Nothing;
            _clientInputs[client] = msg;
        }

        private PlayerPosition CheckPlayerDead(string client)
        {
            var clientState = _gameState[client];
            var redGhost = _gameState["redGhost"];
            var yellowGhost = _gameState["yellowGhost"];
            if (Intersects(clientState.X, clientState.Y, 25, 25, redGhost.X, redGhost.Y, 30, 30)
                || Intersects(clientState.X, clientState.Y, 25, 25, yellowGhost.X, yellowGhost.Y, 30, 30)
                || Intersects(clientState.X, clientState.Y, 25, 25, 88, 40, 15, 95)
                || Intersects(clientState.X, clientState.Y, 25, 25, 248, 40, 15, 95)
                || Intersects(clientState.X, clientState.Y, 25, 25, 128, 240, 15, 95)
                || Intersects(clientState.X, clientState.Y, 25, 25, 288, 240, 15, 95)
                || clientState.X + 5 < 0
                || clientState.X + 5 > 320
                || clientState.Y + 5 < 0
                || clientState.Y + 5 > 320)
            {
                Console.WriteLine("intersects");
                clientState.Move = Moves.Die;
                _gameState[client] = clientState;
            }
            return clientState;
        }

        private PlayerPosition CheckPlayerPoint(string client)
        {
            var clientState = _gameState[client];
            var redGhost = _gameState["redGhost"];
            var yellowGhost = _gameState["yellowGhost"];

            for (int i = 0; i < _stars.Count; i++)
            {
                var star = _stars.ElementAt(i);
                if (Intersects(clientState.X, clientState.Y, 25, 25, star.X, star.Y, 15, 15))
                {
                    Console.WriteLine("star");
                    star.X = 1000;
                    _gameState[star.Name] = star;
                    _stars.RemoveAt(i);
                    clientState.points++;
                    _gameState[client] = clientState;
                }
            }

            return clientState;
        }

        public void PutValue(string name, InputMsg msg)
        {
            Console.WriteLine("BLA");
            if (_isListening && msg.Timestamp > _roundTimestamp)
            {
                //  Console.WriteLine("Listening " + msg.Move);
                _clientInputs[name] = msg;
                Console.WriteLine(msg.Move);
                Console.WriteLine(_gameState[name].X + " " + _gameState[name].Y);
            }
            // Console.WriteLine("Not Listening");
        }

        private bool Intersects(int x1, int y1, int width1, int height1, int x2, int y2, int width2, int height2)
        {
            return !((x1 > (x2 + width2) || (x1 + width1) < x2) || (y1 > (y2 + height2) || (y1 + height1) < y2));
        }


        public void RegisterClient(string name, string url)
        {
            Console.WriteLine(name + " has connected at " + url);

           
            IClient client = (IClient) Activator.GetObject(
                typeof(IClient),
                url);
            
            foreach (IServer peerServer in peerServers.Values)
            {
                peerServer.addPlayer(url, name);
            }
            foreach (var clientInfo in _clients)
            {
                clientInfo.Instance.newClient(name, url);
                client.newClient(clientInfo.Name, clientInfo.Url);
            }
            var info = new ClientInfo(name, url, client);
            _clients.Add(info);
            _clientInputs.TryAdd(name, new InputMsg());
            PlayerPosition pos = new PlayerPosition(name, 8, _clients.Count * 40, Moves.Nothing);
            _gameState.Add(name, pos);
            
        }

        public IList<ClientInfo> ConnectedClients()
        {
            return _clients;
        }

        public void Freeze()
        {
            isFrozen = true;
        }

        public void UnFreeze()
        {
            isFrozen = false;
        }

        public void InjectDelay(string dst_pid)
        {
            _clientDelays.Add(dst_pid);
            Console.WriteLine(_clientDelays.Count);
        }

        public void GlobalState()
        {
            Console.WriteLine("--- Connected Peers ---" + string.Join("@",peerServers));
            Console.WriteLine("Current Round: " + _roundTimestamp);
            Console.WriteLine("Current Leader: " + (leader != null ? leader.getName() : PID));
        }

        public void UpdateState(GameStateMsg state)
        {
            throw new NotImplementedException();
        }
    }
}