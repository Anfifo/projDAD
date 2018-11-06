using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    public class DADTestA
    {
        public int i1;
        public string s1;

        public DADTestA(int pi1, string ps1)
        {
            i1 = pi1;
            s1 = ps1;

            Console.WriteLine(i1.ToString() + s1.ToString() + this.GetType());
        }
        public bool Equals(DADTestA o)
        {
            if (o == null)
            {
                return false;
            }
            else
            {
                return ((this.i1 == o.i1) && (this.s1.Equals(o.s1)));
            }
        }
    }

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

            Console.WriteLine(i1.ToString() + s1.ToString() + i2.ToString() + this.GetType());

        }

        public bool Equals(DADTestB o)
        {
            if (o == null)
            {
                return false;
            }
            else
            {
                return ((this.i1 == o.i1) && (this.s1.Equals(o.s1)) && (this.i2 == o.i2));
            }
        }
    }

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

            Console.WriteLine(i1.ToString() + s1.ToString() + s2.ToString() + this.GetType());
        }

        public bool Equals(DADTestC o)
        {
            if (o == null)
            {
                return false;
            }
            else
            {
                return ((this.i1 == o.i1) && (this.s1.Equals(o.s1)) && (this.s2.Equals(o.s2)));
            }
        }
    }
}
