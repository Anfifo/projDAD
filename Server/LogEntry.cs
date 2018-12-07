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
    public class LogEntry
    {
        /// <summary>
        /// Message received from client or another server
        /// </summary>
        public TSpaceMsg Request;
        /// <summary>
        /// Response prompted by the request received
        /// </summary>
        public TSpaceMsg Response;

        public bool Agreed { get; internal set; }
    }
}
