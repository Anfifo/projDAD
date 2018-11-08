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
        public string ID
        {
            get; set;
        }
    }
}
