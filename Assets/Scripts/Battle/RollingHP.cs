using UnityEngine;
using System.Collections;
//done

public class RollingHP : MonoBehaviour
{
    public int startingHP;
    public int targetHP;
    public int currentPP;

    //0.16 seconds
    public float tickDuration = 0.16f;

    private int displayedHP;
    private float tickTimer;
    private bool initialized = false;
    
    //digits in the hp(hundreds, tens ones)
    //digits in the pp(hundreds, tens,ones)
    public RollingDigit hundreds; 
    public RollingDigit tens;
    public RollingDigit ones;
    public RollingDigit ppHundreds;
    public RollingDigit ppTens;
    public RollingDigit ppOnes;

    // 1. Reads current HP value displayed on the visual meter mid-roll
    public int DisplayedHP => displayedHP;

    void Start()
    {
        StartCoroutine(InitAfterAwake());
    }

    public void SetHp(int currentHP)
    {
        startingHP = currentHP;
        targetHP = currentHP;
        displayedHP = currentHP;
       
       //Displays the correct hp without rolling
        if (initialized)
        {   
            SnapDigits(currentHP);
        }
    }

    public void SetPP(int pp)
    {
        currentPP = Mathf.Max(pp, 0);
        

        if (initialized)
        {
            SnapPPDigits(currentPP);
        }
    }

    IEnumerator InitAfterAwake()
    {
        yield return null; 
       
       //sets the tick
        hundreds.SetTickDuration(tickDuration);
        tens.SetTickDuration(tickDuration);
        ones.SetTickDuration(tickDuration);

        displayedHP = targetHP;
        SnapDigits(displayedHP);
        SnapPPDigits(currentPP);

        tickTimer = tickDuration;
        initialized = true;
    }

    private void SnapDigits(int value)
    {
        int hp = Mathf.Max(value, 0);
        bool showHundreds = hp >= 100;
        bool showTens = hp >= 10 || showHundreds;

        hundreds.currentDigit = showHundreds ? (hp / 100) % 10 : -1;
        tens.currentDigit = showTens ? (hp / 10) % 10 : -1;
        ones.currentDigit = hp % 10;

        hundreds.SnapToCurrent();
        tens.SnapToCurrent();
        ones.SnapToCurrent();
    }

    private void SnapPPDigits(int value)
    {
        int pp = Mathf.Max(value, 0);
        bool showHundreds = pp >= 100;
        bool showTens = pp >= 10 || showHundreds;
        

        ppHundreds.currentDigit = showHundreds ? (pp / 100) % 10 : -1;
        ppTens.currentDigit = showTens ? (pp / 10) % 10 : -1;
        ppOnes.currentDigit = pp % 10;

        ppHundreds.SnapToCurrent();
        ppTens.SnapToCurrent();
        ppOnes.SnapToCurrent();
    }

    public void ApplyDamage(int dmg)
    {
        targetHP = Mathf.Max(0, targetHP - dmg);
    }

    public void PPdeduct(int cost)
    {
        currentPP = Mathf.Max(0, currentPP - cost);
        SnapPPDigits(currentPP);
    }

    // 2. Freezes rolling, stops coroutines, and snaps digits immediately
    public int FreezeAndSnap()
    {
        StopAllCoroutines();
        
        // Equalize target to displayedHP so Update() stops ticking down
        targetHP = displayedHP; 

        // Force sub-frame sprites to snap directly to current displayed digit
        SnapDigits(displayedHP); 
        
        return displayedHP;
    }

    void Update()
    {   
        if (!initialized) return;
        if (displayedHP == targetHP) return;

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f) return;
   
        if (hundreds.IsRolling() || tens.IsRolling() || ones.IsRolling()) return;

        bool isTickingUp = displayedHP < targetHP;

        //halves tick duration if rolling up, otherwise uses full tick duration
        tickTimer = isTickingUp ? (tickDuration * 0.5f) : tickDuration;
        
        //increments or decrements displayed by 1
        displayedHP += isTickingUp ? 1 : -1;
        
       
        int hp = Mathf.Max(displayedHP, 0);

        
        bool showHundreds = hp >= 100;
        bool showTens = hp >= 10 || showHundreds;

        int newOnes = hp % 10;
        int newTens = showTens ? (hp / 10) % 10 : -1;
        int newHundreds = showHundreds ? (hp / 100) % 10 : -1;

        if (newOnes != ones.currentDigit) ones.RollToDigit(newOnes, isTickingUp);
        if (newTens != tens.currentDigit) tens.RollToDigit(newTens, isTickingUp);
        if (newHundreds != hundreds.currentDigit) hundreds.RollToDigit(newHundreds, isTickingUp);
    }
}