using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    using System;

    [Serializable]
    public class DADTestA
    {
        public int i1;
        public string s1;

        public DADTestA(int pi1, string ps1)
        {
            i1 = pi1;
            s1 = ps1;
        }
        public override bool Equals(object o)
        {
            DADTestA a = o as DADTestA;
            if (a == null)
            {
                return false;
            }
            else
            {
                return ((this.i1 == a.i1) && (this.s1.Equals(a.s1)));
            }
        }
    }

    [Serializable]
    public class DADTestB
    {
        public int i1;
        public string s1;
        public int i2;

        public DADTestB(int pi1, string ps1, int pi2)
        {
            i1 = pi1;
            s1 = ps1;
            i2 = pi2;
        }

        public override bool Equals(object o)
        {
            DADTestB b = o as DADTestB;
            if (b == null)
            {
                return false;
            }
            else
            {
                return ((this.i1 == b.i1) && (this.s1.Equals(b.s1)) && (this.i2 == b.i2));
            }
        }
    }

    [Serializable]
    public class DADTestC
    {
        public int i1;
        public string s1;
        public string s2;

        public DADTestC(int pi1, string ps1, string ps2)
        {
            i1 = pi1;
            s1 = ps1;
            s2 = ps2;
        }

        public override bool Equals(object o)
        {
            DADTestC c = o as DADTestC;
            if (c == null)
            {
                return false;
            }
            else
            {
                return ((this.i1 == c.i1) && (this.s1.Equals(c.s1)) && (this.s2.Equals(c.s2)));
            }
        }
    }
}
