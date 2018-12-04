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
        
        public static object Lock = new object();

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
                if (LockedTuples.ContainsKey(userID))
                {

                    Console.WriteLine("Unlocking tuples for user: " + userID);
                    LockedTuplesList = LockedTuplesList.Except(LockedTuples[userID]).ToList();
                    LockedTuples.Remove(userID);
                }
            }
        }

        static public List<int> GetKeys()
        {
            return LockedTuples.Keys.ToList();
        }

        // the order of the values in the Dictionary.ValueCollection is unspecified, 
        // but it is the same order as the associated keys in the Dictionary.KeyCollection returned by the Keys property.
        static public List<List<ITuple>> GetValues()
        {
            return LockedTuples.Values.ToList();
        }

        static public void SetContent(List<int> keys, List<List<ITuple>> values)
        {
            if(keys != null && values != null)
            {
                LockedTuples = ListsToDictionary(keys, values);
                LockedTuplesList = new List<ITuple>(values.SelectMany(x => x));
            }
        }

        static public IDictionary <int, List<ITuple>> ListsToDictionary(List<int> keys, List<List<ITuple>> values)
        {
            return Enumerable.Range(0, keys.Count).ToDictionary(i => keys[i], i => values[i]);

        }

    }

}
