using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    public interface IField
    {
        Type GetType();
        
        object GetValue();

        void SetValue(object value);

        bool Matches(IField field);
    }
}
