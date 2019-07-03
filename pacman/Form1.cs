using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace pacman
{
    public partial class Form1 : Form
    {
        public delegate void RefreshGameDelegate(GameStateMsg msg);

        string serverURL = "tcp://192.168.1.3:8086/Server";
        string clientURL = "tcp://192.168.1.3:8085/Client";

        private bool started;
        private Client client;
        private Boolean isDead;
        private int NUM_PLAYERS;
        private string name;
        private string filename;

        // direction player is moving in. Only one will be true
        bool goup;

        bool godown;
        bool goleft;
        bool goright;

        int boardRight = 320;
        int boardBottom = 320;
        int boardLeft = 0;

        int boardTop = 40;

        //player speed
        int speed = 5;

        int score = 0;
        int total_coins = 61;

        //ghost speed for the one direction ghosts
        int ghost1 = 5;

        int ghost2 = 5;

        //x and y directions for the bi-direccional pink ghost
        int ghost3x = 5;

        int ghost3y = 5;
        private bool isFrozen;
        private bool readFile;
        IEnumerable<string> lines;
        IEnumerator<string> enumerator;
        private long lastRound;
        private int i;

        public Form1(Client client, string pID, string cLIENT_URL, int mSEC_PER_ROUND, int nUM_PLAYERS,
            string sERVER_URL)
        {
            init(client, pID, cLIENT_URL, mSEC_PER_ROUND, nUM_PLAYERS, sERVER_URL);
        }

        public Form1(Client client, string pID, string cLIENT_URL, int mSEC_PER_ROUND, int nUM_PLAYERS,
            string sERVER_URL, string fILENAME)
        {
            try
            {
                this.name = pID;
                Console.WriteLine("fILEFILEFILE " + fILENAME + ".csv");
                lines = File.ReadLines(fILENAME + name + ".csv");
                enumerator = lines.GetEnumerator();
                readFile = true;
                init(client, pID, cLIENT_URL, mSEC_PER_ROUND, nUM_PLAYERS, sERVER_URL);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void init(Client client, string pID, string cLIENT_URL, int mSEC_PER_ROUND, int nUM_PLAYERS,
            string sERVER_URL)
        {
            Console.WriteLine("BLA");
            InitializeComponent();
            label2.Visible = false;
            this.client = client;
            name = pID;
            serverURL = sERVER_URL;
            clientURL = cLIENT_URL;
            NUM_PLAYERS = nUM_PLAYERS;
            timer1.Interval = mSEC_PER_ROUND;
            Console.WriteLine(sERVER_URL + " " + cLIENT_URL);

            IDictionary channelValues = new Hashtable();
            Console.WriteLine(clientURL.Split(':')[2].Split('/')[0]);
            channelValues["port"] = clientURL.Split(':')[2].Split('/')[0];
            channelValues["name"] = "client";
            channelValues["secure"] = false;

            TcpChannel channel = new TcpChannel(channelValues, null, null);
            ChannelServices.RegisterChannel(channel, false);

            RemotingServices.Marshal(client, "Client", typeof(Client));
        }

        private void keyisdown(object sender, KeyEventArgs e)
        {
            InputMsg msg;
            msg.Timestamp = lastRound++;
            msg.Move = Moves.Nothing;
            if (e.KeyCode == Keys.Left)
            {
                goleft = true;
                msg.Move = Moves.Left;
                // pacman.Image = Properties.Resources.Left;
            }
            if (e.KeyCode == Keys.Right)
            {
                goright = true;
                msg.Move = Moves.Right;
                // pacman.Image = Properties.Resources.Right;
            }
            if (e.KeyCode == Keys.Up)
            {
                goup = true;
                msg.Move = Moves.Up;
                //pacman.Image = Properties.Resources.Up;
            }
            if (e.KeyCode == Keys.Down)
            {
                godown = true;
                msg.Move = Moves.Down;
                // pacman.Image = Properties.Resources.down;
            }

            if (e.KeyCode == Keys.Enter)
            {
                tbMsg.Enabled = true;
                tbMsg.Focus();
            }

            Console.WriteLine(msg.Move);
            if (!isDead) client.server.PutValue(name, msg);
        }

        private void refreshGame(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                if (!isFrozen)
                {
                    //Console.WriteLine("SIZE " + client.queue.Count);

                    Task.Run(() =>
                    {
                        if (readFile)
                        {
                            if (enumerator.MoveNext())
                            {
                            }
                            else
                            {
                                enumerator = lines.GetEnumerator();
                                enumerator.MoveNext();
                            }
                            InputMsg inmsg;
                            inmsg.Timestamp = lastRound++;
                            Moves move = Moves.Nothing;
                            if (enumerator.Current != null)
                                Enum.TryParse(enumerator.Current.Split(',')[1], true, out move);
                            inmsg.Move = move;
                            client.server.PutValue(name, inmsg);
                        }
                    });

                    GameStateMsg msg;
                    bool dequeu = this.client.queue.TryDequeue(out msg);
                    if (dequeu)
                    {
                        //sConsole.WriteLine("DQ");
                        List<Task> tasks = new List<Task>();

                        foreach (PlayerPosition pos in msg.Position)
                        {
                            tasks.Add(Task.Run(() => { ParseState(pos); }));
                        }
                        Task.WaitAll(tasks.ToArray());
                    }
                }
            });
        }

        private void ParseState(PlayerPosition pos)
        {
            if (pos.Name.StartsWith("C") && !pacmen.ContainsKey(pos.Name))
            {
                this.Invoke(new MethodInvoker(
                    delegate
                    {
                        Pacman pac = new Pacman(pos.Y);
                        pacmen.Add(pos.Name, pac);
                        Controls.Add(pac.pacman);
                        Console.WriteLine("New pacman {0}", pos.Name);
                    }));
            }
            if (pos.Name.Equals("redGhost"))
            {
                redGhost.Location = new Point(pos.X, pos.Y);
            }
            else if (pos.Name.Equals("yellowGhost"))
            {
                yellowGhost.Location = new Point(pos.X, pos.Y);
            }
            else if (pos.Name.Equals("pinkGhost"))
            {
                pinkGhost.Location = new Point(pos.X, pos.Y);
            }
            else if (pos.Name.Contains("picturebox"))
            {
                ((PictureBox) Controls.Find(pos.Name, false)[0]).Location = new Point(pos.X, pos.Y);
            }
            else
            {
                if (pos.Move == Moves.Die && pos.Name.Equals(name))
                {
                    pacmen[pos.Name].pacman.Location = new Point(1000, 1000);
                    isDead = true;
                    label1.Text = "DEAD";
                }
                else
                {
                    pacmen[pos.Name].pacman.Location = new Point(pos.X, pos.Y);
                    if (pos.Name.Equals(name)) label1.Text = "Points: " + pos.points;
                }
            }
        }

        private void keyisup(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left)
            {
                goleft = false;
            }
            if (e.KeyCode == Keys.Right)
            {
                goright = false;
            }
            if (e.KeyCode == Keys.Up)
            {
                goup = false;
            }
            if (e.KeyCode == Keys.Down)
            {
                godown = false;
            }
        }

        /*private void timer1_Tick(object sender, EventArgs e) {
            label1.Text = "Score: " + score;

            //move player
            if (goleft) {
                if (pacman.Left > (boardLeft))
                    pacman.Left -= speed;
            }
            if (goright) {
                if (pacman.Left < (boardRight))
                pacman.Left += speed;
            }
            if (goup) {
                if (pacman.Top > (boardTop))
                    pacman.Top -= speed;
            }
            if (godown) {
                if (pacman.Top < (boardBottom))
                    pacman.Top += speed;
            }
            //move ghosts
            redGhost.Left += ghost1;
            yellowGhost.Left += ghost2;

            // if the red ghost hits the picture box 4 then wereverse the speed
            if (redGhost.Bounds.IntersectsWith(pictureBox1.Bounds))
                ghost1 = -ghost1;
            // if the red ghost hits the picture box 3 we reverse the speed
            else if (redGhost.Bounds.IntersectsWith(pictureBox2.Bounds))
                ghost1 = -ghost1;
            // if the yellow ghost hits the picture box 1 then wereverse the speed
            if (yellowGhost.Bounds.IntersectsWith(pictureBox3.Bounds))
                ghost2 = -ghost2;
            // if the yellow chost hits the picture box 2 then wereverse the speed
            else if (yellowGhost.Bounds.IntersectsWith(pictureBox4.Bounds))
                ghost2 = -ghost2;
            //moving ghosts and bumping with the walls end
            //for loop to check walls, ghosts and points
            foreach (Control x in this.Controls) {
                // checking if the player hits the wall or the ghost, then game is over
                if (x is PictureBox && x.Tag == "wall" || x.Tag == "ghost") {
                    if (((PictureBox)x).Bounds.IntersectsWith(pacman.Bounds)) {
                        pacman.Left = 0;
                        pacman.Top = 25;
                        label2.Text = "GAME OVER";
                        label2.Visible = true;
                        timer1.Stop();
                    }
                }
                if (x is PictureBox && x.Tag == "coin") {
                    if (((PictureBox)x).Bounds.IntersectsWith(pacman.Bounds)) {
                        this.Controls.Remove(x);
                        score++;
                        //TODO check if all coins where "eaten"
                        if (score == total_coins) {
                            //pacman.Left = 0;
                            //pacman.Top = 25;
                            label2.Text = "GAME WON!";
                            label2.Visible = true;
                            timer1.Stop();
                            }
                    }
                }
            }
                pinkGhost.Left += ghost3x;
                pinkGhost.Top += ghost3y;

                if (pinkGhost.Left < boardLeft ||
                    pinkGhost.Left > boardRight ||
                    (pinkGhost.Bounds.IntersectsWith(pictureBox1.Bounds)) ||
                    (pinkGhost.Bounds.IntersectsWith(pictureBox2.Bounds)) ||
                    (pinkGhost.Bounds.IntersectsWith(pictureBox3.Bounds)) ||
                    (pinkGhost.Bounds.IntersectsWith(pictureBox4.Bounds))) {
                    ghost3x = -ghost3x;
                }
                if (pinkGhost.Top < boardTop || pinkGhost.Top + pinkGhost.Height > boardBottom - 2) {
                    ghost3y = -ghost3y;
                }
        }*/

        private void tbMsg_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //tbChat.Text += "\r\n" + tbMsg.Text;
                ChatMsg msg = new ChatMsg(tbMsg.Text,client.getAndIncrementClock());
                client.publishMsg(name, msg);
                client.sendMessage(name, msg);
                tbMsg.Clear();
                tbMsg.Enabled = false;
                e.Handled = true;
                e.SuppressKeyPress = true;
                this.Focus();
            }
        }

        public void NewClient(string name)
        {
            Console.WriteLine("update form");
            this.BeginInvoke(new MethodInvoker(
                delegate { tbChat.Text += $"\r\n {name} is now connected"; }));
            Console.WriteLine("updated form");
        }

        public void NewMSG(string name, ChatMsg msg)
        {
            this.BeginInvoke(new MethodInvoker(
                delegate { tbChat.Text += $"\r\n{name} said: \r\n {msg.msg}"; }));
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            client.server = Register();
        }

        private IServer Register()
        {
            client.server = (IServer) Activator.GetObject(
                typeof(IServer),
                serverURL);

            client.server.RegisterClient(name, clientURL);
            return client.server;
        }

        public void Freeze()
        {
            isFrozen = true;
        }

        public void Unfreeze()
        {
            isFrozen = false;
        }
    }

    public class Client : MarshalByRefObject, IClient
    {
        private Form1 form;
        public IServer server;
        private int NUM_PLAYERS;

        public delegate void NewClientDelegate(string name);

        public delegate void NewMSGDelegate(string name, ChatMsg msgs);

        public event NewClientDelegate NewClient;
        public event NewMSGDelegate NewMSG;

        public ConcurrentQueue<GameStateMsg> queue;
        public CausalOrderQueue causalQueue;
        public List<IClient> ConnectedClients;
        private int nr;

        public Client(string PID, string CLIENT_URL, int MSEC_PER_ROUND, int NUM_PLAYERS, string SERVER_URL)
        {
            nr = calcNr(PID);
            this.NUM_PLAYERS = NUM_PLAYERS;
            Console.WriteLine(nr - 1);
            ConnectedClients = new List<IClient>();
            queue = new ConcurrentQueue<GameStateMsg>();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new Form1(this, PID, CLIENT_URL, MSEC_PER_ROUND, NUM_PLAYERS, SERVER_URL);
            NewClient += form.NewClient;
            NewMSG += form.NewMSG;
            Application.Run(form);
        }

        public Client(string PID, string CLIENT_URL, int MSEC_PER_ROUND, int NUM_PLAYERS, string SERVER_URL,
            string FILENAME)
        {
            nr = calcNr(PID);

            this.NUM_PLAYERS = NUM_PLAYERS;
            Console.WriteLine(nr - 1);
            queue = new ConcurrentQueue<GameStateMsg>();
            ConnectedClients = new List<IClient>();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new Form1(this, PID, CLIENT_URL, MSEC_PER_ROUND, NUM_PLAYERS, SERVER_URL, FILENAME);
            NewMSG += form.NewMSG;
            NewClient += form.NewClient;
            Application.Run(form);
        }

        private int calcNr(string PID)
        {
            Console.WriteLine("AAAAAAAAAA" + Regex.Match(PID, "\\d+$").Value);
            return Int32.Parse(Regex.Match(PID, "\\d+$").Value);
        }

        public void Print(InputMsg msg)
        {
            Console.WriteLine("ABRIR");
        }

        public void publishMsg(string name, ChatMsg msg)
        {
            Console.WriteLine(" PPEEEEERSSS " + string.Join(",", ConnectedClients));
            foreach (var connectedClient in ConnectedClients)
            {
                connectedClient.sendMessage(name, msg);
            }
        }


        public void UpdateState(GameStateMsg state, string serverName)
        {
            //Console.WriteLine("SERVER ------------------" + serverName);
            this.queue.Enqueue(state);
            //Console.WriteLine("UPDATE " + queue.Count);
        }

        public void newServer(string url)
        {
            this.queue = new ConcurrentQueue<GameStateMsg>();
            Console.WriteLine("New Server");
            this.server = (IServer) Activator.GetObject(
                typeof(IServer),
                url);

            Console.WriteLine(server.ConnectedClients());
        }

        public void newClient(string name, string url)
        {
            ConnectedClients.Add((IClient) Activator.GetObject(typeof(IClient), url));
            if(ConnectedClients.Count == NUM_PLAYERS - 1) causalQueue = new CausalOrderQueue(NUM_PLAYERS);
            Console.WriteLine("new Client");
            if (NewClient != null)
            {
                NewClient(name);
            }
        }

        public void sendMessage(string name, ChatMsg msg)
        {
            Console.WriteLine("Received msg");
            causalQueue.Put(msg);
            IList<ChatMsg> msgs;
            if (causalQueue.TryGetAll(out msgs))
            {
                foreach (var chatMsg in msgs)
                {
                    if (NewMSG != null)
                    {
                        NewMSG(name, chatMsg);
                    }
                }
            }
           
        }

        public int[] getAndIncrementClock()
        {
            int[] clock = (int[]) causalQueue.Clock.Clone();
            Console.WriteLine(string.Join(",", clock));
            clock[nr - 1]++;
            return clock;
        }

        public void Freeze()
        {
            form.Freeze();
        }

        public void UnFreeze()
        {
            form.Unfreeze();
        }

        public void InjectDelay(string dst_pid)
        {
            throw new NotImplementedException();
        }

        public void GlobalState()
        {
            Console.WriteLine("Current Connected Server: " + server.getName());
        }

        public void UpdateState(GameStateMsg state)
        {
            throw new NotImplementedException();
        }
    }

    public class CausalOrderQueue
    {
        private IList<ChatMsg> msgList;
        private int[] clock;
        public int[] Clock{ get => clock; set => clock = value; }

        public CausalOrderQueue(int clockSize)
        {
            this.msgList = new List<ChatMsg>();
            this.clock = new int[clockSize];
        }

        public void Put(ChatMsg msg)
        {
            msgList.Add(msg);
        }

        public bool TryGetOne(out ChatMsg msg)
        {
            foreach (var chatMsg in msgList)
            {
                Console.WriteLine("MSGS");
                int[] vector = chatMsg.vector;
                if (AnalyzeVector(vector))
                {
                    msg = chatMsg;
                    return true;
                }
            }
            msg = new ChatMsg("", new int[0]);
            return false;
        }

        public bool TryGetAll(out IList<ChatMsg> msgs)
        {
            Console.WriteLine("BLA");
            IList<ChatMsg> toDeliver = new List<ChatMsg>();
            var msg = new ChatMsg("", new int[0]);
            bool hasMsgs = false;
            while (TryGetOne(out msg))
            {
                toDeliver.Add(msg);
                hasMsgs = true;
            }
            msgs = toDeliver;
            return hasMsgs;
        }

        private bool AnalyzeVector(int[] vector)
        {
            for (int i = 0; i < vector.Length; i++)
            {
                Console.WriteLine("MSG " + string.Join(",", vector));
                Console.WriteLine("CLOCK " + string.Join(",", clock));
                bool next = false;
                if (vector[i] == clock[i] + 1)
                {
                    for (int j = 0; j < vector.Length; j++)
                    {
                        if (j != i)
                        {
                            next = vector[j] == clock[j];
                        }
                    }
                    if (next)
                    {
                        clock = vector;
                        return next;
                    }
                }
            }
            return false;
        }
    }
}