/*
 * RdP engine     version 0.42
 *
 * supports PNEditor file format (pflow)
 * 
 * author: Ernesto Lindstaedt
 *
 * Last modified  august 2020
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace RdPengine
{
    class PetriNet
    {
        private ArrayList placeList;
        private ArrayList transitionList;
        private ArrayList connList;
        private ArrayList concPlaceList;

        private Dictionary<int,int> refPlaces = new Dictionary<int, int>();
        private String logFilename_load;
        private String logFilename_exec;

        private Boolean somethingChanged;

        public PetriNet()
        {
            placeList = new ArrayList();
            transitionList = new ArrayList();
            connList = new ArrayList();
            concPlaceList = new ArrayList();

            somethingChanged = true;
        }

        public PetriNet(String filename)
        {
            placeList = new ArrayList();
            transitionList = new ArrayList();
            connList = new ArrayList();
            concPlaceList = new ArrayList();

            somethingChanged = true;

            logFilename_load = filename.Replace('.','_') + "_Load Log.txt"; 
            logFilename_exec = filename.Replace('.','_') + "_Exec Log.txt";

            if (LoadFromFile(filename))
            {
                //debugging
                ShowDictionary();
                showArrays();
                
                //post-processing, obligatory!
                Flattening();
                ChecksPlacesWithConcurrency();
                
                //debugging
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
                    LogExec("Error: Place " + p.ID + " with " + p.Tokens + " tokens...");
                    foundProblems = true;
                }
            }
            foreach (Connection c in connList)
            {
                if (c.Multiplicity < 0)
                {
                     LogExec("Error: Connection from " + c.SourceId + " to " + c.DestinationId + " multiplicity " + c.Multiplicity); 
                     foundProblems = true;
                }
                   
            }
            return foundProblems;
        }
        
        /*
         * Execution methods
         */
        public void ExecUntillNothingMoreToDo()
        {
            while (ExecCycle());
        }
        public Boolean ExecCycle()
        {
            if (!somethingChanged)
            {
                //LogExec("\n >>>> >>>> >>>> nothing changed... ");
                return false;
            }
                
            
            Boolean somethingToDo = false;
            
            //SearchForInconsistencies();  
            //if structural changes ocurred: ins/rem  of  place/trans/conn
            
            
            //check triggerable transitions...
            //LogExec("\nTriggerable transitions -> BEFORE concurrency check: ");
            foreach (Transition t in transitionList)
            {
               if(t.Triggerable = IsTriggerable(t)) 
                   somethingToDo = true;
            }

            if (somethingToDo) resolveConcurrencies();
            
            //LogExec("\nTriggerable transitions -> AFTER concurrency check: ");
            //foreach (Transition t in transitionList)   LogExec("  |" + t.Id + "," + t.Label + "|" + t.Triggerable);
            
            if (somethingToDo)
            {
                //LogExec("\n #### #### #### something to do ... ");
                //FIREs TRANSITIONS
                foreach (Transition t in transitionList)
                {
                    if (t.Triggerable && t.Enabled)
                    {
                        //call all Transition Callback methods...
                        t.ExecCallbacks();
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
                                            GetPlaceById(c.SourceId).RemTokens(c.Multiplicity);
                                            //LogExec("\n --- removed " + c.Multiplicity + " tokens at " + GetPlaceById(c.SourceId).ID + "," + GetPlaceById(c.SourceId).Label);
                                            break;
                                        case ArcTypes.Reset:
                                            GetPlaceById(c.SourceId).Tokens = 0;
                                            break;
                                    }

                                }
                            }
                            //generate tokens at output places
                            foreach (Connection c in connList)
                            {
                                if (c.SourceId == t.Id)
                                {
                                    GetPlaceById(c.DestinationId).AddTokens(c.Multiplicity);
                                    //LogExec("\n +++ generated " + c.Multiplicity + " tokens at " + GetPlaceById(c.DestinationId).ID + "," + GetPlaceById(c.DestinationId).Label);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            LogExec("\nException while firing transitions...");
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
            //LogExec("\n### Concurrency verification...");
            foreach (Place cp in concPlaceList)
            {
                //LogExec("\n# Place " + cp.ID + "," + cp.Label);
                Random rnd = new Random();
                //Detects highest priority transitions
                int highestPriority = -1;
                int highestPriorityCounter = 1;  //quantity of transitions with Highest priority
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
                //LogExec("\nhighest priority:" + highestPriority);
                
                //sets only ONE transition as Triggerable (between all highest priority transitions)
                //all other highest priority transitions are setted as non triggerable
                if (highestPriority > -1)
                {
                    //Selects (pseudo randomly) between highest priority transitions
                    int transitionSelected = rnd.Next(0,highestPriorityCounter);
                    int counter = 0;
                    //LogExec("\n# Transition count randomly selected:" + transitionSelected);
                    
                    foreach (Transition t in cp.GetConcTransitionsList())
                    {
                        if (t.Enabled && t.Triggerable)
                        {
                            if (t.Priority == highestPriority)
                            {
                                if (counter != transitionSelected)
                                {
                                    t.Triggerable = false;
                                    //LogExec("\n# Triggerable set to false on " + t.Id + "," + t.Label);
                                }
                                counter++;
                            }
                            else
                            {
                                t.Triggerable = false; 
                                //LogExec("\n# Triggerable set to false on " + t.Id + "," + t.Label);
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
            LogExec("Place " + id + " not found!");
            return null;
        }
        public Place GetPlaceByLabel(String label)
        {
            foreach (Place p in placeList)
            {
                if (p.Label == label)
                    return p;
            }
            LogExec("Place " + label + " not found!");
            return null;
        }       
        public Transition GetTransitionById(int id)
        {
            foreach (Transition t in transitionList)
            {
                if (t.Id == id)
                    return t;
            }
            LogExec("Transition " + id + " not found!");
            return null;
        }
        public Transition GetTransitionByLabel(String label)
        {
            foreach (Transition t in transitionList)
            {
                if (t.Label == label)
                    return t;
            }
            LogExec("Transition " + label + " not found!");
            return null;
        }
         public Connection GetConnection(int sourceId, int destinationId)
         {
             foreach (Connection c in connList)
             {
                 if (c.SourceId == sourceId && c.DestinationId == destinationId)
                     return c;
             }
             LogExec("Connection from " + sourceId + " to " + destinationId + "not found!");
             return null;
         }
         
        /*
         * Remove methods
         */ 
        
        public Boolean RemovePlaceById(int id)
         {
             Place p = GetPlaceById(id);
                 if(p != null)
                 {
                         placeList.RemoveAt(placeList.IndexOf(p));
                         LogExec("Place " + id + " removed.");
                         return true;
                 }
                 LogExec("Can't remove Place " + id + ".");
                 return false;
         }
         public Boolean RemoveTransitionById(int id)
          {
              Transition t = GetTransitionById(id);
                  if(t != null)
                  {
                          transitionList.RemoveAt(transitionList.IndexOf(t));
                          LogExec("Transition " + id + " removed.");
                          return true;
                  }
                  LogExec("Can't remove Transition " + id + ".");
                  return false;
          }     
          public Boolean RemoveConnection(int sourceId, int destinationId)
           {
               Connection c = GetConnection(sourceId, destinationId);
                   if(c != null)
                   {
                           connList.RemoveAt(connList.IndexOf(c));
                           LogExec("Connection from " + sourceId + " to "+ destinationId + " removed.");
                           return true;
                   }
                   LogExec("Can't remove Connection from " + sourceId + " to "+ destinationId + ".");
                   return false;
           }         
        
          /*
           *  Load Petri Net description from file  (pflow format - PNEditor) 
           */
          
          public Boolean LoadFromFile(String filename)
            {
             //erase log file load
             System.IO.File.WriteAllText(@logFilename_load, "Log: File reading events...");
             //erase log file exec
             System.IO.File.WriteAllText(@logFilename_exec, "Log: Execution events...");   
             
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(filename);
            }
            catch (FileNotFoundException)
            {
                Log("\nFile not found...");
                LogExec("\nFile not found...");
                return false;
            }
            catch (ArgumentNullException)
            {
                Log("\nFilename is empty...");
                LogExec("\nFile not found...");
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
            Log("\n\nReference Places dictionary...");
            foreach (KeyValuePair<int, int> pair in refPlaces)
            {
                Log("\n: " + pair.Key + ", " + pair.Value);
            }
        }

        private void showArrays()
        {
            Log("\nGenerated Arrays\n\nPlaces:");
            foreach (Place p in placeList)
            {
                Log("\nid:" + p.ID + ", lbl:" + p.Label + ", toks:" + p.Tokens + ", conc:" + p.HasConcurrency);
            }
            Log("\n\nTransitions:");
            foreach (Transition t in transitionList)
            {
                Log("\nid:" + t.Id + ", lbl:" + t.Label + ", enab:" + t.Enabled);
            }
            Log("\n\nConnections:");
            foreach (Connection c in connList)
            {
                Log("\nsrc:" + c.SourceId + ", dst:" + c.DestinationId + ", type:"+ c.Type + ", mul:" + c.Multiplicity);
            }
        }

        private void Log(String message)
        {
            using (System.IO.StreamWriter logFile =
                new System.IO.StreamWriter(@logFilename_load , true))
            {
                logFile.Write(message);
            }
        }
        private void LogExec(String message)
                {
                    using (System.IO.StreamWriter logFile =
                        new System.IO.StreamWriter(@logFilename_exec , true))
                    {
                        logFile.Write(message);
                    }
                }

        public void showPlacesTransitions()
        {
            LogExec("\n________________");
            LogExec("\n>>> Places:" );
            foreach (Place p in placeList)
            {
                LogExec("\n" + p.ID + "\t| " + p.Label + "\t| " + p.Tokens);
            }
            LogExec("\n>>> Transitions:");
            foreach (Transition t in transitionList)
            {
                LogExec("\n" + t.Id + "\t| " + t.Label + "\t| " + t.Priority + "\t| " + t.Enabled + "\t| " + t.Triggerable);
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
                    Log("\nsubnet +++\n");

                    foreach (XmlNode nod in node.ChildNodes)
                    {
                        Log(nod.Name + ":"+ "\t");
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
                            Log("\n +++ +++\n");
                            foreach (XmlNode no in nod.ChildNodes)
                            {
                                Log("|\t" + no.Name + ":" + "\t");

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
                                    Log("\n +++ +++ +++\n");
                                    foreach (XmlNode n in no.ChildNodes)
                                    {
                                        Log("||\t\t" + n.Name + ":" + "\t");

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
                                    
                                        }
                                        Log("\n");
                                    }
                                }
                                Log("\n");
                            }
                        }
                        Log("\n");
                    }
                }
            }
        }

        private void ReadPlace(XmlNode locNode)
        {
            Place place = new Place(ChangesOcurred, ExecUntillNothingMoreToDo);
            InsertPlace(place);
            foreach (XmlNode locval in locNode)
            {
                Log("  " + locval.Name + ":" + locval.InnerText);
                if (locval.Name == "id")
                    place.ID = Int32.Parse(locval.InnerText);
                if (locval.Name == "tokens")
                    place.AddTokens(Int32.Parse(locval.InnerText));
                if (locval.Name == "label")
                    place.Label = locval.InnerText;
            }
            if (place.Label[0] == '#')
            {
                place.AutoExec = true;
            }
                
        }
        private void ReadRefPlace(XmlNode locNode)
        {
            int id = 0;
            int dst = 0;
            
                foreach (XmlNode locval in locNode)
                {
                    Log("  >>>  " + locval.Name + " " + locval.InnerText);
                    if (locval.Name == "id")
                        id = Int32.Parse(locval.InnerText);
                    if (locval.Name == "connectedPlaceId")
                        dst = Int32.Parse(locval.InnerText);
                }

                try
                {
                    refPlaces.Add(id,dst); //insert in dictionary 
                }
                catch (ArgumentException)
                {
                    Log("\nError processing reference place " + id + " ==> Key already exists in dictionary...\n");
                }
        }
        private void ReadTransition(XmlNode locNode)
        {
            Transition transition = new Transition();
            InsertTransition(transition);
            foreach (XmlNode locval in locNode)
            {
                Log("  " + locval.Name + ":" + locval.InnerText);
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
                Log("  " + locval.Name + ":" + locval.InnerText);
                
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
    }
}