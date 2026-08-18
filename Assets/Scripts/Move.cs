using UnityEngine;
//done
public class Move 
{   //references MoveBase and exposes its values via pbase, which were called in BattleSystem.
    //and BattleDialogBox
    public MoveBase Pbase{get; set;}
    public int cost {get; set;}

    public Move(MoveBase pbase){
       Pbase = pbase;
         cost = pbase.Cost;
    }
}
