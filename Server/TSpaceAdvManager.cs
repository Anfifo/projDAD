using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using CommonTypes;

namespace Server
{
    [Serializable]
    class TSpaceAdvManager
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




        public TSpaceAdvManager(String url, int _mindelay, int _maxdelay, View view)
        {
            Console.WriteLine("Starting server " + url + "...");
            MinDelay = _mindelay;
            MaxDelay = _maxdelay;
            TSpace = new TSpaceStorage();
            ServerID = new Random().Next();
            ProcessedRequests = new TSLog();
            ServerView = view;
            ServerView.Add(url);
            ServerView.ID++;
            URL = url;
        }

        public TSpaceAdvManager(String url, int _mindelay, int _maxdelay)
        {
            Console.WriteLine("I am" + " " + url);
            MinDelay = _mindelay;
            MaxDelay = _maxdelay;
            TSpace = new TSpaceStorage();
            ServerID = new Random().Next();
            ProcessedRequests = new TSLog();
            URL = url;
        }

        public int Quorum(int viewCount)
        {
            return (int)Math.Floor((viewCount / 2.0) + 1);
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
            return null;
        }

        

        /// <summary>
        /// Adds new url to the list of known servers URLs
        /// If the server is alive, its also added to the current view
        /// </summary>
        /// <param name="serverURL">Server URL</param>
        public bool Ping(string serverURL)
        {

            return Ping();
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
            lock (FreezeLock)
            {
                Monitor.PulseAll(FreezeLock);
            }
            Console.WriteLine("UnFreezing");
        }

        public bool Ping()
        {
            CheckFreeze();

            CheckDelay();

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

            CheckFreeze();

            CheckDelay();
            if (!ServerView.Contains(url))
            {
                ServerView.Add(url);
                ServerView.ID ++;
                Console.WriteLine("Server " + url + " added. New view ID: " + ServerView.ID);
            }
        }
        public void RemoveFromView(string url)
        {
            CheckFreeze();

            CheckDelay();


            if (url.Equals(URL))
            {
                //Only lock in SMR (XL already locked)
                //RWL.AcquireWriterLock(Timeout.Infinite);
                
                Console.WriteLine("I have been kicked out of the view.");
                Console.WriteLine("Exiting...");
                Thread.Sleep(10000);
                Process.GetCurrentProcess().Kill();
            }

            if (ServerView.Contains(url))
            {
                ServerView.Remove(url);
                ServerView.ID++;
                Console.WriteLine("Server " + url + " removed. New view ID: " + ServerView.ID);
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

    }

}
