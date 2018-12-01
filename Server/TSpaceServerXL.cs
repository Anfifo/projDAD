using System;
using System.Collections.Generic;
using System.Threading;
using CommonTypes;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;


namespace Server
{
    [Serializable]
    class TSpaceServerXL : MarshalByRefObject, ITSpaceServer
    {
        public TSpaceManager TSMan;


        public TSpaceServerXL(String url, int _mindelay,int _maxdelay, View view)
        {
            TSMan = new TSpaceManager(url, _mindelay, _maxdelay,view);
        }

        public View UpdateView() => TSMan.UpdateView();

        private bool TryConnection(string serverUrl) => TSMan.TryConnection(serverUrl);

        public bool Ping(string serverURL) => TSMan.Ping(serverURL);

        public string Status() => TSMan.Status();

        public void Freeze() => TSMan.Freeze();

        public void Unfreeze() => TSMan.Freeze();

        public TSpaceMsg ProcessRequest(TSpaceMsg msg)
        {

            TSMan.CheckFreeze();
            TSMan.CheckDelay();

            TSpaceMsg response = new TSpaceMsg();
            response.ProcessID = TSMan.ServerID;
            response.OperationID = msg.OperationID;

            if (TSMan.Verbose)
                Console.WriteLine(msg);


            lock (TSpaceManager.ProcessedRequests)
            {
                // Check if request as already been processed
                if (TSpaceManager.ProcessedRequests.Contains(msg.OperationID))
                {
                    response.Code = "Repeated";
                    return response;

                }

                // Add request ID to processed requests
                TSpaceManager.ProcessedRequests.Add(msg.OperationID);
            }

            string command = msg.Code;
            Console.WriteLine("Processing Request " + command + " (seq = " + msg.OperationID + ")" );


            if (!TSMan.ValidView(msg))
            {
                return TSMan.CreateBadViewReply(msg);
            }


            switch (command)
            {
                case "add":
                    TSMan.TSpace.Add(msg.Tuple);
                    response.Code = "ACK";
                    break;

                case "read":
                    response.Tuple = TSMan.TSpace.Read(msg.Tuple);
                    
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
                        List<ITuple> matches = TSMan.TSpace.Take1(msg.Tuple);
                        // Locks all unlocked and matchable tuples for UserID
                        response.Tuples = TSLockHandler.LockTuples(msg.ProcessID, matches);
                    }
                    
                    response.Code = "OK";
                    break;

                case "take2":
                    lock (TSLockHandler.Lock)
                    {
                        // Deletes tuple
                        TSMan.TSpace.Take2(msg.Tuple);
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

        public List<ITuple> GetTuples() => TSMan.GetTuples();


        public void SetTuples(List<ITuple> newState) => TSMan.SetTuples(newState);
    }
}
