using UdonSharp;
using UnityEngine;
using VRC.Udon;

public class KeypadGrantedHandler : UdonSharpBehaviour
{
    [Header("KeypadGrantedHandler By n8bits")]
    [Header("Last Updated: 20250511")]
    [HideInInspector] public string code;  // populated by SetProgramVariable
    public string[] codes;
    public UEPlus.TogglePlus [] toggles;

    public void keypadGranted()
    {
        for (int i=0;i<codes.Length;i++)
        {
            if (code.Equals(codes[i]))
            {
                toggles[i].Interact();
            }
        }
    }
}
