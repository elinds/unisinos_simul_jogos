/*
 * RdP engine     version 0.5
 *
 * supports PNEditor file format (pflow)
 * 
 * author: Ernesto Lindstaedt
 *
 * Last modified  september 2020
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace RdPengine
{
    [Flags]
    public enum DebugMode
    {
        Undefined = 0,
        Off  = 1,
        Exec = 2,
        Load = 4,
        Concurrency = 8,
        Place = 16,
        All = 127
        
    }
    public class PetriNet
    {
        /*
         ExecLoopLimit -> MAX iteractions in ExecUntillNothingMoreToDo()
         to avoid infinite loop (cases where Petri Net never stops, 
         having always some triggerable transitions)
        */
        public const int ExecLoopLimit = 200;

        private ArrayList placeList;
        private ArrayList transitionList;
        private ArrayList connList;
        private ArrayList concPlaceList;

        private Dictionary<int, int> refPlaces = new Dictionary<int, int>();

        private PNid id;

        public PNid Id
        {
            get => id;
            set => id = value;
        }

        private Logger log;
        private String logFilename_load;
        private String logFilename_exec;

        private Boolean somethingChanged;

        public static DebugMode dbg;
        
        private static PetriNetBus bus;

        public PetriNetBus Bus
        {
            get => bus;
            set => bus = value;
        }

        public PetriNet()
        {
            placeList = new ArrayList();
            transitionList = new ArrayList();
            connList = new ArrayList();
            concPlaceList = new ArrayList();

            somethingChanged = true;
        }

        public PetriNet(String filename, DebugMode debugMode = DebugMode.Undefined)
        {
            if(debugMode != DebugMode.Undefined)
                dbg = debugMode;
            
            placeList = new ArrayList();
            transitionList = new ArrayList();
            connList = new ArrayList();
            concPlaceList = new ArrayList();

            somethingChanged = true;

            int startPos = filename.LastIndexOf("/") + 1;
            int length = filename.IndexOf(".") - startPos;
            String netName = filename.Substring(startPos, length);

            log = new Logger(netName);

            //sets PN identity
            this.id = new PNid();
            this.id.Name = netName;
            this.id.Instance = 0; //may be updated by Bus

            //inserts PN reference in Bus
            if (bus == null)
                bus = new PetriNetBus(this);
            else
            {
                bus.AddPetriNet(this);
            }

            if (LoadFromFile(filename))
            {
                //debugging
                if(IsDbgLoad())
                {
                    ShowDictionary();
                    showArrays();
                }
                
                //post-processing, obligatory!
                Flattening();
                ChecksPlacesWithConcurrency();

                //debugging
                if(IsDbgLoad()) 
                    showArrays();
            }
        }

        public void ChangesOcurred()
        {
            somethingChanged = true;
        }

        private Boolean SearchForInconsistencies()
        {
            Boolean foundProblems = false;
            foreach (Place p in placeList)
            {
                if (p.Tokens < 0)
                {
                    if(IsDbgExec()) log.Exec("Error: Place " + p.ID + " with " + p.Tokens + " tokens...");
                    foundProblems = true;
                }
            }

            foreach (Connection c in connList)
            {
                if (c.Multiplicity < 0)
                {
                    if(IsDbgExec()) log.Exec("Error: Connection from " + c.SourceId + " to " + c.DestinationId + " multiplicity " +
                                               c.Multiplicity);
                    foundProblems = true;
                }
            }

            return foundProblems;
        }

        /*
         * Execution methods
         */
        public void ExecUntilNothingMoreToDo()
        {
            int i = ExecLoopLimit;
            while (ExecCycle() && (--i > 0)) ;
            if (i == 0)
            {
                if(IsDbgExec()) log.Exec("\n  + - + - + PETRI NET POSSIBLY IN LOOP: reached ExecLoopLimit in ExecUntilNotinhgMoreToDo()...\n ");
            }
        }

        public Boolean ExecCycle()
        {
            if (!somethingChanged)
            {
                if(IsDbgExec()) log.Exec("\n >>>> >>>> >>>> nothing changed... ");
                return false;
            }


            Boolean somethingToDo = false;

            //SearchForInconsistencies();  
            //if structural changes ocurred: ins/rem  of  place/trans/conn


            //check triggerable transitions...
            if(IsDbgExec()) log.Exec("\nTriggerable transitions -> BEFORE concurrency check: ");
            
            foreach (Transition t in transitionList)
            {
                if (t.Triggerable = IsTriggerable(t))
                    somethingToDo = true;
            }

            if (somethingToDo) resolveConcurrencies();

            if(IsDbgExec()) log.Exec("\nTriggerable transitions -> AFTER concurrency check: ");
            //foreach (Transition t in transitionList)   LogExec("  |" + t.Id + "," + t.Label + "|" + t.Triggerable);

            if (somethingToDo)
            {
                PNidList propagatedTokensPNids = new PNidList();
                
                if(IsDbgExec()) log.Exec("\n #### #### #### something to do ... ");
                
                //FIREs TRANSITIONS
                foreach (Transition t in transitionList)
                {
                    if (t.Triggerable && t.Enabled)
                    {
                        //call all Transition Callback methods...
                        t.ExecCallbacks();

                        //for each transition, propagates a new and different PNids list
                        propagatedTokensPNids.Clear(); 

                        try
                        {
                            //consume tokens from input places
                            foreach (Connection c in connList)
                            {
                                if (c.DestinationId == t.Id)
                                {
                                    switch (c.Type)
                                    {
                                        case ArcTypes.Regular:
                                            propagatedTokensPNids.AddAll(GetPlaceById(c.SourceId).RemTokens(c.Multiplicity));
                                            if(IsDbgExec()) log.Exec("\n --- removed " + c.Multiplicity + " tokens at " + GetPlaceById(c.SourceId).ID + "," + GetPlaceById(c.SourceId).Label);
                                            break;
                                        case ArcTypes.Reset:
                                            GetPlaceById(c.SourceId).Tokens = 0;
                                            break;
                                    }
                                }
                            }

                            if(IsDbgExec()) log.Exec("  PNIDS:" + log.ArrayListContent(propagatedTokensPNids.Pnidlist));
                        }
                        catch (Exception e)
                        {
                            if(IsDbgExec()) log.Exec("\nException while firing transitions: consuming tokens form input places. >> " +
                                                       e.Message + " " + e.GetType().ToString());
                        }

                        try
                        {
                            //generate tokens at output places
                            foreach (Connection c in connList)
                            {
                                if (c.SourceId == t.Id)
                                {
                                    Place place = GetPlaceById(c.DestinationId);
                                    if (place.External)
                                    {
                                        //checks if instance of that external place is being propagated
                                        for (int i = 0; i < propagatedTokensPNids.Count(); i++)
                                        {
                                            PNid p = propagatedTokensPNids.Get(i);
                                            if (p.Name == place.ExternalNetName)
                                            {
                                                Bus.Find(place.ExternalNetName, place.ExternalPlaceName,p.Instance).AddTokens(c.Multiplicity, propagatedTokensPNids);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        place.AddTokens(c.Multiplicity, propagatedTokensPNids);
                                    }
                                    
                                    if(IsDbgExec()) log.Exec("\n +++ generated " + c.Multiplicity + " tokens at " + place.ID + "," + place.Label);
                                    if(IsDbgExec()) log.Exec("  pnids:" + log.ArrayListContent(propagatedTokensPNids.Pnidlist));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if(IsDbgExec()) log.Exec("\nException while firing transitions: generate tokens at output places. >> " +
                                                       e.Message + " " + e.GetType().ToString());
                        }
                    }
                }
            }

            somethingChanged = somethingToDo;
            return somethingToDo;
        }

        public Boolean IsTriggerable(Transition t)
        {
            Boolean transitionCanTrigger = true;
            foreach (Connection c in connList)
            {
                if (c.DestinationId == t.Id)
                {
                    if (c.Type == ArcTypes.Regular)
                    {
                        if (GetPlaceById(c.SourceId).Tokens < c.Multiplicity)
                        {
                            transitionCanTrigger = false;
                            break;
                        }
                    }
                    else
                    {
                        if (c.Type == ArcTypes.Inhibitor)
                        {
                            if (GetPlaceById(c.SourceId).Tokens >= c.Multiplicity)
                            {
                                transitionCanTrigger = false;
                                break;
                            }
                        }

                        // ArcTypes.Reset irrelevant for triggering 
                    }
                }
            }

            return transitionCanTrigger;
        }

        private void resolveConcurrencies()
        {
            if(IsDbgConcurrency()) log.Exec("\n### Concurrency verification...");
            foreach (Place cp in concPlaceList)
            {
                if(IsDbgConcurrency()) log.Exec("\n# Place " + cp.ID + "," + cp.Label);
                Random rnd = new Random();
                //Detects highest priority transitions
                int highestPriority = -1;
                int highestPriorityCounter = 1; //quantity of transitions with Highest priority
                foreach (Transition t in cp.GetConcTransitionsList())
                {
                    if (t.Enabled && t.Triggerable)
                    {
                        if (highestPriority < 0) highestPriority = t.Priority;
                        else
                        {
                            if (t.Priority > highestPriority)
                            {
                                highestPriority = t.Priority;
                                highestPriorityCounter = 1;
                            }
                            else
                            {
                                if (t.Priority == highestPriority)
                                    highestPriorityCounter++;
                            }
                        }
                    }
                }
                if(IsDbgConcurrency()) log.Exec("\nhighest priority:" + highestPriority);

                //sets only ONE transition as Triggerable (between all highest priority transitions)
                //all other highest priority transitions are setted as non triggerable
                if (highestPriority > -1)
                {
                    //Selects (pseudo randomly) between highest priority transitions
                    int transitionSelected = rnd.Next(0, highestPriorityCounter);
                    int counter = 0;
                    if(IsDbgConcurrency()) log.Exec("\n# Transition count randomly selected:" + transitionSelected);

                    foreach (Transition t in cp.GetConcTransitionsList())
                    {
                        if (t.Enabled && t.Triggerable)
                        {
                            if (t.Priority == highestPriority)
                            {
                                if (counter != transitionSelected)
                                {
                                    t.Triggerable = false;
                                    if(IsDbgConcurrency()) log.Exec("\n# Triggerable set to false on " + t.Id + "," + t.Label);
                                }

                                counter++;
                            }
                            else
                            {
                                t.Triggerable = false;
                                if(IsDbgConcurrency()) log.Exec("\n# Triggerable set to false on " + t.Id + "," + t.Label);
                            }
                        }
                    }
                }
            }
        }


        /*
         * Insertion methods
         */

        public void InsertTransition(Transition transition)
        {
            transitionList.Add(transition);
        }

        public void InsertPlace(Place place)
        {
            placeList.Add(place);
        }

        public void InsertConnection(Connection conn)
        {
            connList.Add(conn);
        }

        /*
         * Get methods
         */

        public Transition GetTransition(int pos)
        {
            return (Transition) transitionList[pos];
        }

        public Place GetPlace(int pos)
        {
            return (Place) placeList[pos];
        }

        public Place GetPlaceById(int id)
        {
            foreach (Place p in placeList)
            {
                if (p.ID == id)
                    return p;
            }

            if(IsDbgExec()) log.Exec("Place " + id + " not found!");
            return null;
        }

        public Place GetPlaceByLabel(String label)
        {
            foreach (Place p in placeList)
            {
                if (p.Label == label)
                    return p;
            }

            if(IsDbgExec()) log.Exec("Place " + label + " not found!");
            return null;
        }

        public Transition GetTransitionById(int id)
        {
            foreach (Transition t in transitionList)
            {
                if (t.Id == id)
                    return t;
            }

            if(IsDbgExec()) log.Exec("Transition " + id + " not found!");
            return null;
        }

        public Transition GetTransitionByLabel(String label)
        {
            foreach (Transition t in transitionList)
            {
                if (t.Label == label)
                    return t;
            }

            if(IsDbgExec()) log.Exec("Transition " + label + " not found!");
            return null;
        }

        public Connection GetConnection(int sourceId, int destinationId)
        {
            foreach (Connection c in connList)
            {
                if (c.SourceId == sourceId && c.DestinationId == destinationId)
                    return c;
            }

            if(IsDbgExec()) log.Exec("Connection from " + sourceId + " to " + destinationId + "not found!");
            return null;
        }

        /*
         * Remove methods
         */

        public Boolean RemovePlaceById(int id)
        {
            Place p = GetPlaceById(id);
            if (p != null)
            {
                placeList.RemoveAt(placeList.IndexOf(p));
                if(IsDbgExec()) log.Exec("Place " + id + " removed.");
                return true;
            }

            if(IsDbgExec()) log.Exec("Can't remove Place " + id + ".");
            return false;
        }

        public Boolean RemoveTransitionById(int id)
        {
            Transition t = GetTransitionById(id);
            if (t != null)
            {
                transitionList.RemoveAt(transitionList.IndexOf(t));
                if(IsDbgExec()) log.Exec("Transition " + id + " removed.");
                return true;
            }

            if(IsDbgExec()) log.Exec("Can't remove Transition " + id + ".");
            return false;
        }

        public Boolean RemoveConnection(int sourceId, int destinationId)
        {
            Connection c = GetConnection(sourceId, destinationId);
            if (c != null)
            {
                connList.RemoveAt(connList.IndexOf(c));
                if(IsDbgExec()) log.Exec("Connection from " + sourceId + " to " + destinationId + " removed.");
                return true;
            }

            if(IsDbgExec()) log.Exec("Can't remove Connection from " + sourceId + " to " + destinationId + ".");
            return false;
        }

        /*
         *  Load Petri Net description from file  (pflow format - PNEditor) 
         */

        public Boolean LoadFromFile(String filename)
        {
            if(IsDbgLoad()) log.Load("\nPetriNet internal name: " + id.Name);
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(filename);
            }
            catch (FileNotFoundException)
            {
                if(IsDbgLoad()) log.Load("\nFile not found...");
                if(IsDbgExec()) log.Exec("\nFile not found...");
                return false;
            }
            catch (ArgumentNullException)
            {
                if(IsDbgLoad()) log.Load("\nFilename is empty...");
                if(IsDbgExec()) log.Exec("\nFile not found...");
                return false;
            }

            Navigate(xmlDoc.DocumentElement);
            return true;
        }


        /*
         * for debugging purposes...
         */
        private void ShowDictionary()
        {
            if (IsDbgLoad())
            {
                log.Load("\n\nReference Places dictionary...");
                foreach (KeyValuePair<int, int> pair in refPlaces)
                {
                    log.Load("\n: " + pair.Key + ", " + pair.Value);
                }
            }
        }

        private void showArrays()
        {
            if (IsDbgLoad())
            {
                log.Load("\nGenerated Arrays\n\nPlaces:");
                foreach (Place p in placeList)
                {
                    log.Load("\nid:" + p.ID + ", lbl:" + p.Label + ", toks:" + p.Tokens + ", conc:" + p.HasConcurrency);
                }
    
                log.Load("\n\nTransitions:");
                foreach (Transition t in transitionList)
                {
                    log.Load("\nid:" + t.Id + ", lbl:" + t.Label + ", enab:" + t.Enabled);
                }
    
                log.Load("\n\nConnections:");
                foreach (Connection c in connList)
                {
                    log.Load("\nsrc:" + c.SourceId + ", dst:" + c.DestinationId + ", type:" + c.Type + ", mul:" + c.Multiplicity);
                }
            }
        }

        public void showPlacesTransitions()
        {
            if (IsDbgExec())
            {
                log.Exec("\n________________");
                log.Exec("\n>>> Places:");
                foreach (Place p in placeList)
                {
                    log.Exec("\n" + p.ID + "\t| " + p.Label + "\t| " + p.Tokens);
                }
    
                log.Exec("\n>>> Transitions:");
                foreach (Transition t in transitionList)
                {
                    log.Exec("\n" + t.Id + "\t| " + t.Label + "\t| " + t.Priority + "\t| " + t.Enabled + "\t| " + t.Triggerable);
                }
            }
        }

        /*
         * Flatts entire net, removing ref places and ref arcs (i.e., collapsing subnets)...
         */
        private void Flattening()
        {
            foreach (Connection c in connList)
            {
                if (refPlaces.ContainsKey(c.SourceId))
                {
                    c.SourceId = GetFinalValue(c.SourceId);
                }

                if (refPlaces.ContainsKey(c.DestinationId))
                {
                    c.DestinationId = GetFinalValue(c.DestinationId);
                }
            }
        }

        /*
         * self explanatory...
         */
        private void ChecksPlacesWithConcurrency()
        {
            int conCounter;
            concPlaceList.Clear();
            foreach (Place p in placeList)
            {
                conCounter = 0;
                p.ClearConcTransitions();
                //discovers concurrency for each place
                foreach (Connection c in connList)
                {
                    if (c.SourceId == p.ID && (c.Type == ArcTypes.Regular || c.Type == ArcTypes.Reset))
                        conCounter++;
                }

                //if concurrency present, adds transitions to ConcTransition list
                //and marks concurrency for that place...
                if (conCounter > 1)
                {
                    p.HasConcurrency = true;
                    concPlaceList.Add(p);
                    foreach (Connection c in connList)
                    {
                        if (c.SourceId == p.ID && (c.Type == ArcTypes.Regular || c.Type == ArcTypes.Reset))
                            p.AddConcTransition(GetTransitionById(c.DestinationId));
                    }
                }
            }
        }

        private int GetFinalValue(int key)
        {
            int value;
            while (true)
            {
                value = refPlaces[key];
                if (!refPlaces.ContainsKey(value))
                    break;
                else
                {
                    key = value;
                }
            }
            return value;
        }

        /*
         *  Load XML data into arrays (places, transitions, connections...)
         *  need recursive approach (for recursive subnets reading)
         */
        private void Navigate(XmlNode xmlDoc)
        {
            foreach (XmlNode node in xmlDoc.ChildNodes)
            {
                if (node.Name == "subnet")
                {
                    if(IsDbgLoad()) log.Load("\nsubnet +++\n");

                    foreach (XmlNode nod in node.ChildNodes)
                    {
                        log.Load(nod.Name + ":" + "\t");
                        if (nod.Name == "place")
                        {
                            ReadPlace(nod);
                        }

                        if (nod.Name == "transition")
                        {
                            ReadTransition(nod);
                        }

                        if (nod.Name == "arc")
                        {
                            ReadConnection(nod);
                        }

                        if (nod.Name == "referencePlace")
                        {
                            ReadRefPlace(nod);
                        }

                        if (nod.Name == "subnet")
                        {
                            if(IsDbgLoad()) log.Load("\n +++ +++\n");
                            foreach (XmlNode no in nod.ChildNodes)
                            {
                                if(IsDbgLoad()) log.Load("|\t" + no.Name + ":" + "\t");

                                if (no.Name == "place")
                                {
                                    ReadPlace(no);
                                }

                                if (no.Name == "transition")
                                {
                                    ReadTransition(no);
                                }

                                if (no.Name == "arc")
                                {
                                    ReadConnection(no);
                                }

                                if (no.Name == "referencePlace")
                                {
                                    ReadRefPlace(no);
                                }

                                if (no.Name == "subnet")
                                {
                                    if(IsDbgLoad()) log.Load("\n +++ +++ +++\n");
                                    foreach (XmlNode n in no.ChildNodes)
                                    {
                                        if(IsDbgLoad()) log.Load("||\t\t" + n.Name + ":" + "\t");

                                        if (n.Name == "place")
                                        {
                                            ReadPlace(n);
                                        }

                                        if (n.Name == "transition")
                                        {
                                            ReadTransition(n);
                                        }

                                        if (n.Name == "arc")
                                        {
                                            ReadConnection(n);
                                        }

                                        if (n.Name == "referencePlace")
                                        {
                                            ReadRefPlace(n);
                                        }

                                        if (n.Name == "subnet")
                                        { 
                                            if(IsDbgLoad()) log.Load("\n +++ +++ +++ +++\n");
                                                foreach (XmlNode m in n.ChildNodes)
                                                {
                                                    if(IsDbgLoad()) log.Load("||\t\t\t" + m.Name + ":" + "\t");

                                                    if (m.Name == "place")
                                                    {
                                                        ReadPlace(m);
                                                    }

                                                    if (m.Name == "transition")
                                                    {
                                                        ReadTransition(m);
                                                    }

                                                    if (m.Name == "arc")
                                                    {
                                                        ReadConnection(m);
                                                    }

                                                    if (m.Name == "referencePlace")
                                                    {
                                                        ReadRefPlace(m);
                                                    }

                                                    if (m.Name == "subnet")
                                                    {
                                            
                                                    }

                                                    if(IsDbgLoad()) log.Load("\n");
                                                }
                                        }
                                        if(IsDbgLoad()) log.Load("\n");
                                    }
                                }
                                if(IsDbgLoad()) log.Load("\n");
                            }
                        }
                        if(IsDbgLoad()) log.Load("\n");
                    }
                }
            }
        }

        private void ReadPlace(XmlNode locNode)
        {
            Place place = new Place(ChangesOcurred, ExecUntilNothingMoreToDo, Id, log);
            InsertPlace(place);
            foreach (XmlNode locval in locNode)
            {
                if(IsDbgLoad()) log.Load("  " + locval.Name + ":" + locval.InnerText);
                if (locval.Name == "id")
                    place.ID = Int32.Parse(locval.InnerText);
                if (locval.Name == "tokens")
                    place.AddTokens(Int32.Parse(locval.InnerText), new PNidList(Id));
                if (locval.Name == "label")
                    place.Label = locval.InnerText;
            }

            if (place.Label[0] == '#')
            {
                place.AutoExec = true;
            }
            if (place.Label[0] == '!')
            {
                if (!place.Label.Contains("."))
                {
                    if(IsDbgLoad()) log.Load("Error parsing external place name: " + place.Label + " (delimiter . missing...)");
                }
                place.External= true;
                place.ExternalNetName = place.Label.Substring(1, place.Label.LastIndexOf(".") - 1);
                place.ExternalPlaceName = place.Label.Substring(place.Label.LastIndexOf(".") + 1, place.Label.Length - (place.Label.LastIndexOf(".") + 1));
            }
            else
            {
                place.External = false;
            }
        }

        private void ReadRefPlace(XmlNode locNode)
        {
            int id = 0;
            int dst = 0;

            foreach (XmlNode locval in locNode)
            {
                if(IsDbgLoad()) log.Load("  >>>  " + locval.Name + " " + locval.InnerText);
                if (locval.Name == "id")
                    id = Int32.Parse(locval.InnerText);
                if (locval.Name == "connectedPlaceId")
                    dst = Int32.Parse(locval.InnerText);
            }

            try
            {
                refPlaces.Add(id, dst); //insert in dictionary 
            }
            catch (ArgumentException)
            {
                if(IsDbgLoad()) log.Load("\nError processing reference place " + id + " ==> Key already exists in dictionary...\n");
            }
        }

        private void ReadTransition(XmlNode locNode)
        {
            Transition transition = new Transition();
            InsertTransition(transition);
            foreach (XmlNode locval in locNode)
            {
                if(IsDbgLoad()) log.Load("  " + locval.Name + ":" + locval.InnerText);
                if (locval.Name == "id")
                    transition.Id = Int32.Parse(locval.InnerText);
                if (locval.Name == "label")
                    transition.Label = locval.InnerText;
            }
        }

        private void ReadConnection(XmlNode locNode)
        {
            Connection connection = new Connection();
            InsertConnection(connection);
            foreach (XmlNode locval in locNode)
            {
                if(IsDbgLoad()) log.Load("  " + locval.Name + ":" + locval.InnerText);

                if (locval.Name == "sourceId")
                    connection.SourceId = Int32.Parse(locval.InnerText);
                if (locval.Name == "destinationId")
                    connection.DestinationId = Int32.Parse(locval.InnerText);
                if (locval.Name == "multiplicity")
                    connection.Multiplicity = Int32.Parse(locval.InnerText);
                if (locval.Name == "type")
                {
                    switch (locval.InnerText)
                    {
                        case "regular":
                            connection.Type = ArcTypes.Regular;
                            break;
                        case "inhibitor":
                            connection.Type = ArcTypes.Inhibitor;
                            break;
                        case "reset":
                            connection.Type = ArcTypes.Reset;
                            break;
                    }
                }
            }
        }

        public void SetDebug(DebugMode mode)
        {
            dbg = mode;
            //UnityEngine.Debug.Log("debug:" + dbg);
        }

        public Boolean IsDbgExec()
        {
            return ((dbg & DebugMode.Exec) > 0 || dbg == DebugMode.All);
        }
        public Boolean IsDbgLoad()
        {
            return ((dbg & DebugMode.Load) > 0 || dbg == DebugMode.All);
        }
        public Boolean IsDbgConcurrency()
        {
            return ((dbg & DebugMode.Concurrency) > 0 || dbg == DebugMode.All);
        }

    }
}