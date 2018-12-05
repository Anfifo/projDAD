using System;
using System.Collections.Generic;
namespace CommonTypes
{
    [Serializable]
    public class TSpaceMsg
    {
        public TSpaceMsg(){}


        public string Code
        {
            get; set;
        }

       
        public List<ITuple> Tuples
        {
            get;set;
        }

        public ITuple Tuple
        {
            get; set;
        }

        public int SequenceNumber
        {
            get; set;
        }

        public int ProcessID
        {
            get; set;
        }
        public string OperationID
        {
            get; set;
        }

        public string RequestID
        {
            get; set;
        }

        public View MsgView
        {
            get; set;
        }

        public override string ToString()
        {
            return "\t* Code: " + Code + "  SeqNr: "+ SequenceNumber + "  PID:" + ProcessID +
                " OpID: " + OperationID + "\n\r\t* ReqID: " + RequestID + " Tup: " + (Tuple != null) + 
                " Tup[]: " + (Tuples != null) + "\n\r \t* " + "Suspected Dead: " +
                (SuspectedDead != null ? SuspectedDead + " -> " + (Dead) : " none") + "\n\r \t* " +
                 (MsgView != null? " ViewID :" + MsgView.ID : " no view");
        }

        public string SuspectedDead { get; set; }

        public bool Dead { get; set; }

    }
}
