using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    [Serializable]
    public class TSpaceMsg
    {
        
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
        public int ID
        {
            get; set;
        }

        

    }
}
