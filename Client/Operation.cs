using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

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

            this.buildFields();
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
                // if its a null
                if (splitfields[i] == "null") 
                {
                    Fields.Add(new Field(null));

                    continue;
                }
                // if it is a  class constructor
                if (splitfields[i].Contains('('))
                {
                    string ObjType = splitfields[i].Split('(')[0];

                    Object[] ConstructorArgs = getConstructorArgs(splitfields[i]);
                
                    Type t = Type.GetType("CommonTypes" + '.' + ObjType + "," + "CommonTypes");

                    Object field = Activator.CreateInstance(t, ConstructorArgs);

                    Fields.Add(new Field(field));

                    continue;
                }
                // if its a string
                if (splitfields[i].Contains("\"")) 
                {
                    string Field = splitfields[i].Replace("\"", string.Empty);

                    //if its general wildcard
                    if (Field == "*")
                    {
                        StringField field = new StringField(false, false, true, null);
                        Fields.Add(new Field(field));
                        continue;
                    }

                    //if its finalsubstring wildcard
                    if (Field[0] == '*')
                    {
                        StringField field = new StringField(false, true, false, Field);
                        Fields.Add(new Field(field));
                        continue;
                    }

                    //if initialsubtring wildcard
                    if (Field[Field.Length - 1] == '*')
                    {
                        StringField field = new StringField(true, false, false, Field);
                        Fields.Add(new Field(field));
                        continue;
                    }

                    Fields.Add(new Field(new StringField(false,false,false,Field)));
                    continue;

                }
                //if its a class type
                if (char.IsUpper(splitfields[i][0]))
                {
                    Type field = Type.GetType("CommonTypes" + '.' + splitfields[i] + "," + "CommonTypes");
                    Fields.Add(new Field(field));
                    continue;
                }
                // if int field
                else
                {
                    int field = Int32.Parse(splitfields[i]);

                    Fields.Add(field);

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
        public Object[] getConstructorArgs(string args)
        {
            ArrayList argList = new ArrayList();

            string[] split = args.Split('(', ')');

            string[] splitArgs = split[1].Split('|');


            for (int i = 0; i < splitArgs.Length; i++)
            {
                if (splitArgs[i].Contains("\"")) // string
                {
                    string Arg = splitArgs[i].Replace("\"", string.Empty);

                    argList.Add(Arg);

                    continue;
                }
                else // int
                {

                    int Arg = Int32.Parse(splitArgs[i]);

                    argList.Add(Arg);

                    continue;
                }
            }

            Object[] ConstructorArgs = new Object[argList.Count];

            for (int j = 0; j < ConstructorArgs.Length; j++)
            {
                ConstructorArgs[j] = argList[j];
            }

            return ConstructorArgs;

        }
    }
}
