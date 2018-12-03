using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Timers;
using CommonTypes;

namespace Server
{
    class TSpaceSeverAdvSMR : MarshalByRefObject, ITSpaceServer
    {
        public enum State : int { FOLLOWER, CANDIDATE, LEADER };

        private readonly string URL;

        private  Random timeoutRandom = new Random();
        private System.Timers.Timer electionTimer;
        private System.Timers.Timer heartbeatTimer;
        private List<string> serversURL;
        private List<ITSpaceServer> servers;

        private static Object ElectionLock = new Object();

        delegate TSpaceMsg RemoteDelegate(TSpaceMsg msg);

        private State CurrentState; 


        private int VotesCounter = 0;
        

        private int Quorum;


        private int CurrentTerm;
        private int ElectionTerm;
        private int SendTerm;
        private string VotedFor;
        
            
        

        public TSpaceSeverAdvSMR(List<string> URLs, string url)
        {
            serversURL = URLs;
            servers = new List<ITSpaceServer>();
            

            foreach (string serverURL in serversURL)
            {
                ITSpaceServer server = (ITSpaceServer)Activator.GetObject(typeof(ITSpaceServer), serverURL);
                if (server != null)
                {
                    Console.WriteLine("Add: " + serverURL);
                    servers.Add(server);
                }
            }

            URL = url;

            Quorum = (servers.Count + 1) / 2;
            CurrentTerm = 0;
            VotedFor = null;
            CurrentState = State.FOLLOWER;

            InitElectionTimer();
            //InitHeartbearTimer();
        }

        private void InitElectionTimer()
        {
            electionTimer = new System.Timers.Timer();
            electionTimer.AutoReset = false;
            electionTimer.Elapsed += new ElapsedEventHandler(StartElection);
            ReStartElectionTimer();

        }

        private void ReStartElectionTimer()
        {
            electionTimer.Interval = ElectionTimeout();
            electionTimer.Start();
        }

        private void InitHeartbearTimer()
        {
            heartbeatTimer = new System.Timers.Timer(100);
            heartbeatTimer.AutoReset = true;
            heartbeatTimer.Elapsed += new ElapsedEventHandler(Heartbeat);
            heartbeatTimer.Start();

        }

        private void Heartbeat(object sender, ElapsedEventArgs e)
        {
            if (CurrentState != State.LEADER)
                return;

            Console.WriteLine("Send hearbeat");

            SendTerm = CurrentTerm;

            TSpaceMsg message = new TSpaceMsg()
            {
                Code = "Heartbeat",
                Term = CurrentTerm
            };

            AsyncCallback callback = new AsyncCallback(HeartbeatCallback);
            RemoteDelegate del;
            foreach(ITSpaceServer server in servers)
            {

                del = new RemoteDelegate(server.ProcessRequest);
                del.BeginInvoke(message, callback, null);
            }
        }

        private TSpaceMsg HandleHeartbeat(TSpaceMsg msg)
        {
            int term = msg.Term;
            TSpaceMsg response = new TSpaceMsg();

            if (term > CurrentTerm)
                StepDown(term);

            if (term < CurrentTerm)
            {
                response.Term = CurrentTerm;
                response.Commited = false;
                return response;
            }

            ReStartElectionTimer();

            // IF there are conflicts
                // commit fails
            //ELSE
            response.Term = CurrentTerm;
            response.Commited = true;
            return response;
        }

        public void HeartbeatCallback(IAsyncResult res)
        {
            Console.WriteLine("Heartbeat callback");
            RemoteDelegate del = (RemoteDelegate)((AsyncResult)res).AsyncDelegate;

            TSpaceMsg msg = del.EndInvoke(res);

            int term = msg.Term;
            bool commit = msg.Commited;

            if(CurrentTerm < term)
            {
                StepDown(term);
            }

            if (CurrentTerm != SendTerm)
                return;

            if(commit)
                Console.WriteLine("Return to client");
        }

        public TSpaceMsg ProcessRequest(TSpaceMsg msg)
        {
            Monitor.Enter(ElectionLock);

            string code = msg.Code;
            int term = msg.Term;
            string candidate = msg.Candidate;

            TSpaceMsg response = new TSpaceMsg();
            switch (code)
            {
                case "Leader":
                    electionTimer.Stop();
                    Console.WriteLine("Accept leader");
                    CurrentState = State.FOLLOWER;
                    CurrentTerm = term;
                    
                    break;
                case "Heartbeat":
                    response = HandleHeartbeat(msg);
                    break;
                case "RequestVote":
                    response = HandleRequestVote(term, candidate);
                    break;

            }

            Monitor.Exit(ElectionLock);
            return response;
            
        }

        private void StepDown(int term)
        {
            Console.WriteLine("Step Down");
            CurrentTerm = term;
            CurrentState = State.FOLLOWER;
            VotedFor = null;
            ReStartElectionTimer();
            //heartbeatTimer.Stop();
        }

