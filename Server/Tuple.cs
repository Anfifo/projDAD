using System;
using CommonTypes;
using System.Collections;


namespace Server
{
    class Tuple : ITuple
    {
        private ArrayList tuple = new ArrayList();

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

            for(int i = 0; i < this.getLength(); i++)
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
