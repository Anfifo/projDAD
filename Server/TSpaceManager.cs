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

        private readonly int MinDelay;

        private readonly int MaxDelay;

        private Random random = new Random();

        public bool Verbose = false;

        delegate void DelegateDVRS(string s);

        private static readonly Object FreezeLock = new object();

        // Readers-Writters like lock System
        public static readonly Object ProcessLock = new object();

        public static readonly Object GetStateLock = new object();
        private int ProcessingCounter;

        public static ReaderWriterLock RWL = new ReaderWriterLock();


        public TSpaceManager(String url, int _mindelay, int _maxdelay,View view)
        {
            Console.WriteLine(" I am" + " " + url);
            MinDelay = _mindelay;
            MaxDelay = _maxdelay;
            TSpace = new TSpaceStorage();
            ServerID = new Random().Next();
            ProcessedRequests = new List<string>();
            ServerView = view;
            ServerView.Add(url);
            ServerView.ID++;
            URL = url;

        }

        public TSpaceManager(String url, int _mindelay,int _maxdelay)
        {
            Console.WriteLine(" I am" + " " + url);
            MinDelay = _mindelay;
            MaxDelay = _maxdelay;
            TSpace = new TSpaceStorage();
            ServerID = new Random().Next();
            ProcessedRequests = new List<string>();
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
        public View UpdateView()
        {
            
            Console.WriteLine("Updating view");
            List<string> currentViewURLs = new List<string>();
            foreach (string serverUrl in ServerView.GetUrls())
            {
                if (serverUrl.Equals(URL))
                {
                    Console.WriteLine("adding myself");
                    currentViewURLs.Add(serverUrl);
                    continue;
                }
                if (TryConnection(serverUrl))
                {
                    Console.WriteLine("added to view" + serverUrl);
                    AddToView(serverUrl);
                    currentViewURLs.Add(serverUrl);
                }
                else
                {
                    for(int i=0;i<5;i++)
                        Console.WriteLine("removing from view" + serverUrl);

                    RemoveFromView(serverUrl);
                }
            }
            Console.WriteLine("done updating");
            return new View(currentViewURLs,ServerView.ID);
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
                Console.WriteLine("i pinged" + " " + serverUrl);
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
            Console.WriteLine("received in ping and adding:" + serverURL);
            AddToView(serverURL);
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

        public void AddToView(string url)
        {
            if (!ServerView.Contains(url))
            {
                ServerView.Add(url);
                ServerView.ID ++;
            }
        }
        public void RemoveFromView(string url)
        {
            if (ServerView.Contains(url))
            {
                ServerView.Remove(url);
                ServerView.ID++;
            }
        }

        public bool ValidView(TSpaceMsg msg)
        {
            return msg.MsgView.ID == ServerView.ID;
        }

        public View GetTotalView()
        {
            View view = new View(ServerView.GetUrls(), ServerView.ID);
            view.Add(URL);
            return view;
        }

        public TSpaceMsg CreateBadViewReply(TSpaceMsg msg)
        {
            TSpaceMsg response = new TSpaceMsg
            {
                Code = "badView",
                ProcessID = ServerID,
                OperationID = msg.OperationID,
                RequestID = msg.RequestID,
                MsgView = GetTotalView()
            };
            return response;
        }

        public void setView(View view)
        {
            ServerView = view;
            //ServerView.ID++;

        }

        internal void FinishedProcessing()
        {
            RWL.ReleaseReaderLock();

            /*
            lock (ProcessLock)
            {
                ProcessingCounter--;
                //Console.WriteLine("Done Processing, current counter:" + ProcessingCounter);
                if (ProcessingCounter == 0)
                {
                    //Console.WriteLine("unlocked");
                    Monitor.Exit(GetStateLock);
                }
            }*/
        }

        internal void Processing()
        {

            RWL.AcquireReaderLock(Timeout.Infinite);
            
            /*
            lock (ProcessLock)
            {
                ProcessingCounter++;
                //Console.WriteLine("Starting Processing, current counter:" + ProcessingCounter);
                if (ProcessingCounter == 1)
                {
                    //Console.WriteLine("locked");
                    Monitor.Enter(GetStateLock);
                }
            }
            */
        }
    }

}
