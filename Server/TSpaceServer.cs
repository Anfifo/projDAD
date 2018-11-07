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

        public TSpaceServer()
        {
            TuppleSpace = new TSpaceStorage();
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

            string command = msg.Code;

            TSpaceMsg response = new TSpaceMsg();

            Console.WriteLine("Processing Request " + command + ":");
            switch (command)
            {
                case "add":
                    TuppleSpace.Add(msg.Tuple);
                    response.Code = "OK";
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
            return response;
        }
    }
}
