using System;
using CommonTypes;
using System.Collections;
using System.Collections.Generic;

namespace Server
{
    /// <summary>
    /// Class representing a "View" on a server or client
    /// A view is a list of the current alive servers 
    /// </summary>
    [Serializable]
    public class TSpaceState
    {

        public TSLog ProcessedRequests;
        public int SequenceNumber;
        public View ServerView;
        public List<Message> MessageQueue;
        public List<ITuple> TupleSpace;
        public List<string> LockedTuplesKeys;
        public List<List<ITuple>> LockedTuplesValues;
        public List<ITuple> Freezer;

    }
}
