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
    public class SMRState
    {
        
        public List<string> ProcessedRequests { get => ProcessedRequests; set => ProcessedRequests = value; }
        public int SequenceNumber { get => SequenceNumber; set => SequenceNumber = value; }
        public View ServerView { get => ServerView; set => ServerView = value; }
        public List<Message> MessageQueue { get => MessageQueue; set => MessageQueue = value; }
        public List<ITuple> TupleSpace { get => TupleSpace; set => TupleSpace = value; }


    }
}
