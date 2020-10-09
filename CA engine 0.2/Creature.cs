/*
 * CA engine 0.2
 */
using System.Collections;
using System.Collections.Generic;
using CAengine;
using UnityEngine;
using Random = System.Random;

namespace CAengine
{
    public class Creature: ScriptableObject
    {
        private CA ca;
        private List<Cell> cellsAlive = new List<Cell>();
        private List<GameObject> creatureGo = new List<GameObject>();

        private GameObject gob;
        private int refreshRate;
        private int groundElevation;

        private int factor;
        //private float scale;
        private int cntr = 0;
        
        private int adjustX;
        private int adjustZ;

        public void Init(CA ca, int factor, GameObject gob, int refreshRate = 100, int groudElevation = 0)
        {
            this.ca = ca;
            this.factor = factor;
            this.gob = gob;
            this.refreshRate = refreshRate;
            this.groundElevation = groudElevation;
            cellsAlive = ca.GetAlives();
            
            adjustX = (ca.Lin / 2);
            adjustZ = (ca.Col / 2);

            CreateCreature();
        }        

        public void Update()
        {
            cntr++;
            if (cntr == refreshRate)
            {
                cellsAlive = ca.Next();
                UpdateCreature();
                cntr = 0;
            }
 }
        private void CreateCreature()
        {
            int i = 0;
            while (i < cellsAlive.Count)
            {
                creatureGo.Add(Instantiate(gob));
                gob.transform.position = new Vector3(cellsAlive[i].Lin - adjustX ,cellsAlive[i].Hei - 3 + this.groundElevation,cellsAlive[i].Col - adjustZ);
                i++;
            }
        }
        private void UpdateCreature()
        {
            int i;
            int diff = cellsAlive.Count - creatureGo.Count;
            //equalizes quantities of cells alive and Gobs
            if (diff > 0)  
            {
                i = 0;
                while (i < diff)
                {
                    creatureGo.Add(Instantiate(gob));
                    i++;
                }
            }
            else
            {
                if (diff < 0)
                {
                    i = 0;
                    while (i < (diff * -1))
                    {
                        Destroy(creatureGo[0]);
                        creatureGo.RemoveAt(0);
                        i++;
                    }
                }
            }

            i = 0;
            while (i < cellsAlive.Count)
            {
                creatureGo[i].transform.position = new Vector3(cellsAlive[i].Lin - adjustX ,cellsAlive[i].Hei - 3 + this.groundElevation,cellsAlive[i].Col - adjustZ);
                //creatureGo[i].transform.localScale = new Vector3(1 + cellsAlive[i].Hei, 1, 1 + cellsAlive[i].Hei);
                i++;
            }
        }
        private void ClearCreature()
        {
            int i = 0;
            while (i < creatureGo.Count)
            {
                Destroy(creatureGo[i]);
                i++;
            }
            creatureGo.Clear();
        }
    }
}