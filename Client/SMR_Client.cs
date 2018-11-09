using System;
using System.Collections.Generic;
using CommonTypes;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Linq;
using System.Threading;



namespace Client
{
    class SMR_Client : ITSpaceAPI
    {
        // View of the tuple spaces servers.
        private List<ITSpaceServer> View { get; set; } = new List<ITSpaceServer>();

        // OperationID of the tuple spaces servers view.
        private int ViewId { get; set; }

        // Client OperationID
        private readonly int ClientID;

        // Delegate for remote assync call to the tuple space servers
        delegate TSpaceMsg RemoteAsyncDelegate(TSpaceMsg request);

        // List that stores all the proposed sequence numbers
        private static List<int> ProposedSeq = new List<int>();

        // Counter for the acks
        private static int AcksCounter;

        // Stores the tuple returned by the
        private static ITuple Tuple;

        // Counter for operation message unique identifier
        private static int OperationCounter = 0;

        // Counter for all messages sent to the server
        private static int RequestCounter;

        // Object to use as reference for the lock to the Tuple 
        private static Object LockRef = new Object();


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="viewUrls">Urls of the tuple space servers</param>
        /// <param name="viewId">OperationID of the current view</param>
        public SMR_Client(List<string> viewUrls, int viewId)
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
            // Create message
            TSpaceMsg request = new TSpaceMsg();

            request.Code = "add";
            request.Tuple = tuple;
            // Create unique message identifier
            request.OperationID = ClientID + "_" +  (OperationCounter++);
            // Get the sequence number of the request in the view
            request.SequenceNumber = GetSequenceNumber(request.OperationID);
            request.RequestID = ClientID + "_" + (RequestCounter++);

            
            AsyncCallback remoteCallback = new AsyncCallback(SMR_Client.AcksCallback);


            // Clear acks count 
            AcksCounter = 0;

            // Repeat until all replicas have acknowledged receipt
            while(AcksCounter < View.Count)
            {
                // Send request to all replicas in the view
                this.Multicast(request, remoteCallback);
            }

            Console.WriteLine("Add: OK");   
        }

        /// <summary>
        /// Returns a tuple from the distributed tuple space, whithout deleting it.
        /// </summary>
        /// <param name="template">Template of the requested tuple.</param>
        /// <returns></returns>
        public ITuple Read(ITuple template)
        {

            // Create message
            TSpaceMsg request = new TSpaceMsg();
            request.Code = "read";
            request.Tuple = template;
            request.RequestID = ClientID + "_" + (RequestCounter++);

            // Create unique message identifier
            request.OperationID = ClientID + "_" + (OperationCounter++);

            // Get the sequence number of the request in the view
            request.SequenceNumber = this.GetSequenceNumber(request.OperationID);

            AsyncCallback remoteCallback = new AsyncCallback(SMR_Client.ReadCallback);

            // Clear responses to previous request
            // Clear previous answers
            lock (LockRef)
            {
                Tuple = null;
            }
            AcksCounter = 0;

            // Send multicast request to all members of the view
            this.Multicast(request, remoteCallback);

            // Waits until one replica returns a tuple or
            // all replicas answered that they dont have a match
            while (AcksCounter < View.Count)
            {
                lock (LockRef)
                {
                    if (Tuple != null)
                        break;
                }
            }

            Console.WriteLine("Read: OK");


            // Return after the first replica answers
            return Tuple;
        }

