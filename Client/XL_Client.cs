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
        // View of the tuple spaces servers.
        private List<ITSpaceServer> view { get; set; }

        // ID of the tuple spaces servers view.
        private int viewId { get; set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="viewUrls">Url of the tuple space servers.</param>
        public XL_Client(List<string> viewUrls, int viewId)
        {
            foreach(string serverUrl in viewUrls)
            {
                ITSpaceServer server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), serverUrl);
                view.Add(server);
            }
            this.viewId = viewId;

        }
       


        /// <summary>
        /// Adds a tuple to the distributed tuple space.
        /// </summary>
        /// <param name="tuple">Tuple to be added.</param>
        public void Add(ITuple tuple)
        {
            // Send multicast message to all members of the view

            // Put request is repeated until all replicas have acknowledge receipt

            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a tuple from the distributed tuple space, whithout deleting it.
        /// </summary>
        /// <param name="template">Template of the requested tuple.</param>
        /// <returns></returns>
        public ITuple Read(ITuple template)
        {
            // Send multicast message to all members of the view

            // Return first response

            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a tuple from the distributed tuple space, deleting it.
        /// </summary>
        /// <param name="template">Template of the requested tuple.</param>
        /// <returns></returns>
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
    }
}
