using System;
using System.Collections.Generic;
using CommonTypes;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Linq;

namespace Client
{
    class XL_Client : ITSpaceAPI
    {
        // View of the tuple spaces servers.
        private List<ITSpaceServer> View { get; set; } = new List<ITSpaceServer>();

        // ID of the tuple spaces servers view.
        private int ViewId { get; set; }

        // Sequence number of the last request
        private static int SequenceNumber;

        // Client ID
        private readonly int ClientID;

        // Delegate for remote assync call to the tuple space servers
        delegate TSpaceMsg RemoteAsyncDelegate(TSpaceMsg request);

        // Counter for the number of acknowledgements received 
        private static HashSet<int> AcksID = new HashSet<int>();

        // Stores the tuple returned by the
        private static ITuple Tuple;

        // Stores the matching tuples returned by all tuple space servers
        private static List<List<ITuple>> MatchingTuples = new List<List<ITuple>>();


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="viewUrls">Url of the tuple space servers.</param>
        public XL_Client(List<string> viewUrls, int viewId)
        {
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);

            // Get the reference for the tuple space servers
            foreach (string serverUrl in viewUrls)
            {

                ITSpaceServer server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), serverUrl);
                if (server != null)
                    View.Add(server);
            }

            ViewId = viewId;

            // Set the client unique identifier
            ClientID = new Random().Next();

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
            message.SequenceNumber = ++SequenceNumber;
            message.ProcessID = ClientID;

            AcksID.Clear();

            // Create local callback
            AsyncCallback remoteCallback = new AsyncCallback(XL_Client.AddCallback);

            // Repeat request until all replicas have acknowledged receipt
            while (AcksID.Count < View.Count)
            {
                //Send multicast message to all members of the view
                this.Multicast(message, remoteCallback);

            }

            Console.WriteLine("ADD: OK");

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
            message.SequenceNumber = ++SequenceNumber;
            message.ProcessID = ClientID;

            // Create local callback.
            AsyncCallback remoteCallback = new AsyncCallback(XL_Client.ReadCallback);

            Tuple = null;

            // Send multicast message to all members of the view.
            this.Multicast(message, remoteCallback);

            // Waits until one replica returns a tuple or
            // all replicas answered that they dont have a match
            while (Tuple == null && AcksID.Count < View.Count) ;

            // Return first response.
            Console.WriteLine("READ: OK");
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
                selectedTuple = this.Take1(template);
            
            Console.WriteLine("Take: Phase 1 completed");


            /*------------------------------------------------
                Phase 2: Removing the selected tuple
             ------------------------------------------------*/
            TSpaceMsg message = new TSpaceMsg();
            message.Code = "take2";
            message.SequenceNumber = ++SequenceNumber;
            message.Tuple = selectedTuple;
            message.ProcessID = ClientID;

            // Create local callback.
            AsyncCallback remoteCallback = new AsyncCallback(XL_Client.AddCallback);

            //Repeat until all replicas have acknowledged deletion
            while (AcksID.Count < View.Count)
            {
                // Send multicast request to remove tuples to all members of the view
                this.Multicast(message, remoteCallback);
            }
            Console.WriteLine("Take: Phase 2 completed");

            Console.WriteLine("Take: OK");
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
            message.SequenceNumber = ++SequenceNumber;
            message.ProcessID = ClientID;

            // Clear responses from previour requests
            AcksID.Clear();
            MatchingTuples.Clear();

            // Create local callback.
            AsyncCallback remoteCallback = new AsyncCallback(XL_Client.Take1Callback);


            // Repeat until all replicas have responded
            while (AcksID.Count < View.Count)
            {
                //Send multicast take request to all members of the view
                this.Multicast(message, remoteCallback);
            }


            // Select one tuple from the intersection of all matching tuples lists
            List<ITuple> intersection = MatchingTuples[0];

            foreach (List<ITuple> tupleList in MatchingTuples)
            {
                intersection.Intersect(tupleList, new TupleComparator());
            }

            // If intersection = {}
            // Send release locks to all replicas
            if (intersection.Count == 0)
            {
                //Create message
                message.Code = "releaseLocks";
                message.SequenceNumber = ++SequenceNumber;

                // Clear acks
                AcksID.Clear();

                // Create remote callback
                remoteCallback = new AsyncCallback(XL_Client.AddCallback);

                // Repeat until all replicas have acknowledged release
                while (AcksID.Count < View.Count)
                {
                    //Send multicast take request to all members of the view
                    this.Multicast(message, remoteCallback);
                }

                Console.WriteLine("Take 1: intersection = {} \nRelease locks acknowleged\nRepeat phase 1");
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

            // Response corresponds to a old request
            if (response.SequenceNumber < SequenceNumber)
                return;

            if (response.Code.Equals("ACK"))
            {
                AcksID.Add(response.ProcessID);
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

            // Response corresponds to a old request
            if (response.SequenceNumber < SequenceNumber)
                return;

            // Stores the tuple returned 
            // and the ID of the server that answered
            if (response.Code.Equals("OK"))
            {
                Tuple = response.Tuple;
                AcksID.Add(response.ProcessID);
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

            // Response corresponds to a old request
            if (response.SequenceNumber < SequenceNumber)
            {
                return;
            }
            // Stores the list of matching tuples 
            // and the ID of the server that answered
            if (response.Code.Equals("OK"))
            {
                // Tuples have to be added before the acks are incremented
                MatchingTuples.Add(response.Tuples);
                AcksID.Add(response.ProcessID);

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

            foreach (ITSpaceServer server in View)
            {
                // Create delegate for remote method
                remoteDel = new RemoteAsyncDelegate(server.ProcessRequest);

                // Call remote method
                remoteDel.BeginInvoke(message, asyncCallback, null);
            }
        }

        /// <summary>
        /// Class that implements a custom comparator for the tuples.
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

