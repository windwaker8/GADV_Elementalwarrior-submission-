using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BattleDialogBox : MonoBehaviour
{   [SerializeField] int lettersPerSecond;

    
     //used for the dialog font, because the other one is legacy
    [SerializeField] TextMeshProUGUI dialogText;
    [SerializeField] GameObject ActionSelector;
    [SerializeField] GameObject MoveSelector;
    [SerializeField] GameObject MoveDes;
    

    [SerializeField] List <TextMeshProUGUI> actionTexts;
    [SerializeField] List<TextMeshProUGUI> moveTexts;

    [SerializeField] TextMeshProUGUI ppText;
    [SerializeField] TextMeshProUGUI typeText;


    Color highlightedColor;

    private void Start()
    {  //initializes GlobalSettings into the script
        highlightedColor = GlobalSettings.i.HighlightedColour;
    } 

    public void SetDialog(string dialog)
    {
       dialogText.text = dialog;
    }
 
 //Types dialog out letter by letter at a rate of 1/lettersPerSecond seconds
    public IEnumerator TypeDialog(string dialog)
    {
        dialogText.text = "";
        foreach(var letter in dialog.ToCharArray())
        {
            dialogText.text += letter;

            yield return new WaitForSeconds(1f/lettersPerSecond);
        }
    }
    public void EnableDialogText(bool enabled)
    {
     dialogText.enabled = enabled;
    }
   
   //Shows or hides the action menu (Bash, Moves Run)
    public void EnableActionSelector(bool enabled)
    {
     ActionSelector.SetActive(enabled);
    }

    //Shows or hides the move selector and move description box
    public void EnableMoveSelector(bool enabled)
    {   MoveSelector.SetActive(enabled);
        MoveDes.SetActive(enabled);
    }
    
    // Highlights the selected action text in color and turns unselected ones black
    public void UpdateActionSelection(int selectedAction)
    {
        for(int i= 0; i <actionTexts.Count; ++i)
        {
            if(i == selectedAction)
            {
                actionTexts[i].color = highlightedColor;


            }
            else
            {
                actionTexts[i].color = Color.black;
            }
        }
    }
    
    //highlights the move's name and updates the cost and the type display to match the move
    public void UpdateMoveSelection(int selectedMove, Move move)
    {
        for(int i= 0; i <moveTexts.Count; ++i)
        {
            if(i == selectedMove)
            {
                moveTexts[i].color = highlightedColor;
            }
            else
            {
                moveTexts[i].color = Color.black;
            }
        }
        
        ppText.text = $"PP {move.Pbase.Cost}";
        typeText.text = $"Type {move.Pbase.Type}";
    }
     

     //sets the move names so long as there are enough GameObjects with TextMeshProUGUI in the inspector. 
     // If there are more moves than TextMeshProUGUI objects, the rest will be ignored.
     public void SetMoveNames(List<Move> moves)
  {
    for (int i = 0; i < moveTexts.Count; ++i)
    {
        if (i < moves.Count)
        {
            moveTexts[i].text = moves[i].Pbase.Name;
        }
        else
        {
            moveTexts[i].text = "-";
        }
    }
  }
}
