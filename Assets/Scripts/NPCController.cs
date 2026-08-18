using UnityEngine;
//done


public class NPCController : MonoBehaviour, Interface
{   
    [SerializeField] Dialog dialog;

    // Exposes this NPC's dialog for read only access.
    public Dialog Dialog => dialog;

    public void Interact()
    {
        StartCoroutine(DialogManager.Instance.showDialog(dialog));
    }
}