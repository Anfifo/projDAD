using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    public interface ITSpaceServer
    {
        string Status();
        void Freeze();
        void Unfreeze();
        TSpaceMsg ProcessRequest(TSpaceMsg msg);
        bool Ping(string serverURL);
        View UpdateView();

        List<ITuple> GetTuples();
    }
}
