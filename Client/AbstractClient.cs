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
    abstract class AbstractClient
    {
        // View of the tuple spaces servers.
        internal List<ITSpaceServer> View { get; set; } = new List<ITSpaceServer>();

        private View ServerView;

        // OperationID of the tuple spaces servers view.
        internal int ViewId { get; set; }

        // Sequence number of the last request sent
        internal static int SequenceNumber;

        // Client OperationID
        internal readonly int ClientID;


        // Delegate for remote assync call to the tuple space servers
        internal delegate TSpaceMsg RemoteAsyncDelegate(TSpaceMsg request);

        // Counter for the number of acknowledgements received 
        internal static int AcksCounter;

        // Stores the tuple returned by the
        internal static ITuple Tuple;

        // Stores the matching tuples returned by all tuple space servers
        internal static List<List<ITuple>> MatchingTuples = new List<List<ITuple>>();

        // Object to use as reference for the lock to the Tuple 
        internal static Object LockRef = new Object();

        //Log variables
        internal static int TakeCounter = 0;
        internal static int AddCounter = 0;
        internal static int ReadCounter = 0;

        internal static bool verbose = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="viewUrls">Url of the tuple space servers.</param>
        public AbstractClient(List<string> viewUrls, int viewId, int clientID)
        {
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);

            // Get the reference for the tuple space servers
            UpdateView(viewUrls, viewId);
            
            // Set the client unique identifier
            ClientID = clientID;
        }

        /// <summary>
        /// Updates view of servers
        /// </summary>
        /// <param name="viewURLs">List of the view's servers URLs</param>
        /// <param name="viewID">View version number</param>
        public void UpdateView(List<string> viewURLs, int viewID)
        {
            //Clear previous view
            View.Clear();

            ServerView = new View(viewURLs);

            ITSpaceServer server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), viewURLs[0]);
            viewURLs = server.UpdateView();
            View.Add(server);
            
            // Get the reference for the tuple space servers
            foreach (string serverUrl in viewURLs)
            {

                server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), serverUrl);

                // Check if its a valid reference
                try
                {
                    View.Add(server);
                    Console.WriteLine("Sucessfully connected to " + serverUrl);

                }
                catch (System.Net.Sockets.SocketException)
                {
                    Console.WriteLine("Failed to connect to  " + serverUrl);
                }
            }

            Console.WriteLine("View count = " + View.Count);
            ViewId = viewID;
        }



        /****************************************************************
         *                     AUX FUNCTIONS / CLASSES
         ****************************************************************/


        /// <summary>
        /// Class that implements a custom equality comparator for the tuples.
        /// </summary>
        internal class TupleComparator : IEqualityComparer<ITuple>
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

