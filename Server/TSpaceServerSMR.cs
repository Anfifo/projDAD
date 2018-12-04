using System;
using System.Collections.Generic;
using CommonTypes;
using System.Threading;

namespace Server
{
    [Serializable]
    class TSpaceServerSMR : MarshalByRefObject, ITSpaceServer
    {
        public TSpaceManager TSMan;
        // Stores the request in hold
        private static List<Message> MessageQueue = new List<Message>();

        // Stores the most recent sequence number
        private static int SequenceNumber;

        // Lock reference for take operation
        private static Object TakeLock = new Object();

        private static Object FreezeLock = new object();

        
        /// <summary>
        /// Ensures que object doesn't get cleaned while being served
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }


        public TSpaceServerSMR(string url, int _mindelay,int _maxdelay)
        {
            TSMan = new TSpaceManager(url, _mindelay, _maxdelay);
        }

        public TSpaceServerSMR(string url, int _mindelay, int _maxdelay,View view)
        {
            TSMan = new TSpaceManager(url, _mindelay, _maxdelay,view);
        }

        public bool Ping() => TSMan.Ping();

        public string Status() => TSMan.Status();

        public void Freeze() => TSMan.Freeze();

        public void Unfreeze() => TSMan.Unfreeze();

        public TSpaceMsg SMRProcessRequest(TSpaceMsg msg)
        {
            TSMan.CheckFreeze();

            TSMan.CheckDelay();

            TSpaceMsg response = new TSpaceMsg
            {
                ProcessID = TSMan.ServerID,
                OperationID = msg.OperationID,
                RequestID = msg.RequestID,
                MsgView = TSMan.GetTotalView()
            };

            //Console.WriteLine("client:" +  msg.MsgView.ToString() + "server:" +TSMan.GetTotalView().ToString());
            // Verifying View! Wrong view sends updated view
            if (!TSMan.ValidView(msg))
            {
                //Console.WriteLine("client:" +  msg.MsgView.ToString() + "server:" +TSMan.GetTotalView().ToString());
                Console.WriteLine("Wrong View ( s = " + TSMan.ServerView + "; c = " + msg.MsgView + " )" );
                return TSMan.CreateBadViewReply(msg);
            }

            lock (TSpaceManager.ProcessedRequests)
            {
                // Check if request as already been processed
                if (TSpaceManager.ProcessedRequests.Contains(msg.RequestID))
                {
                    // Check if it was processed in a previous viwew
                    if(TSpaceManager.ProcessedRequests.GetByKey(msg.RequestID).Request.MsgView.ID < TSMan.ServerView.ID)
                    {
                        if (TSpaceManager.ProcessedRequests.Log.Count > 150)
                            TSpaceManager.ProcessedRequests.Log.RemoveRange(0, 100);
                        Console.WriteLine("Processed in previous view");
                        Console.WriteLine(TSpaceManager.ProcessedRequests.GetByKey(msg.RequestID).Request.MsgView.ID);
                        //Console.WriteLine(TSMan.ServerView.ID);
                        TSpaceManager.ProcessedRequests.UpdateView(msg.RequestID, TSMan.ServerView);
                        TSpaceMsg resp = TSpaceManager.ProcessedRequests.GetByKey(msg.RequestID).Response;
                        if (resp == null)
                        {
                            Console.WriteLine("NULL RESPONSE SAVED");
                            return null ;
                        }

                        return resp;
                    } 
                    else
                    {
                        //Console.WriteLine("repeated");
                        response.Code = "Repeated";

                        //Console.WriteLine("Repeated message response was:" + TSpaceManager.ProcessedRequests.GetByKey(msg.RequestID).Response);
                        return response;
                    }

                }
                Console.WriteLine("Starting processing of request " + msg.RequestID);
            
                // Add sequence number of request to processed requests

                TSpaceManager.ProcessedRequests.Add(msg);

            }

            string command = msg.Code;
            //Console.WriteLine("Processing Request " + command + " (seq = " + msg.RequestID + ")");
            
            Message update = null;
            // Sequence number proposal request
            if (command.Equals("proposeSeq"))
            {
                Console.WriteLine("Propose request received " + msg.OperationID);
                // Increment sequence number
                Interlocked.Increment(ref SequenceNumber);
                response.SequenceNumber = SequenceNumber;
                response.Code = "proposedSeq";

                lock (MessageQueue)
                {
                    Message newMessage = new Message();
                    newMessage.ProcessID = msg.ProcessID;
                    newMessage.SequenceNumber = SequenceNumber;
                    newMessage.Deliverable = false;
                    newMessage.MessageID = msg.OperationID;

                    // Add message to queue
                    MessageQueue.Add(newMessage);
                    MessageQueue.Sort();
                }

                Console.WriteLine("Return propose answer" + msg.OperationID);
                return response;
            }
            // Message with agreed sequence number
            else
            {
                update = UpdateMessage(msg.OperationID, msg.SequenceNumber);
                if (update == null)
                {
                    Console.WriteLine("Err: operation message not in queue");
                    response.Code = "Err";

                    return response;
                }
            }
            // Wait for message to be in the head of the queue
            Monitor.Enter(MessageQueue);
            while (MessageQueue.Count == 0 || !MessageQueue[0].MessageID.Equals(msg.OperationID))
            {
                //Console.WriteLine("stuck at while");
                Monitor.Wait(MessageQueue);
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
                    //else
                        //Console.WriteLine("Match found");
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

            // Delete processed message from queue
            if (update != null)
            {
                lock (MessageQueue)
                {
                    MessageQueue.Remove(update);
                    Monitor.PulseAll(MessageQueue);
                }
            }
            Console.WriteLine("Return response " + msg.OperationID);

            return response;

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
                foreach(Message msg in MessageQueue)
                {
                    if (msg.MessageID.Equals(id))
                    {
                        msg.SequenceNumber = sequenceNumber;
                        msg.Deliverable = true;
                        MessageQueue.Sort();
                        Monitor.PulseAll(MessageQueue);
                        return msg;
                    }
                }
            }

            return null;
        }

