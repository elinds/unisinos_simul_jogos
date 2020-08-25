using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdPengine
{
    public enum Tokens
    {
        In = 0,
        Out = 1,
        InOrOut = 2
    }
    class Place
    {
        private int _iD;
        private String _label;
        private int _tokens;
        
        private Boolean _hasConcurrency;
        private ArrayList concTransitionsList;

        private Boolean _autoExec;
        
        public delegate void Del();
        private Del ChangeOcurredCallback;
        private Del CallAutoExec;
        private Dictionary<String,Del> callbacksWhenTokensAdded = new Dictionary<String,Del>();
        private Dictionary<String,Del> callbacksWhenTokensRemoved = new Dictionary<String,Del>();
        
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
                if (value < _tokens && callbacksWhenTokensRemoved.Count > 0)
                {
                    ExecCallbacksWhenTokensRemoved();
                }
                else
                {
                    if (value > _tokens && callbacksWhenTokensAdded.Count > 0)
                    {
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

        public Place(Del callbackChangeOcurred, Del callbackAutoExec)
        {
            this._tokens = 0;
            this._hasConcurrency = false;
            concTransitionsList = new ArrayList();
            this.ChangeOcurredCallback = callbackChangeOcurred;
            this.CallAutoExec = callbackAutoExec;
            this._autoExec = false;
        }

        public void AddTokens(int quantity)
        {
            if (this._tokens + quantity >= 0)
            {
                this._tokens += quantity;
                ChangeOcurredCallback();
                if(callbacksWhenTokensAdded.Count > 0)
                    ExecCallbacksWhenTokensAdded();
                if (_autoExec)
                    CallAutoExec();
            }
        }
        public Boolean RemTokens(int quantity)
        {
            if (this._tokens - quantity >= 0)
            {
                this._tokens = this._tokens - quantity;
                ChangeOcurredCallback();
                if(callbacksWhenTokensRemoved.Count > 0)
                    ExecCallbacksWhenTokensRemoved();
                if (_autoExec)
                    CallAutoExec();
                return true;
            }
            return false;
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