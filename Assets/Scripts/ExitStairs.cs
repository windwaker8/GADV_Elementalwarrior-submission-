using UnityEngine;
using System.Collections;

public class ExitStairs : MonoBehaviour, Interface
{

 [Header("Dialog Setup")]
    [SerializeField] private Dialog exitDialog;

    private bool isInteracting = false;

    public void Interact()
    {
        if (isInteracting) return;
        StartCoroutine(HandleExitSequence());
    }

    private IEnumerator HandleExitSequence()
    {
        isInteracting = true;

        
        if (exitDialog != null && exitDialog.Lines != null && exitDialog.Lines.Count > 0)
        {
            yield return DialogManager.Instance.showDialog(exitDialog);
        }

        // 2. Quit the game application
        EndGame();
    }

    private void EndGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // Stops play mode in Unity Editor
        #else
            Application.Quit(); // Closes the built game executable
        #endif
    }

}
