using CommonTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessCreationService
{
    public class ProcessCreationService : MarshalByRefObject
    {

        public ProcessCreationService()
        {

        }

        public void StartServer(string url, int mindelay, int maxdelay, string algorithm)
        {

            //Initialize a process startinfo with the server.exe file
            ProcessStartInfo info = new ProcessStartInfo(AuxFunctions.GetProjPath() + "\\Server\\bin\\Debug\\Server.exe");

            //info.UseShellExecute = false;

            info.CreateNoWindow = false;

            //add the arguments to the info
            info.Arguments = url + " " + mindelay + " " + maxdelay + " " + algorithm;

            //Start the process
            Process P = Process.Start(info);

        }

        public void StartClient(string script, string id, string algorithm)
        {
            //Initialize a process startinfo with the client.exe file
            ProcessStartInfo info = new ProcessStartInfo(AuxFunctions.GetProjPath() + "\\Client\\bin\\Debug\\Client.exe");

            //info.UseShellExecute = false;

            info.CreateNoWindow = false;

            //add the arguments to the info
            info.Arguments = script + " " + id + " " + algorithm;

            //Start the process
            Process P = Process.Start(info);


        }
    }
}
