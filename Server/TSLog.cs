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
    public class TSLog
    {

        public List<LogEntry> Log = new List<LogEntry>();

        public void Add(TSpaceMsg request)
        {
            LogEntry entry = new LogEntry
            {
                Request = request
            };

            Log.Add(entry);
        }

        public void Add(LogEntry entry)
        {
            Log.Add(entry);
        }

        public bool Contains(String ID)
        {
            return GetByKey(ID) != null;
        }

        public LogEntry GetByKey(String ID)
        {
            foreach(LogEntry entry in Log){
                if (entry.Request.RequestID == ID || entry.Request.OperationID == ID)
                    return entry;
            }
            return null;
        }

        public void UpdateResponse(String id, TSpaceMsg response)
        {
            foreach (LogEntry entry in Log)
            {
                if (entry.Request.RequestID == id)
                {
                    entry.Response = response;
                }
            }
        }

        public void UpdateView(String id, View view)
        {
            foreach (LogEntry entry in Log)
            {
                if (entry.Request.RequestID == id)
                {
                    entry.Request.MsgView = view;
                    entry.Response.MsgView = view;
                }
            }
        }
    }
}
