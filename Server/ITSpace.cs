using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Server
{
    interface ITSpace
    {
        ITuple Read(ITuple tuple);

        List<ITuple> Take1(ITuple tuple);

        Boolean Take2(ITuple tuple);

        void Add(ITuple tuple);

    }
}
