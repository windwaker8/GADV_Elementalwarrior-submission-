    using UnityEngine;
    using UnityEngine.UI;
    //done
    public class BattleUnit : MonoBehaviour
    {
        [SerializeField] Character _Base;
        [SerializeField] int level;

        public CharacterStat battleUnit { get; set; }

        public string DisplayName {get; set;}

    // Setup for Wild Enemies using a passed Character base
    public void Setup(Character enemyBase)
    {
        _Base = enemyBase;
        battleUnit = new CharacterStat(_Base, level);
        GetComponent<Image>().sprite = battleUnit.Base._sprite;
        gameObject.SetActive(true);
    }

        // Setup for Player / Party (uses persistent CharacterStat instance)
        public void Setup(CharacterStat existingUnit)
        {
            battleUnit = existingUnit;
            GetComponent<Image>().sprite = battleUnit.Base._sprite;
        }
        
        // Explicitly toggle GameObject AND Image component visibility
        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
            
            if (TryGetComponent<Image>(out var img))
            {
                img.enabled = isVisible;
                if(!isVisible)
                {
                img.sprite = null; // Clears ghost sprites from previous battles
            }
            }
        }
    }