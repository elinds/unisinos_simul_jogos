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
        
        public Transition()
        {
            this._enabled = true;
            this._triggerable = false;
            this._priority = 0;
        }
    }
}