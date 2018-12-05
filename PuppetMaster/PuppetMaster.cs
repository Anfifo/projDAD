using CommonTypes;
using System;
using System.IO;

namespace PuppetMaster
{

    class PuppetMaster
    {
        
        static void Main(string[] args)
        {

            //StreamReader reader = File.OpenText(args[0]);

            StreamReader reader = null;


            Console.WriteLine("Choose type of command introduction: s for script w for writing commands");
            string inputtype = Console.ReadLine();

            //if user choose script mode
            if (inputtype == "s")
            {
                try
                {
                    //open the script
                    reader = File.OpenText(AuxFunctions.GetProjPath() + "\\scripts\\PuppetMaster\\" + "script.txt");

                }
                catch (FileNotFoundException e)
                {

                    Console.WriteLine("Introduce a correct script file");
                }
            }


            Console.WriteLine("Choose the algorithm: x for XL and s for SMR");

            //get the type of algorithm
            string algorithm = Console.ReadLine();

            Console.WriteLine("Choose the mode: a for advanced, b for basic");

            string  mode = Console.ReadLine();


            //initiliaze a executer of commands
            PuppetMasterService MasterofPuppets = new PuppetMasterService(algorithm,mode);

            //get the commands from cmd input
            if (reader == null){

                while (true)
                {
                    Console.WriteLine("Write a command:" + "\n\r" );
                    string Command = Console.ReadLine();

                    //execute the command
                    MasterofPuppets.Execute(Command);

                }
            }

            //choose the typeof execution
            Console.WriteLine("Choose type of execution: s for step, nothing for complete");

            //get the type of execution
            string command = Console.ReadLine();

            //if step by step add a readline to stop the full execution
            if (command == "s")
            {
                Console.WriteLine("Step by Step execution");
                Console.ReadLine();

                string line;

                while ((line = reader.ReadLine()) != null)
                {

                    MasterofPuppets.Execute(line);

                    Console.ReadLine();
                }
            }

            //if complete just execute the lines
            else
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {

                    MasterofPuppets.Execute(line);

                }
            }

            Console.ReadLine();
        }
    }



}
