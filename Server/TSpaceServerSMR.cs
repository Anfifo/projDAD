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


        public TSpaceServerSMR(string url, int _mindelay,int _maxdelay, List<string> servers)
        {
            TSMan = new TSpaceManager(url, _mindelay, _maxdelay, servers);
        }

        public bool Ping() => TSMan.Ping();

        public string Status() => TSMan.Status();

        public void Freeze() => TSMan.Freeze();

        public void Unfreeze() => TSMan.Unfreeze();

        public TSpaceMsg ProcessRequest(TSpaceMsg msg)
        {
                TSMan.CheckFreeze();

                TSMan.CheckDelay();

                TSpaceMsg response = new TSpaceMsg
                {
                    ProcessID = TSMan.ServerID,
                    OperationID = msg.OperationID,
                    RequestID = msg.RequestID
                };

                Console.WriteLine(msg);
            // Verifying View! Wrong view sends updated view
            /* if (!TSMan.ValidView(msg))
             {
                 Console.WriteLine("invalid view");
                 return TSMan.CreateBadViewReply(msg);
             }
             */

            lock (TSpaceManager.ProcessedRequests)
                {
                    // Check if request as already been processed
                    if (TSpaceManager.ProcessedRequests.Contains(msg.RequestID))
                    {
                        response.Code = "Repeated";
                        return response;
                    }
                    Console.WriteLine("Processed RequestID " + msg.RequestID);

                    // Add sequence number of request to processed requests
                    TSpaceManager.ProcessedRequests.Add(msg.RequestID);

                }

            string command = msg.Code;
                Console.WriteLine("Processing Request " + command + " (seq = " + msg.RequestID + ")");

                Message update = null;
                // Sequence number proposal request
                if (command.Equals("proposeSeq"))
                {

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
                    Monitor.Wait(MessageQueue);

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
                        else
                            Console.WriteLine("Match found");
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

                        Console.WriteLine("Current Tuple Space not in XL mode");
                        response.Code = "ERR";


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

        public List<string> UpdateView() => TSMan.UpdateView();

        public List<ITuple> GetTuples() => TSMan.TSpace.getAll();

        public void SetTuples(List<ITuple> newState) => TSMan.SetTuples(newState);
    }
}
