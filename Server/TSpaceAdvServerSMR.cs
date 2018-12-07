using System;
using System.Collections.Generic;
using CommonTypes;
using System.Threading;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Timers;
using System.Threading.Tasks;

namespace Server
{
    [Serializable]
    class TSpaceAdvServerSMR : MarshalByRefObject, ITSpaceServer
    {
        public TSpaceAdvManager TSMan;
        // Stores the request in hold
        private static List<Message> MessageQueue = new List<Message>();

        // Stores the most recent sequence number
        private static int SequenceNumber;

        // Lock reference for take operation
        private static Object TakeLock = new Object();

        private static Object FreezeLock = new object();

        private static Object SequenceNumberLock = new object();

        private static Object StateLock = new object();

        private static Dictionary<string, int> AggredOperations = new Dictionary<string, int>();

     

        // Delegate for remote assync call to the tuple space servers
        delegate TSpaceMsg RemoteAsyncDelegate(TSpaceMsg request);

        delegate void RemoteUpdateViewDelegate(string url, string id, int seq);

        delegate bool RemoteDeletefromViewDelegate(string url, bool tryRemove);

        private Dictionary<string, int> AddingToView = new Dictionary<string, int>();

        // Counter for the number of acknowledgements received 
        static int AcksCounter;

        static int ViewAcksCounter;

        /// <summary>
        /// Ensures que object doesn't get cleaned while being served
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }


        public TSpaceAdvServerSMR(string url, int _mindelay,int _maxdelay)
        {
            TSMan = new TSpaceAdvManager(url, _mindelay, _maxdelay);
            InitUpdateViewTimer();
        }

        public TSpaceAdvServerSMR(string url, int _mindelay, int _maxdelay,View view)
        {
            TSMan = new TSpaceAdvManager(url, _mindelay, _maxdelay,view);
            InitUpdateViewTimer();
        }

        public bool Ping() => TSMan.Ping();

        public void AddToView(string url) => TSMan.AddToView(url);

        public void AddToView(string url,string id, int seq)
        {
            Message update = UpdateMessage(id, seq);
            AddToView(url);
            RemoveFromQueue(update);
        }

        public void RemoveFromView(string url, string id, int seq)
        {
            //Update aggred sequence number of update view msg
            Message update = UpdateMessage(id, seq);

            //Remove server from view
            TSMan.RemoveFromView(url);

            //Remove from queue all messages from this process
            RemoveMessagesFrom(url);

            //Remove update view message from queue
            RemoveFromQueue(update);
            
            //Remove from dead suspects
            lock (SuspectedDead)
            {
                if (SuspectedDead.ContainsKey(url))
                {
                    SuspectedDead.Remove(url);
                }
                Monitor.Pulse(SuspectedDead);
            }
        }


        public string Status() => TSMan.Status();

        public void Freeze() => TSMan.Freeze();

        public void Unfreeze() => TSMan.Unfreeze();

