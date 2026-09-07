namespace flipbits.UEPlus
{

    using UdonSharp;
    using UnityEngine;
    using VRC.SDKBase;
    using VRC.Udon;

    public class EnableWhileInsideTrigger : UdonSharpBehaviour
    {
        [SerializeField] GameObject[] toggleMe;
        public override void OnPlayerTriggerEnter(VRCPlayerApi api)
        {
            if (Networking.LocalPlayer.Equals(api))
            {
                foreach (GameObject o in toggleMe)
                {
                    o.SetActive(!o.activeSelf);
                }

            }
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi api)
        {
            if (Networking.LocalPlayer.Equals(api))
            {
                foreach (GameObject o in toggleMe)
                {
                    o.SetActive(!o.activeSelf);
                }

            }
        }
    }

}
