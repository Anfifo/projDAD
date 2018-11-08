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

            ProcessStartInfo info = new ProcessStartInfo(GetProjPath() + "\\Client\\bin\\Debug\\Client.exe");

            //info.UseShellExecute = false;

            info.CreateNoWindow = false;

            info.Arguments = script;

            Process P = Process.Start(info);


        }
        public void StartServer(string url,int mindelay,int maxdelay)
        {

            ProcessStartInfo info = new ProcessStartInfo(GetProjPath() + "\\Server\\bin\\Debug\\Server.exe" );

            //info.UseShellExecute = false;

            info.CreateNoWindow = false;

            info.Arguments = url + " " + mindelay + " " + maxdelay;

            Process P = Process.Start(info);


        }

        public string GetProjPath()
        {
            string current = Directory.GetCurrentDirectory();

            DirectoryInfo binPath = Directory.GetParent(current);

            string binStringPath = binPath.ToString();

            DirectoryInfo classPath = Directory.GetParent(binStringPath);

            string classStringPath = classPath.ToString();

            DirectoryInfo projPath = Directory.GetParent(classStringPath);

            return projPath.ToString();


        }

    }
}
