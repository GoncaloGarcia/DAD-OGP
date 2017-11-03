using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pacman
{
    class PacServer
    {

        //TODO: Inputs

        private int NUM_PLAYERS;
        private int MSEC_PER_ROUND;
        private Dictionary<string, List<string>> clientInputs;
        private List<MockClient> clients;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="NUM_PLAYERS"> The number of players to wait for before starting</param>
        public PacServer(int NUM_PLAYERS)
        {
            this.NUM_PLAYERS = NUM_PLAYERS;
            clients = new List<MockClient>();
        }

        /// <summary>
        /// Instanciates a remote client object and adds it to the list of clients
        /// then creates an entry in the Dictionary that stores the inputs
        /// TODO: This should be invokable by the clients themselves
        /// </summary>
        /// <param name="name">The name by which the client should be identified</param>
        /// <param name="url">The url to use in CreateInstance</param>
        public void RegisterClient(string name, string url)
        {
            MockClient client = (MockClient) Activator.CreateInstance(
                typeof(MockClient),
                null,
                url);
           
            clients.Add(client);
            clientInputs.Add(name, new List<string>());
        }


        static void Main(string[] args)
        {

        }
    }
}

