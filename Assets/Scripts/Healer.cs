using UnityEngine;
using System.Collections;
//done


// plays a greeting line with an npc. 
//heals the player upon interaction.
public class Healer : MonoBehaviour, Interface
{
    [Header("Dialog Setup")]
    [SerializeField] private Dialog healDialog;


    //only allows one heal instance at a time.
    private bool isBusy = false;

    public void Interact()
    {
        if (!isBusy)
        {
            StartCoroutine(HandleInteractionSequence());
        }
    }

   
    private IEnumerator HandleInteractionSequence()
    {
        isBusy = true;

        // 1. Play initial dialogue.
        if (healDialog != null && healDialog.Lines != null && healDialog.Lines.Count > 0)
        {
            yield return DialogManager.Instance.showDialog(healDialog);
        }

        // 2. Trigger healing on player.
        if (GameManager.Instance != null && GameManager.Instance.PlayerStat != null)
        {
            GameManager.Instance.PlayerStat.RestoreAll();
        }

       

        isBusy = false;
    }
}