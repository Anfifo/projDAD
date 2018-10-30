using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    public interface IField
    {
        //  TODO: Find correct type for getType
        object GetType();
        void SetType(object type);

        object GetValue();

        void SetValue(object value);

        bool Matches(IField field);
    }
}
