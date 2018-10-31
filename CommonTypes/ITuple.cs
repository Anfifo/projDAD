using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    public interface ITuple
    {
        ITuple Add(Object field);

        ArrayList GetFields();

        int getLength();

        bool Matches(ITuple tuple);
    }
}
