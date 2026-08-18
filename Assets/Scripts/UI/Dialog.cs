using UnityEngine;
using System.Collections.Generic;
//Done


[System.Serializable]
public class Dialog
{
    // The list of dialogue lines for this conversation
    [SerializeField] List<string> lines;

    // Public property to read the conversation lines
    public List<string> Lines
    {
        get { return lines; }
    }
}