using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
//Done

public class BattleHud : MonoBehaviour
{   
    [SerializeField] TextMeshProUGUI nameText;

    //References the Rolling HpMeter to trigger it's logic
    [SerializeField] RollingHP HpMeter;
    

    //creates a reference to the player's stats.
    CharacterStat chara;

    public int DisplayedHP => HpMeter.DisplayedHP;
  
    public void SetData(CharacterStat characterStat)
    {
        chara = characterStat;
        nameText.text = characterStat.Base.Name;
        
        // Pass current HP and current PP to render active stats
        HpMeter.SetHp(characterStat.HP);
        HpMeter.SetPP(characterStat.PP);
    }

    public void UpdatePP(int currentPP)
    {
        // Instantly updates PP via SnapPPDigits without rolling
        HpMeter.SetPP(currentPP); 
    }

    public void ApplyDamage(int damage)
    {
        // Rolls the HP down smoothly over time
        HpMeter.ApplyDamage(damage); 
    }

    public int FreezeAndSyncHP()
    {   //stops the HpMeter from rolling, snaps the digits and stops all coroutines.
        return HpMeter.FreezeAndSnap();
    }
}