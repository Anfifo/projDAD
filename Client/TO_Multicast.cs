using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace Client
{
    class TO_Multicast
    {

        /// <summary>
        /// Largest agreed sequence number it has seen in the view.
        /// </summary>
        private int agreedSeq;

        /// <summary>
        /// Largest proposed sequence number. 
        /// </summary>
        private int proposedSeq;

        /// <summary>
        /// Group of processes in the view.
        /// </summary>
        private string[] view;

        /// <summary>
        /// Number of the current view.
        /// </summary>
        private int viewNr;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="view">Group of processes in the view.</param>
        /// <param name="viewNr">View number.</param>
        public TO_Multicast(string[] view, int viewNr)
        {
            this.viewNr = viewNr;
            this.view = view;
        }

        /// <summary>
        /// Getter and setter for the view.
        /// </summary>
        public string[] View
        {
            get { return view; }
            set { view = value;  }
        }

        /// <summary>
        /// Getter and setter for the view number.
        /// </summary>
        public int ViewNr
        {
            get { return viewNr;  }
            set { viewNr = value;  }
        }
        
        /// <summary>
        /// Multicast send with total order.
        /// </summary>
        /// <param name="message">message to send</param>
        public void Send(ITSpaceRequest message)
        {
            // Multicast send <message, i>.


            // Repeat until all processes in the view have answered.


            // Select A = largest proposed sequence number.


            // Multicast send <i, A>.


            // Repeat until all processes have acknowledged.

        }

 
    }
}
