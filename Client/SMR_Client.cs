using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Client
{
    class SMR_Client : ITSpaceAPI
    {
        private string[] view;
        public string[] GetView()
        {
            return view;
        }

        public void Put(ITuple tuple)
        {
            // Send total order multicast to all members of the view

            throw new NotImplementedException();
        }

        public ITuple Read(ITuple template)
        {
            // Send total order multicast to all members of the view

            // Return after the first replica answers

            throw new NotImplementedException();
        }

        public ITuple Take(ITuple template)
        {
            // Send total order multicast to all members of the view

            // Select one tuple from the subset the first replica returns

            // Send total order multicast to all members of the view with deletion request

            throw new NotImplementedException();
        }

        public void UpdateView(string[] group)
        {
            this.view = group;
        }
    }
}
