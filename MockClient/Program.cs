using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace pacman
{
    public class MockClient
    {

        public MockClient()
        {
            
        }

        static void Main(string[] args)
        {
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterActivatedClientType(
                typeof(MockClient),
                "tcp://localhost:1234/Client");
        }
    }
}
