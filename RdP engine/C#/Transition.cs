using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdPengine
{
    class Transition
    {
        
        public int Id
        {
            get => _id;
            set => _id = value;
        }
        public string Label
        {
            get => _label;
            set => _label = value;
        }
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }
        public bool Triggerable
        {
            get => _triggerable;
            set => _triggerable = value;
        }
        public int Priority
        {
            get => _priority;
            set => _priority = value;
        }
        
        private int _id;
        private String _label;
        private bool _enabled;
        private bool _triggerable;
        private int _priority;
        
        //private ArrayList callbacksList; 
        public delegate void Del();
        private Dictionary<String,Del> callbacks = new Dictionary<String,Del>();

        public Transition()
        {
            this._enabled = true;
            this._triggerable = false;
            this._priority = 0;
        }
        
        /*
         * transition callback manipulation
         */
        public void AddCallback(Del method, String key)
        {
            callbacks.Add(key,method);
        }

        public void ExecCallbacks()
        {
            foreach (KeyValuePair<String, Del> pair in callbacks)
            {
                pair.Value();
            }
        }

        public void RemoveAllCallbacks()
        {
            callbacks.Clear();
        }
        public void RemoveCallbacksByKey(String key)
        {
            callbacks.Remove(key);
        }       

    }
}