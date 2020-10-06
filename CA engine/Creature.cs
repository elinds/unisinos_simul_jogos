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
        private int cntr = 0;

        public void Init(CA ca, int factor, GameObject gob, int refreshRate = 100, int groudElevation = 0)
        {
            this.ca = ca;
            this.factor = factor;
            this.gob = gob;
            this.refreshRate = refreshRate;
            this.groundElevation = groudElevation;
            cellsAlive = ca.GetAlives();
            createCreature();
        }        

        public void Update()
        {
            cntr++;
            if (cntr == refreshRate)
            {
                clearCreature();
                cellsAlive = ca.Next();
                createCreature();
                cntr = 0;
            }
 }
        private void createCreature()
        {
            int adjustX = (ca.Lin / 2) / factor;
            int adjustZ = (ca.Col / 2) / factor;
            int i = 0;
            while (i < cellsAlive.Count)
            {
                creatureGo.Add(Instantiate(gob));
                gob.transform.position = new Vector3(cellsAlive[i].Lin - adjustX ,cellsAlive[i].Hei - 3 + this.groundElevation,cellsAlive[i].Col - adjustZ);
                //gob.transform.localScale = new Vector3(-0.5f, -0.5f, -0.5f);
                i++;
            }
        }

        private void clearCreature()
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