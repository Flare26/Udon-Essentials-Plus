
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
//n8bits
public class TriggerEnterDisabler : UdonSharpBehaviour
{
    [Header("Trigger Enter Disabler by n8")]
    [Header("v1.0.0")]
    [SerializeField] GameObject[] toDisable;
    void Start()
    {
        
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (Networking.LocalPlayer.Equals(player))
        {
            foreach(GameObject o in toDisable)
            {
                o.SetActive(false);
            }

        }

    }
}
