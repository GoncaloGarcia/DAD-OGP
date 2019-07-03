using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace pacman {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            //Client client = new Client();
            Array.ForEach(args, x => Console.WriteLine(x));
            if (args.Length == 5)
            {
                Client c = new Client(args[0], args[1], Int32.Parse(args[2]), Int32.Parse(args[3]), args[4]);
            }
            else if(args.Length == 6)
            {
                Client c = new Client(args[0], args[1], Int32.Parse(args[2]), Int32.Parse(args[3]), args[4], args[5]);
            }




        }
    }
}
