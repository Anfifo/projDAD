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

        ITuple Take(ITuple tuple);

        ITuple Put(ITuple tuple);

        string GetName();

    }
}
