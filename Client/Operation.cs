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
        //fields of the operation
        string fields;
        //name of the operation
        string type;
        //fields after parsing
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
        /// <summary>
        /// parse the string of fields
        /// </summary>
        public void buildFields()
        {
            //get rid of the commas inside ClassTest(,)
            fields = replace(fields);

            //now that the commas are removed, we can split the fields using the characther ','
            string[] splitfields = fields.Split(',');

            // get tuple field objects
            for (int i = 0; i < splitfields.Length; i++)
            {
                // if its a null
                if (splitfields[i] == "null") 
                {
                    //add to the list
                    Fields.Add(new Field(new NullValue()));

                    continue;
                }
                // if it is a  class constructor
                if (splitfields[i].Contains('('))
                {
                    //get name of the type of the class
                    string ObjType = splitfields[i].Split('(')[0];

                    //parse and get the list of objects that the constructor receives

                    Object[] ConstructorArgs = getConstructorArgs(splitfields[i]);
                
                    //get the object type
                    Type t = Type.GetType("CommonTypes" + '.' + ObjType + "," + "CommonTypes");

                    //create an instance of that object 
                    Object field = Activator.CreateInstance(t, ConstructorArgs);

                    //add to the list
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
                        StringValue field = new StringValue(false, false, true, null);
                        //add to the list
                        Fields.Add(new Field(field));
                        continue;
                    }

                    //if its finalsubstring wildcard
                    if (Field[0] == '*')
                    {
                        StringValue field = new StringValue(false, true, false, Field);
                        //add to the list
                        Fields.Add(new Field(field));
                        continue;
                    }

                    //if initialsubtring wildcard
                    if (Field[Field.Length - 1] == '*')
                    {
                        StringValue field = new StringValue(true, false, false, Field);
                        //add to the list
                        Fields.Add(new Field(field));
                        continue;
                    }

                    //if its not an wildcard its just a normal string
                    //add to the list
                    Fields.Add(new Field(new StringValue(false,false,false,Field)));
                    continue;

                }
                //if its a class type
                if (char.IsUpper(splitfields[i][0]))
                {
                    Type field = Type.GetType("CommonTypes" + '.' + splitfields[i] + "," + "CommonTypes");
                    //add to the list
                    Fields.Add(new Field(field));
                    continue;
                }
                // if int field
                else
                {
                    int field = Int32.Parse(splitfields[i]);

                    //add to the list
                    Fields.Add(field);

                    continue;
                }
            }

        }

        // replace commas between ( ) with " | "
        public string replace(string S)
        {
            //split between ( )
            string[] splitS = S.Split('(', ')');

            //if there are no parathensis then there is no need to replace anything
            if (splitS.Length == 1)
            {
                return S;
            }

            //initialize the new string
            string newS = "";

            //iterate trough the splited string
            for (int i = 0; i < splitS.Length; i++)
            {
                //if we are on the last step we need to add the rest of the string
                if (i == splitS.Length - 1)
                {
                    newS = newS + splitS[i];
                    continue;
                }

                // the content between ( ) will always be on the even position of the string
                if (i % 2 != 0)
                {
                    //replace the commas with '|'
                    splitS[i] = splitS[i].Replace(",", "|");

                    // add to the new string, reconstruting what we initially add
                    newS = newS + splitS[i - 1] + '(' + splitS[i] + ')';
                }

            }
            return newS;
        }

        // get arguments for constructor 
        public Object[] getConstructorArgs(string args)
        {
            //arguments parsed
            ArrayList argList = new ArrayList();

            //split objectname of the args
            string[] split = args.Split('(', ')');

            //split the args
            string[] splitArgs = split[1].Split('|');

            //iterate trough the args
            for (int i = 0; i < splitArgs.Length; i++)
            {
                if (splitArgs[i].Contains("\"")) // string
                {
                    //for the strings replace the " " " with nothing
                    string Arg = splitArgs[i].Replace("\"", string.Empty);

                    //add to the arglist
                    argList.Add(Arg);

                    continue;
                }
                else // int
                {
                    // get the int
                    int Arg = Int32.Parse(splitArgs[i]);

                    //add to the arglist
                    argList.Add(Arg);

                    continue;
                }
            }

            //initialize a vector of Objects since this is the type of object we need to use reflection
            Object[] ConstructorArgs = new Object[argList.Count];

            //iterate trough the arraylist and add each element to the vector
            for (int j = 0; j < ConstructorArgs.Length; j++)
            {
                ConstructorArgs[j] = argList[j];
            }

            return ConstructorArgs;

        }
    }
}