        /// <summary>
        /// Returns a tuple from the distributed tuple space, deleting it.
        /// </summary>
        /// <param name="template">Template of the tuple requested</param>
        /// <returns>Tuple matching the template</returns>
        public ITuple Take(ITuple template)
        {
            // Create message
            TSpaceMsg request = new TSpaceMsg();
            request.Code = "take1";
            request.Tuple = template;


            // Create remote callback
            AsyncCallback remoteCallback = new AsyncCallback(SMR_Client.ReadCallback);

            // Clear response from last request
            lock (LockRef)
            {
                Tuple = null;
            }

            // Repeat until one matching tuple is found
            while (true)
            {
                // Create unique message identifier
                request.OperationID = ClientID + "_" + (OperationCounter++);
                
                // Get the sequence number of the request in the view
                request.SequenceNumber = this.GetSequenceNumber(request.OperationID);
                
                // Create unique identifier for the round
                request.RequestID = ClientID + "_" + (RequestCounter++);


                Console.WriteLine("Operation counter " + RequestCounter);

                //Clear acks from last request
                AcksCounter = 0;

                // Repeat untill all replicas have answered
                while (AcksCounter < View.Count)
                {
                    // Send take request to all members of the view
                    this.Multicast(request, remoteCallback);
                }

                // Return if there is a match
                // Repeat otherwise
                Monitor.Enter(LockRef);
                if (Tuple != null)
                {
                    Monitor.Exit(LockRef);
                    break;
                }
                Monitor.Exit(LockRef);
            }

            Console.WriteLine("Take: OK");

            return Tuple;
        }

        
        /****************************************************************
         *                   CALLBACK FUNCTIONS
         ****************************************************************/
        private static void PropesedSeqCallback(IAsyncResult result)
        {
            RemoteAsyncDelegate del = (RemoteAsyncDelegate)((AsyncResult)result).AsyncDelegate;

            // Retrieve results.
            TSpaceMsg response = del.EndInvoke(result);

            if (response.Code.Equals("proposedSeq"))
            {
                lock (ProposedSeq)
                {
                    // Store porposed sequence number
                    ProposedSeq.Add(response.SequenceNumber);
                    Interlocked.Increment(ref AcksCounter);
                }
            }
            
        }

        /// <summary>
        /// Callback function for the acknowledgements.
        /// </summary>
        /// <param name="result">Async call result.</param>
        private static void AcksCallback(IAsyncResult result)
        {
            RemoteAsyncDelegate del = (RemoteAsyncDelegate)((AsyncResult)result).AsyncDelegate;

            // Retrieve results.
            TSpaceMsg response = del.EndInvoke(result);

            if (response.Code.Equals("ACK"))
            {
                Interlocked.Increment(ref AcksCounter);
            }
        }

        private static void ReadCallback(IAsyncResult result)
        {
            RemoteAsyncDelegate del = (RemoteAsyncDelegate)((AsyncResult)result).AsyncDelegate;

            // Retrieve results.
            TSpaceMsg response = del.EndInvoke(result);

            // Stores the tuple returned 
            // and the OperationID of the server that answered
            if (response.Code.Equals("OK"))
            {
                if (response.Tuple != null)
                {
                    lock (LockRef)
                    {
                        Tuple = response.Tuple;
                    }
                }

                Interlocked.Increment(ref AcksCounter);

            }
        }

       
        /****************************************************************
         *                     AUX FUNCTIONS / CLASSES
         ****************************************************************/

        /// <summary>
        /// Determines the agreed sequence number of a message
        /// </summary>
        /// <param name="id">Message OperationID</param>
        /// <returns>Agreed sequence number</returns>
        private int GetSequenceNumber(string id)
        {
           
            Console.WriteLine("Message " + id + " : request proposed sequence number");

            // Create request message
            TSpaceMsg message = new TSpaceMsg();
            message.Code = "proposeSeq";
            message.OperationID = id;
            message.ProcessID = ClientID;
            message.RequestID = ClientID + "_" + (RequestCounter++);
            message.SequenceNumber = -1;



            RemoteAsyncDelegate remoteDel;
            // Create local callback
            AsyncCallback asyncCallback = new AsyncCallback(SMR_Client.PropesedSeqCallback);

            // Clear proposed sequence number for previous messages
            lock (ProposedSeq)
            {
                AcksCounter = 0;
                ProposedSeq.Clear();
            }

            // Send message to all replicas until all have proposed a sequence number
            while(AcksCounter < View.Count)
            {
                foreach (ITSpaceServer server in View)
                {
                    // Create delegate for remote method
                    remoteDel = new RemoteAsyncDelegate(server.ProcessRequest);

                    // Call remote method
                    remoteDel.BeginInvoke(message, asyncCallback, null);
                }
            }
            int agreedSeq;
            lock (ProposedSeq)
            {
                // Agreed sequence number = highest proposed sequence number
                agreedSeq = ProposedSeq.Max();
            }

            Console.WriteLine("Message " + message.OperationID + " (agreedSeq = " + agreedSeq + ")");

            return agreedSeq;
        }

        
       
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