        public TSpaceMsg SMRProcessRequest(TSpaceMsg msg)
        {
            
            TSMan.CheckFreeze();

            TSMan.CheckDelay();

            lock (AggredOperations)
            {
                if (msg.Code != "proposeSeq")
                {
                    AggredOperations[msg.OperationID] = msg.SequenceNumber;
                }
                else
                {
                    //Console.WriteLine("Not Agreed: " + msg.OperationID);

                }
            }

            TSpaceMsg response = new TSpaceMsg
            {
                ProcessID = TSMan.URL,
                OperationID = msg.OperationID,
                RequestID = msg.RequestID,
                MsgView = TSMan.GetTotalView()
            };

            // Verifying View! Wrong view sends updated view
            if (!TSMan.ValidView(msg))
            {
                Console.WriteLine("Wrong View ( s = " + TSMan.ServerView.ID + "; c = " + msg.MsgView.ID + " )" );
                return TSMan.CreateBadViewReply(msg);
            }

            lock (TSpaceAdvManager.ProcessedRequests)
            {
                // Check if request as already been processed
                if (TSpaceAdvManager.ProcessedRequests.Contains(msg.RequestID))
                {
                    // Check if it was processed in a previous viwew
                    LogEntry Temp = TSpaceAdvManager.ProcessedRequests.GetByKey(msg.RequestID);
                    if ((TSpaceAdvManager.ProcessedRequests.GetByKey(msg.RequestID).Request.MsgView.ID < TSMan.ServerView.ID) ||
                            (Temp.Response != null && Temp.Response.ProcessID != TSMan.URL))
                    {
                        
                        Console.WriteLine("Processed in previous view:" +
                                            TSpaceAdvManager.ProcessedRequests.GetByKey(msg.RequestID).Request.MsgView.ID);



                        TSpaceAdvManager.ProcessedRequests.UpdateView(msg.RequestID, TSMan.ServerView);
                        TSpaceMsg resp = TSpaceAdvManager.ProcessedRequests.GetByKey(msg.RequestID).Response;

                        if (resp == null)
                        {
                            return null;
                        }
                        resp.MsgView = TSMan.ServerView;

                        TSpaceManager.ProcessedRequests.UpdateView(msg.RequestID, TSMan.ServerView);
                        TSpaceManager.ProcessedRequests.UpdateResponse(msg.RequestID, resp);


                        // Return the response sent in the previous view
                        return resp;
                    } 
                    else
                    {
                        response.Code = "Repeated";
                        //Console.WriteLine("Repetido");
                        return response;
                    }

                }
            
                // Add sequence number of request to processed requests
                TSpaceAdvManager.ProcessedRequests.Add(msg);

            }

            string command = msg.Code;
            
            Message update = null;
            
            // Sequence number proposal request
            if (command.Equals("proposeSeq"))
            {

                lock (AggredOperations)
                {
                    //Already was executed corresponsing operation
                    if (AggredOperations.ContainsKey(msg.OperationID))
                    {
                        Console.WriteLine("Operation already was executed: " + msg.OperationID);
                        response.Code = "proposedSeq";
                        response.SequenceNumber = AggredOperations[msg.OperationID];
                        return response;
                    }


                    lock (MessageQueue)
                    {
                        // Increment sequence number
                        Interlocked.Increment(ref SequenceNumber);
                    }
                    response.SequenceNumber = SequenceNumber;
                    response.Code = "proposedSeq";
                    AddMessageToQueue(msg);
                }

                Console.WriteLine("Proposing (id = " + msg.OperationID + "; seq = " + response.SequenceNumber + ")");

                return response;
            }

            // Message with agreed sequence number
            else
            {
                //Update message queue with agreed seq number
                update = UpdateMessage(msg.OperationID, msg.SequenceNumber);
                if (update == null)
                {
                   
                    Console.WriteLine("Err: operation message not in queue");
                    //response.Code = "Err";

                    //return response;
                }
            }
            
            
            //Wait for the message head of queue
            Monitor.Enter(MessageQueue);
            while (MessageQueue.Count == 0 || !MessageQueue[0].MessageID.Equals(msg.OperationID))
            {
                TSMan.FinishedProcessing();
                Monitor.Wait(MessageQueue, 500);
                TSMan.Processing();
            }
            Monitor.Exit(MessageQueue);
            

            Console.WriteLine("Execute operation " + msg.OperationID + ": code = " + command);

            // Execute the operation
            if (TSMan.Verbose)
                Console.WriteLine(msg);

            switch (command)
            {
                case "add":
                    lock (TakeLock)
                    {
                        if (msg.Tuple == null)
                        {
                            response.Code = "ERR";
                            break;
                        }

                        TSMan.TSpace.Add(msg.Tuple);
                        response.Code = "ACK";
                        break;
                    }

                case "read":
                    if (msg.Tuple == null)
                    {
                        response.Code = "ERR";
                        break;
                    }
                    response.Tuple = TSMan.TSpace.Read(msg.Tuple);
                    response.Code = "OK";
                    if (response.Tuple == null)
                        Console.WriteLine("Match not Found");
                    break;

                case "take1":
                    lock (TakeLock)
                    {
                        if (msg.Tuple == null)
                        {
                            response.Code = "ERR";
                            break;
                        }

                        // Get matching tuple
                        response.Tuple = TSMan.TSpace.Read(msg.Tuple);
                        response.Code = "OK";
                        if (response.Tuple != null)
                        {
                            // Delete it
                            TSMan.TSpace.Take2(response.Tuple);
                        }
                    }
                    response.Code = "OK";
                    break;

                case "take2":
                    Console.WriteLine("Current Tuple Space not in XL mode");
                    response.Code = "ERR";
                    break;


                // Operation exclusive of the XL Tuple Space
                case "releaseLocks":

                    lock(TakeLock)
                        response.Code = "ERR";
                    Console.WriteLine("Current Tuple Space not in XL mode");
                    break;

                default:
                    Console.WriteLine("Invalid command.");
                    break;
            }
            Console.WriteLine("Pre Remove");


            RemoveFromQueue(msg.OperationID);


            Console.WriteLine("Return response: " + response.OperationID);
            
            return response;
        }

