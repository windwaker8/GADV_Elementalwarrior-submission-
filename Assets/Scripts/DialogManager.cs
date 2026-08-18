using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
//Done

// Global singleton controlling the dialogue box UI: typewriter-style text reveal,
// advancing through a Dialog's lines on input, and notifying the rest
// of the game (via OnShowDialog/OnHideDialog) when dialogue opens or closes
// so systems like GameManager can switch states.
public class DialogManager : MonoBehaviour
{
    [SerializeField] GameObject dialogBox;
    [SerializeField] TextMeshProUGUI dialogText;

    // Typing speed for the reveal effect in TypeDialog.
    [SerializeField] int lettersPerSecond;

    // Fired the moment a dialogue sequence begins (before the first line is typed).
    public event Action OnShowDialog;

    // Fired once the dialogue box is closed after its final line.
    public event Action OnHideDialog;

    // Global singleton instance, set once in Awake.
    public static DialogManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    Dialog dialog;
    int currentLine = 0;

    // True while a line is still being typed out; blocks advancing to the next line early.
    bool isTyping;

    // Opens the dialogue box and types out the given Dialog's first line.
    // Yields (blocks the caller) until the box is closed by HandleUpdate reaching
    // the end of the lines — this is what lets callers like Healer or Boss
    // use yield return DialogManager.Instance.showDialog(...) to wait for the whole
    // conversation to finish before continuing their own sequence.
    public IEnumerator showDialog(Dialog dialog)
    {
        yield return new WaitForEndOfFrame();
        OnShowDialog?.Invoke();

        this.dialog = dialog;
        currentLine = 0; // Reset line counter when starting new dialogue.

        dialogBox.SetActive(true);
        StartCoroutine(TypeDialog(dialog.Lines[0]));

        // Pause this coroutine until HandleUpdate() closes the dialogue box.
        yield return new WaitUntil(() => !dialogBox.activeSelf);
    }

    // Called every frame while the game is in the Dialog state (see GameManager).
    // On confirm: if a line is still typing, does nothing (handled implicitly by the isTyping
    // guard — a press only advances once the current line has fully revealed); otherwise advances
    // to the next line, or closes the box and fires OnHideDialog if this was the last line.
    public void HandleUpdate()
    {
        if ((Keyboard.current.jKey.wasPressedThisFrame) && !isTyping)
        {
            ++currentLine;

            if (currentLine < dialog.Lines.Count)
            {
                StartCoroutine(TypeDialog(dialog.Lines[currentLine]));
            }
            else
            {
                dialogBox.SetActive(false); // Turning this off releases the showDialog yield.
                currentLine = 0;
                OnHideDialog?.Invoke();
            }
        }
    }

    // Reveals a line one character at a time at lettersPerSecond, typewriter-style.
    public IEnumerator TypeDialog(string line)
    {
        isTyping = true;
        dialogText.text = "";

        foreach (var letter in line.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(1f / lettersPerSecond);
        }

        isTyping = false;
    }
}