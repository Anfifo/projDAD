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
            if (this.getLength() != tuple.getLength())
                return false;

            for (int i = 0; i < this.getLength(); i++)
            {
                Console.WriteLine("Field index = " + i);
                Console.WriteLine(this.tuple[i].GetType().ToString());
                Field field1 = (Field)tuple.GetFields()[i];
                Field field2 = (Field)this.tuple[i];

                Console.WriteLine("Field 1 type: " + field1.GetFieldType().ToString());
                Console.WriteLine("Field 2 type: " + field2.GetFieldType().ToString());

                if (!field2.Matches(field1))
                {
                    Console.WriteLine("Field index = " + i + "FAIL");
                    return false;
                }
                Console.WriteLine("Field index = " + i + "OK");

            }

            return true;
        }
    }
}