        internal void ChangeState(TSpaceAdvServerSMR server, string url)
        {
            TSpaceAdvManager.RWL.AcquireWriterLock(Timeout.Infinite);

                TSpaceState serverState;
                Console.WriteLine("Synchronizing state...");
                serverState = server.GetTSpaceState(url);
                this.SetTSpaceState(serverState);

            TSpaceAdvManager.RWL.ReleaseWriterLock();
        }

        private static void AddMessageToQueue(TSpaceMsg msg)
        {
            lock (MessageQueue)
            {
                Console.WriteLine("AddMessageToQueue " + msg.OperationID + " => seq = " + SequenceNumber);
                Message newMessage = new Message();
                newMessage.ProcessID = msg.ProcessID;
                newMessage.SequenceNumber = SequenceNumber;
                newMessage.Deliverable = false;
                newMessage.MessageID = msg.OperationID;

                // Add message to queue
                MessageQueue.Add(newMessage);
                MessageQueue.Sort();
            }
        }

        public void RemoveFromQueue(string id)
        {
            lock (MessageQueue)
            {
                    Message msg = GetMessageFromQueue(id);
                if (msg == null)
                    Console.WriteLine("WHAT THE ACTUAL FUCK");
                RemoveFromQueue(msg);
            }
        }

        private void RemoveFromQueue(Message msg)
        {
            // Delete processed message from queue
            if (msg != null)
            {
                string id = msg.MessageID;
                //Console.WriteLine("RemovingFromQueue " + msg.MessageID + " => seq = " + msg.SequenceNumber );
                
                    //Console.WriteLine("MidRemoving " + msg.MessageID);
                    MessageQueue.Remove(msg);
                    MessageQueue.Sort();
                    Monitor.PulseAll(MessageQueue);
                
                //Console.WriteLine("RemoveFromQueue " + (GetMessageFromQueue(id) == null).ToString() + msg.MessageID);
            }
        }

        private void WaitTurn(string id)
        {
            // Wait for message to be in the head of the queue
            Monitor.Enter(MessageQueue);
            while (MessageQueue.Count == 0 || !MessageQueue[0].MessageID.Equals(id))
            {
                Monitor.Wait(MessageQueue, 500);
            }
            Monitor.Exit(MessageQueue);
        }

        /// <summary>
        /// Update sequence number of the message with the given id
        /// </summary>
        /// <param name="id">Message id</param>
        /// <param name="sequenceNumber">Agreed sequence number</param>
        /// <returns></returns>
        private Message UpdateMessage(string id, int sequenceNumber)
        {
            lock (MessageQueue)
            {
                
                Message msg = GetMessageFromQueue(id);
                //Update global sequence number
                SequenceNumber = Math.Max(SequenceNumber, sequenceNumber);
                //If the message was not in queue, then add it
                if (msg == null)
                {
                    msg = new Message
                    {
                        MessageID = id,
                    };
                    MessageQueue.Add(msg);
                }

                foreach (Message msg2 in MessageQueue)
                    Console.WriteLine("Updateid=> " + msg2.MessageID + "; Updateseq => " + msg2.SequenceNumber);

                //Update the message sequence number and set it as deliverable
                msg.SequenceNumber = sequenceNumber;
                msg.Deliverable = true;
                MessageQueue.Sort();
                
   

                Monitor.PulseAll(MessageQueue);

                return msg;

            }

        }

        private Message GetMessageFromQueue(string id)
        {
            lock (MessageQueue)
            {
                foreach (Message msg in MessageQueue)
                {
                    if (msg.MessageID.Equals(id))
                        return msg;
                }
                return null;
            }
        }

