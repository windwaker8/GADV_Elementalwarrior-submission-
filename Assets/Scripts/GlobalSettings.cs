using UnityEngine;

public class GlobalSettings : MonoBehaviour
{   //allows for scripts to use HighlightedColour, changing the colour of the text
    [SerializeField] Color highlightedColour;

    public Color HighlightedColour => highlightedColour;

    public static GlobalSettings i { get; private set; }


    private void Awake()
    {
        i = this;
    }

}
