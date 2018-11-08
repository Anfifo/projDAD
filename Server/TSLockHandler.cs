using CommonTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    static public class TSLockHandler
    {
        static IDictionary<int, List<ITuple>> LockedTuples = new Dictionary<int, List<ITuple>>();
        static List<ITuple> LockedTuplesList = new List<ITuple>();

        static public List<ITuple> LockTuples(int userID, List<ITuple> matches)
        {
            List<ITuple> UserLockedTuples = new List<ITuple>();
            
            lock (LockedTuples)
            {
                for (int i = 0; i < matches.Count; i++)
                {
                    if (!LockedTuplesList.Contains(matches[i]))
                    {
                        UserLockedTuples.Add(matches[i]);
                        LockedTuplesList.Add(matches[i]);
                    }
                }
                if (UserLockedTuples.Count > 0)
                {
                    LockedTuples.Add(userID, UserLockedTuples);
                    Console.WriteLine("Locking tuples for user: " + userID);
                }
            }

            return UserLockedTuples;
        }

        static public void UnlockTuples(int userID)
        {
            lock (LockedTuples)
            {
                Console.WriteLine("Unlocking tuples for user: " + userID);
                LockedTuplesList = LockedTuplesList.Except(LockedTuples[userID]).ToList();
                LockedTuples.Remove(userID);
            }
        }

    }

}