        public bool Ping(string serverURL) => TSMan.Ping(serverURL);

        public View UpdateView() => TSMan.UpdateView();

        public List<ITuple> GetTuples() => TSMan.TSpace.getAll();

        public void SetTuples(List<ITuple> newState) => TSMan.SetTuples(newState);

        public void SetSMRState(TSpaceState smr)
        {
            lock (TSpaceManager.ProcessedRequests)
            {
                MessageQueue = smr.MessageQueue;
                TSpaceManager.ProcessedRequests = smr.ProcessedRequests;
                TSMan.setView(smr.ServerView);
                TSMan.SetTuples(smr.TupleSpace);
                SequenceNumber = smr.SequenceNumber;
                Console.WriteLine("Starting with view: " + smr.ServerView.ID);
            }

        }

        public TSpaceState GetSMRState(string Url)
        {

            TSpaceState smr = new TSpaceState();
           
            TSpaceManager.RWL.AcquireWriterLock(Timeout.Infinite);


            smr.MessageQueue = MessageQueue;
            smr.SequenceNumber = SequenceNumber;
                
            TSMan.AddToView(Url);
            smr.ServerView = TSMan.GetTotalView();

            smr.ProcessedRequests = TSpaceManager.ProcessedRequests; //its static, cant be accessed with instance
            smr.TupleSpace = TSMan.GetTuples();

            TSpaceManager.RWL.ReleaseWriterLock();

            return smr;
        }

        public TSpaceMsg ProcessRequest(TSpaceMsg msg)
        {
            TSpaceMsg response;

            //Console.WriteLine("started processing");
            //Console.WriteLine(msg);

            TSMan.Processing();

            response = SMRProcessRequest(msg);

            lock (TSpaceManager.ProcessedRequests)
            {
                if (response.Code != "Repeated" && response.Code != "badView" && TSpaceManager.ProcessedRequests.Contains(msg.RequestID))
                {
                    TSpaceManager.ProcessedRequests.UpdateResponse(msg.RequestID, response);
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
