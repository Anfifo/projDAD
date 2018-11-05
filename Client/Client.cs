using System;
using System.Collections;
using System.IO;
using System.Linq;
using Tuple = CommonTypes.Tuple;

namespace Client
{
    class Client
    {
        public static ArrayList Operations = new ArrayList();

        static void Main(string[] args)
        {
            //StreamReader reader = File.OpenText(args[0]);

            StreamReader reader = File.OpenText("script.txt");

            string line;

            while ((line = reader.ReadLine()) != null)
            {
                string[] splitLine = line.Split('\t');
                string[] fields = null;

                fields = splitLine[0].Split('<', '>');

                // if doesnt have field then its not a tuple operation
                if (fields.Length == 1)
                {

                    fields = fields[0].Split(' ');

                }
                if (fields.Length == 1 & fields[0] == "end-repeat")
                {
                    // end-repeat
                    Operations.Add(new Operation(fields[0], "0"));
                }

                switch (fields[0])
                {
                    case "wait":
                        Operations.Add(new Operation(fields[0], fields[1].Replace(" ", string.Empty)));
                        break;
                    case "begin-repeat":
                        Operations.Add(new Operation(fields[0], fields[1].Replace(" ", string.Empty)));
                        break;

                }
                fields[0] = fields[0].Replace(" ", string.Empty);

                switch (fields[0])
                {
                    case "add":
                        Operations.Add(new Operation(fields[0], fields[1].Replace(" ", string.Empty)));
                        break;
                    case "read":
                        Operations.Add(new Operation(fields[0], fields[1].Replace(" ", string.Empty)));
                        break;
                    case "take":
                        Operations.Add(new Operation(fields[0], fields[1].Replace(" ", string.Empty)));
                        break;

                }

            }

            for (int i = 0; i < Operations.Count; i++)
            {
                Operation op = (Operation)Operations[i];

                op.buildFields();

            }

            ExecuteOperations(Operations);

            Console.ReadLine();
        }

        static void ExecuteOperations(ArrayList Operations)
        {
            for (int i = 0; i < Operations.Count; i++)
            {
                Operation Op = (Operation)Operations[i];
                switch (Op.getType())
                {
                    case "add":

                        Console.WriteLine("WE ADDING");

                        Tuple tuple = new Tuple(Op.getFields());

                        break;

                    case "take":

                        Console.WriteLine("WE TAKING");

                        Tuple Tuple = new Tuple(Op.getFields());

                        break;

                    case "read":

                        Console.WriteLine("WE READING");

                        Tuple Tupl3 = new Tuple(Op.getFields());

                        break;

                    case "wait":


                        Console.WriteLine("WE WAITING");

                        System.Threading.Thread.Sleep((int)Op.getFields()[0]);

                        Console.WriteLine("WE STOPPED WAITING");

                        break;

                    case "begin-repeat":

                        int TimesToRepeat = (int)Op.getFields()[0];

                        Console.WriteLine("WE REPEATING" + " " + TimesToRepeat);

                        ArrayList OperationsToBeRepeated = new ArrayList();
                        for (int j = i+1; j < Operations.Count; j++)
                        {

                            Operation O = (Operation)Operations[j];


                            if (O.getType() == "end-repeat")
                            {
                                break;
                            }


                            OperationsToBeRepeated.Add(Operations[j]);
                            i++;

                        }


                        for (int t = 0; t < TimesToRepeat; t++)
                        {
                            ExecuteOperations(OperationsToBeRepeated);
                        }



                        break;
                    case "end-repeat":
                        Console.WriteLine("WE STOPPED REPEATING");
                        break;
                }
            }
        }

    }

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

    public class ClassTest
    {

        public ClassTest(int i, string l, int j, string O)
        {
            Console.WriteLine("I:" + i);
            Console.WriteLine("l:" + l);
            Console.WriteLine("J:" + j);
            Console.WriteLine("O:" + O);
        }
    }

    public class DADTestA
    {
        public int i1;
        public string s1;

        public DADTestA(int pi1, string ps1)
        {
            i1 = pi1;
            s1 = ps1;

            Console.WriteLine(i1.ToString() +  s1.ToString() + this.GetType());
        }
        public bool Equals(DADTestA o)
        {
            if (o == null)
            {
                return false;
            }
            else
            {
                return ((this.i1 == o.i1) && (this.s1.Equals(o.s1)));
            }
        }
    }

    public class DADTestB
    {
        public int i1;
        public string s1;
        public int i2;

        public DADTestB(int pi1, string ps1, int pi2)
        {
            i1 = pi1;
            s1 = ps1;
            i2 = pi2;

            Console.WriteLine(i1.ToString() + s1.ToString() + i2.ToString() + this.GetType());

        }

        public bool Equals(DADTestB o)
        {
            if (o == null)
            {
                return false;
            }
            else
            {
                return ((this.i1 == o.i1) && (this.s1.Equals(o.s1)) && (this.i2 == o.i2));
            }
        }
    }

    public class DADTestC
    {
        public int i1;
        public string s1;
        public string s2;

        public DADTestC(int pi1, string ps1, string ps2)
        {
            i1 = pi1;
            s1 = ps1;
            s2 = ps2;

            Console.WriteLine(i1.ToString() + s1.ToString() + s2.ToString() + this.GetType());
        }

        public bool Equals(DADTestC o)
        {
            if (o == null)
            {
                return false;
            }
            else
            {
                return ((this.i1 == o.i1) && (this.s1.Equals(o.s1)) && (this.s2.Equals(o.s2)));
            }
        }
    }
}