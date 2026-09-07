namespace flipbits.UEPlus
{

    using UdonSharp;
    using UnityEngine;
    using VRC.SDKBase;
    using VRC.Udon;
    using UnityEngine.UI;

    public class EnableCanvasWhileInsideTrigger : UdonSharpBehaviour
    {
        [SerializeField] Canvas toggleMe;
        public override void OnPlayerTriggerEnter(VRCPlayerApi api)
        {
            if (Networking.LocalPlayer.Equals(api))
            {
                toggleMe.enabled = true;

            }
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi api)
        {
            if (Networking.LocalPlayer.Equals(api))
            {
                toggleMe.enabled = false;

            }
        }
    }

}
