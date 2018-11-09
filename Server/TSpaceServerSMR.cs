using System;
using System.Collections.Generic;
using CommonTypes;
using System.Threading;

namespace Server
{
    [Serializable]
    class TSpaceServerSMR : MarshalByRefObject, ITSpaceServer
    {
        public TSpaceStorage TuppleSpace;

        private readonly int ServerID;

        // Stores the OperationID of the processed requests
        private static List<string> ProcessedRequests;

        // Stores the request in hold
        private static List<Message> MessageQueue = new List<Message>();

        private Boolean Frozen = false;

        // Stores the most recent sequence number
        private static int SequenceNumber;

        // Lock reference for take operation
        private static Object TakeLock = new Object();

        public TSpaceServerSMR()
        {
            TuppleSpace = new TSpaceStorage();
            ServerID = new Random().Next();
            ProcessedRequests = new List<string>();

        }

        public string Status()
        {
            return "I live";
        }

        public void Freeze()
        {
            Frozen = true;
        }

        public void Unfreeze()
        {
            Frozen = false;
        }

        public TSpaceMsg ProcessRequest(TSpaceMsg msg)
        {

            TSpaceMsg response = new TSpaceMsg
            {
                ProcessID = ServerID,
                OperationID = msg.OperationID,
                RequestID = msg.RequestID
            };

            lock (ProcessedRequests)
            {
                // Check if request as already been processed
                if (ProcessedRequests.Contains(msg.RequestID))
                {
                    Console.WriteLine("Repeated " + msg.Code);
                    response.Code = "Repeated";
                    return response;
                }
                Console.WriteLine("Processed RequestID " + msg.RequestID);

                // Add sequence number of request to processed requests
                ProcessedRequests.Add(msg.RequestID);
            }


            string command = msg.Code;
            Console.WriteLine("Processing Request " + command + " (seq = " + msg.RequestID + ")" );

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
            switch (command)
            {
                case "add":
                    TuppleSpace.Add(msg.Tuple);
                    response.Code = "ACK";
                    break;

                case "read":
                    response.Tuple = TuppleSpace.Read(msg.Tuple);

                    response.Code = "OK";
                    if (response.Tuple == null)
                        Console.WriteLine("Match not Found");
                    else
                        Console.WriteLine("Match found");
                    break;

                case "take1":
                    lock (TakeLock)
                    {
                        Console.WriteLine("Start read");
                        // Get matching tuple
                        response.Tuple = TuppleSpace.Read(msg.Tuple);
                        response.Code = "OK";
                        Console.WriteLine("End read");
                        if (response.Tuple != null)
                        {
                            Console.WriteLine("Start delete");
                            // Delete it
                            TuppleSpace.Take2(response.Tuple);
                        }
                    }

                    response.Code = "OK";

                    break;

                case "take2":
                    try
                    {
                        response.Code = "ACK";
                    }
                    catch (InvalidCastException)
                    {
                        Console.WriteLine("Current Tuple Space not in XL mode");
                        response.Code = "ERR";
                    }

                    break;


                // Operation exclusive of the XL Tuple Space
                case "releaseLocks":
                    try
                    {
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

            // Delete processed message from queue
            if (update != null)
            {
                lock (MessageQueue)
                {
                    MessageQueue.Remove(update);
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
    }
}