        private void RemoveMessagesFrom(string id)
        {
            lock (MessageQueue)
            {
                foreach (Message msg in MessageQueue)
                {
                    if (msg.ProcessID.Equals(id))
                    {
                        MessageQueue.Remove(msg);
                        MessageQueue.Sort();

                    }
                }
            }

        }

        public bool Ping(string serverURL) => TSMan.Ping(serverURL);

        public View UpdateView() => TSMan.UpdateView();

        public List<ITuple> GetTuples() => TSMan.TSpace.getAll();

        public void SetTuples(List<ITuple> newState) => TSMan.SetTuples(newState);

        /// <summary>
        /// Initializes the tuple space state
        /// </summary>
        /// <param name="smr">state</param>
       
        public void SetTSpaceState(TSpaceState smr)
        {
            Console.WriteLine("started setting state");
            lock (TSpaceAdvManager.ProcessedRequests)
            {
                MessageQueue = smr.MessageQueue;
                
                TSpaceAdvManager.ProcessedRequests = smr.ProcessedRequests;
                TSMan.setView(smr.ServerView);
                TSMan.SetTuples(smr.TupleSpace);
                SequenceNumber = smr.SequenceNumber;
                Console.WriteLine("Starting with view: " + smr.ServerView);
                Console.WriteLine("Start in queue: " + MessageQueue.Count);
                foreach (Message msg in MessageQueue)
                {
                    Console.WriteLine("ID:"+msg.MessageID + "   " + msg.Request);
                        
                }
                // foreach (Message m in MessageQueue){
                //     Console.WriteLine("In queue => id = " + m.MessageID + ";" + " seq = " + m.SequenceNumber + ";" + " deliverable = " + m.Deliverable);
                // }
            }
            

        }

        /// <summary>
        /// Gets the tuple space state
        /// </summary>
        /// <param name="Url">url of the server requesting the state</param>        /// <returns></returns>
        public TSpaceState GetTSpaceState(string Url)
        {
            Console.WriteLine("Started getting state");
            TSpaceAdvManager.RWL.AcquireWriterLock(Timeout.Infinite);
            Console.WriteLine("Acquire lock");

            TSpaceState smr = new TSpaceState();
            //Create operationID
            string id = TSMan.URL + "_" + (ViewUpdateCounter++);

            int seqNum;
            lock (AddingToView)
            {
                seqNum = GetSequenceNumber(id, null);
                AddingToView.Add(Url, seqNum);
            }

            Console.WriteLine("My msg id => " + id + "; " + "seq => " + seqNum);
            lock(MessageQueue)
            foreach (Message msg in MessageQueue)
                Console.WriteLine("id=> " + msg.MessageID + "; seq => " + msg.SequenceNumber);
            //Wait to be head of queue
            WaitTurn(id);
            Console.WriteLine("left waitturn" + " " + id);

            //Get current list of servers
            List<string> serversUrl = TSMan.ServerView.DeepUrlsCopy();

            //Copy state

            smr.SequenceNumber = SequenceNumber;

            TSMan.AddToView(Url);

            smr.ServerView = TSMan.GetTotalView();

            //its static, cant be accessed with instance
            smr.ProcessedRequests = TSpaceAdvManager.ProcessedRequests;
            smr.TupleSpace = TSMan.GetTuples();

            //Send update view to all servers
            UpdateView(Url, id, serversUrl, AddingToView[Url], true);

            Console.WriteLine("id of something:" + id);
            RemoveFromQueue(id);

            smr.MessageQueue = new List<Message>();

            foreach (Message msg2 in MessageQueue)
                smr.MessageQueue.Add(Message.DeepClone(msg2));

            Console.WriteLine("number of elements:" + smr.MessageQueue.Count);

            foreach (Message msg2 in MessageQueue)
                Console.WriteLine("Updateid=> " + msg2.MessageID + "; Updateseq => " + msg2.SequenceNumber);

            AddingToView.Remove(Url);
            TSpaceAdvManager.RWL.ReleaseWriterLock();
            Console.WriteLine("Finished getting state");
            Console.WriteLine("release lock");

            return smr;
            
        }

