using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    public interface ITuple
    {
        ITuple Add(IField field);

        List<IField> GetFields();

        int getLength();

        bool Matches(ITuple tuple);
    }
}
