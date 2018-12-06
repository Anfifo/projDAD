using System;
using System.Collections.Generic;
using System.Threading;
using CommonTypes;
using System.Timers;
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
        //public static List<string> ProcessedRequests { get; set; }

        public static TSLog ProcessedRequests;

        // URL of the server
        public readonly string URL;
        
        public View ServerView;

        private readonly int MinDelay;

        private readonly int MaxDelay;

        private Random random = new Random();

        public bool Verbose = false;

        delegate void DelegateDVRS(string s);

        private static readonly Object FreezeLock = new object();

        public static ReaderWriterLock RWL = new ReaderWriterLock();

        private System.Timers.Timer viewUpdateTimer;


        public TSpaceManager(String url, int _mindelay, int _maxdelay,View view)
        {
            Console.WriteLine(" I am" + " " + url);
            MinDelay = _mindelay;
            MaxDelay = _maxdelay;
            TSpace = new TSpaceStorage();
            ServerID = new Random().Next();
            ProcessedRequests = new TSLog();
            ServerView = view;
            ServerView.Add(url);
            ServerView.ID++;
            URL = url;
            InitUpdateViewTimer();


        }

        public TSpaceManager(String url, int _mindelay, int _maxdelay)
        {
            Console.WriteLine(" I am" + " " + url);
            MinDelay = _mindelay;
            MaxDelay = _maxdelay;
            TSpace = new TSpaceStorage();
            ServerID = new Random().Next();
            ProcessedRequests = new TSLog();
            URL = url;

            InitUpdateViewTimer();
            
        }

        private void InitUpdateViewTimer()
        {
            viewUpdateTimer = new System.Timers.Timer(2000);
            viewUpdateTimer.AutoReset = true;
            viewUpdateTimer.Elapsed += new ElapsedEventHandler(PeriodicViewUpdate);
            viewUpdateTimer.Start();
        }

        public void PeriodicViewUpdate(object sender, ElapsedEventArgs e)
        {
            UpdateView();

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
        public void UpdateView()
        {
            //TSpaceManager.RWL.AcquireWriterLock(Timeout.Infinite);

            //Console.WriteLine("Updating view");
            this.UpdateView(URL);
        }

        /// <summary>
        /// Checks if server at the given location is alive
        /// </summary>
        /// <param name="serverUrl">Server URL</param>
        /// <returns>True if the server is alive; false otherwise.</returns>
        public bool TryConnection(string serverUrl,string url)
        {          
            // Get the reference for the tuple space server
            ITSpaceServer server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), serverUrl);

            // Check if its a valid reference
            try
            {
                // Ping server
                //Console.WriteLine("i pinged" + " " + serverUrl);
                if (server != null && server.Ping(url))
                {
                    //Console.WriteLine("Alive:  " + serverUrl);
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
            //Console.WriteLine("received in ping and adding:" + serverURL);
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
                Console.WriteLine("Adding server: " + url);
                ServerView.Add(url);
                ServerView.ID ++;
                Console.WriteLine("View updated to: " + ServerView.ID);
            }
        }
        public void RemoveFromView(string url)
        {
            Console.WriteLine("Remove from view: " + url);
            if (ServerView.Contains(url))
            {
                ServerView.Remove(url);
                ServerView.ID++;
                Console.WriteLine("View updated to: " + ServerView.ID);
            }
            

        }

        public bool ValidView(TSpaceMsg msg)
        {
            if (msg.MsgView == null)
                Console.WriteLine("NO VIEW SENT WITH MSG");
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
                ProcessID = URL,
                OperationID = msg.OperationID,
                RequestID = msg.RequestID,
                MsgView = GetTotalView()
            };
            return response;
        }

        public void setView(View view)
        {
            ServerView = view;
        }

        internal void FinishedProcessing()
        {
            RWL.ReleaseReaderLock();
        }

        internal void Processing()
        {
            RWL.AcquireReaderLock(Timeout.Infinite);
        }

        internal void UpdateView(string url)
        {
            //TSpaceManager.RWL.AcquireWriterLock(Timeout.Infinite);

            //Console.WriteLine("Updating view");
            List<string> currentViewURLs = new List<string>();
            List<string> servers = new List<string>();

            for (int i = 0; i < ServerView.GetUrls().Count; i++)
            {
                servers.Add(ServerView.GetUrls()[i]);
            }

            foreach (string serverUrl in servers)
                {
                    // Dont check itself
                    if (serverUrl.Equals(URL))
                    {
                        currentViewURLs.Add(serverUrl);
                        continue;
                    }
                    //Verify if connection is valid
                    if (TryConnection(serverUrl, url))
                    {
                        //Console.WriteLine("Adding to view: " + serverUrl);
                        AddToView(serverUrl);
                        currentViewURLs.Add(serverUrl);
                    }
                    else
                    {
                        RemoveFromView(serverUrl);
                    }
                }
            //TSpaceManager.RWL.ReleaseWriterLock();
           
        }
    }

}
