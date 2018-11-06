using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    public class StringField
    {
        public Boolean InitialSubString;
        public Boolean FinalSubString;
        public Boolean AnyString;
        public String field;


        public StringField(Boolean IS, Boolean FS, Boolean AS, String f)
        {
            InitialSubString = IS;
            FinalSubString = FS;
            AnyString = AS;
            field = f;
        }
    }
}
