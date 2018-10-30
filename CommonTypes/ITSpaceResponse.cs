using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    interface ITSpaceResponse
    {
        //  TODO: Find correct type for getType
        string GetType();
        ITuple GetTuple();

    }
}
