using System;
using System.Collections.Generic;
using System.Threading;
using CommonTypes;


namespace Server
{
    [Serializable]
    class TSpaceServerXL : MarshalByRefObject, ITSpaceServer
    {
        public TSpaceStorage TuppleSpace;

        private readonly int ServerID;

        private Boolean Frozen = false;

        // Stores the id of the requests already processed
        private static List<string> ProcessedRequests;

        private int mindelay;
        private int maxdelay;

        private Random random = new Random();

        public TSpaceServerXL(int _mindelay,int _maxdelay)
        {
            mindelay = _mindelay;
            maxdelay = _maxdelay;
            TuppleSpace = new TSpaceStorage();
            ServerID = new Random().Next();
            ProcessedRequests = new List<string>();

        }


        public string Status()
        {   
            if(mindelay+maxdelay != 0)
                Thread.Sleep(random.Next(mindelay, maxdelay));
            return "I live" + this.ServerID;
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
            if (mindelay + maxdelay != 0)
                Thread.Sleep(random.Next(mindelay, maxdelay));
            TSpaceMsg response = new TSpaceMsg();
            response.ProcessID = ServerID;

            lock (ProcessedRequests)
            {
                // Check if request as already been processed
                if (ProcessedRequests.Contains(msg.OperationID))
                {
                    response.Code = "Repeated";
                    return response;

                }

                // Add request ID to processed requests
                ProcessedRequests.Add(msg.OperationID);
            }

            string command = msg.Code;
            Console.WriteLine("Processing Request " + command + " (seq = " + msg.OperationID + ")" );
            
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

                    
                    lock (TSLockHandler.Lock)
                    {
                        // find suitable matches for tuple
                        List<ITuple> matches = TuppleSpace.Take1(msg.Tuple);
                        // Locks all unlocked and matchable tuples for UserID
                        response.Tuples = TSLockHandler.LockTuples(msg.ProcessID, matches);
                    }
                    
                    response.Code = "OK";
                    break;

                case "take2":
                    lock (TSLockHandler.Lock)
                    {
                        // Deletes tuple
                        TuppleSpace.Take2(msg.Tuple);
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
    }
}
