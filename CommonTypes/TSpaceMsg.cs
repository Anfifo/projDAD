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

        public override string ToString()
        {
            return "\t* Message - Code: " + Code + " -- SequenceNumber: "+ SequenceNumber + " -- ProcessID:" + ProcessID +
                "\n\r\t* -- OperationID: " + OperationID + " -- RequestID: " + RequestID + "\n\r\t* -- Tuple: " + (Tuple != null) + 
                "-- TupleList: " + (Tuples != null) + "\n\r" ;
        }

    }
}
