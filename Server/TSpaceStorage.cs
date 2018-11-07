﻿using System;
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
        public List<ITuple> TS = new List<ITuple>();
        
        public List<ITuple> getAll()
        {
            return TS;
        }

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

        public List<ITuple> Take1(ITuple tuple)
        {
            List<ITuple> list = new List<ITuple>();

            for (int i = 0; i < TS.Count; i++)
            {
                Console.WriteLine(i);
                if (TS[i].Matches(tuple))
                {
                    Console.WriteLine("Tuple matches");
                    list.Add(TS[i]);
                }
            }
            Console.WriteLine("Take 1 finished");
            return list;
        }

        public void Take2(ITuple tuple)
        {
            for( int i = 0; i < TS.Count; i++)
            {
                if (TS[i].Matches(tuple))
                {
                    TS.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
