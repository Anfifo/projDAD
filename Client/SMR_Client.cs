using System;
using System.Collections.Generic;
using CommonTypes;
using System.Runtime.Remoting.Messaging;
using System.Linq;
using System.Threading;

namespace Client
{
    class SMR_Client : AbstractClient, ITSpaceAPI
    {
        // List that stores all the proposed sequence numbers
        private static List<int> ProposedSeq = new List<int>();

        // Counter for operation message unique identifier
        private static int OperationCounter = 0;

        // Counter for all messages sent to the server
        private static int RequestCounter;
        
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="viewUrls">Urls of the tuple space servers</param>
        /// <param name="viewId">OperationID of the current view</param>
        public SMR_Client(List<string> viewUrls, int clientID) : 
            base(viewUrls, clientID)
        {
            Console.WriteLine("SMR Client id = " + ClientID);
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
            // Create message
            TSpaceMsg request = new TSpaceMsg();

            request.Code = "add";
            request.Tuple = tuple;
            // Create unique message identifier
            request.OperationID = ClientID + "_" +  (OperationCounter++);
            // Get the sequence number of the request in the view
            request.SequenceNumber = GetSequenceNumber(request.OperationID);
            request.RequestID = ClientID + "_" + (RequestCounter++);

            request.MsgView = GetCurrentView();

            AsyncCallback remoteCallback = new AsyncCallback(SMR_Client.AcksCallback);


            // Clear acks count 
            AcksCounter = 0;

            // Repeat until all replicas have acknowledged receipt
            while(AcksCounter < View.Count)
            {
                // Send request to all replicas in the view
                this.Multicast(request, remoteCallback);

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
            Console.WriteLine("Read started");
            if (View.Count == 0)
            {
                Console.WriteLine("No tuple space servers available.");
                return null;
            }

            // Create message
            TSpaceMsg request = new TSpaceMsg();
            request.Code = "read";
            request.Tuple = template;
            

            AsyncCallback remoteCallback = new AsyncCallback(SMR_Client.ReadCallback);

            // Clear responses to previous request
            // Clear previous answers
            lock (LockRef)
            {
                Tuple = null;
            }

            bool matchFound = false;

            while (!matchFound)
            {

                // Create unique id of the request
                request.RequestID = ClientID + "_" + (RequestCounter++);

                // Create unique message identifier
                request.OperationID = ClientID + "_" + (OperationCounter++);

                // Get the sequence number of the request in the view
                request.SequenceNumber = this.GetSequenceNumber(request.OperationID);

                request.MsgView = GetCurrentView();


                AcksCounter = 0;

                
                // Waits until one replica returns a tuple or
                // all replicas answered that they dont have a match
                while (AcksCounter < View.Count)
                {
                    // Send multicast request to all members of the view
                    this.Multicast(request, remoteCallback);

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
            Console.WriteLine("Read " + (++ReadCounter) + ": OK");

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
            
            if (View.Count == 0)
            {
                Console.WriteLine("No tuple space servers available.");
                return null;
            }
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

                request.MsgView = GetCurrentView();



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
                lock (LockRef)
                {
                    if (Tuple != null)
                        break;
                }
            }

            Console.WriteLine("Take "+ (++TakeCounter) + ": OK");
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

            if (!AbstractClient.ValidView(response))
            {
                return;
            }
                

            if (response.Code.Equals("proposedSeq"))
            {
                //Console.WriteLine("Proposed Seq");

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

            if (!AbstractClient.ValidView(response))
                return;


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


            if (!AbstractClient.ValidView(response))
                return;

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
                    if (verbose)  
                    {
                        Console.WriteLine("Response:");
                        Console.WriteLine(response);
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
           
            //Console.WriteLine("Message " + id + " : request proposed sequence number");

            // Create request message
            TSpaceMsg message = new TSpaceMsg();
            message.Code = "proposeSeq";
            message.OperationID = id;
            message.ProcessID = ClientID;
            message.RequestID = ClientID + "_" + (RequestCounter++);
            message.SequenceNumber = -1;
            message.MsgView = GetCurrentView();


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
                this.Multicast(message, asyncCallback);

              
                if (AcksCounter > 3)
                    {
                        Console.WriteLine(message.RequestID+message.Code+message.SequenceNumber);
                        Console.WriteLine("this happened?" + " " + "AcksCounter:" + AcksCounter + " " + "ViewCounter:" + " " + View.Count);

                        throw new Exception();
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
            if (AbstractClient.CheckNeedUpdateView())
            {
                Console.WriteLine("Update to " + GetCurrentView());
                message.MsgView = GetCurrentView();
            }

            //Console.WriteLine("Sending " + message.Code + " in view " + message.MsgView);

            RemoteAsyncDelegate remoteDel;
            foreach (ITSpaceServer server in View)
            {
                // Create delegate for remote method
                remoteDel = new RemoteAsyncDelegate(server.ProcessRequest);

                // Call remote method
                remoteDel.BeginInvoke(message, asyncCallback, null);
            }
        }
    }
}
