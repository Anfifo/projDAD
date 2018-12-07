using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    /// <summary>
    /// Interface that represents the operations offered by a server hosting a TupleSpace
    /// </summary>
    public interface ITSpaceServer
    {
        string Status();
        void Freeze();
        void Unfreeze();
        TSpaceMsg ProcessRequest(TSpaceMsg msg);
        bool Ping(string serverURL);
        void UpdateView();

        List<ITuple> GetTuples();

    }
}
