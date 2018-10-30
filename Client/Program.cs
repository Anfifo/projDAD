using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Client
{
    class Program
    {
        public static ArrayList Operations = new ArrayList();

        static void Main(string[] args)
        {
            StreamReader reader = File.OpenText("script.txt");

            string line;

            while ((line = reader.ReadLine()) != null)
            {
                string[] items = line.Split('\t');
                string[] fields = null;
                foreach (string item in items)
                {
                    fields = item.Split('<','>');
                }
                    fields[0] = fields[0].Replace(" ", string.Empty);
                    switch (fields[0])
                    {
                        case "add":
                            Operations.Add(new Operation(fields[0],fields[1].Replace(" ",string.Empty)));
                            break;
                        case "read":
                            Operations.Add(new Operation(fields[0],fields[1].Replace(" ", string.Empty)));
                            break;
                        case "take":
                            Operations.Add(new Operation(fields[0],fields[1].Replace(" ", string.Empty)));
                            break;
                        case "wait":
                            Operations.Add(new Operation(fields[0],fields[1].Replace(" ", string.Empty)));
                            break;
                        case "begin-repeat":
                            Operations.Add(new Operation(fields[0],fields[1].Replace(" ", string.Empty)));
                            break;
                        case "end-repeat":
                            Operations.Add(new Operation(fields[0],fields[1].Replace(" ", string.Empty)));
                            break;

                    }

            }

            for (int i = 0; i < Operations.Count; i++)
            {
                Operation op = (Operation)Operations[i];

                Console.WriteLine(op.getType());

                op.getArgs();

                foreach (Object o in op.getArguments())
                {
                   Console.WriteLine(o.GetType() + "   " + o.ToString());
                }

                Console.WriteLine();
            } 

            Console.ReadLine();
        }

    }

   public class Operation
    {
        string fields;
        string type;
        ArrayList arguments = new ArrayList();

        public Operation(string _type, string _fields)
        {
            type = _type;
            fields = _fields;
        }

        public string getFields()
        {
            return fields;
        }

        public string getType()
        {
            return type;
        }

        public ArrayList getArguments()
        {
            return arguments;
        }

        public void getArgs()
        {
            string[] splitarguments = fields.Split(',');

            for(int i = 0;i < splitarguments.Length; i++)
            {
                if (splitarguments[i].Contains('(')) /* bad condition */
                {
                    string ObjType = splitarguments[i].Split('(')[0];
                    
                    string ClassArgs = splitarguments[i].Split('(')[1];


                    //Object[] ConArgs = new Object[9999]; /* i am sick please understand dont want to think */

                    //ConArgs = GetConArgs(fields);

                    Type t = Type.GetType("Client" + '.' + ObjType);
                    Object o = Activator.CreateInstance(t);
                    arguments.Add(o);
                    continue;
                }
                if (splitarguments[i].Contains("\"")) /* bad condition */
                {
                    string arg = splitarguments[i].Replace("\"", string.Empty);

                    arguments.Add(arg);

                    continue;
                }
                else /* bad condition */
                {
                    int Arg = Int32.Parse(splitarguments[i]);

                    arguments.Add(Arg);

                    continue;
                }
            }
        }

        public Object[] GetConArgs(string Args)
        {


            return new Object[1];
           
        }
    }

    public class ClassTest
    {

        public ClassTest()
        {
        }
    }
}
