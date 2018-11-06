using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    public class StringField
    {
        Boolean InitialSubString;
        Boolean FinalSubString;
        Boolean AnyString;
        String field;


        public StringField(Boolean IS, Boolean FS, Boolean AS, String f)
        {
            InitialSubString = IS;
            FinalSubString = FS;
            AnyString = AS;
            field = f;
        }
    }
}
