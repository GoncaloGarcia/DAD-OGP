using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using pacman;

namespace pacman
{
    class ProcessCreationServer : MarshalByRefObject, IPCS

    {
        static void Main(string[] args)
        {
            new ProcessCreationServer();
            Console.ReadLine();
        }

        ProcessCreationServer()
        {
            IDictionary channelValues = new Hashtable();
            channelValues["port"] = "11000";
            channelValues["name"] = "pcs";
            channelValues["secure"] = "false";
            TcpChannel channel = new TcpChannel(channelValues, null, null);
            ChannelServices.RegisterChannel(channel, false);
            RemotingServices.Marshal(this, "PCS", typeof(IPCS));
        }

     
        [STAThread]
        public void StartClient(string PID, string CLIENT_URL, string SERVER_URL, int MSEC_PER_ROUND, int NUM_PLAYERS)
        {
            Console.WriteLine("Started Client");
            try
            {
                Console.WriteLine($"{PID} {CLIENT_URL} {MSEC_PER_ROUND} {NUM_PLAYERS} {SERVER_URL}");
                runProcess("\\pacman\\bin\\Debug\\s.exe",
                    $"{PID} {CLIENT_URL} {MSEC_PER_ROUND} {NUM_PLAYERS} {SERVER_URL}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        public void StartClient(string PID, string CLIENT_URL, string SERVER_URL, int MSEC_PER_ROUND, int NUM_PLAYERS,
            string filename)
        {
            Console.WriteLine("Started Client with file");
            try
            {
                Console.WriteLine($"{PID} {CLIENT_URL} {MSEC_PER_ROUND} {NUM_PLAYERS} {SERVER_URL} {filename}");
                runProcess("\\pacman\\bin\\Debug\\s.exe",
                    $"{PID} {CLIENT_URL} {MSEC_PER_ROUND} {NUM_PLAYERS} {SERVER_URL} {filename}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        public void StartServer(string PID, string SERVER_URL, int MSEC_PER_ROUND, int NUM_PLAYERS)
        {
            Console.WriteLine("Started Server");
            runProcess("\\PacServer\\bin\\Debug\\pacman.exe", $"{PID} {SERVER_URL} {MSEC_PER_ROUND} {NUM_PLAYERS}");
            //new RemoteServer(PID, SERVER_URL, MSEC_PER_ROUND, NUM_PLAYERS);
        }

        public void StartServer(string PID, string SERVER_URL, int MSEC_PER_ROUND, int NUM_PLAYERS, string LEADER_URL)
        {
            Console.WriteLine("Started Slave Server");
            runProcess("\\PacServer\\bin\\Debug\\pacman.exe", $"{PID} {SERVER_URL} {MSEC_PER_ROUND} {NUM_PLAYERS} {LEADER_URL}");
        }

        private static void runProcess(string path, string args)
        {
            var projectPath = Path.GetDirectoryName(Path.GetDirectoryName(
                Path.GetDirectoryName(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase))));
            var solPath = Path.Combine(projectPath + path);
            Console.WriteLine(solPath);
            Process proc = new Process();
            proc.StartInfo.FileName = solPath;
            proc.StartInfo.Arguments = args;
            proc.StartInfo.CreateNoWindow = false;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            proc.Start();
            
        }
    }
}