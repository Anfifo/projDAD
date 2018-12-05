using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonTypes;

namespace Server
{
    class TSpaceStorage : ITSpace
    {
        public List<ITuple> TS = new List<ITuple>();

        /// <summary>
        /// All tuples stored in this tuppleSpace
        /// </summary>
        /// <returns> all tuples currently in tuppleSpace</returns>
        public List<ITuple> getAll()
        {
            return TS;
        }

        public void setTuples(List<ITuple> newTuples)
        {
            TS = newTuples;
        }

        /// <summary>
        /// Adds given tupple to tupple space
        /// </summary>
        /// <param name="tuple">Tuple to be added</param>
        public void Add(ITuple tuple)
        {

            lock (TS)
                TS.Add(tuple);
        
        }

        public ITuple Read(ITuple tuple)
        {
            
            foreach(ITuple tup in TS)
            {
                if (tuple.Matches(tup))
                {
                    lock(TS)
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
                if (tuple.Matches(TS[i]))
                {
                    list.Add(TS[i]);
                }
            }
            return list;
        }

        public Boolean Take2(ITuple tuple)
        {
            for( int i = 0; i < TS.Count; i++)
            {
                if (tuple.Matches(TS[i]))
                {
                    lock(TS)
                        TS.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }

}
