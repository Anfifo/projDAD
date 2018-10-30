using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    public interface ITSpaceServer
    {
        int GetPort();
        void Start();
        void Run();
        void Stop();


    }
}
