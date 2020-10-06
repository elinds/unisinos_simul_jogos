using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace CAengine
{    public enum Neighbourhood
    {
        Moore = 0,
        VonNeumann = 1
    }
    public class CA
    {
        private int[,,] cell;
        private int[,,] ncell;
        private int lin, col;
        private int maxState;
        private Neighbourhood n;
        private RuleSet rs;
        private Boolean rainbow;
        private int generations;

        private List<Cell> alive = new List<Cell>();
        public int Lin
        {
           get => lin;
           set => lin = value;
        }

        public int Col
        {
           get => col;
           set => col = value;
        }     
        private Dictionary<String,int[,]> patterns= new Dictionary<String,int[,]>();
        private Dictionary<String, RuleSet> rules = new Dictionary<String, RuleSet>();

        /*
         * states: 0 -> dead    >0 and higher -> alive
         */
        public CA(int lin, int col,int states, Neighbourhood n, RuleSet rs, int generations = 1)
        {
            InitializeDictionaries();
            this.rs = rs;
            InitializeCA(lin, col, states, n, generations);
        }

        public CA(int lin, int col, int states, Neighbourhood n, String ruleName, int generations = 1)
        {
            InitializeDictionaries();
            if (rules.ContainsKey(ruleName))
                this.rs = rules[ruleName];
            InitializeCA(lin, col, states, n, generations);
        }

        private void InitializeCA(int lin, int col, int states, Neighbourhood n, int generations)
        {
            cell = new int[lin, col, generations];
            ncell =  new int[lin, col, generations];
            this.maxState = states - 1;
            this.generations = generations;
            
            this.lin = lin;
            this.col = col;
            this.n = n;
            
            this.rainbow = (states > 2) ? true : false;          
        }
        public void SetRegion( double density,
            int infLimValue , int supLimValue,
            int iniLine , int finLine  , int iniCol, int finCol)
        {
            Random rnd = new Random();
            for (int i = iniLine; i < finLine; i++)
            {
                for (int j = iniCol; j < finCol; j++)
                {
                    if (rnd.NextDouble() < density)
                    {
                        cell[i, j, 0] = (rainbow)?rnd.Next(infLimValue, supLimValue):supLimValue;
                    }
                }
            }
        }

        private int countNeighbours(int i, int j) //count not dead neighbours
        {
            int cntr = 0;
            int ci, cj;
            //Moore
            for (int ii = i - 1; ii <= i + 1; ii++)
            {
                for (int jj = j - 1; jj <= j + 1; jj++)
                {
                    ci = ii < 0 ? lin - 1 : ((ii == lin)? 0 : ii);
                    cj = jj < 0 ? col - 1 : ((jj == col)? 0 : jj);
                    cntr += (cell[ci, cj, 0] > 0 ? 1: 0);
                }
            }
            return cntr - ((cell[i,j, 0]>0)?1:0);
        }
        private int sumNeighbours(int i, int j) //count not dead neighbours
        {
            int cntr = 0;
            int ci, cj;
            //Moore
            for (int ii = i - 1; ii <= i + 1; ii++)
            {
                for (int jj = j - 1; jj <= j + 1; jj++)
                {
                    ci = ii < 0 ? lin - 1 : ((ii == lin)? 0 : ii);
                    cj = jj < 0 ? col - 1 : ((jj == col)? 0 : jj);
                    cntr += (cell[ci, cj, 0] > 0 ? cell[ci, cj, 0] : 0);
                }
            }
            return cntr - cell[i,j,0];
        }
        
        /*
         * Step generates next cell space state in ncell
         */
        private void Step()
        {
            int neiAlive = 0;
            int sumAlive = 0;
            Boolean foundCase;
            for (int i = 0; i < lin; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    neiAlive = countNeighbours(i, j);
                    if(rainbow) sumAlive = sumNeighbours(i, j);
                    //Rule: S
                    if (cell[i, j, 0] == 0) //if cell dead
                    {
                        foundCase = false;
                        for (int s = 0; s < rs.S.Count; s++)
                        {
                            if (neiAlive == rs.getS(s))
                            {
                                foundCase = true;
                            }
                        }
                        if (foundCase) ncell[i, j, 0] = (rainbow)?sumAlive/neiAlive:maxState;
                        else ncell[i, j, 0] = 0;
                    }
                    //Rule: B
                    if (cell[i, j, 0] > 0)  //if cell not dead
                    { 
                        foundCase = false;
                        for (int b = 0; b < rs.B.Count; b++)
                        { 
                            if (neiAlive == rs.getB(b))
                            {
                                foundCase = true;
                            }
                        }
                        if(!foundCase) ncell[i, j, 0] = 0;
                        else ncell[i, j, 0] = cell[i,j, 0];
                    }                    
                }
            }           
        }

        public List<Cell> Next()
        {
           Step();
           alive.Clear();
           if (generations > 1)
           {
             for (int k = generations - 1; k > 0; k--)
             {
                 for (int m = 0; m < lin; m++)
                    for (int n = 0; n < col; n++) 
                        cell[m, n, k] = cell[m, n, k - 1];
             }
           }         
           for (int i = 0; i < lin; i++)
           {
               for (int j = 0; j < col; j++)
               {
                   cell[i, j, 0] = ncell[i, j, 0]; //update cell Cell Space
                   //if cell alive, add it to alive cells List
                   if(cell[i,j,0] > 0) 
                       alive.Add(new Cell(i,j, 0 , cell[i,j,0]));
               }
           }
           if (generations > 1)
           {
               for (int k = generations - 1; k > 0; k--)
               {
                   for (int m = 0; m < lin; m++)
                        for (int n = 0; n < col; n++) 
                            if(cell[m,n,k] > 0) 
                                alive.Add(new Cell(m,n, k , cell[m,n,k]));
               }
           }
           return alive;
        }
        public List<Cell> GetAlives()
        {
            alive.Clear();
            for (int i = 0; i < lin; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    if(cell[i,j,0] > 0)
                        alive.Add(new Cell(i,j, 0,cell[i,j,0]));
                }
            }
            if (generations > 1)
            {
                for (int k = generations - 1; k > 0; k--)
                {
                    for (int m = 0; m < lin; m++)
                    for (int n = 0; n < col; n++) 
                        if(cell[m,n,k] > 0) 
                            alive.Add(new Cell(m,n, k , cell[m,n,k]));
                }
            }
            return alive;
        }

        public void SetCell(int i, int j, int value)
        {
            int ci, cj;
            ci = i < 0 ? lin - i : ((i >= lin)? i - lin : i);
            cj = j < 0 ? col - i : ((j >= col)? j - col : j);
            cell[ci, cj,0] = value;
        }

        public int GetCell(int i, int j)
        {
            return cell[i, j,0];
        }

        public Boolean SetPattern(int pi, int pj, String patternName)
        {
            if (patterns.ContainsKey(patternName))
            {
                int[,] pat = patterns[patternName];
                for(int i = 0; i < Math.Sqrt(pat.Length); i++)
                {
                    for (int j = 0; j < Math.Sqrt(pat.Length); j++)
                    {
                        SetCell(pi + i,pj + j,pat[i,j]);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        private void InitializeDictionaries()
        {
            rules.Add("GameOfLife", new RuleSet("3","23"));
            rules.Add("Amoeba",new RuleSet("357","1358"));
            
            //Patterns need to be square...
            patterns.Add("Glider",new int[3,3]{{0,1,0},{0,0,1},{1,1,1}});
            patterns.Add(("Blinker"), new int[3,3]{{0,1,0},{0,1,0},{0,1,0}});
            patterns.Add("Toad",new int[4,4]{{0,0,0,0},{0,1,1,1},{1,1,1,0},{0,0,0,0}});
        }
    }
}