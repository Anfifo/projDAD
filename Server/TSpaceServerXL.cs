using System;
using System.Collections.Generic;
using CommonTypes;


namespace Server
{
    [Serializable]
    class TSpaceServerXL : MarshalByRefObject, ITSpaceServer
    {
        public TSpaceStorage TuppleSpace;

        private readonly int ServerID;

        // Stores the id of the requests already processed
        private List<string> ProcessedRequests;

        public TSpaceServerXL()
        {
            TuppleSpace = new TSpaceStorage();
            ServerID = new Random().Next();
            ProcessedRequests = new List<string>();

        }


        public void Run()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public TSpaceMsg ProcessRequest(TSpaceMsg msg)
        { 

            TSpaceMsg response = new TSpaceMsg();
            response.ProcessID = ServerID;

            // Check if request as already been processed
            if (ProcessedRequests.Contains(msg.ID))
            {
                response.Code = "Repeated";
                return response;
                
            }

            // Add request ID to processed requests
            ProcessedRequests.Add(msg.ID);

            string command = msg.Code;
            Console.WriteLine("Processing Request " + command + " (seq = " + msg.ID + ")" );
            
            switch (command)
            {
                case "add":
                    TuppleSpace.Add(msg.Tuple);
                    response.Code = "ACK";
                    break;

                case "read":
                    Console.WriteLine("Search match");
                    response.Tuple = TuppleSpace.Read(msg.Tuple);
                    
                    response.Code = "OK";
                    if (response.Tuple == null)
                        Console.WriteLine("Not Found");
                    else
                        Console.WriteLine("Match found");
                    break;

                case "take1":
                    // find suitable matches for tuple
                    List<ITuple> matches = TuppleSpace.Take1(msg.Tuple);
                    // Locks all unlocked and matchable tuples for UserID
                    response.Tuples = TSLockHandler.LockTuples(msg.ProcessID, matches); 
                    response.Code = "OK";
                    break;

                case "take2":
                    // Deletes tuple
                    TuppleSpace.Take2(msg.Tuple);
                    // Unlocks all tuples previously locked under UserID
                    TSLockHandler.UnlockTuples(msg.ProcessID);
                    response.Code = "ACK";
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

            return response;
        }
    }
}
