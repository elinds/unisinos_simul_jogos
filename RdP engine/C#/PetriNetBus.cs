using System.Collections;

namespace RdPengine
{
    public class PetriNetBus
    {
        private PetriNet val;
        private ArrayList refs;

        public PetriNet Val
        {
            get => val;
            set => val = value;
        }

        public PetriNetBus(PetriNet aPN)
        {
            if (refs == null)
            {
                refs = new ArrayList();
                refs.Add(aPN);
            }
        }

        public void AddPetriNet(PetriNet aPN)
        {
            int instanceCntr = 0;
            foreach (PetriNet pn in refs)
            {
                if (aPN.Id.Name == pn.Id.Name)
                    instanceCntr++;
            }
            if (instanceCntr > 0)
                aPN.Id.Instance = instanceCntr;
            refs.Add(aPN);
        }

        public Place Find(string netName, string placeName, int instance = 0)
        {
            foreach (PetriNet pn in refs)
            {
                if (pn.Id.Name == netName && pn.Id.Instance == instance)
                {
                    Place p = pn.GetPlaceByLabel(placeName);
                    //if(p == null)
                    //  logexec error
                      return p;
                }
            }
            return null;   //cant find net or place
        }
    }
}