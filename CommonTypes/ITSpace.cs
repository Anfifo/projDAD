using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    public interface ITSpace
    {
        ITuple Read(ITuple tuple);

        ITuple Take(ITuple tuple);

        void Put(ITuple tuple);

        string GetName();
    }
}
