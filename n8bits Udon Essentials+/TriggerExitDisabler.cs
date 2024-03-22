// Trigger Exit Disabler by N8bits
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UEPlus
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None )]
    public class TriggerExitDisabler : UdonSharpBehaviour
    {
        [Header("Trigger Exit Disabler by n8")]
        [SerializeField] GameObject[] disables; 
        public override void OnPlayerTriggerExit(VRCPlayerApi api)
        {
            if (api.displayName.Equals(Networking.LocalPlayer.displayName))
            {
                foreach ( GameObject o in disables)
                {
                    if (o.activeSelf == true)
                        o.SetActive(false);
                }
            }
        }
    }

}
