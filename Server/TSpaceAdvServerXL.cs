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
    class TSpaceAdvServerXL : MarshalByRefObject, ITSpaceServer
    {
        public TSpaceManager TSMan;


        public TSpaceAdvServerXL(String url, int _mindelay,int _maxdelay)
        {
            TSMan = new TSpaceManager(url, _mindelay, _maxdelay);
        }

        public TSpaceAdvServerXL(string url, int _mindelay, int _maxdelay, View view)
        {
            TSMan = new TSpaceManager(url, _mindelay, _maxdelay, view);
        }

        public bool Ping(string serverURL) => TSMan.Ping(serverURL);

        public string Status() => TSMan.Status();

        public void Freeze() => TSMan.Freeze();

        public void Unfreeze() => TSMan.Freeze();

        public List<ITuple> Freezer = new List<ITuple>();

        public TSpaceMsg XLProcessRequest(TSpaceMsg msg)
        {

            TSMan.CheckFreeze();
            TSMan.CheckDelay();

            TSpaceMsg response = new TSpaceMsg
            {
                ProcessID = TSMan.URL,
                OperationID = msg.OperationID,
                MsgView = TSMan.GetTotalView()
            };

            // Verifying View! Wrong view sends updated view
            if (!TSMan.ValidView(msg))
            {
                //Console.WriteLine("client:" +  msg.MsgView.ToString() + "server:" +TSMan.GetTotalView().ToString());
                Console.WriteLine("Wrong View ( s = " + TSMan.ServerView + "; c = " + msg.MsgView + " )");
                return TSMan.CreateBadViewReply(msg);
            }

            if (TSMan.Verbose)
                Console.WriteLine(msg);


            lock (TSpaceManager.ProcessedRequests)
            {
                // Check if request as already been processed
                if (TSpaceManager.ProcessedRequests.Contains(msg.OperationID))
                {
                    // Check if it was processed in a previous viwew
                    if (TSpaceManager.ProcessedRequests.GetByKey(msg.OperationID).Request.MsgView.ID < TSMan.ServerView.ID)
                    {
                        if (TSpaceManager.ProcessedRequests.Log.Count > 150)
                            TSpaceManager.ProcessedRequests.Log.RemoveRange(0, 100);
                        Console.WriteLine("Processed in previous view");
                        Console.WriteLine(TSpaceManager.ProcessedRequests.GetByKey(msg.OperationID).Request.MsgView.ID);
                        //Console.WriteLine(TSMan.ServerView.ID);
                        TSpaceManager.ProcessedRequests.UpdateView(msg.OperationID, TSMan.ServerView);
                        TSpaceMsg resp = TSpaceManager.ProcessedRequests.GetByKey(msg.OperationID).Response;
                        if (resp == null)
                        {
                            Console.WriteLine("NULL RESPONSE SAVED");
                            return null;
                        }

                        return resp;
                    }
                    else
                    {
                        //Console.WriteLine("repeated");
                        response.Code = "Repeated";

                        //Console.WriteLine("Repeated message response was:" + TSpaceManager.ProcessedRequests.GetByKey(msg.OperationID).Response);
                        return response;
                    }

                }
                Console.WriteLine("Starting processing of request " + msg.OperationID);

                // Add sequence number of request to processed requests

                TSpaceManager.ProcessedRequests.Add(msg);

            }

            string command = msg.Code;
            Console.WriteLine("Processing Request " + command + " (seq = " + msg.OperationID + ")" );




            switch (command)
            {
                case "add":
                    TSMan.TSpace.Add(msg.Tuple);
                    if (Freezer.Contains(msg.Tuple))
                        TSMan.TSpace.Take2(msg.Tuple);
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
                        if(!(TSMan.TSpace.Take2(msg.Tuple)));
                            Freezer.Add(msg.Tuple);
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

        internal void changeState(TSpaceAdvServerXL server, string url)
        {
            TSpaceAdvManager.RWL.AcquireWriterLock(Timeout.Infinite);
            TSpaceState serverState;
            Console.WriteLine("getting state from server");
            serverState = server.GetTSpaceState(url);
            Console.WriteLine("got the state" + serverState.ServerView.ToString());
            Console.WriteLine("Setting previous state");
            this.SetTSpaceState(serverState);
            Console.WriteLine("I defined this view:" + this.TSMan.ServerView);
            TSpaceManager.RWL.ReleaseWriterLock();
        }

        public List<ITuple> GetTuples() => TSMan.GetTuples();


        public void SetTuples(List<ITuple> newState) => TSMan.SetTuples(newState);


        public void UpdateView() => TSMan.UpdateView();
        

        public void SetTSpaceState(TSpaceState xl)
        {
            lock (TSpaceManager.ProcessedRequests)
            {
                TSpaceManager.ProcessedRequests = xl.ProcessedRequests;
                TSMan.setView(xl.ServerView);
                TSMan.SetTuples(xl.TupleSpace);
                Freezer = xl.Freezer;
                TSLockHandler.SetContent(xl.LockedTuplesKeys, xl.LockedTuplesValues);
                Console.WriteLine("Starting with view: " + xl.ServerView.ID);
            }

        }

        public TSpaceState GetTSpaceState(string Url)
        {

            TSpaceState xl = new TSpaceState();

            TSpaceManager.RWL.AcquireWriterLock(Timeout.Infinite);


            xl.LockedTuplesKeys = TSLockHandler.GetKeys();
            xl.LockedTuplesValues = TSLockHandler.GetValues();

            TSMan.AddToView(Url);
            xl.ServerView = TSMan.GetTotalView();

            xl.ProcessedRequests = TSpaceManager.ProcessedRequests; //its static, cant be accessed with instance
            xl.TupleSpace = TSMan.GetTuples();

            xl.Freezer = Freezer;

            TSpaceManager.RWL.ReleaseWriterLock();

            return xl;
        }

        public TSpaceMsg ProcessRequest(TSpaceMsg msg)
        {
            TSpaceMsg response;

            //Console.WriteLine("started processing");
            //Console.WriteLine(msg);

            TSMan.Processing();

            response = XLProcessRequest(msg);

            lock (TSpaceManager.ProcessedRequests)
            {
                if (response.Code != "Repeated" && response.Code != "badView" && TSpaceManager.ProcessedRequests.Contains(msg.OperationID))
                {
                    TSpaceManager.ProcessedRequests.UpdateResponse(msg.OperationID, response);
                    //Console.WriteLine("SAVED THIS TRASH: " + response.ToString());
                }

            }

            TSMan.FinishedProcessing();


            //Console.WriteLine("finished processing");
            //Console.WriteLine("RESPONSE:" + response);

            return response;
        }

        void ITSpaceServer.UpdateView()
        {
            throw new NotImplementedException();
        }
    }
}
