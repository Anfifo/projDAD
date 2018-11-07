using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Tuple = CommonTypes.Tuple;

namespace Client
{
    class Client
    {
        public static ArrayList Operations = new ArrayList();

        public static List<string> L = new List<string>();

        public static ITSpaceAPI XL;

        static void Main(string[] args)
        {
            L.Add("tcp://localhost:50001/S");
            XL = new XL_Client(L, 2);
            try
            {
                ExecuteFile(args[0]);
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                
            }
            /*   Field f = new Field(new StringField(false,true,false,"o"));
               Field f2 = new Field(new StringField(false, false, false, "ola"));
           
               ArrayList a = new ArrayList();
               a.Add(f);

               ArrayList a2 = new ArrayList();

               a2.Add(f2);

               Tuple t = new Tuple(a);
               Tuple t2 = new Tuple(a2);

               Console.WriteLine(t.Matches(t2)); */

           /* Field f = new Field(new DADTestA(1, "ola"));
            Field f2 = new Field(new DADTestA(1, "ola"));
            Console.WriteLine(f.Matches(f2)); */

            Console.ReadLine();
            //ExecuteFile("script.txt");
        }

        static void ExecuteFile(string filename)
        {
            StreamReader reader = File.OpenText(filename);

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

                        Tuple tupleA = new Tuple(Op.getFields());
                        XL.Add(tupleA);  
                        break;

                    case "take":

                        Console.WriteLine("WE TAKING");

                        Tuple tupleT = new Tuple(Op.getFields());

                        XL.Take(tupleT);

                        break;

                    case "read":

                        Console.WriteLine("WE READING");

                        Tuple tupleR = new Tuple(Op.getFields());
                    
                        XL.Read(tupleR);

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
}