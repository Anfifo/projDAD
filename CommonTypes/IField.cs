using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    /// <summary>
    /// Represents the content of a tuple
    /// </summary>
    public interface IField
    {
        Type GetFieldType();
        
        object GetValue();

        bool Matches(IField field);
    }
}
