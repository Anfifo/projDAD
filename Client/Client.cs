using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Tuple = CommonTypes.Tuple;
using CommonTypes;

namespace Client
{
    class Client
    {
        //Operations to be executed by the client
        public static ArrayList operations = new ArrayList();

        //List of active servers
        public static List<string> Servers = new List<string>();

        //SMR Client or XL Clie
        public static ITSpaceAPI ClientType;

        static void Main(string[] args)
        {
            //filename of script
            string filename = " ";
            string clientid;
            //ClientID random or user introduced
            int clientID = 0;
            //Type of algorithm
            string algorithm = "x"; // pre definido ou escolher no inicio?
            
            Servers.Add("tcp://localhost:50001/S");
            //PuppetMaster initialization
            if(args.Length == 3)
            {
                filename = args[0];
                clientid = args[1];
                algorithm = args[2];
                Int32.TryParse(clientid, out clientID);
                Console.WriteLine(clientID);
            }
            //Command Line initialization with script
            if(args.Length == 1)
            {
                filename = args[0];
                Console.WriteLine("Introduza o numero unico do cliente");
                clientid = Console.ReadLine();

                Int32.TryParse(clientid, out clientID);
            }
            //Command Line initialization without a script
            if (args.Length == 0 )
            {
                Console.WriteLine("Introduza o nome do script que pretende executar");
                filename = Console.ReadLine();
                Console.WriteLine("Introduza o numero unico do cliente");
                clientid = Console.ReadLine();

                Int32.TryParse(clientid, out clientID);
            }
            if (algorithm == "x")
                ClientType = new XL_Client(Servers, 1, clientID);
            else
                ClientType = new SMR_Client(Servers, 1, clientID);

            try
            {
                //Get the script from the script folder
                string filePath =  AuxFunctions.GetProjPath() + "\\scripts\\Client\\" + filename;
                ExecuteFile(filePath);

            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                
            }

            Console.ReadLine();
        }

        /// <summary>
        /// Executes a script file
        /// </summary>
        /// <param name="filename">name of script file</param>
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
                    operations.Add(new Operation(fields[0], "0"));
                }

                switch (fields[0])
                {
                    case "wait":
                        operations.Add(new Operation(fields[0], fields[1].Replace(" ", string.Empty)));
                        break;
                    case "begin-repeat":
                        operations.Add(new Operation(fields[0], fields[1].Replace(" ", string.Empty)));
                        break;

                }
                fields[0] = fields[0].Replace(" ", string.Empty);

                switch (fields[0])
                {
                    case "add":
                        operations.Add(new Operation(fields[0], fields[1].Replace(" ", string.Empty)));
                        break;
                    case "read":
                        operations.Add(new Operation(fields[0], fields[1].Replace(" ", string.Empty)));
                        break;
                    case "take":
                        operations.Add(new Operation(fields[0], fields[1].Replace(" ", string.Empty)));
                        break;

                }

            }

            ExecuteOperations(operations);
        }

        /// <summary>
        /// Executes operations specified in the script
        /// </summary>
        /// <param name="Operations">Operations to be executed</param>
        static void ExecuteOperations(ArrayList operations)
        {
            
            for (int i = 0; i < operations.Count; i++)
            {
                
                Operation operation = (Operation)operations[i];

                switch (operation.getType())
                {

                    case "add":

                        Console.WriteLine("WE ADDING");

                        Tuple tupleA = new Tuple(operation.getFields());
                        ClientType.Add(tupleA);  
                        break;

                    case "take":

                        Console.WriteLine("WE TAKING");

                        Tuple tupleT = new Tuple(operation.getFields());

                        ClientType.Take(tupleT);

                        break;

                    case "read":

                        Console.WriteLine("WE READING");

                        Tuple tupleR = new Tuple(operation.getFields());

                        ClientType.Read(tupleR);

                        break;

                    case "wait":


                        Console.WriteLine("WE WAITING");

                        System.Threading.Thread.Sleep((int)operation.getFields()[0]);

                        Console.WriteLine("WE STOPPED WAITING");

                        break;

                    case "begin-repeat":

                        int timesToRepeat = (int)operation.getFields()[0];

                        Console.WriteLine("WE REPEATING" + " " + timesToRepeat);

                        ArrayList operationsToBeRepeated = new ArrayList();

                        //Collect operations to be repeated
                        for (int j = i+1; j < operations.Count; j++)
                        {

                            Operation O = (Operation)operations[j];


                            if (O.getType() == "end-repeat")
                            {
                                break;
                            }


                            operationsToBeRepeated.Add(operations[j]);
                            i++;

                        }

                        //Repeat the operations
                        for (int t = 0; t < timesToRepeat; t++)
                        {
                            ExecuteOperations(operationsToBeRepeated);
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