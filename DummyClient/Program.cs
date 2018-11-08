using CommonTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
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
                tuple.Add(new Field(new StringValue(false, false, false, "steve")));
                TSpaceMsg msg = new TSpaceMsg
                {
                    Code = "add",
                    Tuple = tuple
                };

                Console.WriteLine(obj.ProcessRequest(msg).Code);


                tuple = new Tuple();
                tuple.Add(new Field(new StringValue(false, false, false, "steve")));
                msg = new TSpaceMsg
                {
                    Code = "read",
                    Tuple = tuple
                };

                ITuple tup = obj.ProcessRequest(msg).Tuple;
                if(tup == null)
                {
                    Console.WriteLine("OMFG");
                }
                Console.WriteLine(tup);
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate server");
            }

            Console.ReadLine();

        }
    }
}

