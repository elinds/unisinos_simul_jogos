using System;
using RdPengine;

namespace RdPengine
{
    class Program
    {
        static void Main(string[] args)
        {
            PetriNet pn = new PetriNet("testPN8.pflow");

            //pn.GetTransitionById(16).Priority = 10;
            //pn.GetTransitionByLabel("rec").Priority = 10;
            
            pn.showPlacesTransitions();
            for (int i = 0; i < 5; i++)
            {
                pn.ExecCycle();
                pn.showPlacesTransitions();
            }
        }
    }
}