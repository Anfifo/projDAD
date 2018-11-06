using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Client
{
    class XL_Client : ITSpaceAPI
    {
        private string[] view = { "server1", "server2", "server3" };

        public string[] GetView()
        {
            return view;
        }

        public void Put(ITuple tuple)
        {
            // Send multicast message to all members of the view

            // Put request is repeated until all replicas have acknowledge receipt

            throw new NotImplementedException();
        }

        public ITuple Read(ITuple template)
        {
            // Send multicast message to all members of the view

            // Return first response

            throw new NotImplementedException();
        }

        public ITuple Take(ITuple template)
        {
            /*------------------------------------------------
                Phase 1: Selecting the tuple to be removed 
             ------------------------------------------------*/

            // Send multicast request with the template to all replicas

            // Repeat until all replicas have responded

            // Select one tuple from the intersection of all answers

            // If intersection = {}
                // send multicast request to release all replicas
                // repeat phase 1



            /*------------------------------------------------
                Phase 2: Removing the selected tuple
             ------------------------------------------------*/

            // Send multicast request to remove tuples to all members of the view

            //Repeat until all replicas have acknowledged deletion


            throw new NotImplementedException();
        }

        public void UpdateView(string[] group)
        {
            this.view = group;
        }
    }
}
