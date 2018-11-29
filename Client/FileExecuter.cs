using System;
using System.Collections;
using System.IO;
using System.Linq;
using Tuple = CommonTypes.Tuple;

namespace Client
{
    class FileExecuter
    {
        //Operations to be executed by the client
        public ArrayList operations = new ArrayList();

        //Client to execute operations on
        public ITSpaceAPI clientType;

        public FileExecuter(ITSpaceAPI ClientType)
        {
            clientType = ClientType;

        }

        /// <summary>
        /// Parses a file and constructs the operations to be executed
        /// </summary>
        /// <param name="filename">path to the script</param>
        public void ExecuteFile(string filename)
        {
            //open the file
            StreamReader reader = File.OpenText(filename);

            //each line of the file
            string line;

            //iterate troug the lines
            while ((line = reader.ReadLine()) != null)
            {
                //this line might be necessary it isnt for now doent remember why it was useful
                //string[] splitLine = line.Split('\t');

                //fields inside the tuple operation 
                string[] fields = null;

                //split the string to get the fields
                fields = line.Split('<', '>');

                // if doesnt have field then its not a tuple operation
                if (fields.Length == 1)
                {

                    fields = fields[0].Split(' ');

                }
                //if it is an end-repeat
                if (fields.Length == 1 & fields[0] == "end-repeat")
                {
                    // construct an end repeat operation
                    operations.Add(new Operation(fields[0], "0"));
                }

                //if it is not an end-repeat it might be an wait or begin-repeat
                switch (fields[0])
                {
                    case "wait":
                        // construct a wait operation
                        operations.Add(new Operation(fields[0], fields[1].Replace(" ", string.Empty)));
                        break;
                    case "begin-repeat":
                        // construct a begin-repeat operation
                        operations.Add(new Operation(fields[0], fields[1].Replace(" ", string.Empty)));
                        break;

                }

                //get rid of the space on the operation name (ex: "add " -> "add")
                fields[0] = fields[0].Replace(" ", string.Empty);

                //then its a tuple operation
                switch (fields[0])
                {
                    case "add":
                        // construct an add operation
                        operations.Add(new Operation(fields[0], fields[1].Replace(" ", string.Empty)));
                        break;
                    case "read":
                        // construct a read operation
                        operations.Add(new Operation(fields[0], fields[1].Replace(" ", string.Empty)));
                        break;
                    case "take":
                        // construct a take operation
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
        public void ExecuteOperations(ArrayList operations)
        {

            //iterate trough the built operations
            for (int i = 0; i < operations.Count; i++)
            {
                //Cast to use the method .getType() 
                Operation operation = (Operation)operations[i];

                switch (operation.getType())
                {

                    case "add":

                        Console.WriteLine("WE ADDING");

                        //build the tuple to add
                        Tuple tupleA = new Tuple(operation.getFields());

                        //execute the operation on the client
                        clientType.Add(tupleA);
                        break;

                    case "take":

                        Console.WriteLine("WE TAKING");

                        //build the template tuple to take
                        Tuple tupleT = new Tuple(operation.getFields());

                        //execute the operation on the client
                        clientType.Take(tupleT);

                        break;

                    case "read":

                        Console.WriteLine("WE READING");

                        //build the template tuple to take
                        Tuple tupleR = new Tuple(operation.getFields());

                        //execute the operation on the client
                        clientType.Read(tupleR);

                        break;

                    case "wait":


                        Console.WriteLine("WE WAITING");
                        
                        //Wait x time
                        System.Threading.Thread.Sleep((int)operation.getFields()[0]);

                        Console.WriteLine("WE STOPPED WAITING");

                        break;

                    case "begin-repeat":

                        //times we are repeating
                        int timesToRepeat = (int)operation.getFields()[0];

                        Console.WriteLine("WE REPEATING" + " " + timesToRepeat);

                        //operations that we will repeat
                        ArrayList operationsToBeRepeated = new ArrayList();

                        //Collect operations to be repeated, start on the i+1 
                        for (int j = i + 1; j < operations.Count; j++)
                        {

                            Operation O = (Operation)operations[j];

                            //if we saw end-repeat we stop
                            if (O.getType() == "end-repeat")
                            {
                                break;
                            }

                            //add operation to be repeated
                            operationsToBeRepeated.Add(operations[j]);

                            //increment the upper for operation
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
