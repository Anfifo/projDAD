﻿using CommonTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Pcs = ProcessCreationService.ProcessCreationService;

namespace PuppetMaster
{
    public class PuppetMasterService

    {
        //delegate to make async serverstart call to PCS
        delegate void startServerDel(string id,string URL, int mindelay, int maxdelay,string id2, string algorithm,string mode);

        //delegate to make async clientstart call to PCS
        delegate void startClientDel(string URL, string random,string serverurl, string algorithm,string mode);

        //delegate to make async serverstatus call to all servers
        delegate string serverStatus();

        //delegate for other async calls to the servers
        delegate void serverCrash(string id);

        //delegate for freeze and unfreeze async calls
        delegate void serverOperations();

        //List of clients
        Dictionary<string, string> Clients = new Dictionary<string, string>();

        //List of servers
        Dictionary<string, string> Servers = new Dictionary<string, string>();

        //List of ProcessCreationServices
        ArrayList PCS = new ArrayList();

        //Tcp channel to get responses
        static TcpChannel channel;

        //random for servers and client id's
        static Random random = new Random();

        //type of algorithm XL or SMR
        string algorithm;

        //type of implementation
        string mode;

        public PuppetMasterService(string _algorithm,string _mode)
        {
            algorithm = _algorithm;
            mode = _mode;
            PCS = getPCS();
            channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);
        }

        //Gets the available PCS from a config file
        ArrayList getPCS()
        {
            StreamReader reader = File.OpenText(AuxFunctions.GetProjPath() + "\\scripts\\PuppetMaster\\config.txt");

            //each line is a PCS
            string line;

            ArrayList PCS = new ArrayList();

            while ((line = reader.ReadLine()) != null)
            {
                //add PCS to the list
                PCS.Add(line);

            }

            return PCS;


        }
        //Parses and executes a command
        public void Execute(string Command)
        {
            //replace newlines with nothing
            string newCommand = Command.Replace(Environment.NewLine, string.Empty);

            //split by spaces
            string[] splitfields = newCommand.Split(' ');

            //the first entry is the command type
            string CommandType = splitfields[0];

            switch (CommandType)
            {
                case "Server":

                    Console.WriteLine("WE SERVER");

                    int mindelay = 0;
                    int maxdelay = 0;
                    string serverid2 = " ";

                    if (splitfields.Length == 4)
                    {
                        serverid2 = splitfields[3];
                    }

                    if (splitfields.Length == 5)
                    {
                        maxdelay = Int32.Parse(splitfields[4]);
                        mindelay = Int32.Parse(splitfields[3]);
                    }


                    //if the length is 6 then there is a min and max delay
                    if (splitfields.Length == 6)
                    {
                        maxdelay = Int32.Parse(splitfields[4]);
                        mindelay = Int32.Parse(splitfields[3]);
                        serverid2 = splitfields[5];
                    }

                    this.StartServer(splitfields[1], splitfields[2], mindelay, maxdelay, serverid2);

                    //start the server

                    break;

                case "Client":

                    Console.WriteLine("WE CLIENT");

                    //start the client 
                    this.StartClient(splitfields[1], splitfields[2], splitfields[3]);

                    break;

                case "Status":

                    Console.WriteLine("WE STATUS");

                    //make a status call to all servers
                    this.Status();

                    break;

                case "Crash":

                    Console.WriteLine("WE CRASH" + splitfields[1]);

                    //crash a specific server
                    this.Crash(splitfields[1]);

                    break;

                case "Freeze":

                    Console.WriteLine("WE FREEZE" + splitfields[1]);

                    //freeze a specific server
                    this.Freeze(splitfields[1]);

                    break;

                case "Unfreeze":

                    Console.WriteLine("WE UNFREEZE" + splitfields[1]);

                    //unfreeze a specific server
                    this.Unfreeze(splitfields[1]);

                    break;

                case "Wait":

                    Console.WriteLine("WE WAIT" + splitfields[1]);

                    int time = Int32.Parse(splitfields[1]);

                    //make puppetmaster wait
                    this.Wait(time);

                    break;

            }
        }

