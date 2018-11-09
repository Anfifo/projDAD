using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace ProcessCreationService
{
    class ProcessCreationServiceMain
    {
        
        static void Main(string[] args)
        {
            TcpChannel channel = new TcpChannel(10000);
            ChannelServices.RegisterChannel(channel, true);
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

        public void StartServer(string url, int mindelay, int maxdelay, string algorithm)
        {

            ProcessStartInfo info = new ProcessStartInfo(AuxFunctions.GetProjPath() + "\\Server\\bin\\Debug\\Server.exe");

            //info.UseShellExecute = false;

            info.CreateNoWindow = false;

            info.Arguments = url + " " + mindelay + " " + maxdelay + " " + algorithm;

            Process P = Process.Start(info);

        }

        public void StartClient(string script,string id, string algorithm)
        { 

            ProcessStartInfo info = new ProcessStartInfo(AuxFunctions.GetProjPath() + "\\Client\\bin\\Debug\\Client.exe");

            //info.UseShellExecute = false;

            info.CreateNoWindow = false;
            info.Arguments = script + " " + id + " " + algorithm;

            Process P = Process.Start(info);


        }


    }
}
