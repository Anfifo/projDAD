using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PCS = ProcessCreationService.ProcessCreationService;

namespace PuppetMaster
{

    class Program
    {

        static void Main(string[] args)
        {
            
            //StreamReader reader = File.OpenText(args[0]);

            StreamReader reader = File.OpenText("script.txt");

            PuppetMasterService MasterofPuppets = new PuppetMasterService();


            if (reader == null){

                while (true)
                {
                    Console.WriteLine("Write a command:" + "\n\r" );
                    string Command = Console.ReadLine();
                    MasterofPuppets.Execute(Command);

                }
            }

            //if (args[1] == "step")
            if (true)
            {

                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    Console.WriteLine("Step by Step execution");

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

public class PuppetMasterService
{
    ArrayList Clients = new ArrayList();

    ArrayList Servers = new ArrayList();
   
    public PuppetMasterService()
    {
    }

    public void Execute(string Command)
    {
        string newCommand = Command.Replace(Environment.NewLine, string.Empty);

        string[] splitfields = newCommand.Split(' ');

        string CommandType = splitfields[0];

        switch (CommandType)
        {
            case "Server":

                Console.WriteLine("WE SERVER");

                int mindelay = 0;
                int maxdelay = 0;


                if (splitfields.Length == 5)
                {
                    maxdelay = Int32.Parse(splitfields[4]);
                    mindelay = Int32.Parse(splitfields[3]);
                }
                if (splitfields.Length == 4) {
                    mindelay = Int32.Parse(splitfields[3]);
                }

                this.StartServer(splitfields[1],splitfields[2],mindelay,maxdelay);

                break;

            case "Client":

                Console.WriteLine("WE CLIENT");

                this.StartClient(splitfields[1], splitfields[1], splitfields[2]);

                break;

            case "Status":

                Console.WriteLine("WE STATUS");

                this.Status();

                break;

            case "Crash":

                Console.WriteLine("WE CRASH" + splitfields[1]);

                this.Crash(splitfields[1]);
                       
                break;

            case "Freeze":

                Console.WriteLine("WE FREEZE" + splitfields[1]);

                this.Freeze(splitfields[1]);

                break;

            case "Unfreeze":

                Console.WriteLine("WE UNFREEZE" + splitfields[1]);

                this.Unfreeze(splitfields[1]);

                break;

            case "Wait":

                Console.WriteLine("WE WAIT" + splitfields[1]);

                int time = Int32.Parse(splitfields[1]);

                this.Wait(time);

                break;

        }
    }


    void StartServer(string serverid, string URL, int mindelay, int maxdelay)
    {

    }

    void StartClient(string clientid, string URL, string script)
    {
        PCS P = (PCS)Activator.GetObject(typeof(PCS),"tcp://localhost:8086/ProcessCreationService");
        P.StartClient(script);
    }

    void Status()
    {

    }

    void Crash(string processname)
    {

    }

    void Freeze(string processname)
    {

    }

    void Unfreeze(string processname)
    {

    }

    void Wait(int time)
    {

    }
}
