﻿using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Net.Sockets;
using CommonTypes;

namespace Client
{
    class DummyClient
    {
        public static ArrayList Operations = new ArrayList();

        static void Main(string[] args)
        {    
			TcpChannel channel = new TcpChannel();
			ChannelServices.RegisterChannel(channel,true);

            ITSpaceServer obj = (ITSpaceServer) Activator.GetObject(
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
	 		catch(SocketException)
	 		{
	 			System.Console.WriteLine("Could not locate server");
	 		}

			Console.ReadLine();
		
        }
    }
}