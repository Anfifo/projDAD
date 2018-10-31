using System;
using CommonTypes;
using System.Collections;


namespace CommonTypes
{
    public class Tuple : ITuple
    {
        private ArrayList tuple;

        public Tuple()
        {
            tuple = new ArrayList();
        }

        public Tuple(ArrayList fields)
        {
            tuple = new ArrayList(fields);
        }

        public ITuple Add(Object field)
        {
            tuple.Add(field);
            return this;
        }

        public ArrayList GetFields()
        {
            return tuple;
        }

        public int getLength()
        {
            return tuple.Count;
        }

        public bool Matches(ITuple tuple)
        {
            if (this.getLength() != tuple.getLength())
                return false;

            for (int i = 0; i < this.getLength(); i++)
            {
                Object f1 = tuple.GetFields()[i];
                Object f2 = this.tuple[i];

                if (!f1.GetType().Equals(f2.GetType()))
                    return false;
            }

            return true;
        }
    }
}
