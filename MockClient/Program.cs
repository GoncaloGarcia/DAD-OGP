using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace pacman
{
    public class MockClient : MarshalByRefObject,  IClient
    {
        private string CLIENT_NAME = "abc333";
      
        static void Main(string[] args)
        {
           MockClient client = new MockClient();


            IDictionary channelValues = new Hashtable();
            channelValues["port"] = "8085";
            channelValues["name"] = "client";

            TcpChannel channel = new TcpChannel(channelValues, null, null);
            ChannelServices.RegisterChannel(channel, true); 
           
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(MockClient),
                "Client",
                WellKnownObjectMode.Singleton);

            IServer server = client.Register();

            while (true)
            {
                Thread.Sleep(500);
                client.SendRandomData(server);
            }
        }

        private IServer Register()
        {
            IServer server = (IServer) Activator.GetObject(
                typeof(IServer),
                "tcp://localhost:8086/Server");

            server.RegisterClient(CLIENT_NAME, "tcp://localhost:8085/Client");
            return server;
        }

        private void SendRandomData(IServer server)
        {
            InputMsg msg;
            msg.Move = Moves.Down;
            msg.Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            server.PutValue(CLIENT_NAME, msg);
        }

        public void Print(InputMsg msg)
        {
            Console.WriteLine(msg.Move);
        }

        public void UpdateState(GameStateMsg state)
        {
           Console.WriteLine(state.roundTimestamp);
        }

        public void Freeze()
        {
            throw new NotImplementedException();
        }

        public void UnFreeze()
        {
            throw new NotImplementedException();
        }

        public void InjectDelay(string dst_pid)
        {
            throw new NotImplementedException();
        }
    }

   
}
