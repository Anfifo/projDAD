using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Operation
    {
        string fields;
        string type;
        ArrayList Fields = new ArrayList();

        public Operation(string _type, string _fields)
        {
            type = _type;
            fields = _fields;
        }


        public string getType()
        {
            return type;
        }

        public ArrayList getFields()
        {
            return Fields;
        }

        public void buildFields()
        {
            fields = replace(fields);
            string[] splitfields = fields.Split(',');
            // get tuple field objects
            for (int i = 0; i < splitfields.Length; i++)
            {

                if (splitfields[i] == "null") // if null
                {
                    Fields.Add(null);

                    continue;
                }
                if (splitfields[i].Contains('(')) // if a class constructor
                {
                    string ObjType = splitfields[i].Split('(')[0];

                    Object[] ConstructorArgs = getConstructorArgs(splitfields[i]);

                    Type t = Type.GetType("Client" + '.' + ObjType);

                    Object Field = Activator.CreateInstance(t, ConstructorArgs);

                    Fields.Add(Field);

                    continue;
                }
                if (splitfields[i].Contains("\"")) // if string
                {
                    string field = splitfields[i].Replace("\"", string.Empty);

                    if (field == "*")
                    {
                        StringField Field = new StringField(false, false, true, null);
                        Fields.Add(Field);
                        continue;
                    }

                    if (field[0] == '*')
                    {
                        StringField Field = new StringField(false, true, false, field);
                        Fields.Add(Field);
                        continue;
                    }


                    if (field[field.Length - 1] == '*')
                    {
                        StringField Field = new StringField(false, true, false, field);
                        Fields.Add(Field);
                        continue;
                    }

                    Fields.Add(field);
                    continue;

                }
                if (char.IsUpper(splitfields[i][0]))
                {
                    Type field = Type.GetType("Client" + '.' + splitfields[i]);
                    Fields.Add(fields);
                    continue;
                }
                else // if int
                {
                    int Field = Int32.Parse(splitfields[i]);

                    Fields.Add(Field);

                    continue;
                }
            }

        }

        // replace commas between ( ) with " | "
        public string replace(string S)
        {
            string[] splitS = S.Split('(', ')');

            if (splitS.Length == 1)
            {
                return S;
            }
            string newS = "";

            for (int i = 0; i < splitS.Length; i++)
            {
                if (i == splitS.Length - 1)
                {
                    newS = newS + splitS[i];
                    continue;
                }

                if (i % 2 != 0)
                {
                    splitS[i] = splitS[i].Replace(",", "|");

                    newS = newS + splitS[i - 1] + '(' + splitS[i] + ')';
                }

            }
            return newS;
        }

        // get arguments for constructor 
        public Object[] getConstructorArgs(string Args)
        {
            ArrayList ArgList = new ArrayList();

            string[] split = Args.Split('(', ')');

            string[] splitArgs = split[1].Split('|');


            for (int i = 0; i < splitArgs.Length; i++)
            {
                if (splitArgs[i].Contains("\"")) // string
                {
                    string Arg = splitArgs[i].Replace("\"", string.Empty);

                    ArgList.Add(Arg);

                    continue;
                }
                else // int
                {

                    int Arg = Int32.Parse(splitArgs[i]);

                    ArgList.Add(Arg);

                    continue;
                }
            }

            Object[] ConstructorArgs = new Object[ArgList.Count];

            for (int j = 0; j < ConstructorArgs.Length; j++)
            {
                ConstructorArgs[j] = ArgList[j];
            }

            return ConstructorArgs;

        }
    }

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
