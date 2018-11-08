using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;
using System.Runtime.Remoting;


namespace Server
{
    [Serializable]
    class TSpaceServerXL : MarshalByRefObject, ITSpaceServer
    {
        public TSpaceStorage TuppleSpace;

        private readonly int ServerID;

        private List<int> ProcessedRequests;

        public TSpaceServerXL()
        {
            TuppleSpace = new TSpaceStorage();
            ServerID = new Random().Next();
            ProcessedRequests = new List<int>();

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
            response.SequenceNumber = msg.SequenceNumber;

            // Check if request as already been processed
            if (ProcessedRequests.Contains(msg.SequenceNumber))
            {
                response.Code = "Repeated";
                return response;
                
            }

            // Add sequence number of request to processed requests
            ProcessedRequests.Add(msg.SequenceNumber);

            string command = msg.Code;
            Console.WriteLine("Processing Request " + command + " (seq = " + msg.SequenceNumber + ")" );
            
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
                        Console.WriteLine("Not Found");
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
