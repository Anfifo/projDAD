using System;
using CommonTypes;
using System.Collections;


namespace CommonTypes
{
    [Serializable]
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
            if(tuple == null)
            {
                Console.WriteLine("Someone passed me a null please dont pass me a null");
            }
            if (this.getLength() != tuple.getLength())
            {
                return false;
            }
            for (int i = 0; i < this.getLength(); i++)
            {
                Field field1 = (Field)tuple.GetFields()[i];
                Field field2 = (Field)this.tuple[i];

                if (!field2.Matches(field1))
                {
                    return false;
                }

            }

            return true;
        }
    }
}
