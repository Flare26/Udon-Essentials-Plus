
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
//n8bits
public class TriggerEnterDisabler : UdonSharpBehaviour
{
    [SerializeField] GameObject[] toDisable; 
    void Start()
    {
        
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer)
            return;

        foreach(GameObject o in toDisable)
        {
            o.SetActive(false);
        }
    }
}
