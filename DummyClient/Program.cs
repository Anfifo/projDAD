using CommonTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using Tuple = CommonTypes.Tuple;

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
                "tcp://localhost:8086/S");

            try
            {
                Tuple tuple = new Tuple();
                tuple.Add(new Field("steve"));
                TSpaceMsg msg = new TSpaceMsg
                {
                    Code = "add",
                    Tuple = tuple
                };


                Console.WriteLine(obj.ProcessRequest(msg).Code);
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate server");
            }

            Console.ReadLine();

        }
    }
}

