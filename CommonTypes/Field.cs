using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    public class Field : IField
    {
        object value;

        public Field(object Value)
        {
            value = Value;
        }

        public Type GetFieldType()
        {
            return value.GetType();
        }


        public object GetValue()
        {
            return value;
        }

        public bool Matches(IField field)
        {

            if (field.GetFieldType().ToString() != this.GetFieldType().ToString())
            {
                return false;

            }

            if (value is StringField)
            {
                Console.WriteLine("here1");
                StringField fieldString = (StringField)field.GetValue();

                if (fieldString.AnyString)
                {
                    return true;
                }
                if (fieldString.InitialSubString)
                {
                    StringField sf = (StringField)value;

                    string s = sf.field;

                    string iS = fieldString.field;

                    if(s.Substring(0,iS.Length-1) == iS)
                    {
                        return true;
                    }
                    return false;

                }

                if (fieldString.FinalSubString)
                {
                    Console.WriteLine("here2");
                    StringField mystringfield = (StringField)value;

                    string mystring = mystringfield.field;
                    
                    string input = fieldString.field;

                    if (input.Substring(input.Length - mystring.Length - 1, input.Length - 1) == mystring);

                }

                else
                {
                    StringField sf = (StringField)value;

                    string s = sf.field;

                    string S = fieldString.field;

                    if(s == S)
                    {
                        return true;
                    }
                    return false;
                }
            }

            if (value is Type)
            {
                Type valueType = (Type)value;

                return field.GetType().ToString() == valueType.ToString();
            }
            else
            {
                /* object[] o = new object[1];

                 Type t = Type.GetType(value.GetType().ToString());


                 return (Boolean)field.GetValue().GetType().GetMethod("Equals").Invoke(field.GetValue(), o);

               */
            }


            return false;
        }
    }
}
