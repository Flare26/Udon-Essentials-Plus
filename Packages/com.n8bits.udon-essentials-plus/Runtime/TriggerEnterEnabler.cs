// Trigger enter enabler by N8bits
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UEPlus
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None )]
    public class TriggerEnterEnabler : UdonSharpBehaviour
    {
        [Header("Trigger Enter Enabler by n8")]
        [Header("v1.0.0")]
        [SerializeField] GameObject[] enables; 
        public override void OnPlayerTriggerEnter(VRCPlayerApi api)
        {
            if (Networking.LocalPlayer.Equals(api))
            {
                foreach ( GameObject o in enables)
                {
                    if (o.activeSelf == false)
                        o.SetActive(true);
                }
            }
        }
    }
}
