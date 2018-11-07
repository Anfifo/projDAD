using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;


namespace Server
{
    class Server
    {
        static void Main(string[] args)
        {
            string Url = "tcp://localhost:8086/S";
            int MinDelay = 0;
            int MaxDelay = 0;
            int Port;
            string Name;

            if (args.Length > 0)
            {
                Url = args[0];

            }

            if (args.Length == 3)
            {
                MinDelay = Int32.Parse(args[1]);
                MaxDelay = Int32.Parse(args[2]);
            }

            Port = getPortFromURL(Url);
            Name = getNameFromURL(Url);



            TcpChannel channel = new TcpChannel(Port);
            ChannelServices.RegisterChannel(channel, true);

            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(TSpaceServer),
                Name,
                WellKnownObjectMode.Singleton);

            System.Console.WriteLine("<enter> para sair...");
            System.Console.ReadLine();
        }

        static int getPortFromURL(string url)
        {
            string[] splitUrl = url.Split(':', '/');

            return Int32.Parse(splitUrl[4]);
        }

        static string getNameFromURL(string url)
        {
            string[] splitUrl = url.Split(':', '/');

            return splitUrl[5];
        }
    }
}