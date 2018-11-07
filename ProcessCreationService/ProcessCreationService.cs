﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace ProcessCreationService
{
    class ProcessCreationServiceMain
    {
        static void Main(string[] args)
        {
            TcpChannel channel = new TcpChannel(10000);
            ChannelServices.RegisterChannel(channel, false);
            ProcessCreationService PCS = new ProcessCreationService();
            RemotingServices.Marshal(PCS,"ProcessCreationService", typeof(ProcessCreationService));
            System.Console.WriteLine("<enter> para sair...");
            System.Console.ReadLine();

        }
    }

    public class ProcessCreationService : MarshalByRefObject
    {
        public ProcessCreationService()
        {

        }

        public void StartClient(string script)
        {
            
            //ProcessStartInfo info = new ProcessStartInfo(@"C:\\Users\\paranois3\\Dropbox\\projDAD\\Client\\bin\\Debug\\Client.exe");
            ProcessStartInfo info = new ProcessStartInfo(@"C:\\Leonor_Loureiro\\Tecnico\\4Ano\\1Semestre\\DAD\\projDAD\\Client\\bin\\Debug\Client.exe");
            

            //info.UseShellExecute = false;

            info.CreateNoWindow = false;

            info.Arguments = "script.txt";

            Process P = Process.Start(info);


        }
        public void StartServer(string url,int mindelay,int maxdelay)
        {

            //ProcessStartInfo info = new ProcessStartInfo(@"C:\\Users\\paranois3\\Dropbox\\projDAD\\Server\\bin\\Debug\\Server.exe");
            ProcessStartInfo info = new ProcessStartInfo(@"C:\\Leonor_Loureiro\\Tecnico\\4Ano\\1Semestre\\DAD\\projDAD\\Server\\bin\\Debug\\Server.exe");
            //info.UseShellExecute = false;

            info.CreateNoWindow = false;

            info.Arguments = url + " " + mindelay + " " + maxdelay;

            Process P = Process.Start(info);


        }

    }
}
