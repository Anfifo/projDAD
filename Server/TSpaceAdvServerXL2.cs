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
    class TSpaceAdvServerXL2 : MarshalByRefObject, ITSpaceServer
    {
        public TSpaceAdvManager TSMan;

        enum UpdateType : int { INSERT, REMOVE}

        private static object UpdateViewLock = new object();

        private static int UpdateViewCounter = 0;


        delegate void UpdateViewDel(string subject);



        public TSpaceAdvServerXL2(String url, int _mindelay,int _maxdelay)
        {
            TSMan = new TSpaceAdvManager(url, _mindelay, _maxdelay);
            InitUpdateViewTimer();
        }

        public TSpaceAdvServerXL2(string url, int _mindelay, int _maxdelay, View view)
        {
            TSMan = new TSpaceAdvManager(url, _mindelay, _maxdelay, view);
            InitUpdateViewTimer();
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

        public void RemoveFromView(string subject)
        {
            TSpaceAdvManager.RWL.AcquireWriterLock(Timeout.Infinite);
                

            //Remove server from view
            TSMan.RemoveFromView(subject);

            //TODO: Remove from dead subjects

            TSpaceAdvManager.RWL.ReleaseWriterLock();


        }


        public bool Ping(string serverURL) => TSMan.Ping(serverURL);

        public string Status() => TSMan.Status();

        public void Freeze() => TSMan.Freeze();

        public void Unfreeze() => TSMan.Freeze();

        public List<ITuple> Freezer = new List<ITuple>();

        public TSpaceMsg XLProcessRequest(TSpaceMsg msg)
        {

            TSMan.CheckFreeze();
            TSMan.CheckDelay();

            TSpaceMsg response = new TSpaceMsg
            {
                ProcessID = TSMan.URL,
                OperationID = msg.OperationID,
                MsgView = TSMan.GetTotalView()
            };

            // Verifying View! Wrong view sends updated view
            if (!TSMan.ValidView(msg))
            {
                //Console.WriteLine("client:" +  msg.MsgView.ToString() + "server:" +TSMan.GetTotalView().ToString());
                Console.WriteLine("Wrong View ( s = " + TSMan.ServerView + "; c = " + msg.MsgView + " )");
                return TSMan.CreateBadViewReply(msg);
            }

            if (TSMan.Verbose)
                Console.WriteLine(msg);


            lock (TSpaceManager.ProcessedRequests)
            {
                // Check if request as already been processed
                if (TSpaceManager.ProcessedRequests.Contains(msg.OperationID))
                {
                    // Check if it was processed in a previous viwew
                    if (TSpaceManager.ProcessedRequests.GetByKey(msg.OperationID).Request.MsgView.ID < TSMan.ServerView.ID)
                    {
                        if (TSpaceManager.ProcessedRequests.Log.Count > 150)
                            TSpaceManager.ProcessedRequests.Log.RemoveRange(0, 100);
                        Console.WriteLine("Processed in previous view");
                        Console.WriteLine(TSpaceManager.ProcessedRequests.GetByKey(msg.OperationID).Request.MsgView.ID);
                        //Console.WriteLine(TSMan.ServerView.ID);
                        TSpaceManager.ProcessedRequests.UpdateView(msg.OperationID, TSMan.ServerView);
                        TSpaceMsg resp = TSpaceManager.ProcessedRequests.GetByKey(msg.OperationID).Response;
                        if (resp == null)
                        {
                            Console.WriteLine("NULL RESPONSE SAVED");
                            return null;
                        }

                        return resp;
                    }
                    else
                    {
                        //Console.WriteLine("repeated");
                        response.Code = "Repeated";

                        //Console.WriteLine("Repeated message response was:" + TSpaceManager.ProcessedRequests.GetByKey(msg.OperationID).Response);
                        return response;
                    }

                }
                Console.WriteLine("Starting processing of request " + msg.OperationID);

                // Add sequence number of request to processed requests

                TSpaceManager.ProcessedRequests.Add(msg);

            }

            string command = msg.Code;
            Console.WriteLine("Processing Request " + command + " (seq = " + msg.OperationID + ")" );




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
                        if(!(TSMan.TSpace.Take2(msg.Tuple)));
                            Freezer.Add(msg.Tuple);
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

            Console.WriteLine("Return answer");
            return response;
        }

        internal void ChangeState(TSpaceAdvServerXL2 server, string url)
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
        

        public void SetTSpaceState(TSpaceState xl)
        {
            
                TSpaceManager.ProcessedRequests = xl.ProcessedRequests;
                TSMan.setView(xl.ServerView);
                TSMan.SetTuples(xl.TupleSpace);
                Freezer = xl.Freezer;
                TSLockHandler.SetContent(xl.LockedTuplesKeys, xl.LockedTuplesValues);
            

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

        private void UpdateAll(string url, string operationID)
        {
            
            Monitor.Enter(UpdateViewLock);

            UpdateViewCounter = 0;
            AsyncCallback callback = new AsyncCallback(UpdateViewCallback);

            while (UpdateViewCounter < TSMan.Quorum(TSMan.ServerView.Count))
            {
                Console.WriteLine("Send UpdateView to all servers");
                Multicast(url, callback, operationID);

                //Releases until it acquires the lock or timeout elapses
                Monitor.Wait(UpdateViewLock, 2000);
            }
            Monitor.Exit(UpdateViewLock);
        }


        private void UpdateViewCallback(IAsyncResult result)
        {
            lock (UpdateViewLock)
            {
                    UpdateViewCounter++;
                    Monitor.PulseAll(UpdateViewLock);
            }
        }


        private void Multicast(string subject, AsyncCallback asyncCallback, string operation)
        {
            
            View view = CheckViewValid();

            List<ITSpaceServer> servers = TSMan.ServerView.GetProxys(TSMan.URL);
            
            foreach (ITSpaceServer server in servers)
            {
               
                try
                {
                    if (operation.Equals("AddToView"))
                    {
                        UpdateViewDel remoteDel = new UpdateViewDel(((TSpaceAdvServerXL2)server).AddToView);
                        remoteDel.BeginInvoke(subject, asyncCallback, null);

                    }

                    else if (operation.Equals("RemoveFromView"))
                    {
                        UpdateViewDel remoteDel = new UpdateViewDel(((TSpaceAdvServerXL2)server).RemoveFromView);
                        remoteDel.BeginInvoke(subject, asyncCallback, null);

                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to send");
                }
            }
        }

        private View CheckViewValid() { return TSMan.ServerView; }

        private TSpaceState CopyState(string Url)
        {
            Console.WriteLine("CopyState(" + Url + ")");
            TSpaceState xl = new TSpaceState();
            xl.LockedTuplesKeys = TSLockHandler.GetKeys();
            xl.LockedTuplesValues = TSLockHandler.GetValues();

            TSMan.AddToView(Url);
            xl.ServerView = TSMan.GetTotalView();

            xl.ProcessedRequests = TSpaceAdvManager.ProcessedRequests; //its static, cant be accessed with instance
            xl.TupleSpace = TSMan.GetTuples();

            xl.Freezer = Freezer;
            return xl;
        }

        public TSpaceMsg ProcessRequest(TSpaceMsg msg)
        {
            TSpaceMsg response;

            //Console.WriteLine("started processing");
            //Console.WriteLine(msg);

            TSMan.Processing();

            response = XLProcessRequest(msg);

            lock (TSpaceManager.ProcessedRequests)
            {
                if (response.Code != "Repeated" && response.Code != "badView" && TSpaceManager.ProcessedRequests.Contains(msg.OperationID))
                {
                    TSpaceManager.ProcessedRequests.UpdateResponse(msg.OperationID, response);
                    //Console.WriteLine("SAVED THIS TRASH: " + response.ToString());
                }

            }

            TSMan.FinishedProcessing();

            return response;
        }
        //================= COPY ====================/

        delegate bool PingDelegate(string url);

        private Dictionary<string, int> SuspectedDead = new Dictionary<string, int>();

        private System.Timers.Timer viewUpdateTimer;

        delegate bool RemoteDeletefromViewDelegate(string url, bool tryRemove);



        void ITSpaceServer.UpdateView()
        {
            throw new NotImplementedException();
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


            // Ping server
            if (server == null)
            {
                return false;
            }

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

            }

            Console.WriteLine("Suspect dead: " + deadURL);

            List<string> serversUrl = TSMan.ServerView.DeepUrlsCopy();

            AsyncCallback remoteCallback = new AsyncCallback(RemoveFromViewCallback);

            // Send vote requests to remove suspected dead server from view
            TSpaceAdvServerSMR server;
            foreach (string serverUrl in serversUrl)
            {
                server = (TSpaceAdvServerSMR)Activator.GetObject(typeof(ITSpaceServer), serverUrl);

                RemoteDeletefromViewDelegate remoteDel = new RemoteDeletefromViewDelegate(server.TryConnection);
                try
                {
                    // Call remote method
                    remoteDel.BeginInvoke(deadURL, false, remoteCallback, deadURL);
                }
                catch (Exception) { }

            }

            Monitor.Enter(SuspectedDead);
            //TODO: Change quorum to update on view change and be just variable
            while (SuspectedDead.ContainsKey(deadURL) &&
                (SuspectedDead[deadURL] < TSMan.Quorum(TSMan.ServerView.GetUrls().Count)))
                Monitor.Wait(SuspectedDead);
            Monitor.Exit(SuspectedDead);

            Console.WriteLine("Confirmed dead: " + deadURL);

            lock (SuspectedDead)
            {
                //Already has been removed
                if (!SuspectedDead.ContainsKey(deadURL))
                {
                    return;
                }
            }


            //Send view update to all servers and wait for majority
            UpdateAll(deadURL, "RemoveFromView");

        }

        private void RemoveFromViewCallback(IAsyncResult result)
        {
            string serverURL = (string)result.AsyncState;
            RemoteDeletefromViewDelegate del = (RemoteDeletefromViewDelegate)((AsyncResult)result).AsyncDelegate;

            // Retrieve results.
            bool isDead = !del.EndInvoke(result);

            lock (SuspectedDead)
            {
                if (isDead && SuspectedDead.ContainsKey(serverURL))
                {
                    SuspectedDead[serverURL]++;
                }
                Monitor.PulseAll(SuspectedDead);
            }
        }
        //================= COPY END ====================/

    }

}
