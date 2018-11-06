using System;
using System.Collections.Generic;
using CommonTypes;
using System.Runtime.Remoting;

namespace Client
{
    class XL_Client : ITSpaceAPI
    {
        // View of the tuple spaces servers.
        private List<ITSpaceServer> View { get; set; }

        // ID of the tuple spaces servers view.
        private int ViewId { get; set; }

        // Sequence number of the last request
        private int SequenceNumber;

        // Client ID
        private readonly int ClientID;

        // Delegate for remote assync call to the tuple space servers
        delegate TSpaceMsg RemoteAsyncDelegate(TSpaceMsg request);

        // Counter for the number of acknowledgements received 
        private int AckCounter;


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
            TSpaceMsg message = new TSpaceMsg();

            message.Code = "add";
            message.Tuple = tuple;
            message.SequenceNumber = ++SequenceNumber;
            message.ClientID = ClientID;

            

            RemoteAsyncDelegate remoteDel;


            
            // Create local callback
            AsyncCallback remoteCallback = new AsyncCallback(XL_Client.RemoteAsyncCallback);

            //Send multicast message to all members of the view
            foreach (ITSpaceServer server in View) {

                // Create delegate for remote method
                remoteDel = new RemoteAsyncDelegate(server.ProcessRequest);

                // Call remote method
                remoteDel.BeginInvoke(message, remoteCallback, null);
            }
            // Put request is repeated until all replicas have acknowledge receipt

            
        }

        /// <summary>
        /// Returns a tuple from the distributed tuple space, whithout deleting it.
        /// </summary>
        /// <param name="template">Template of the requested tuple.</param>
        /// <returns></returns>
        public ITuple Read(ITuple template)
        {
            // Send multicast message to all members of the view

            // Return first response

            throw new NotImplementedException();
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

        public static void RemoteAsyncCallback(IAsyncResult result)
        {

        }
    }
}