        private void UpdateView(string updateServer, string operationID, List<string> allServers, int seqNum, bool insert)
        {

            // Clear previous responses
            ViewAcksCounter = 0;
            majorityHasView = false;

            AsyncCallback remoteCallback = new AsyncCallback(UpdateViewCallback);
            TSpaceAdvServerSMR server;
            RemoteUpdateViewDelegate remoteDel;

            foreach (string serverUrl in allServers)
            {
                server = (TSpaceAdvServerSMR)Activator.GetObject(typeof(ITSpaceServer), serverUrl);
                //Add new server
                if (insert)
                {
                    remoteDel = new RemoteUpdateViewDelegate(server.AddToView);
                }
                //Remove server
                else
                {
                    remoteDel = new RemoteUpdateViewDelegate(server.RemoveFromView);
                }
                try
                {
                    // Call remote method
                    remoteDel.BeginInvoke(updateServer, operationID, seqNum, remoteCallback, null);
                }
                catch (Exception) { }

            }

            // Wait for all
            Monitor.Enter(UpdateViewLock);
            while (ViewAcksCounter < TSMan.Quorum(allServers.Count))
                Monitor.Wait(UpdateViewLock);
            Monitor.Exit(UpdateViewLock);
        }


        /// <summary>
        /// Process a request
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public TSpaceMsg ProcessRequest(TSpaceMsg msg)
        {
            TSpaceMsg response;

            //Acquires writers/readers lock
            TSMan.Processing();

            response = SMRProcessRequest(msg);

            lock (TSpaceAdvManager.ProcessedRequests)
            {
                if (response.Code != "Repeated" && response.Code != "badView" && TSpaceAdvManager.ProcessedRequests.Contains(msg.RequestID))
                {
                    TSpaceAdvManager.ProcessedRequests.UpdateResponse(msg.RequestID, response);
                }

            }

            //Releases writers/readers lock
            TSMan.FinishedProcessing();

            return response;
        }
        /********************************************************/

        // List that stores all the proposed sequence numbers
        private static List<int> ProposedSeq = new List<int>();

        // Counter for all messages sent to the server
        private static int RequestCounter;

        // Counter for operation message unique identifier
        private static int ViewUpdateCounter = 0;

        //Lock object to update view
        private static readonly Object UpdateViewLock = new object();

        private bool majorityHasView = false;

        delegate bool PingDelegate(string url);

        private Dictionary<string, int> SuspectedDead = new Dictionary<string, int>();

        private System.Timers.Timer viewUpdateTimer;


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
        private void UpdateViewCallback(IAsyncResult result)
        {
            Interlocked.Increment(ref ViewAcksCounter);
            lock (UpdateViewLock)
            {
                Monitor.Pulse(UpdateViewLock);
            }
        }

