using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdPengine
{
    class Place
    {
 

        private int _iD;
        private String _label;
        private int _tokens;
        
        private Boolean _hasConcurrency;
        private ArrayList concTransitionsList; 

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
            set => _tokens = value;
        }
       public bool HasConcurrency
        {
            get => _hasConcurrency;
            set => _hasConcurrency = value;
        }

        public Place()
        {
            this._tokens = 0;
            this._hasConcurrency = false;
            concTransitionsList = new ArrayList();
        }

        public void AddTokens(int quantity)
        {
            this._tokens += quantity;
        }

        public Boolean RemTokens(int quantity)
        {
            if (this._tokens - quantity >= 0)
            {
                this._tokens = this._tokens - quantity;
                return true;
            }

            return false;
        }
        
        public Boolean IsEmpty()
        {
            return this._tokens == 0;
        }

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

    }
}