using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Server
{
    class TSpaceServer : ITSpaceServer
    {
        public int Port;

        public TSpaceServer(int port)
        {
            Port = port;
        }

        public int GetPort()
        {
            return Port;
        }

        public void Run()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public TSpaceMsg ProcessRequest(TSpaceMsg msg)
        {

            return null;
        }
    }
}
