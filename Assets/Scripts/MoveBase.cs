using UnityEngine;
//done
using System.Collections;
using System.Collections.Generic;



// Static template data for a single move scriptableObject. wraps at runtime to Move.cs.
// values are read by CharacterStat.TakeDamage for damage calcs. 


[CreateAssetMenu(fileName="Move", menuName = "Elemental or Physical/Create new Move.")]
public class MoveBase : ScriptableObject
{
   [SerializeField] string name;

   
  //references the enum for the elemental types via a dropdown. The rest are typed into the inspector.
   [SerializeField] ElementalType type;
   [SerializeField] int power;
   [SerializeField] int accuracy;
   [SerializeField] int cost;

   
   

 public string Name
   {
      get{return name;}
   }


public ElementalType Type 
{
    get{return type;}
}
public int Power
{
    get{return power;}
}
public int Accuracy
{
    get {return accuracy;}
}


//The PP cost of a move. If the PP cost < PP amount currently have by character, then it can be selected
public int Cost
{
    get {return cost;}
}


//Leaves out None as the only Physical(atk-def) type. Compared to Special (Skill -IQ)
public bool IsSpecial
{
    get
    {
        return type == ElementalType.Lightning || type == ElementalType.Wind;
    }
}

}
