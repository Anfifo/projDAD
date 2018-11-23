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
            //Post the PCS remote service
            TcpChannel channel = new TcpChannel(10000);
            ChannelServices.RegisterChannel(channel, true);
            ProcessCreationService PCS = new ProcessCreationService();
            RemotingServices.Marshal(PCS,"ProcessCreationService", typeof(ProcessCreationService));
            System.Console.WriteLine("<enter> para sair...");
            System.Console.ReadLine();

        }
    }
}
