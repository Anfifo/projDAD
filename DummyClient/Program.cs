using CommonTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace DummyClient
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);

            ITSpaceServer obj = (ITSpaceServer)Activator.GetObject(
                typeof(ITSpaceServer),
                "tcp://localhost:8086/TSpaceServer");

            try
            {
                TSpaceMsg msg = new TSpaceMsg
                {
                    Code = "take1"
                };

                Console.WriteLine(obj.ProcessRequest(msg));
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate server");
            }

            Console.ReadLine();

        }
    }
}

