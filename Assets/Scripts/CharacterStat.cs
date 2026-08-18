using UnityEngine;
//done
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CharacterStat 
{
    public Character Base { get; set; }
    public int Level { get; set; }
    public int HP { get; set; }
    public int PP { get; set; }
  
    // EXP
    public int CurrentExp { get; set; }
    public int ExpYield => Base != null ? Base.expYield : 0;

    // Fixed stored stats 
    public int atk { get; set; }
    public int def { get; set; }
    public int skill { get; set; }
    public int iq { get; set; }
    public int speed { get; set; }
    public int MaxHp { get; set; }
    public int MaxPP { get; set; }

    public List<Move> Moves { get; set; }

    public CharacterStat(Character pBase, int plevel)
    {
        Base = pBase;
        Level = plevel;

        // Initialize base stats once on creation
        InitializeStats();

        HP = MaxHp;
        PP = MaxPP;

        CurrentExp = 0;

        // Initialize moves based on character level (max 4 moves)
        Moves = new List<Move>();
        foreach (var moveUnit in Base.learnSet)
        {
            if (moveUnit.Level <= Level)
            {
                Moves.Add(new Move(moveUnit.Base));
            }

            if (Moves.Count >= 4)
                break;
        }
    }

    private void InitializeStats()
    {
        // 1. Set base values from ScriptableObject
        atk = Base.atk;
        def = Base.def;
        skill = Base.skill;
        iq = Base.iq;
        speed = Base.speed;
        MaxHp = Base.HP;
        MaxPP = Base.PP;

        // 2. If instantiated above Level 1, apply level-up rolls up to starting level
        if (Base.IsPlayerCharacter && Level > 1)
        {
            for (int i = 1; i < Level; i++)
            {
                ApplyLevelUpGrowth();
            }
        }
    }

    public bool GainExp(int amount, out bool leveledUp)
    {
        CurrentExp += amount;
        leveledUp = false;

        int expToNextLevel = 12 * Level; 
        if (CurrentExp >= expToNextLevel)
        {
            Level++;
            CurrentExp -= expToNextLevel;
            leveledUp = true;

            // Roll and lock in new stats ONLY on level-up
            if (Base.IsPlayerCharacter)
            {
                ApplyLevelUpGrowth();
            }
        }

        return leveledUp;
    }

    public Learnset GetMoveAtLevel()
    {
       return Base.learnSet.Where(move => move.Level == Level).FirstOrDefault();
    }

    public void LearnMove(Learnset newMove)
    {
        if (Moves.Count >= 4) return;
        Moves.Add(new Move(newMove.Base));
    }

    private void ApplyLevelUpGrowth()
    {
        // Calculate random growth once, then add directly to current saved stat
        atk += Mathf.FloorToInt(Base.atkGrowth + (Random.Range(0, 4)) / 2f);
        def += Mathf.FloorToInt(Base.defGrowth + (Random.Range(0, 4)) / 2f);
        skill += Mathf.FloorToInt(Base.skillGrowth + (Random.Range(0, 4)) / 2f);
        iq += Mathf.FloorToInt(Base.iqGrowth + (Random.Range(0, 4)) / 2f);
        speed += Mathf.FloorToInt(Base.speedGrowth + (Random.Range(0, 4)) / 2f);

        int hpIncrease = Mathf.FloorToInt(Base.hpGrowth + (Random.Range(0, 4)) / 2f) + 5;
        int ppIncrease = Mathf.FloorToInt(Base.ppGrowth + (Random.Range(0, 4)) / 2f) + 5;

        MaxHp += hpIncrease;  
        MaxPP += ppIncrease;

        // Give extra HP/PP on level up, slight HP/PP recovery
        HP += hpIncrease;
        PP += ppIncrease;
    }

    public bool TakeDamage(Move move, CharacterStat attacker, out int damageDealt, out bool isSuperEffective)
    {
        isSuperEffective = false;
        float attack;

        // Special boss move. Uses the target's atk stat as damage calculation instead of the 
        //attacker's skill stat using IsSpecial.

        if (move.Pbase.IsSpecial)
        {
            if (move.Pbase.Name == "Toppling Wind")
            {
                attack = this.atk;
            }
            else
            {
                attack = attacker.skill;
            }
        }
        else
        {
            attack = attacker.atk;
        }
        
        //Checks using IsSpecial to determine if the move is physical (def) or special(iq).

        // lowest defense can go (if float defense is somehow 0) is 1 to avoid a divide by 0.


        float defense = (move.Pbase.IsSpecial) ? this.iq : this.def;
        defense = Mathf.Max(1f, defense);
        
        float calculatedDamage;
        

        
        if (move.Pbase.Power > 0)
        {
            calculatedDamage = move.Pbase.Power * (attack / (float)defense);
        }

        //Bash exclusive damage calculation.
        else
        {
            calculatedDamage = (2 * attack) - (float)defense;
        }

        // Check for Weakness (2x Multiplier to calculatedDamage), excluding None, which is used by Bash.
        if (this.Base.Weakness != ElementalType.None && move.Pbase.Type == this.Base.Weakness)
        {
            calculatedDamage *= 2.0f;
            isSuperEffective = true;
        }
        

        //HP cannot be negative.
        damageDealt = Mathf.Max(1, Mathf.FloorToInt(calculatedDamage));
        HP = Mathf.Max(0, HP - damageDealt);
       
        return HP <= 0; 
    }

    public Move ChooseMove()
    {
        // Get all moves that cost less than or equal to current PP
        var usableMoves = Moves.Where(m => m.Pbase.Cost <= PP).ToList();

        if (usableMoves.Count == 0) return null; // No PP left for any moves
        

        //randomly picks a move 
        int r = UnityEngine.Random.Range(0, usableMoves.Count);
        return usableMoves[r];
    }

    public void DeductPP(int amount)
    {   //Deducts PP from both player and enemy to ensure enemies don't spam moves.

        PP = Mathf.Max(0, PP - amount);
    }

    public void RestoreAll()
    {  //Heals the target. Used in Healer.cs
        HP = MaxHp;
        PP = MaxPP;
    }
}