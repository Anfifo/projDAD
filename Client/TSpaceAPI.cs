using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Client
{
    interface ITSpaceAPI
    {
        ITuple Read(ITuple template);

        ITuple Take(ITuple template);

        void Add(ITuple tuple);
    }
}
