﻿using System;
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
        public TSpaceStorage TuppleSpace;

        private readonly int ServerID;

        private Boolean Frozen = false;

        // Stores the id of the requests already processed
        private static List<string> ProcessedRequests;

        // URL of the server
        private readonly string URL;

        // Stores the urls of all known servers
        private List<string> AllServersURLs;

        // Stores the ID of the current view
        private int ViewID;

        private int MinDelay;
        private int MaxDelay;

        private Random random = new Random();

        private bool verbose = false;


        delegate void DelegateDVRS(string s);

        private static Object FreezeLock = new object();

        public TSpaceServerXL(String url, int _mindelay,int _maxdelay, List<string> servers)
        {
            MinDelay = _mindelay;
            MaxDelay = _maxdelay;
            TuppleSpace = new TSpaceStorage();
            ServerID = new Random().Next();
            ProcessedRequests = new List<string>();
            AllServersURLs = servers;
            URL = url;

        }

        /// <summary>
        /// Updates the current view of live servers
        /// </summary>
        /// <returns>Current view of servers</returns>
        public List<string> UpdateView()
        {
            List<string> currentViewURLs = new List<string>();
            foreach (string serverUrl in AllServersURLs)
            {
                if (TryConnection(serverUrl))
                {
                    currentViewURLs.Add(serverUrl);
                }
            }
            return currentViewURLs;
        }

        /// <summary>
        /// Checks if server at the given location is alive
        /// </summary>
        /// <param name="serverUrl">Server URL</param>
        /// <returns>True if the server is alive; false otherwise.</returns>
        private bool TryConnection(string serverUrl)
        {
          
            // Get the reference for the tuple space server
            ITSpaceServer server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), serverUrl);

            // Check if its a valid reference
            try
            {
                // Ping server
                if (server != null && server.Ping(URL))
                {
                    Console.WriteLine("Alive:  " + serverUrl);
                    return true;
                }

            }
            catch (System.Net.Sockets.SocketException)
            {
                Console.WriteLine("Dead: " + serverUrl);
            }

            return false;
        }

        /// <summary>
        /// Adds new url to the list of known servers URLs
        /// If the server is alive, its also added to the current view
        /// </summary>
        /// <param name="serverURL">Server URL</param>
        public bool Ping(string serverURL)
        {
            if(!AllServersURLs.Contains(serverURL))
                AllServersURLs.Add(serverURL);

            return true;
        }

        public string Status()
        {   
            if(MinDelay+MaxDelay != 0)
                Thread.Sleep(random.Next(MinDelay, MaxDelay));
            return "I live" + this.ServerID;
        }

        public void Freeze()
        {
            Frozen = true;
        }

        public void Unfreeze()
        {
            Frozen = false;
            Console.WriteLine("got here");
        }

        public TSpaceMsg ProcessRequest(TSpaceMsg msg)
        {

            Monitor.Enter(FreezeLock);
            while (Frozen)
                Monitor.Wait(FreezeLock);
            Monitor.Exit(FreezeLock);

            if (MinDelay + MaxDelay != 0)
                Thread.Sleep(random.Next(MinDelay, MaxDelay));

            TSpaceMsg response = new TSpaceMsg();
            response.ProcessID = ServerID;
            response.OperationID = msg.OperationID;

            if (verbose)
                Console.WriteLine(msg);


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
