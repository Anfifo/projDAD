using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    [Serializable]
    public class StringValue
    {
        public Boolean InitialSubString;
        public Boolean FinalSubString;
        public Boolean AnyString;
        public String field;


        public StringValue(Boolean IS, Boolean FS, Boolean AS, String f)
        {
            InitialSubString = IS;
            FinalSubString = FS;
            AnyString = AS;
            field = f;
        }
    }
}
