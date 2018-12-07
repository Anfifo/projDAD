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

            View newview = new View();
            string mode = "b";

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

            if (args.Length == 5)
            {

                MinDelay = Int32.Parse(args[1]);
                MaxDelay = Int32.Parse(args[2]);
                algorithm = args[3];
                mode = args[4];
                
            }

            if(args.Length == 6)
            {
                MinDelay = Int32.Parse(args[1]);
                MaxDelay = Int32.Parse(args[2]);
                algorithm = args[4];
                serverid2 = args[3];
                mode = args[5];

            }




            if (algorithm == "x")
            {
                if (mode == "a")
                {
                    //Server we are going to execute
                    TSpaceAdvServerXL TS = null;
                    //Server we are going to get our state from
                    TSpaceAdvServerXL server = null;
                    if (!serverid2.Equals("none"))
                    {
                        TS = new TSpaceAdvServerXL(Url, MinDelay, MaxDelay);
                    }

                    else
                    {
                        Console.WriteLine("no previous state exists");
                        //if we dont get a state we initialize with an empty view
                        TS = new TSpaceAdvServerXL(Url, MinDelay, MaxDelay, newview);
                    }

                    RemotingServices.Marshal(TS, Name, typeof(TSpaceAdvServerXL));

                    try
                    {
                        if (!serverid2.Equals("none"))
                        {

                            //get the remote server
                            server = (TSpaceAdvServerXL)Activator.GetObject(typeof(TSpaceAdvServerXL), serverid2);

                          
                            //execute changestate on the server we are going to execute
                            TS.ChangeState(server, Url);

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

                if (mode == "b")
                {
                    TSpaceServerXL TS = null;
                    TSpaceServerXL server = null;
                    if (!serverid2.Equals("none"))
                    {
                        TS = new TSpaceServerXL(Url, MinDelay, MaxDelay);
                    }

                    else
                    {
                        Console.WriteLine("no previous state exists");
                        TS = new TSpaceServerXL(Url, MinDelay, MaxDelay, newview);
                    }

                    RemotingServices.Marshal(TS, Name, typeof(TSpaceServerXL));

                    try
                    {
                        if (!serverid2.Equals("none"))
                        {

                            server = (TSpaceServerXL)Activator.GetObject(typeof(TSpaceServerXL), serverid2);
                        
                            TS.changeState(server, Url);

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

            }
            if (algorithm == "s")
            {
                if (mode == "a")
                {
                    TSpaceAdvServerSMR TS = null;
                    TSpaceAdvServerSMR server = null;
                    if (!serverid2.Equals("none"))
                    {
                         TS = new TSpaceAdvServerSMR(Url, MinDelay, MaxDelay);
                    }

                    else
                    {
                        Console.WriteLine("no previous state exists");
                            TS = new TSpaceAdvServerSMR(Url, MinDelay, MaxDelay, newview);
                    }

                    RemotingServices.Marshal(TS, Name, typeof(TSpaceAdvServerSMR));

                    try
                    {
                        if (!serverid2.Equals("none"))
                        {

                                server = (TSpaceAdvServerSMR)Activator.GetObject(typeof(TSpaceAdvServerSMR), serverid2);


                            TS.ChangeState(server, Url);

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

                if (mode == "b")
                {
                    TSpaceServerSMR TS = null;
                    TSpaceServerSMR server = null;
                    if (!serverid2.Equals("none"))
                    {
                        TS = new TSpaceServerSMR(Url, MinDelay, MaxDelay);
                    }

                    else
                    {
                        Console.WriteLine("no previous state exists");
                        TS = new TSpaceServerSMR(Url, MinDelay, MaxDelay, newview);
                    }

                    RemotingServices.Marshal(TS, Name, typeof(TSpaceServerSMR));

                    try
                    {
                        if (!serverid2.Equals("none"))
                        {

                            server = (TSpaceServerSMR)Activator.GetObject(typeof(TSpaceServerSMR), serverid2);

                            TS.changeState(server, Url);

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