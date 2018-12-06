using System;

namespace Server
{
    [Serializable]
    class Slot
    {
        public enum UpdateType : int { INSERT, REMOVE, NONE};
        public UpdateType Type { get; set; }
        //The server that is being added/removed
        public string Subject { get; set; }
        //The server that reserverd the slot
        public string From { get; set; }
        //Lock object
        private static object Lock = new object();


        public Slot()
        {
            Type = UpdateType.NONE;
            Subject = null;
            From = null;
        }
        public bool IsFree()
        {
            lock (Lock)
            {
                return Type == UpdateType.NONE;
            }
        }

        public bool ReserveSlot(UpdateType type, string subject, string from)
        {
            lock (Lock)
            {
                if (!IsFree())
                    return false;

                Type = type;
                Subject = subject;
                From = from;

                return true;
            }
        }

        public void ReleaseSlot(UpdateType type, string from, string subject)
        {
            lock (Lock)
            {
                if (type != Type || !from.Equals(From) || !subject.Equals(subject))
                    return;

                ReleaseSlot();
            }
        }

        public void ReleaseSlot()
        {
            Subject = null;
            From = null;
            Type = UpdateType.NONE;
        }

        public void ReleaseFrom(string from)
        {
            lock (Lock)
            {
                if (From.Equals(from))
                    ReleaseSlot();
                    
            }
        }

     

    }
}
