using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CommonTypes;


namespace Server
{
    [Serializable]
    class TSpaceAdvServerXL : MarshalByRefObject, ITSpaceServer
    {
        public TSpaceAdvManager TSMan;

        enum UpdateType : int { INSERT, REMOVE }

        private static object UpdateViewLock = new object();

        private static int UpdateViewCounter = 0;

        public List<ITuple> Freezer = new List<ITuple>();

        private Dictionary<string, int> SuspectedDead = new Dictionary<string, int>();

        private System.Timers.Timer ViewUpdateTimer;

        delegate bool PingDelegate(string url);

        delegate bool DeleteFromViewDel(string url, bool tryRemove);

        private static object RemoveFromViewLock = new object();


        delegate void UpdateViewDel(string subject);


        public TSpaceAdvServerXL(String url, int _mindelay,int _maxdelay)
        {
            TSMan = new TSpaceAdvManager(url, _mindelay, _maxdelay);
            InitUpdateViewTimer();
        }

        public TSpaceAdvServerXL(string url, int _mindelay, int _maxdelay, View view)
        {
            TSMan = new TSpaceAdvManager(url, _mindelay, _maxdelay, view);
            InitUpdateViewTimer();

        }

        private void InitUpdateViewTimer()
        {
            ViewUpdateTimer = new System.Timers.Timer(2000);
            ViewUpdateTimer.AutoReset = true;
            ViewUpdateTimer.Elapsed += new ElapsedEventHandler(PeriodicViewUpdate);
            ViewUpdateTimer.Start();
        }

        public void PeriodicViewUpdate(object sender, ElapsedEventArgs e)
        {
            foreach (string url in TSMan.ServerView.GetUrls())
            {
                new Task(() => { TryConnection(url, true); }).Start();
            }

        }

        /// <summary>
        /// Checks if server at the given location is alive
        /// </summary>
        /// <param name="serverUrl">Server URL</param>
        /// <returns>True if the server is alive; false otherwise.</returns>
        public bool TryConnection(string serverUrl, bool tryRemove)
        {
         
            TSMan.CheckFreeze();

            TSMan.CheckDelay();


            if (serverUrl.Equals(TSMan.URL))
            {
                return true;
            }

            // Get the reference for the tuple space server
            ITSpaceServer server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), serverUrl);


            PingDelegate del = new PingDelegate(server.Ping);
            IAsyncResult asyncResult = del.BeginInvoke(TSMan.URL, null, null);

            asyncResult.AsyncWaitHandle.WaitOne(5000, false);

            if (asyncResult.IsCompleted)
            {
                try
                {
                    del.EndInvoke(asyncResult);
                    return true;
                }
                catch (Exception)
                {
                    if (tryRemove)
                        TryRemoveFromView(serverUrl);

                    return false;
                }
            }
            if (tryRemove)
                TryRemoveFromView(serverUrl);

