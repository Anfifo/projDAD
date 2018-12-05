using System;
using System.Collections.Generic;
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
            string serverid2 = "none";
            List<ITuple> newState = new List<ITuple>();
            TSpaceAdvServerSMR server = null;
            TSpaceState serverState = null;
            View newview = new View();

            //Type of algorithm for server
            string algorithm = "x";


            if (args.Length > 0)
            {
                Url = args[0];
                algorithm = args[1];
            }

            Port = getPortFromURL(Url);
            Name = getNameFromURL(Url);

            channel = new TcpChannel(Port);
            ChannelServices.RegisterChannel(channel, true);

            if (args.Length == 4)
            {

                MinDelay = Int32.Parse(args[1]);
                MaxDelay = Int32.Parse(args[2]);
                algorithm = args[3];
            }

            if(args.Length == 5)
            {
                MinDelay = Int32.Parse(args[1]);
                MaxDelay = Int32.Parse(args[2]);
                algorithm = args[4];
                serverid2 = args[3];

                //get the remote object of the other server
                //get the view from the other server
                //newview = server.UpdateView();
                //Console.WriteLine(newview.ToString());
                //get the tuples from the other server
                //newState = server.GetTuples();

            }




            if (algorithm == "x") {
                //RemotingConfiguration.RegisterWellKnownServiceType(typeof(TSpaceServerXL), Name, WellKnownObjectMode.Singleton);
                TSpaceServerXL TS = new TSpaceServerXL(Url, MinDelay,MaxDelay);
                RemotingServices.Marshal(TS, Name, typeof(ITSpaceServer));

                //set the tuples of the new server
                TS.SetTuples(newState);

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
                TSpaceAdvServerSMR TS = null;

                //checking if there is a previous state
                if (!serverid2.Equals("none"))
                {
                    TS = new TSpaceAdvServerSMR(Url, MinDelay, MaxDelay);
                }
                else
                {
                    TS = new TSpaceAdvServerSMR(Url, MinDelay, MaxDelay, newview);
                }

                RemotingServices.Marshal(TS, Name, typeof(TSpaceAdvServerSMR));
                //RemotingConfiguration.RegisterWellKnownServiceType(typeof(TSpaceServerSMR), Name, WellKnownObjectMode.Singleton);

                //set the tuples of the new server
                
                try
                {
                    if (!serverid2.Equals("none"))
                    {
                        server = (TSpaceAdvServerSMR)Activator.GetObject(typeof(TSpaceAdvServerSMR), serverid2);

                        
                        // Gets current state
                        Console.WriteLine("Getting current state");
                        serverState = server.GetSMRState(Url);

                        //Copy state 
                        TS.SetSMRState(serverState);
                        Console.WriteLine("Initializing server with state: " + serverState.ServerView.ToString());

                    }
                    else
                    {
                        Console.WriteLine("No previous state");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.GetType().ToString());
                    Console.WriteLine(e.StackTrace);


                }

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