        /// <summary>
        /// Determines the agreed sequence number of a message
        /// </summary>
        /// <param name="id">Message OperationID</param>
        /// <returns>Agreed sequence number</returns>
        private int GetSequenceNumber(string id, string suspectDead)
        {
            Monitor.Enter(SequenceNumberLock);
           
            //Console.WriteLine("Message " + id + " : request proposed sequence number");

            // Create request message
            TSpaceMsg message = new TSpaceMsg();
            message.Code = "proposeSeq";
            message.OperationID = id;
            // Replace ID with server url
            message.ProcessID = TSMan.URL;
            message.RequestID = message.ProcessID + "_" + (RequestCounter++);
            message.SequenceNumber = -1;
            
            message.SuspectedDead = suspectDead;


            // Create local callback
            AsyncCallback asyncCallback = new AsyncCallback(PropesedSeqCallback);

            // Clear proposed sequence number for previous messages
            lock (ProposedSeq)
            {
                AcksCounter = 1;
                ProposedSeq.Clear();

                // Add my proposal
                lock (MessageQueue)
                {
                    Interlocked.Increment(ref SequenceNumber);
                }
                ProposedSeq.Add(SequenceNumber);
                AddMessageToQueue(message);
            }
            
            // Send message to all replicas until all have proposed a sequence number
            while (AcksCounter < TSMan.Quorum(TSMan.ServerView.Count))
            {
                Console.WriteLine("trying to get a seq");
                if(message.MsgView != TSMan.ServerView)
                {
                    AcksCounter = 1;
                    ProposedSeq.Clear();
                    ProposedSeq.Add(SequenceNumber);
                    message.MsgView = TSMan.ServerView;
                }
                this.Multicast(message, asyncCallback);
              
            }

            int agreedSeq;
            lock (ProposedSeq)
            {
                // Agreed sequence number = highest proposed sequence number
                agreedSeq = ProposedSeq.Max();
            }
           

            Console.WriteLine("Message " + message.OperationID + " (agreedSeq = " + agreedSeq + ")");

            Monitor.Exit(SequenceNumberLock);
            //UpdateMessage(id, agreedSeq);
            
            return agreedSeq;
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
        private  void PropesedSeqCallback(IAsyncResult result)
        {
            RemoteAsyncDelegate del = (RemoteAsyncDelegate)((AsyncResult)result).AsyncDelegate;

            // Retrieve results.
            TSpaceMsg response = del.EndInvoke(result);

            if (response == null || 
                (response.Code.Equals("badView") && response.MsgView.ID > TSMan.ServerView.ID))
            {
                return;
            }


            if (response.Code.Equals("proposedSeq"))
            {
                lock (ProposedSeq)
                {
                    // Store porposed sequence number
                    ProposedSeq.Add(response.SequenceNumber);
                    Interlocked.Increment(ref AcksCounter);
                    Console.WriteLine(TSMan.ServerView.Count);
                    Console.WriteLine("AcksCounter:"+ AcksCounter);
                }
            }
        }


        /// <summary>
        /// Makes an async remote invocation of TSpaceServer.ProcessRequest
        /// </summary>
        /// <param name="message">Argument of the ProcessRequest method.</param>
        /// <param name="asyncCallback">Callback function of the remote call.</param>
        private void Multicast(TSpaceMsg message, AsyncCallback asyncCallback)
        {
            //Never happens in this implementation
            if (message.Code.Equals("badView") && message.MsgView.ID > TSMan.ServerView.ID)
            {
                Console.WriteLine("Cleaning Acks because of bad view: " + AcksCounter);
                AcksCounter = 0;
            }

            List<ITSpaceServer> servers = TSMan.ServerView.GetProxys(TSMan.URL);
            RemoteAsyncDelegate remoteDel;
            int i = 0;
            Console.WriteLine(TSMan.ServerView);
            foreach (ITSpaceServer server in servers)
            {
                
                // Create delegate for remote method
                remoteDel = new RemoteAsyncDelegate(server.ProcessRequest);
                try
                {
                    
                    // Call remote method
                    remoteDel.BeginInvoke(message, asyncCallback, null);
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to send");
                }
            }
        }

        /// <summary>
        /// Checks if server at the given location is alive
        /// </summary>
        /// <param name="serverUrl">Server URL</param>
        /// <returns>True if the server is alive; false otherwise.</returns>
        public bool TryConnection(string serverUrl, bool tryRemove)
        {
            //Console.WriteLine("Trying connection " + serverUrl + ": " + tryRemove);
            TSMan.CheckFreeze();

            TSMan.CheckDelay();

            
            if (serverUrl.Equals(TSMan.URL))
            {
                //Console.WriteLine("Return false #2");

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
                    //Console.WriteLine("Return true");

                    return true;
                }
                catch (Exception)
                {
                    if (tryRemove)
                        TryRemoveFromView(serverUrl);
                    //Console.WriteLine("Return false #2");

                    return false;
                }
            }
            if (tryRemove)
                TryRemoveFromView(serverUrl);

            //Console.WriteLine("Return false #3");


            return false;
        }

        private void TryRemoveFromView(string deadURL)
        {
            lock(SuspectedDead){
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
                    remoteDel.BeginInvoke(deadURL,false, remoteCallback, deadURL);
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

            // Get operation sequence number
            string operationID = TSMan.URL + "_" + (ViewUpdateCounter++);
            int seqNum = GetSequenceNumber(operationID, deadURL);
          

            //Send view update to all servers and wait for majority
            UpdateView(deadURL, operationID, serversUrl, seqNum, false);

        }

        void ITSpaceServer.UpdateView()
        {
            throw new NotImplementedException();
        }
    }
}




