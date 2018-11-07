using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Pcs = ProcessCreationService.ProcessCreationService;

namespace PuppetMaster
{

    class PuppetMaster
    {

        static void Main(string[] args)
        {

            //StreamReader reader = File.OpenText(args[0]);

            StreamReader reader = null;

            try
            {
                reader = File.OpenText("script.txt");

            }catch (FileNotFoundException e){
                
                
            }


            PuppetMasterService MasterofPuppets = new PuppetMasterService();


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

public class PuppetMasterService
{
    Dictionary<string, string> Clients = new Dictionary<string, string>();

    Dictionary<string,string> Servers = new Dictionary<string, string>();

    ArrayList PCS = new ArrayList();
   
    public PuppetMasterService()
    {
        PCS = getPCS();

    }

    ArrayList getPCS()
    {
        StreamReader reader = File.OpenText("config.txt");

        string line;

        ArrayList PCS = new ArrayList();

        while ((line = reader.ReadLine()) != null)
        {
            PCS.Add(line);
            
        }

        return PCS;


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

                this.StartServer(splitfields[1],splitfields[2],mindelay,maxdelay);

                break;

            case "Client":

                Console.WriteLine("WE CLIENT");

                this.StartClient(splitfields[1], splitfields[2], splitfields[3]);

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
        Console.WriteLine(URL);
        Servers.Add(serverid, URL);
        Pcs P = (Pcs)Activator.GetObject(typeof(Pcs), (string)PCS[0]);
        P.StartServer(URL,mindelay,maxdelay);
        
    }

    void StartClient(string clientid, string URL, string script)
    {
        Clients.Add(clientid,URL);

        Pcs P = (Pcs)Activator.GetObject(typeof(Pcs),(string)PCS[0]);

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

// This classes might be needed to save more information
public class Client
    {
    string URL;

    string id;
    public Client(string _id, string _URL)
    {
        id = _id;
        URL = _URL;
    }
}

public class Server
{
    string URL;

    string id;
    public Server(string _id, string _URL)
    {
        id = _id;
        URL = _URL;
    }
}