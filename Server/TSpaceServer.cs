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
    class TSpaceServer : MarshalByRefObject, ITSpaceServer
    {
        public ITSpace TuppleSpace;

        private readonly int ServerID;

        private List<int> ProcessedRequests;

        public TSpaceServer()
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

            if (command.Equals("add"))
            {
                TuppleSpace.Add(msg.Tuple);
                response.Code = "ACK";

            } else if (command.Equals("read"))
            {
                response.Tuple = TuppleSpace.Read(msg.Tuple);
                response.Code = "OK";

            } else if (command.Equals("take1")){
                Console.WriteLine("Take 1 begin");
                response.Tuples = TuppleSpace.Take1(msg.Tuple);
                Console.WriteLine("Take 1 end: " +response.Tuples.Count);
                response.Code = "OK";
            } else if (command.Equals("take2"))
            {
                TuppleSpace.Take2(msg.Tuple);
                response.Code = "ACK";

            }else
            {
                Console.WriteLine("Invalid command.");
                response.Code = "Invalid";
            }

            Console.WriteLine("Request answered: " + response.Code + " (seq = " + response.SequenceNumber + ")");
            return response;


            /*
            switch (command)
            {
                case "add":
                    TuppleSpace.Add(msg.Tuple);
                    response.Code = "ACK";
                    Console.WriteLine(msg);
                    Console.WriteLine(((TSpaceStorage)TuppleSpace).getAll()[0]);
                    break;

                case "read":
                    response.Tuple = TuppleSpace.Read(msg.Tuple);
                    response.Code = "OK";
                    if (response.Tuple == null)
                        Console.WriteLine("Not Found");
                    Console.WriteLine(msg);
                    break;

                case "take1":
                    response.Tuples = TuppleSpace.Take1(msg.Tuple);
                    response.Code = "OK";
                    Console.WriteLine(msg);
                    break;

                case "take2":
                    TuppleSpace.Take2(msg.Tuple);
                    response.Code = "ACK";
                    break;

                default:
                    Console.WriteLine("Invalid command.");
                    break;
            }

            return response;*/
        }
    }
}
