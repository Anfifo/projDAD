using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using CommonTypes;


namespace Server
{
    class Server
    {
        public static TcpChannel channel;
        
        static void Main(string[] args)
        {
            string Url = "tcp://localhost:50001/S";
            int MinDelay = 0;
            int MaxDelay = 0;
            int Port;
            string Name;
            List<String> Servers = new List<string>();
            Servers.Add("tcp://localhost:50001/S");
            Servers.Add("tcp://localhost:50002/S");

            //Type of algorithm for server
            string algorithm = "x";

            if (args.Length > 0)
            {
                Url = args[0];
                algorithm = args[1];
                // Todo -> receive servers list as args

            }  
            
            if (args.Length == 4)
            {

                MinDelay = Int32.Parse(args[1]);
                MaxDelay = Int32.Parse(args[2]);
                algorithm = args[3];
            }

            Port = getPortFromURL(Url);
            Name = getNameFromURL(Url);

            Servers.Remove(Url);


            channel = new TcpChannel(Port);
            ChannelServices.RegisterChannel(channel, true);

            if (algorithm == "x") {
                //RemotingConfiguration.RegisterWellKnownServiceType(typeof(TSpaceServerXL), Name, WellKnownObjectMode.Singleton);
                TSpaceServerXL TS = new TSpaceServerXL(Url, MinDelay,MaxDelay, Servers);
                RemotingServices.Marshal(TS, Name, typeof(ITSpaceServer));

                try
                {
                    TS.UpdateView();
                    
                }catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.GetType().ToString());
                    Console.WriteLine(e.StackTrace);


                }
            }
            if (algorithm == "s")
            {
                TSpaceServerSMR TS = new TSpaceServerSMR(MinDelay, MaxDelay);
                RemotingServices.Marshal(TS, Name, typeof(TSpaceServerSMR));
                //RemotingConfiguration.RegisterWellKnownServiceType(typeof(TSpaceServerSMR), Name, WellKnownObjectMode.Singleton);
            }



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