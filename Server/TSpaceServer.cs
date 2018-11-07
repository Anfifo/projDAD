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

        public TSpaceServer()
        {
        }

        public int GetPort()
        {
            return 0;
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

            Console.WriteLine("Processing Request " + command + ":");
            switch (command)
            {
                case "add":
                    Console.WriteLine(msg);
                    break;

                case "read":
                    Console.WriteLine(msg);
                    break;

                case "take1":
                    Console.WriteLine(msg);
                    break;

                case "take2":
                    Console.WriteLine(msg);
                    break;

                default:
                    Console.WriteLine("Invalid command");
                    break;
            }
            return null;
        }
    }
}
