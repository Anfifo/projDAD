using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    public class TSpaceMsg
    {
        string Code
        {
            get; set;
        }

        string Type
        {
            get; set;
        }

        ITuple Tuple
        {
            get;set;
        }

        int SequenceNumber
        {
            get; set;
        }

        int ID
        {
            get; set;
        }



    }
}
