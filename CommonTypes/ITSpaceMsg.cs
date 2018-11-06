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

        public string Type
        {
            get; set;
        }

        public ITuple Tuple
        {
            get;set;
        }

        public int SequenceNumber
        {
            get; set;
        }

        public int ClientID
        {
            get; set;
        }
        public int ID
        {
            get; set;
        }

        

    }
}
