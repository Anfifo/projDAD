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

        void Put(ITuple tuple);

        //TODO: figure wtf view is
        object GetView();

        void UpdateView(object group);
    }
}
