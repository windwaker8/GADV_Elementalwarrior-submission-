using System.Collections;
using UnityEngine;
//Done


// An interactable boss encounter. On interact, plays a pre-fight line, then hands off
// to a fixed battle via GameManager.StartFixedBattle. 
public class Boss : MonoBehaviour, Interface
{
    [Header("Dialog Setup")]
    [SerializeField] private Dialog bossDialog;


   // used in BattleSystem.cs by replacing the random encounters with the scriptableObject enemy.
    [Header("Battle Setup")]
    [SerializeField] private Character specificEnemy;

    // Guards against re-triggering the interaction sequence while one is already playing.
    private bool isInteracting = false;

    public void Interact()
    {
        if (isInteracting) return;

        StartCoroutine(HandleInteractionSequence());
    }

    
    private IEnumerator HandleInteractionSequence()
    {
        isInteracting = true;

        // 1. Play dialogue from bossDialog
        if (bossDialog != null && bossDialog.Lines != null && bossDialog.Lines.Count > 0)
        {
            yield return DialogManager.Instance.showDialog(bossDialog);
        }

        // 2. Hand over battle activation to GameManager. On victory, remove this boss
        // from the scene so it can't be fought again.
        GameManager.Instance.StartFixedBattle(specificEnemy, () =>
        {
            Destroy(gameObject);
        });

        isInteracting = false;
    }
}