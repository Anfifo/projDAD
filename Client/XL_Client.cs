using System;
using System.Collections.Generic;
using CommonTypes;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Linq;
using System.Threading;

namespace Client
{
    class XL_Client : AbstractClient, ITSpaceAPI
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="viewUrls">Url of the tuple space servers.</param>
        public XL_Client(List<string> viewUrls, int viewId, int clientID):
            base(viewUrls, viewId, clientID)
        {
            Console.WriteLine("XL Client id = " + ClientID);

        }

        /// <summary>
        /// Adds a tuple to the distributed tuple space.
        /// </summary>
        /// <param name="tuple">Tuple to be added.</param>
        public void Add(ITuple tuple)
        {
            if (View.Count == 0)
            {
                Console.WriteLine("No tuple space servers available.");
                return;
            }

            // Create request message
            TSpaceMsg message = new TSpaceMsg();
            message.Code = "add";
            message.Tuple = tuple;
            message.OperationID = ClientID + "_" + (++SequenceNumber);

            //Clear acks
            AcksCounter = 0;

            // Create local callback
            AsyncCallback remoteCallback = new AsyncCallback(XL_Client.AddCallback);

            // Repeat request until all replicas have acknowledged receipt
            while (AcksCounter < View.Count)
            {
                //Send multicast message to all members of the view
                this.Multicast(message, remoteCallback);

            }

            Console.WriteLine("Add " + (++AddCounter) + ": OK");
        }

        /// <summary>
        /// Returns a tuple from the distributed tuple space, whithout deleting it.
        /// </summary>
        /// <param name="template">Template of the requested tuple.</param>
        /// <returns></returns>
        public ITuple Read(ITuple template)
        {
            if (View.Count == 0)
            {
                Console.WriteLine("No tuple space servers available.");
                return null;
            }

            // Create request message.
            TSpaceMsg message = new TSpaceMsg();
            message.Code = "read";
            message.Tuple = template;


            // Create local callback.
            AsyncCallback remoteCallback = new AsyncCallback(XL_Client.ReadCallback);

            // Clear previous answers
            lock (LockRef)
            {
                Tuple = null;
            }

            bool matchFound = false;

            while (!matchFound)
            {
                message.OperationID = ClientID + "_" + (++SequenceNumber);
                AcksCounter = 0;

                // Waits until one replica returns a tuple or
                // all replicas answered that they dont have a match
                while (AcksCounter < View.Count)
                {
                    // Send multicast message to all members of the view.
                    this.Multicast(message, remoteCallback);
                    
                    // Return after first response
                    lock (LockRef)
                    {
                        if (Tuple != null)
                        {
                            matchFound = true;
                            break;
                        }
                    }
                }

                lock (LockRef)
                {
                    if (Tuple != null)
                        matchFound = true;
                }
            }

            // Return first response.
            Console.WriteLine("Read " + (++ReadCounter) + ": OK");

            return Tuple;
        }

        /// <summary>
        /// Returns a tuple from the distributed tuple space, deleting it.
        /// </summary>
        /// <param name="template">Template of the requested tuple.</param>
        /// <returns></returns>
        public ITuple Take(ITuple template)
        {

            if (View.Count == 0)
            {
                Console.WriteLine("No tuple space servers available.");
                return null;
            }

            /*------------------------------------------------
                Phase 1: Selecting the tuple to be removed 
             ------------------------------------------------*/

            ITuple selectedTuple = null;

            // Repeat phase 1 until all replicas return at least 
            // one common matching tuple
            while (selectedTuple == null)
            {
                Console.WriteLine("Take 1: " + SequenceNumber);
                selectedTuple = this.Take1(template);
            }
            
            Console.WriteLine("Take: Phase 1 completed");


            /*------------------------------------------------
                Phase 2: Removing the selected tuple
             ------------------------------------------------*/
            TSpaceMsg message = new TSpaceMsg();
            message.Code = "take2";
            message.Tuple = selectedTuple;
            message.ProcessID = ClientID;
            message.OperationID = ClientID + "_" + (++SequenceNumber);


            // Create local callback.
            AsyncCallback remoteCallback = new AsyncCallback(XL_Client.AddCallback);

            //Clear acks
            AcksCounter = 0;

            //Repeat until all replicas have acknowledged deletion
            while (AcksCounter < View.Count)
            {
                // Send multicast request to remove tuples to all members of the view
                this.Multicast(message, remoteCallback);
            }
            Console.WriteLine("Take: Phase 2 completed");

            Console.WriteLine("Take " + (++TakeCounter) + ": OK");
            return message.Tuple;
            
        }

