using System;
using System.Collections;

namespace RdPengine
{
    public class PNidList
    {
        private ArrayList _pnidlist;

        public ArrayList Pnidlist
        {
            get => _pnidlist;
            set => _pnidlist = value;
        }
        
        public PNidList(PNid pnid = null)
        {
            this._pnidlist = new ArrayList();
            if(pnid != null)
                _pnidlist.Add(pnid);
        }

        public PNid Get(int index)
        {
            return (PNid) Pnidlist[index];
        }
        public void Add(PNid pnid)
        {
            if(!IsInPNidList(pnid)) //only adds PNid if its not already stored
                _pnidlist.Add(pnid);
        }

        private Boolean IsInPNidList(PNid pnid)
        {
            foreach (PNid pn in _pnidlist)
            {
                if (pn.Name == pnid.Name && pn.Instance == pnid.Instance)
                    return true;
            }
            return false;
        }

        public void AddAll(ArrayList pnids)
        {
            foreach (PNid p in pnids)
            {
                this.Add(p);
            }
        }

        public void Clear()
        {
            this._pnidlist.Clear();
        }

        public int Count()
        {
            return this._pnidlist.Count;
        }
    }
}