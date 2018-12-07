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
    /// <summary>
    /// Class responsible for handling all the tuple space storing logic
    /// </summary>
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

        /// <summary>
        /// Sets the current tuple space content
        /// </summary>
        /// <param name="newTuples"></param>
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
            Console.WriteLine("% Added: " + tuple);

            lock (TS)
                TS.Add(tuple);
        
        }

        /// <summary>
        /// Finds a tuple that matches the one given
        /// </summary>
        /// <param name="tuple"></param>
        /// <returns> tuple that matches the argument</returns>
        public ITuple Read(ITuple tuple)
        {
            foreach (ITuple tup in TS)
            {
                lock (TS)
                    if (tuple.Matches(tup))
                    {
                        Console.WriteLine("% Read: " + tuple);
                        return tup;
                    }
            }

            return null;
        }

        /// <summary>
        /// Finds list of tuples matching the given tuple
        /// </summary>
        /// <param name="tuple"> tuple to be matched to </param>
        /// <returns> list of matches found</returns>
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

            Console.WriteLine("% Take1 (matches): " + list.Count + "Tuple like " + tuple);
            return list;
        }

        /// <summary>
        /// Deletes the given tuple from the tupleStorage
        /// </summary>
        /// <param name="tuple"></param>
        /// <returns>true if the tuple was found</returns>
        public Boolean Take2(ITuple tuple)
        {
            for ( int i = 0; i < TS.Count; i++)
            {
                lock(TS)
                if (tuple.Matches(TS[i]))
                {
                    Console.WriteLine("% Take2 (Removing): " + tuple);
                    TS.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }

}
