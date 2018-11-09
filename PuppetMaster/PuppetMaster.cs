using CommonTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;
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

                Console.WriteLine("rename the script file");
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
    delegate void startServerDel(string URL,int mindelay,int maxdelay);

    delegate void startClientDel(string URL);

    delegate string serverStatus();

    delegate void serverOperations(string id);

    Dictionary<string, string> Clients = new Dictionary<string, string>();

    Dictionary<string,string> Servers = new Dictionary<string, string>();

    ArrayList PCS = new ArrayList();

    static TcpChannel channel;

    public PuppetMasterService()
    {

        PCS = getPCS();
        channel = new TcpChannel();
        ChannelServices.RegisterChannel(channel, true); 
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
        
        Servers.Add(serverid, URL);

       Pcs P = (Pcs)Activator.GetObject(typeof(Pcs),(string)PCS[0]);


        startServerDel RemoteDel = new startServerDel(P.StartServer);
        IAsyncResult RemAr = RemoteDel.BeginInvoke(URL,mindelay,maxdelay, null ,null);
        

    }

    void StartClient(string clientid, string URL, string script)
    {
        Clients.Add(clientid,URL);

        Pcs P = (Pcs)Activator.GetObject(typeof(Pcs),(string)PCS[0]);

        startClientDel RemoteDel = new startClientDel(P.StartClient);

        IAsyncResult RemAr = RemoteDel.BeginInvoke(script,null, null);

    }

    void Status()
    {
        foreach(KeyValuePair<string,string> ServerProcess in Servers)
        {
            Console.WriteLine(ServerProcess.Value);
            ITSpaceServer server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), ServerProcess.Value);
            
            serverStatus RemoteDel = new serverStatus(server.Status);

            AsyncCallback callback = new AsyncCallback(PuppetMasterService.statusCallback);

            IAsyncResult RemAr = RemoteDel.BeginInvoke(callback, null);
           

        }
    }

    void Crash(string processname)
    {

    }

    void Freeze(string processid)
    {
        ITSpaceServer server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), Servers[processid]);
        server.Freeze();
    }

    void Unfreeze(string processid)
    {
        ITSpaceServer server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), Servers[processid]);
        server.Unfreeze();
    }

    void Wait(int time)
    {

    }

    static void statusCallback(IAsyncResult result)
    {
        serverStatus del = (serverStatus)((AsyncResult)result).AsyncDelegate;

        string state = del.EndInvoke(result);

        Console.WriteLine(state);
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