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
                    Fields.Add(new Field(null));

                    continue;
                }
                if (splitfields[i].Contains('(')) // if a class constructor
                {
                    string ObjType = splitfields[i].Split('(')[0];

                    Object[] ConstructorArgs = getConstructorArgs(splitfields[i]);
                
                    Type t = Type.GetType("CommonTypes" + '.' + ObjType + "," + "CommonTypes");

                    Object field = Activator.CreateInstance(t, ConstructorArgs);

                    Fields.Add(new Field(field));

                    continue;
                }
                if (splitfields[i].Contains("\"")) // if string
                {
                    string Field = splitfields[i].Replace("\"", string.Empty);

                    if (Field == "*")
                    {
                        StringField field = new StringField(false, false, true, null);
                        Fields.Add(new Field(field));
                        continue;
                    }

                    if (Field[0] == '*')
                    {
                        StringField field = new StringField(false, true, false, Field);
                        Fields.Add(new Field(field));
                        continue;
                    }


                    if (Field[Field.Length - 1] == '*')
                    {
                        StringField field = new StringField(false, true, false, Field);
                        Fields.Add(new Field(field));
                        continue;
                    }

                    Fields.Add(new Field(new StringField(false,false,false,Field)));
                    continue;

                }
                if (char.IsUpper(splitfields[i][0]))
                {
                    Type field = Type.GetType("CommonTypes" + '.' + splitfields[i] + "," + "CommonTypes");
                    Fields.Add(new Field(field));
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
}
