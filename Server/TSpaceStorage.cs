using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Server
{
    class TSpaceStorage : ITSpace
    {
        public ArrayList TS = new ArrayList();

        public void Add(ITuple tuple)
        {
            TS.Add(tuple);
        }

        public ITuple Read(ITuple tuple)
        {
            foreach(ITuple tup in TS)
            {
                if (tup.Matches(tuple))
                {
                    return tup;
                }
            }

            return null;
        }

        public ITuple[] Take1(ITuple tuple)
        {

            throw new NotImplementedException();
        }

        public void Take2(ITuple tuple)
        {

            throw new NotImplementedException();
        }
    }
}