        private TSpaceMsg HandleRequestVote(int term , string candidate) {

            /*Console.WriteLine("My State = " + CurrentState);
            Console.WriteLine("My Vote Counter = " + VotesCounter);
            Console.WriteLine("My Term = " + CurrentTerm);
            Console.WriteLine("Candidate Term = " + term);
            Console.WriteLine("Candidate = " + candidate);*/

            TSpaceMsg msg = new TSpaceMsg();

            // If it has not voted in that term
            if (term > CurrentTerm)
            {
                CurrentTerm = term;
                // Vote for the candidate
                CurrentState = State.FOLLOWER;
                VotedFor = candidate;

                msg.Term = term;
                msg.VoteGranted = true;
                ReStartElectionTimer();
            }
            else
            {
                msg.VoteGranted = false;
                msg.Term = CurrentTerm;
            }

            return msg;

/*
            // Dont vote for out of date candidates
            if (term < CurrentTerm)
            {
                Console.WriteLine("Old term");

                msg.Term = CurrentTerm;
                msg.VoteGranted = false;
                return msg;
            }
            //Dont vote for two different leaders
            if (VotedFor != null && !VotedFor.Equals(candidate))
            {
                Console.WriteLine("Already Voted");
                msg.Term = CurrentTerm;
                msg.VoteGranted = false;
                return msg;
            }

            //Check how up to date our log is
            //Reject leaders with old logs
            //Reject leaders with shorter logs

            Console.WriteLine("Vote for: " + candidate);

            VotedFor = candidate;

            msg.Term = CurrentTerm;
            msg.VoteGranted = true;

            return msg;*/
        } 

        public void StartElection(object sender, ElapsedEventArgs e)
        {            
            if (CurrentState == State.LEADER)
                return;

            ReStartElectionTimer();

            // Start new term
            CurrentTerm++;
            ElectionTerm = CurrentTerm;
            // Set state as candidate
            CurrentState = State.CANDIDATE;
            // Votes for itself
            VotesCounter = 1;
            VotedFor = URL;
                                  
            Console.WriteLine("Start election term " + ElectionTerm );

            // Sends request vote messages to other nodes
            TSpaceMsg message = new TSpaceMsg()
            {
                Code = "RequestVote",
                Candidate = URL,
                Term = ElectionTerm
            };

            AsyncCallback remoteCallback = new AsyncCallback(ElectionCallback);
            RemoteDelegate del;
            foreach (ITSpaceServer server in servers)
            {

                del = new RemoteDelegate(server.ProcessRequest);
                del.BeginInvoke(message, remoteCallback, null);
            }
        }



        public void ElectionCallback(IAsyncResult res)
        {
            RemoteDelegate del = (RemoteDelegate)((AsyncResult)res).AsyncDelegate;

            TSpaceMsg msg = del.EndInvoke(res);
            int term = msg.Term;
            bool voteGranted = msg.VoteGranted;

            //If the server that replied is at a later round
            if (term > CurrentTerm)
            {
                // Step down as candidate
                CurrentTerm = term;
                VotedFor = null;
                CurrentState = State.FOLLOWER;
                
                ReStartElectionTimer();
                return;
            }
            if (term != ElectionTerm)
                return;

            if (voteGranted)
            {
                Interlocked.Increment(ref VotesCounter);
                Console.WriteLine("VoteGranted  " + VotesCounter );
            }

            // If it has been voted on by the majority
            // becames the leader
            if(VotesCounter > Quorum )
            {

                Console.WriteLine("Becames leader : " + CurrentTerm);
                CurrentState = State.LEADER;
                ElectionTerm = 0;
                electionTimer.Stop();

                TSpaceMsg declare = new TSpaceMsg()
                {
                    Code = "Leader"
                };

                RemoteDelegate d;
                foreach (ITSpaceServer server in servers)
                {

                    d = new RemoteDelegate(server.ProcessRequest);
                    d.BeginInvoke(declare, null, null);
                }

                //InitHeartbearTimer();
            }
                     
        }


        /// <summary>
        /// Generates a random timeout interval between 150ms and 300ms
        /// </summary>
        /// <returns>timeout interval</returns>
        private int ElectionTimeout()
        {
            return timeoutRandom.Next(1500, 3000);
        }



        public void Freeze()
        {
            throw new NotImplementedException();
        }

        public List<ITuple> GetTuples()
        {
            throw new NotImplementedException();
        }

        public bool Ping(string serverURL)
        {
            throw new NotImplementedException();
        }

       

        public string Status()
        {
            throw new NotImplementedException();
        }

        public void Unfreeze()
        {
            throw new NotImplementedException();
        }

        public View UpdateView()
        {
            throw new NotImplementedException();
        }





    }
}
