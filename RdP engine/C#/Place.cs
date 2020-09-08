using System;
using System.Collections;
using System.Collections.Generic;

namespace RdPengine
{
    public enum Tokens
    {
        In = 0,
        Out = 1,
        InOrOut = 2
    }
    public class Place
    {
        private int _iD;
        private String _label;
        private int _tokens;
        private Queue tokensQueue; 
        
        private Boolean _hasConcurrency;
        private ArrayList concTransitionsList;

        private Boolean _autoExec; //for places starting with #
        
        private Boolean _external;  //for places starting with !
        private String externalNetName;
        private String externalPlaceName;

        public delegate void Del();
        private Del ChangeOcurredCallback;
        private Del CallAutoExec;
        private Dictionary<String,Del> callbacksWhenTokensAdded = new Dictionary<String,Del>();
        private Dictionary<String,Del> callbacksWhenTokensRemoved = new Dictionary<String,Del>();

        private Logger log;
        private PNid pnid;
        
        public bool External
        {
            get => _external;
            set => _external = value;
        }
        public string ExternalNetName
        {
            get => externalNetName;
            set => externalNetName = value;
        }

        public string ExternalPlaceName
        {
            get => externalPlaceName;
            set => externalPlaceName = value;
        }
        public bool AutoExec
        {
            get => _autoExec;
            set => _autoExec = value;
        }   
        public int ID
        {
            get => _iD;
            set => _iD = value;
        }
        public string Label
        {
            get => _label;
            set => _label = value;
        }
        public int Tokens
        {
            get => _tokens;
            set {
                //detects if its a REMOVE TOKENS operation
                if (value < _tokens)
                {
                    //dequeue pnids (they are lost, i.e., not propagated to transition->output places) 
                    for (int i = 0; i < (_tokens - value); i++)
                        this.tokensQueue.Dequeue();
                    
                    if(callbacksWhenTokensRemoved.Count > 0)
                        ExecCallbacksWhenTokensRemoved();
                }
                else
                {   //detects if its a ADD TOKEN operation
                    if (value > _tokens)
                    {
                        //enqueue only additional pnids (all null)
                        for (int i = 0; i < (value - _tokens) ; i++)
                            this.tokensQueue.Enqueue(new PNidList(pnid));
                        
                        if(callbacksWhenTokensAdded.Count > 0)
                            ExecCallbacksWhenTokensAdded();
                    }  
                }
                _tokens = value;
                
                ChangeOcurredCallback();
                
                if (_autoExec)
                    CallAutoExec();
            }
        }

        public bool HasConcurrency
        {
            get => _hasConcurrency;
            set => _hasConcurrency = value;
        }

        public Place(Del callbackChangeOcurred, Del callbackAutoExec, PNid pnid, Logger log)
        {
            this._tokens = 0;
            tokensQueue = new Queue(); 
            
            this._hasConcurrency = false;
            concTransitionsList = new ArrayList();
            
            this.ChangeOcurredCallback = callbackChangeOcurred;
            this.CallAutoExec = callbackAutoExec;
            this._autoExec = false;

            this.pnid = pnid;
            this.log = log;
        }

        public void AddTokens(int quantity) //invoked by user 
        {
            this.AddTokens(quantity, new PNidList(pnid));
        }
        public void AddTokens(int quantity, PetriNet pn) //invoked by user 
        {
            this.AddTokens(quantity, new PNidList(pn.Id));
        }
        
        public void AddTokens(int quantity, PNidList pnids) //directly invoked by ExecCycle 
        {
            if (this._tokens + quantity >= 0)
            {
                for (int i = 0; i < quantity; i++)
                    this.tokensQueue.Enqueue(pnids); //to clone each one?
                this._tokens += quantity;

                ChangeOcurredCallback();
                
                if (callbacksWhenTokensAdded.Count > 0)
                    ExecCallbacksWhenTokensAdded();
                
                if (_autoExec)
                    CallAutoExec();
            }
        }
        public ArrayList RemTokens(int quantity) //returns ArrayList of PNids
        {
            ArrayList removedTokens = new ArrayList();  //of PNids (possibly repeated)
            
            if (this._tokens - quantity >= 0)
            {
                this._tokens = this._tokens - quantity;
                try
                {
                    for (int i = 0; i < quantity; i++)
                    {
                        PNidList pl = (PNidList) this.tokensQueue.Dequeue();
                        foreach (PNid p in pl.Pnidlist)
                        {
                            removedTokens.Add(p);
                        }
                    }
                }
                catch (Exception e)
                {
                    log.Exec("\nPlace " + _label + ": Exception while trying to remove tokens (dequeue) . >> " + e.Message + " " + e.GetType().ToString());
                }
                
                ChangeOcurredCallback();
                
                if(callbacksWhenTokensRemoved.Count > 0)
                    ExecCallbacksWhenTokensRemoved();


                /*
                 autoexec only when adding tokens...
                 if (_autoExec)
                    CallAutoExec();
                */
                
                return removedTokens;  //returns ArrayList of PNids
            }
            return null;
        }
        
        public Boolean IsEmpty()
        {
            return this._tokens == 0;
        }

        /*
         * Place concorrency list manipulation
         */
        public void AddConcTransition(Transition t)
        {
            concTransitionsList.Add(t);
        }

        public void ClearConcTransitions()
        {
            concTransitionsList.Clear();
        }

        public ArrayList GetConcTransitionsList()
        {
            return concTransitionsList;
        }
        
        /*
         * Place callback manipulation
         */
        public void AddCallback(Del method, String key, Tokens mode)
        {
            switch (mode)
            {
                case RdPengine.Tokens.In: AddCallbackTokensAdded(method, key);
                    break;
                case RdPengine.Tokens.Out: AddCallbackTokensRemoved(method, key);
                    break;
                case RdPengine.Tokens.InOrOut: 
                    AddCallbackTokensRemoved(method, key);
                    AddCallbackTokensAdded(method, key);
                    break;
            }
        }
        public void RemoveCallback(String key)
        {
            if(callbacksWhenTokensAdded.ContainsKey(key))
                callbacksWhenTokensAdded.Remove(key);
            if (callbacksWhenTokensRemoved.ContainsKey(key))
                callbacksWhenTokensRemoved.Remove(key);
        }  
        private void AddCallbackTokensAdded(Del method, String key)
        {
            callbacksWhenTokensAdded.Add(key,method);
        }

        private void ExecCallbacksWhenTokensAdded()
        {
            foreach (KeyValuePair<String, Del> pair in callbacksWhenTokensAdded)
            {
                pair.Value();
            }
        }
        public void AddCallbackTokensRemoved(Del method, String key)
        {
            callbacksWhenTokensRemoved.Add(key,method);
        }
        private void ExecCallbacksWhenTokensRemoved()
        {
            foreach (KeyValuePair<String, Del> pair in callbacksWhenTokensRemoved)
            {
                pair.Value();
            }
        }
        public void RemoveAllCallbacks()
        {
            callbacksWhenTokensAdded.Clear();
            callbacksWhenTokensRemoved.Clear();
        }
    }
}