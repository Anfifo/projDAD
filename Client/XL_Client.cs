using System;
using System.Collections.Generic;
using CommonTypes;
using System.Runtime.Remoting.Messaging;

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


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="viewUrls">Url of the tuple space servers.</param>
        public XL_Client(List<string> viewUrls, int viewId)
        {
            // Get the reference for the tuple space servers
            foreach(string serverUrl in viewUrls)
            {
                ITSpaceServer server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), serverUrl);
                if(server != null)
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
            // Create request message
            TSpaceMsg message = new TSpaceMsg();
            message.Code = "add";
            message.Tuple = tuple;
            message.SequenceNumber = ++SequenceNumber;
            message.ProcessID = ClientID;

            AcksID.Clear();

            RemoteAsyncDelegate remoteDel;
                                   
            // Create local callback
            AsyncCallback remoteCallback = new AsyncCallback(XL_Client.AddCallback);

            // Repeat request until all replicas have acknowledged receipt
            while (AcksID.Count < View.Count)
            {
                //Send multicast message to all members of the view
                foreach (ITSpaceServer server in View)
                {

                    // Create delegate for remote method
                    remoteDel = new RemoteAsyncDelegate(server.ProcessRequest);

                    // Call remote method
                    remoteDel.BeginInvoke(message, remoteCallback, null);
                }
            
            } 
            

            
        }

        /// <summary>
        /// Returns a tuple from the distributed tuple space, whithout deleting it.
        /// </summary>
        /// <param name="template">Template of the requested tuple.</param>
        /// <returns></returns>
        public ITuple Read(ITuple template)
        {

            // Create request message.
            TSpaceMsg message = new TSpaceMsg();
            message.Code = "read";
            message.Tuple = template;
            message.SequenceNumber = ++SequenceNumber;
            message.ProcessID = ClientID;


            RemoteAsyncDelegate remoteDel;
            
            // Create local callback.
            AsyncCallback remoteCallback = new AsyncCallback(XL_Client.ReadCallback);

            Tuple = null;

            // Send multicast message to all members of the view.
            foreach (ITSpaceServer server in View)
            {
                // Create delegate for remote method.
                remoteDel = new RemoteAsyncDelegate(server.ProcessRequest);

                remoteDel.BeginInvoke(message, remoteCallback, null);
            }

            // Waits until one replica returns a tuple or
            // all replicas answered that they dont have a match
            while (Tuple == null && AcksID.Count < View.Count) ;

            // Return first response.
            return Tuple;               
        }

        /// <summary>
        /// Returns a tuple from the distributed tuple space, deleting it.
        /// </summary>
        /// <param name="template">Template of the requested tuple.</param>
        /// <returns></returns>
        public ITuple Take(ITuple template)
        {
            /*------------------------------------------------
                Phase 1: Selecting the tuple to be removed 
             ------------------------------------------------*/

            // Send multicast request with the template to all replicas
            
            // Create request message.
            TSpaceMsg message = new TSpaceMsg();
            message.Code = "take1";
            message.Tuple = template;
            message.SequenceNumber = ++SequenceNumber;
            message.ProcessID = ClientID;


            // Repeat until all replicas have responded

            // Select one tuple from the intersection of all answers

            // If intersection = {}
            // send multicast request to release all replicas
            // repeat phase 1



            /*------------------------------------------------
                Phase 2: Removing the selected tuple
             ------------------------------------------------*/

            // Send multicast request to remove tuples to all members of the view

            //Repeat until all replicas have acknowledged deletion


            throw new NotImplementedException();
        }

      
        /***********************************************************
         *                  CALLBACK FUNCTIONS
         ***********************************************************/
        /// <summary>
        /// Callback function for the add operation.
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


            if(response.Code.Equals("ACK"))
                AcksID.Add(response.ProcessID);
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

    }
}