            return false;
        }

        private void TryRemoveFromView(string deadURL)
        {
            
            lock (SuspectedDead)
            {
                if (SuspectedDead.ContainsKey(deadURL) || !TSMan.ServerView.Contains(deadURL))
                    return;
                SuspectedDead.Add(deadURL, 0);
                TSpaceAdvManager.RWL.AcquireWriterLock(Timeout.Infinite);

            }
            Console.WriteLine("Suspected dead " + deadURL);
            List<string> servers = new List<string>();

            for (int i = 0; i < TSMan.ServerView.GetUrls().Count; i++)
            {
                servers.Add(TSMan.ServerView.GetUrls()[i]);
            }

            Monitor.Enter(SuspectedDead);
            while (SuspectedDead.ContainsKey(deadURL) &&
                (SuspectedDead[deadURL] < TSMan.Quorum(TSMan.ServerView.GetUrls().Count)))
            {
                TSpaceAdvServerXL server;
                DeleteFromViewDel del;
                AsyncCallback callback = new AsyncCallback(TryRemoveFromViewCallback);
                foreach (string serverUrl in servers)
                {                    
                    server = (TSpaceAdvServerXL)Activator.GetObject(typeof(ITSpaceServer), serverUrl);
                    del = new DeleteFromViewDel(server.TryConnection);
                    try
                    {
                        del.BeginInvoke(deadURL, false, callback, deadURL);
                    }
                    catch (Exception) { }

                }
                //Releases until it acquires the lock or timeout elapses
                Monitor.Wait(SuspectedDead, 2000);
            }
            Monitor.Exit(SuspectedDead);
            
            Console.WriteLine("Confirmed Dead " + deadURL);

            lock (SuspectedDead)
            {
                //Already has been removed
                if (!SuspectedDead.ContainsKey(deadURL))
                {
                    Console.WriteLine("Already has been removed");
                    TSpaceAdvManager.RWL.ReleaseWriterLock();
                    return;
                }
            }

            
            //TODO add variable refering current remove
            //Only inc ack if in that remove
            Console.WriteLine("Entering wait");
            Monitor.Enter(RemoveFromViewLock);
            UpdateViewCounter = 0;
            while(UpdateViewCounter < TSMan.Quorum(servers.Count))
            {
                TSpaceAdvServerXL server;
                UpdateViewDel del;

                AsyncCallback callback = new AsyncCallback(RemoveFromViewCallback);

                List<string> testServers = new List<string>(servers);
                foreach (string serverUrl in testServers)
                {
                    server = (TSpaceAdvServerXL)Activator.GetObject(typeof(ITSpaceServer), serverUrl);
                    del = new UpdateViewDel(server.RemoveFromView);
                    try
                    {
                        del.BeginInvoke(deadURL, callback, serverUrl);
                    }
                    catch (Exception) { }
                }

                Monitor.Wait(RemoveFromViewLock, 2000);
            }
            Monitor.Exit(RemoveFromViewLock);
                
            TSpaceAdvManager.RWL.ReleaseWriterLock();
            Console.WriteLine("Confirm remove of server " + deadURL + " => " + UpdateViewCounter);

        }

        private void RemoveFromViewCallback(IAsyncResult result)
        {
            string serverURL = (string)result.AsyncState;

            

            Interlocked.Increment( ref UpdateViewCounter);
            lock (RemoveFromViewLock)
            {
                Monitor.PulseAll(RemoveFromViewLock);
            }
            Console.WriteLine("RemoveCallback ->  " + serverURL + " -- " + UpdateViewCounter);

        }


        private void TryRemoveFromViewCallback(IAsyncResult result)
        {
            
            string serverURL = (string)result.AsyncState;

            DeleteFromViewDel del = (DeleteFromViewDel)((AsyncResult)result).AsyncDelegate;
            // Retrieve results.
            bool isDead = !del.EndInvoke(result);


            lock (SuspectedDead)
            {
                if (isDead && SuspectedDead.ContainsKey(serverURL))
                {
                    Console.WriteLine("Is dead votes " + SuspectedDead[serverURL]);
                    SuspectedDead[serverURL]++;
                }
                Monitor.PulseAll(SuspectedDead);
            }

        }

        public void AddToView(string subject)
        {

            //Himself
            if (subject.Equals(TSMan.URL))
                return;

            try
            {
                TSpaceAdvManager.RWL.AcquireWriterLock(Timeout.Infinite);

                //Add server to view
                TSMan.AddToView(subject);

                TSpaceAdvManager.RWL.ReleaseWriterLock();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.GetType().ToString());
                Console.WriteLine(e.StackTrace);
            }

        }
        int removeCounter = 0;
        public void RemoveFromView(string subject)
        {

            if (!TSMan.ServerView.Contains(subject))
            {
                Console.WriteLine("Already removed ");
                return;
            }

            //Remove server from view
            TSMan.RemoveFromView(subject);

            if (!TSMan.ServerView.Contains(subject))
            {
                Console.WriteLine("Already removed");
                return;
            }

            //Remove server from view
            TSMan.RemoveFromView(subject);
            Console.WriteLine("Remove from view # 2 => " + (++removeCounter));

            //Remove from dead suspects
            lock (SuspectedDead)
            {
                if (SuspectedDead.ContainsKey(subject))
                {
                    SuspectedDead.Remove(subject);
                }
                Monitor.Pulse(SuspectedDead);
            }
            

        }
        //private bool TryConnection(string serverUrl,string url) => TSMan.TryConnection(serverUrl,url);

        public bool Ping(string serverURL) => TSMan.Ping(serverURL);

        public string Status() => TSMan.Status();

        public void Freeze() => TSMan.Freeze();

        public void Unfreeze() => TSMan.Unfreeze();

        public TSpaceMsg XLProcessRequest(TSpaceMsg msg)
        {

            TSMan.CheckFreeze();
            TSMan.CheckDelay();

            TSpaceMsg response = new TSpaceMsg
            {
                ProcessID = TSMan.URL,
                RequestID = msg.RequestID,
                MsgView = TSMan.GetTotalView()
            };

            // Verifying View! Wrong view sends updated view
            if (!TSMan.ValidView(msg))
            {
                Console.WriteLine("client:" +  msg.MsgView.ToString() + "server:" +TSMan.GetTotalView().ToString());
                //Console.WriteLine("Wrong View ( s = " + TSMan.ServerView + "; c = " + msg.MsgView + " )");
                return TSMan.CreateBadViewReply(msg);
            }

            if (TSMan.Verbose)
                Console.WriteLine(msg);


            lock (TSpaceAdvManager.ProcessedRequests)
            {
                // Check if request as already been processed
                if (TSpaceAdvManager.ProcessedRequests.Contains(msg.RequestID))
                {
                    LogEntry Temp = TSpaceAdvManager.ProcessedRequests.GetByKey(msg.RequestID);
                    // Check if it was processed in a previous viwew
                    if (Temp.Request.MsgView.ID < TSMan.ServerView.ID ||
                        (Temp.Response != null && Temp.Response.ProcessID != TSMan.URL))
                    {
                        Console.WriteLine("Processed in previous view");
                        Console.WriteLine(TSpaceAdvManager.ProcessedRequests.GetByKey(msg.RequestID).Request.MsgView.ID);
                        //Console.WriteLine(TSMan.ServerView.ID);

                        TSpaceMsg resp = TSpaceAdvManager.ProcessedRequests.GetByKey(msg.RequestID).Response;
                        
                        if (resp == null)
                        {
                            Console.WriteLine("NULL RESPONSE SAVED");
                            return null;
                        }

                        resp.MsgView = TSMan.ServerView;

                        TSpaceAdvManager.ProcessedRequests.UpdateView(msg.RequestID, TSMan.ServerView);
                        TSpaceAdvManager.ProcessedRequests.UpdateResponse(msg.RequestID, resp);


                        Console.WriteLine(resp);
                        return resp;
                    }
                    else
                    {
                        
                        //Console.WriteLine("repeated");
                        response.Code = "Repeated";

                       // Console.WriteLine("Repeated message response was:" + TSpaceAdvManager.ProcessedRequests.GetByKey(msg.RequestID).Response
                         //   + "\r\n IN RESPONSE TO " + msg);
                        
                        return response;
                    }

                }
                //Console.WriteLine("Starting processing of request " + msg.RequestID);

                // Add sequence number of request to processed requests

                TSpaceAdvManager.ProcessedRequests.Add(msg);

            }

            string command = msg.Code;
            Console.WriteLine("Processing Request " + command + " (seq = " + msg.RequestID + ")" );




            switch (command)
            {
                case "add":
                    TSMan.TSpace.Add(msg.Tuple);
                    if (Freezer.Contains(msg.Tuple))
                        TSMan.TSpace.Take2(msg.Tuple);
                    response.Code = "ACK";
                    break;

                case "read":
                    response.Tuple = TSMan.TSpace.Read(msg.Tuple);
                    
                    response.Code = "OK";
                    if (response.Tuple == null)
                        Console.WriteLine("Match not Found");
                    else
                        Console.WriteLine("Match found");
                    break;

                case "take1":

                    
                    lock (TSLockHandler.Lock)
                    {
                        // find suitable matches for tuple
                        List<ITuple> matches = TSMan.TSpace.Take1(msg.Tuple);
                        // Locks all unlocked and matchable tuples for UserID
                        response.Tuples = TSLockHandler.LockTuples(msg.ProcessID, matches);
                    }
                    
                    response.Code = "OK";
                    break;

                case "take2":
                    lock (TSLockHandler.Lock)
                    {
                        // Deletes tuple
                        if (!(TSMan.TSpace.Take2(msg.Tuple))) ;
                        Freezer.Add(msg.Tuple);
                        //TSMan.TSpace.Take2(msg.Tuple);
                        // Unlocks all tuples previously locked under UserID
                        TSLockHandler.UnlockTuples(msg.ProcessID);
                    }
                    response.Code = "ACK";
                    break;

                // Operation exclusive of the XL Tuple Space
                case "releaseLocks":
                    try
                    {
                        TSLockHandler.UnlockTuples(msg.ProcessID);
                        response.Code = "ACK";
                    }
                    catch (InvalidCastException)
                    {
                        Console.WriteLine("Current Tuple Space not in XL mode");
                        response.Code = "ERR";
                    }

                    break;
                default:
                    Console.WriteLine("Invalid command.");
                    break;
            }

            Console.WriteLine("Return answer to " + command + " (seq = " + msg.RequestID + ")");
            return response;
        }

        internal void ChangeState(TSpaceAdvServerXL server, string url)
        {
            TSpaceAdvManager.RWL.AcquireWriterLock(Timeout.Infinite);
            TSpaceState serverState;
            Console.WriteLine("Synchronizing state...");
            serverState = server.GetTSpaceState(url);
            //Console.WriteLine("Setting state state...");
            this.SetTSpaceState(serverState);
            TSpaceAdvManager.RWL.ReleaseWriterLock();
            Console.WriteLine("Starting with view: " + TSMan.ServerView);

        }

        public List<ITuple> GetTuples() => TSMan.GetTuples();


        public void SetTuples(List<ITuple> newState) => TSMan.SetTuples(newState);


        public void UpdateView() => TSMan.UpdateView();

        public void UpdateView(string url)
        {
            throw new NotImplementedException();
        }


        public void SetTSpaceState(TSpaceState smr)
        {
            lock (TSpaceAdvManager.ProcessedRequests)
            {
                TSpaceAdvManager.ProcessedRequests = smr.ProcessedRequests;
                TSMan.setView(smr.ServerView);
                TSMan.SetTuples(smr.TupleSpace);
                TSLockHandler.SetContent(smr.LockedTuplesKeys, smr.LockedTuplesValues);
                Console.WriteLine("Starting with view: " + smr.ServerView.ID);
            }

        }

        public TSpaceState GetTSpaceState(string Url)
        {

            //Acquire the lock to stop the server from processing requests
            TSpaceAdvManager.RWL.AcquireWriterLock(Timeout.Infinite);
            Console.WriteLine("Getting state for: " + Url);

            //Copy State
            TSpaceState xl = CopyState(Url);

            //Propagate add server to all 
            UpdateAll(Url, "AddToView");

            Console.WriteLine("Return state to: " + Url);

            // Release lock allowing the server to process requests
            TSpaceAdvManager.RWL.ReleaseWriterLock();

            return xl;
        }

        private TSpaceState CopyState(string Url) { 
            TSpaceState xl = new TSpaceState();


            xl.LockedTuplesKeys = TSLockHandler.GetKeys();
            xl.LockedTuplesValues = TSLockHandler.GetValues();

            TSMan.AddToView(Url);

            xl.ServerView = TSMan.GetTotalView();

            xl.ProcessedRequests = TSpaceAdvManager.ProcessedRequests; //its static, cant be accessed with instance
            xl.TupleSpace = TSMan.GetTuples();

            return xl;

        }

        private void UpdateAll(string url, string RequestID)
        {

            Monitor.Enter(UpdateViewLock);

            UpdateViewCounter = 0;
            AsyncCallback callback = new AsyncCallback(UpdateViewCallback);

            Multicast(url, callback, RequestID);

            while (UpdateViewCounter < TSMan.Quorum(TSMan.ServerView.Count))
            {

                Multicast(url, callback, RequestID);

                //Releases until it acquires the lock or timeout elapses
                Monitor.Wait(UpdateViewLock, 2000);
            }
            Console.WriteLine("All updated = " + UpdateViewCounter);

            Monitor.Exit(UpdateViewLock);
        }


        private void Multicast(string subject, AsyncCallback asyncCallback, string operation)
        {

            List<string> servers = TSMan.ServerView.GetUrls(); ;
           
            foreach (string serverUrl in servers)
            {
               
                TSpaceAdvServerXL server = (TSpaceAdvServerXL)Activator.GetObject(typeof(ITSpaceServer), serverUrl);
                try
                {
                    if (operation.Equals("AddToView"))
                    {
                        UpdateViewDel remoteDel = new UpdateViewDel(server.AddToView);
                        remoteDel.BeginInvoke(subject, asyncCallback, null);

                    }

                    else if (operation.Equals("RemoveFromView"))
                    {
                        UpdateViewDel remoteDel = new UpdateViewDel(server.RemoveFromView);
                        remoteDel.BeginInvoke(subject, asyncCallback, null);

                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to send");
                }

            }
        }

        private void UpdateViewCallback(IAsyncResult result)
        {
            Interlocked.Increment(ref UpdateViewCounter);
            lock (UpdateViewLock)
            {
                Monitor.PulseAll(UpdateViewLock);
            }
        }

        public TSpaceMsg ProcessRequest(TSpaceMsg msg)
        {
            TSpaceMsg response;

            //Console.WriteLine("started processing");
            //Console.WriteLine(msg);

            TSMan.Processing();

            response = XLProcessRequest(msg);

            lock (TSpaceAdvManager.ProcessedRequests)
            {
                if (response.Code != "Repeated" && response.Code != "badView" && TSpaceAdvManager.ProcessedRequests.Contains(msg.RequestID))
                {
                    TSpaceAdvManager.ProcessedRequests.UpdateResponse(msg.RequestID, response);
                    //Console.WriteLine("SAVED THIS TRASH: " + response.ToString());
                }

            }

            TSMan.FinishedProcessing();


            //Console.WriteLine("finished processing");
            //Console.WriteLine("RESPONSE:" + response);

            return response;
        }
    }
}
