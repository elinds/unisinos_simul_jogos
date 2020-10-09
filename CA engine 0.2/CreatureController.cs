/*
 * CA engine 0.2
 */
using CAengine;
using UnityEngine;

public class CreatureController : MonoBehaviour
{
    public int caWidth;
    public int caHeight;

    private Creature blinker, amoeba, amoeba2, glider, tower;
   
    public GameObject gob, gob2; //gameobjects de uma celula da criatura

    void Start()
    {
        int factor = 1;
        caWidth = 128 * factor;
        caHeight = 72 * factor;
        
        /*CA caAmoeba2 = new CA(caWidth, caHeight, 2,Neighbourhood.Moore,
            new RuleSet("358", "23")); 
        caAmoeba2.SetRegion(0.83,0,1,10,30,5,25);
        amoeba2 = (Creature) ScriptableObject.CreateInstance("Creature");
        amoeba2.Init(caAmoeba2, factor, gob, 100);*/

        CA caAmoeba = new CA(caWidth, caHeight, 2,Neighbourhood.Moore,
            "Amoeba");  
        caAmoeba.SetRegion(0.81,0,1,50,60,45,55);
        amoeba = (Creature) ScriptableObject.CreateInstance("Creature");
        amoeba.Init(caAmoeba, factor, gob, 11);

        CA caBlinker = new CA(caWidth,caHeight,2,Neighbourhood.Moore,"GameOfLife");
        caBlinker .SetPattern(65,50,"Blinker");
        blinker = (Creature) ScriptableObject.CreateInstance("Creature");
        blinker.Init(caBlinker, factor, gob,10);
        
        CA caGlider = new CA(caWidth,caHeight,2,Neighbourhood.Moore,"GameOfLife");
        caGlider.SetPattern(55,10,"Glider");
        caGlider.SetPattern(30,40,"Glider");
        caGlider.SetPattern(60,20,"Glider");
        
        glider = (Creature) ScriptableObject.CreateInstance("Creature");
        glider.Init(caGlider, factor, gob2, 10, 15);
        
        CA caTower = new CA(caWidth,caHeight,2,Neighbourhood.Moore,"GameOfLife",10);
        caTower .SetPattern(45,20,"Blinker");
        tower = (Creature) ScriptableObject.CreateInstance("Creature");
        tower.Init(caTower, factor, gob,333);
        
    }
    
    void Update()
    {
        //amoeba2.Update();
        amoeba.Update();
        blinker.Update();
        glider.Update();
        tower.Update();
    }
}