        //Starts a server available at URL
        void StartServer(string serverid, string URL, int mindelay, int maxdelay,string serverid2)
        {

            //Process Creation Service that we are going to use

            string PCStoUse = "none";

            //Search for the list of PCS and choose what to use
            for (int i = 0; i < PCS.Count; i++)
            {
                if ((getIP((string)PCS[i])).Equals(getIP(URL)))
                {
                    PCStoUse = (string)PCS[i];
                }
            }
            
            //If we need to get a state from another server 
            if(serverid2.Trim().Count() == 0)
            {
                //if there is a server to get state from
                if (!(Servers.Count() == 0))
                {
                    //Choose the oldest one
                    serverid2 = Servers.ElementAt(0).Value;
                }

                //if there is a server to get state from
                else
                    serverid2 = "none";
            }
            //add to the list of servers
            Servers.Add(serverid, URL);

            //get the PCS remote object
            Pcs P = (Pcs)Activator.GetObject(typeof(Pcs), PCStoUse);

            startServerDel RemoteDel = new startServerDel(P.StartServer);

            //make the async call 
            IAsyncResult RemAr = RemoteDel.BeginInvoke(serverid,URL, mindelay, maxdelay, serverid2, this.algorithm,this.mode, null, null);


        }


        //Starts a client available at URL
        void StartClient(string clientid, string URL, string script)
        {
            //Process Creation Service that we are going to use
            string PCStoUse = "none";

            string serverurl;
            for (int i = 0; i < PCS.Count; i++)
            {
                if ((getIP((string)PCS[i])).Equals(getIP(URL)))
                {
                    PCStoUse = (string)PCS[i];
                }
            }
            //add to the list of servers
            Clients.Add(clientid, URL);

            //get the PCS remote object
            Pcs P = (Pcs)Activator.GetObject(typeof(Pcs), PCStoUse);

            //if there is no server to ask
            if (Servers.Count() == 0)
                serverurl = "none";
            //if there is we get a random one 
            else
                serverurl = Servers.ElementAt(random.Next(0, Servers.Count - 1)).Value;

            startClientDel RemoteDel = new startClientDel(P.StartClient);

            //make the async call
            IAsyncResult RemAr = RemoteDel.BeginInvoke(script, random.Next().ToString(),serverurl, this.algorithm,this.mode, null, null);

        }

        void Status()
        {
            //iterate trough all servers
            foreach (KeyValuePair<string, string> ServerProcess in Servers)
            {
                //get the server remote object
                ITSpaceServer server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), ServerProcess.Value);

                serverStatus RemoteDel = new serverStatus(server.Status);

                //initialize the callback to get the result of the asyn call
                AsyncCallback callback = new AsyncCallback(PuppetMasterService.statusCallback);

                //make the async call
                IAsyncResult RemAr = RemoteDel.BeginInvoke(callback, null);


            }
        }

        void Crash(string id)
        {
            Pcs P = (Pcs)Activator.GetObject(typeof(Pcs), (string)PCS[0]);

            serverCrash RemoteDel = new serverCrash(P.Crash);

            IAsyncResult RemAr = RemoteDel.BeginInvoke(id, null,null);

            Servers.Remove(id);

        }

        void Freeze(string processid)
        {
            ITSpaceServer server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), Servers[processid]);

            serverOperations RemoteDel = new serverOperations(server.Freeze);

            IAsyncResult RemAr = RemoteDel.BeginInvoke(null, null);
        }

        void Unfreeze(string processid)
        {

            ITSpaceServer server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), Servers[processid]);

            serverOperations RemoteDel = new serverOperations(server.Unfreeze);

            IAsyncResult RemAr = RemoteDel.BeginInvoke(null, null);
        }

        void Wait(int time)
        {
            System.Threading.Thread.Sleep(time);


        }

        //Callback to get the status of the machines running
        static void statusCallback(IAsyncResult result)
        {
            serverStatus del = (serverStatus)((AsyncResult)result).AsyncDelegate;

            string state = del.EndInvoke(result);

            //print the state of the server
            Console.WriteLine(state);
        }

        public string getIP(string Url)
        {

            string[] splitUrl = Url.Split(':');

            return splitUrl[1];

        }

    }
}

