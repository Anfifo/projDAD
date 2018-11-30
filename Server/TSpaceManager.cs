using System;
using System.Collections.Generic;
using System.Threading;
using CommonTypes;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;


namespace Server
{
    [Serializable]
    class TSpaceManager
    {
        public TSpaceStorage TSpace;

        public int ServerID { get; }

        private Boolean Frozen = false;

        // Stores the id of the requests already processed
        public static List<string> ProcessedRequests { get; set; }

        // URL of the server
        private readonly string URL;

        // Stores the urls of all known servers
        //private List<string> AllServersURLs;

        private View ServerView;

        // Stores the ID of the current view
        private int ViewID;

        private readonly int MinDelay;
        private readonly int MaxDelay;

        private Random random = new Random();

        public bool Verbose = false;


        delegate void DelegateDVRS(string s);

        private static Object FreezeLock = new object();


        public TSpaceManager(String url, int _mindelay,int _maxdelay, List<string> servers)
        {
            MinDelay = _mindelay;
            MaxDelay = _maxdelay;
            TSpace = new TSpaceStorage();
            ServerID = new Random().Next();
            ProcessedRequests = new List<string>();
            ServerView = new View(servers);
            URL = url;

        }

        public void CheckFreeze()
        {
            Monitor.Enter(FreezeLock);
            while (Frozen)
                Monitor.Wait(FreezeLock);
            Monitor.Exit(FreezeLock);
        }

        public void CheckDelay()
        {
            if (MinDelay + MaxDelay != 0)
                Thread.Sleep(random.Next(MinDelay, MaxDelay));
        }

        /// <summary>
        /// Updates the current view of live servers
        /// </summary>
        /// <returns>Current view of servers</returns>
        public List<string> UpdateView()
        {
            List<string> currentViewURLs = new List<string>();
            foreach (string serverUrl in ServerView.GetUrls())
            {
                if (TryConnection(serverUrl))
                {
                    currentViewURLs.Add(serverUrl);
                    ServerView.Add(serverUrl);
                }
            }
            return currentViewURLs;
        }

        /// <summary>
        /// Checks if server at the given location is alive
        /// </summary>
        /// <param name="serverUrl">Server URL</param>
        /// <returns>True if the server is alive; false otherwise.</returns>
        public bool TryConnection(string serverUrl)
        {          
            // Get the reference for the tuple space server
            ITSpaceServer server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), serverUrl);

            // Check if its a valid reference
            try
            {
                // Ping server
                if (server != null && server.Ping(URL))
                {
                    Console.WriteLine("Alive:  " + serverUrl);
                    return true;
                }

            }
            catch (System.Net.Sockets.SocketException)
            {
                Console.WriteLine("Dead: " + serverUrl);
            }

            return false;
        }

        /// <summary>
        /// Adds new url to the list of known servers URLs
        /// If the server is alive, its also added to the current view
        /// </summary>
        /// <param name="serverURL">Server URL</param>
        public bool Ping(string serverURL)
        {
            ServerView.Add(serverURL);
            return true;
        }

        public string Status()
        {   
            if(MinDelay+MaxDelay != 0)
                Thread.Sleep(random.Next(MinDelay, MaxDelay));
            return "I live" + this.ServerID + " " + "we have this many tuples:" + TSpace.getAll().Count;
        }

        public void Freeze()
        {
            Frozen = true;
            Console.WriteLine("Freezing");
        }

        public void Unfreeze()
        {
            Frozen = false;
            Console.WriteLine("UnFreezing");
        }

        public bool Ping()
        {
            return true;
        }


        public List<ITuple> GetTuples()
        {
            return TSpace.getAll();
        }

        public void SetTuples(List<ITuple> newState)
        {
            TSpace.setTuples(newState);
        }
    }
}
