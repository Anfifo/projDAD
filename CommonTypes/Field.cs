using System;

namespace CommonTypes
{   
    [Serializable]
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
            Console.WriteLine("Template type: " + GetFieldType().ToString());
            Console.WriteLine("Tuple type: " + field.GetFieldType().ToString());

            if (value is StringValue)
            {
                if(!(field.GetValue() is StringValue))
                {
                    return false;
                }

                StringValue myStringField = (StringValue)this.value;

                if (myStringField.AnyString)
                {
                    Console.WriteLine("Anystring");

                    return true;
                }
                if (myStringField.InitialSubString)
                {
                    Console.WriteLine("InitialSubstring");
                    string myString = myStringField.field;

                    StringValue inputStringField = (StringValue)field.GetValue();

                    string inputString = inputStringField.field;

                    if (inputString.Substring(0, myString.Length - 1) == myString.Substring(0, myString.Length - 1))
                    {
                        Console.WriteLine("they MATCH");
                        return true;
                    }
                    Console.WriteLine("they dont MATCH");
                    return false;

                }

                if (myStringField.FinalSubString)
                {
                    Console.WriteLine("FinalSubstring");
                    string myString = myStringField.field;

                    StringValue inputStringField = (StringValue)field.GetValue();

                    string inputString = inputStringField.field;

                    if (inputString.Substring(inputString.Length - myString.Length + 1) == myString.Substring(1))
                    {
                        return true;
                    }
                    return false;

                }

                else
                {

                    StringValue sf = (StringValue)field.GetValue();

                    string s = sf.field;

                    string S = myStringField.field;

                    if(s == S)
                    {
                     
                        return true;
                    }
                    return false;
                }
            }
            
            // Class type
            if (value.GetType().ToString() == "System.RuntimeType" )
            {

                Type valueType = (Type)value;

                return (valueType.ToString() == field.GetValue().ToString());

            }
            // Object
            else
            {
                if(value is NullValue)
                {
                    Console.WriteLine("Value == null");
                    Type StringField = Type.GetType("CommonTypes" + "." + "StringValue,CommonTypes");

                    return !field.GetFieldType().ToString().Equals(StringField.ToString());
                    
                }

                if (field is StringValue)
                    return false;

                object[] o = new object[1];

                 Type t = Type.GetType(value.GetType().ToString());

                 o[0] = value;

                 return (Boolean)field.GetValue().GetType().GetMethod("Equals").Invoke(field.GetValue(), o);

               
            }

        }
    }
}