        /// <summary>
        /// Executes the phase 1 of the take operation.
        /// </summary>
        /// <param name="template">Template of the requested tuple.</param>
        /// <returns></returns>
        private ITuple Take1(ITuple template)
        {
            // Create request message.
            TSpaceMsg message = new TSpaceMsg();
            message.Code = "take1";
            message.Tuple = template;
            message.ProcessID = ClientID;
            message.OperationID = ClientID + "_" + (++SequenceNumber);


            // Clear responses from previour requests
            lock (MatchingTuples)
            {
                AcksCounter = 0;
                MatchingTuples.Clear();
            }

            // Create local callback.
            AsyncCallback remoteCallback = new AsyncCallback(XL_Client.Take1Callback);

            // Repeat until all replicas have responded
            while (AcksCounter < View.Count)
            {
                //Send multicast take request to all members of the view
                this.Multicast(message, remoteCallback);
            }
            Console.WriteLine("Callback count = " + MatchingTuples.Count);


            List<ITuple> intersection;

            lock (MatchingTuples)
            {
                // Select one tuple from the intersection of all matching tuples lists
                intersection = MatchingTuples[0];

                foreach (List<ITuple> tupleList in MatchingTuples)
                {
                    intersection.Intersect(tupleList, new TupleComparator());
                }
            }


            // If intersection = {}
            // Send release locks to all replicas
            if (intersection.Count == 0)
            {
                //Create message
                message.Code = "releaseLocks";
                message.OperationID = ClientID + "_" + (++SequenceNumber);


                // Clear acks
                AcksCounter = 0;

                // Create remote callback
                remoteCallback = new AsyncCallback(XL_Client.AddCallback);

                // Repeat until all replicas have acknowledged release
                while (AcksCounter < View.Count)
                {
                    //Send multicast take request to all members of the view
                    this.Multicast(message, remoteCallback);
                }

                Console.WriteLine("Take 1: intersection = {}");
                return null;
            }

            return intersection[0];
        }


        /***********************************************************
         *                  CALLBACK FUNCTIONS
         ***********************************************************/

        /// <summary>
        /// Callback function for the acknowledgements.
        /// </summary>
        /// <param name="result">Async callback result.</param>
        public static void AddCallback(IAsyncResult result)
        {
            RemoteAsyncDelegate del = (RemoteAsyncDelegate)((AsyncResult)result).AsyncDelegate;
            // Retrieve results.
            TSpaceMsg response = del.EndInvoke(result);

            if (response.Code.Equals("ACK"))
            {
                Interlocked.Increment(ref AcksCounter);
            }
        }

        /// <summary>
        /// Callback function for the read operation.
        /// </summary>
        /// <param name="result">Async callback result.</param>
        public static void ReadCallback(IAsyncResult result)
        {
            RemoteAsyncDelegate del = (RemoteAsyncDelegate)((AsyncResult)result).AsyncDelegate;

            // Retrieve results.
            TSpaceMsg response = del.EndInvoke(result);
         
            // Stores the tuple returned 
            // and the OperationID of the server that answered
            if (response.Code.Equals("OK"))
            {
                Console.WriteLine("Callback OKKKKKKKKKKKKKKKKKKKKKKKKK:" + response.OperationID);
                lock (LockRef)
                {
                    if (response.Tuple != null)
                    {
                        Tuple = response.Tuple;
                    }
                }
                Interlocked.Increment(ref AcksCounter);
            }
        }

        /// <summary>
        /// Callback function for the phase 1 of the take operation.
        /// </summary>
        /// <param name="result">Async calback result.</param>
        public static void Take1Callback(IAsyncResult result)
        {
            
            RemoteAsyncDelegate del = (RemoteAsyncDelegate)((AsyncResult)result).AsyncDelegate;
            
            // Retrieve results.
            TSpaceMsg response = del.EndInvoke(result);
            

            // Stores the list of matching tuples 
            // and the OperationID of the server that answered
            if (response.Code.Equals("OK"))
            {
                // Tuples have to be added before the acks are incremented
                lock (MatchingTuples) {
                    MatchingTuples.Add(new List<ITuple>(response.Tuples));
                    Interlocked.Increment(ref AcksCounter);         
                }
                if (verbose)
                {
                    Console.WriteLine("Response:");
                    Console.WriteLine(response);
                }

            }
        }


        /****************************************************************
         *                     AUX FUNCTIONS / CLASSES
         ****************************************************************/


        /// <summary>
        /// Makes an async remote invocation of TSpaceServer.ProcessRequest
        /// </summary>
        /// <param name="message">Argument of the ProcessRequest method.</param>
        /// <param name="asyncCallback">Callback function of the remote call.</param>
        private void Multicast(TSpaceMsg message, AsyncCallback asyncCallback)
        {
            RemoteAsyncDelegate remoteDel;

            if (verbose)
            {
                Console.WriteLine(message);
                Console.WriteLine("Sending:");
            }


            foreach (ITSpaceServer server in View)
            {
                // Create delegate for remote method
                remoteDel = new RemoteAsyncDelegate(server.ProcessRequest);

                // Call remote method
                remoteDel.BeginInvoke(message, asyncCallback, null);
            }
        }

        /// <summary>
        /// Class that implements a custom equality comparator for the tuples.
        /// </summary>
        private class TupleComparator : IEqualityComparer<ITuple>
        {
            public bool Equals(ITuple x, ITuple y)
            {
                return x.Matches(y);
            }

            public int GetHashCode(ITuple obj)
            {
                throw new NotImplementedException();
            }
        }
    }



}

