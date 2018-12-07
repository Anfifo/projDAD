using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Server
{
    /// <summary>
    /// Interface with Tuple Space storing and handling operations
    /// </summary>
    interface ITSpace
    {
        /// <summary>
        /// Finds a tuple that match given argument
        /// </summary>
        /// <param name="tuple">tuple to be matched to</param>
        /// <returns>one tuple matching the one given</returns>
        ITuple Read(ITuple tuple);

        /// <summary>
        /// finds list of matching tuples to the given tuple
        /// </summary>
        /// <param name="tuple">tuple to be matched to</param>
        /// <returns>list of matching tuples</returns>
        List<ITuple> Take1(ITuple tuple);

        /// <summary>
        /// Removes a match of the given tuple from the TupleSpace
        /// </summary>
        /// <param name="tuple"></param>
        /// <returns>True if the remove was successful</returns>
        Boolean Take2(ITuple tuple);

        /// <summary>
        /// Adds given tuple to the tuple space
        /// </summary>
        /// <param name="tuple">tuple to be added</param>
        void Add(ITuple tuple);

    }
}
