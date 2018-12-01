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
        internal static List<ITSpaceServer> View { get; set; } = new List<ITSpaceServer>();

        private static View ServerView = new View();

        // OperationID of the tuple spaces servers view.
        internal int ViewId { get; set; }

        // Variable tracking the validity of the view
        internal static bool InvalidView { get; set; }

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

        // The view suggested by servers
        internal static View SuggestedView = null;

        // Stores the matching tuples returned by all tuple space servers
        internal static List<List<ITuple>> MatchingTuples = new List<List<ITuple>>();

        // Object to use as reference for the lock to the Tuple 
        internal static Object LockRef = new Object();

        internal static Object ViewLock = new Object();

        //Log variables
        internal static int TakeCounter = 0;
        internal static int AddCounter = 0;
        internal static int ReadCounter = 0;

        internal static bool verbose = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="viewUrls">Url of the tuple space servers.</param>
        public AbstractClient(List<string> viewUrls, int clientID)
        {
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);

            // Get the reference for the tuple space servers
            SetNewView(new View(viewUrls, -1));
            
            // Set the client unique identifier
            ClientID = clientID;
        }
        
        public View GetCurrentView()
        {
            return ServerView;
        }

        public static bool ValidView(TSpaceMsg msg)
        {
            lock(ServerView){

                if(msg.MsgView == null)
                {
                    Console.WriteLine("NO VIEW SENT WITH MESSAGE!!");
                    return false;
                }

                if (msg.Code.Equals("badView") && msg.MsgView.ID > ServerView.ID)
                {
                   
                    InvalidView = true;
                    SuggestView(msg.MsgView);
                    return false;
                }
            }
            return true;
        }

        public static bool CheckNeedUpdateView()
        {
            if (InvalidView && SuggestedView != null)
            {
                DebugPrint("Updating view to :" +  SuggestedView.ID);
                SetNewView(SuggestedView);
                SuggestedView = null;
                InvalidView = false;
                return true;
            }

            return false;
        }


        public static void SetNewView(View view)
        {
            DebugPrint("Setting view to:" + view);
            //Clear previous view
            View.Clear();

            ServerView = view;
            ITSpaceServer server = null;
            
            // Get the reference for the tuple space servers
            foreach (string serverUrl in ServerView.GetUrls())
            {
                DebugPrint(serverUrl);
                server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), serverUrl);

                // Check if its a valid reference
                View.Add(server);
                Console.WriteLine("Sucessfully connected to " + serverUrl);
            }
            
            Console.WriteLine("View count = " + View.Count);
        }

        public static void SuggestView(View view)
        {
            DebugPrint("Suggesting View: " + view);
            if(SuggestedView == null || view.ID > SuggestedView.ID)
                SuggestedView = view;
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
        public static void DebugPrint(string text)
        {
            Console.WriteLine(text);
        }

    }


}

