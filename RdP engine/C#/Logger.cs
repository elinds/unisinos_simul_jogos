using System;
using System.Collections;

namespace RdPengine
{
    public class Logger
    {
        private String logFilename_load;
        private String logFilename_exec;
        public Logger(String netName)
        {
            logFilename_load = netName + "_Load Log.txt"; 
            logFilename_exec = netName + "_Exec Log.txt";
            
            //erase log file load
            System.IO.File.WriteAllText(@logFilename_load, "Log: File reading events...");
            //erase log file exec
            System.IO.File.WriteAllText(@logFilename_exec, "Log: Execution events...");  
        }

        
        public void Load(String message)
        {
            using (System.IO.StreamWriter logFile =
                new System.IO.StreamWriter(@logFilename_load , true))
            {
                logFile.Write(message);
            }
        }
        public void Exec(String message)
        {
            using (System.IO.StreamWriter logFile =
                new System.IO.StreamWriter(@logFilename_exec , true))
            {
                logFile.Write(message);
            }
        }

        public String ArrayListContent(ArrayList arrList)
        {
            String ret = ">";

            if (arrList == null)
            {
                ret += "-";
            }
            else
            {
                for (int i = 0; i < arrList.Count; i++)
                {
                    try
                    {
                        if (arrList[i] != null)
                        {
                            ret += (arrList[i].ToString() + "|");
                        }
                        else
                        {
                            ret += "x";
                        }
                    }                    
                    catch (Exception e)
                    {
                        //UnityEngine.Debug.Log("Logger exception:" + e.GetType().ToString());
                    }

                }
            }
            return ret;
        }
    }
}