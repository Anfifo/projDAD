using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CommonTypes;

namespace Client
{
    class Client
    {
       

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
            Servers.Add("tcp://localhost:50002/S");

            //PuppetMaster initialization
            if (args.Length == 3)
            {
                filename = args[0];
                clientid = args[1];
                algorithm = args[2];
                Int32.TryParse(clientid, out clientID);
                Console.WriteLine(clientID);
            }
            //Command Line initialization with script
            if (args.Length == 1)
            {
                filename = args[0];
                Console.WriteLine("Introduza o numero unico do cliente");
                clientid = Console.ReadLine();

                Int32.TryParse(clientid, out clientID);
            }
            //Command Line initialization without a script
            if (args.Length == 0)
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
                string filePath = AuxFunctions.GetProjPath() + "\\scripts\\Client\\" + filename;

                //Initializes a file executer to execute a script file, passes the client to execute the operations on
                FileExecuter exec = new FileExecuter(ClientType);

                //Executes the script file
                exec.ExecuteFile(filePath);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);

            }

            Console.ReadLine();
        }
        
  


    }
}