using CommonTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    [Serializable]
    public class Message : IComparable<Message>
    {
        // Request operation message
       
        public TSpaceMsg Request { get; set; }

        // Message OperationID
        public string MessageID { get; set; }

        // OperationID of the sender process
        public string ProcessID { get; set; }

        // Message sequence number
        public int SequenceNumber { get; set; }

        public bool Deliverable { get; set; }


        // Default comparator
        public int CompareTo(Message other)
        {
            //If two sequence numbers are the same
            if (SequenceNumber == other.SequenceNumber)
            {
                // Undeliverble message is smaller
                if (Deliverable && !other.Deliverable)
                    return 1;

                // Smaller process id
                return ProcessID.CompareTo(other.ProcessID);
            }

            // Msg with smaller sequence number is smaller
            return SequenceNumber.CompareTo(other.SequenceNumber);
            
        }
    }
}
