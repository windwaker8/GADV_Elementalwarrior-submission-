   using UnityEngine;
   //done
   using System.Collections;
   using System.Collections.Generic;
   

   //Used to create scriptableObjects in the Assets menu in Unity
   [CreateAssetMenu(fileName ="Character", menuName = "Character/create a character or enemy")]
   public class Character : ScriptableObject
   {  //All serialized fields are used by the scriptable object. Sprite is needed to load into BattleSystem
      [SerializeField] string name;
      [SerializeField] Sprite sprite;

      // Weakness Setup, None is not a weakness
      [SerializeField] private ElementalType weakness = ElementalType.None;
      

      //Values used for stats. Level although not shown is set to 1.
      [SerializeField] int MaxHP;
      [SerializeField] int MaxPP;
      [SerializeField] int Atk;
      [SerializeField] int Def;
      [SerializeField] int Skill;
      [SerializeField] int IQ;
      [SerializeField] int Speed;
      // Used by the Player character. Ignored by enemies. 


      [SerializeField] int AtkGrowth;
      [SerializeField] int DefGrowth;
      [SerializeField] int SkillGrowth;
      [SerializeField] int IQGrowth;
      [SerializeField] int SpeedGrowth;
      [SerializeField] int HPGrowth;
      [SerializeField] int PPGrowth;

      // Used by the enemies for the player to level up.
      [SerializeField] int ExpYield;
      
      [SerializeField] bool isPlayerCharacter = false;
      [SerializeField] List<Learnset> moveSet;

      // Public Getter for Weakness
      public ElementalType Weakness => weakness;
      
      // read only values that are used by CharacterStat to calculate stat growth
      public string Name => name;
      public Sprite _sprite => sprite;
      
      public int HP => MaxHP;
      public int PP => MaxPP;
      public int atk => Atk;
      public int def => Def;
      public int skill => Skill;
      public int iq => IQ;
      public int speed => Speed;

      public int atkGrowth => AtkGrowth;
      public int defGrowth => DefGrowth;
      public int skillGrowth => SkillGrowth;
      public int iqGrowth => IQGrowth;
      public int speedGrowth => SpeedGrowth;
      public int hpGrowth => HPGrowth;
      public int ppGrowth => PPGrowth;

      public int expYield => ExpYield;
     
      public List<Learnset> learnSet => moveSet;
      public bool IsPlayerCharacter => isPlayerCharacter;
   }
   

   //Pairs a move scriptableObject and a level for player 
   [System.Serializable]
   public class Learnset
   {
      [SerializeField] MoveBase moveBase;
      [SerializeField] int level;

      public MoveBase Base => moveBase;
      public int Level => level;
   }
   
   //The enum for weaknesses.
   public enum ElementalType
   {
      None,
      Lightning,
      Wind
   }