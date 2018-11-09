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

            if (inputtype == "s")
            {
                try
                {
                    reader = File.OpenText(AuxFunctions.GetProjPath() + "\\scripts\\PuppetMaster\\" + "script.txt");

                }
                catch (FileNotFoundException e)
                {

                    Console.WriteLine("Introduce a correct script file");
                }
            }


            Console.WriteLine("Choose the algorithm: x for XL and s for SMR");
            string algorithm = Console.ReadLine();
            PuppetMasterService MasterofPuppets = new PuppetMasterService(algorithm);


            if (reader == null){

                while (true)
                {
                    Console.WriteLine("Write a command:" + "\n\r" );
                    string Command = Console.ReadLine();
                    MasterofPuppets.Execute(Command);

                }
            }

            Console.WriteLine("Choose type of execution: s for step, nothing for complete");

            string command = Console.ReadLine();

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
