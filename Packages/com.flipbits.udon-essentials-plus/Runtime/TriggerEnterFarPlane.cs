namespace flipbits.UEPlus
{

    using UdonSharp;
    using UnityEngine;
    using VRC.SDKBase;
    using VRC.SDK3.Rendering;

    public class TriggerEnterFarPlane : UdonSharpBehaviour
    {
        public float outdoorFarClip = 2000f;
        private float defaultFarClip;

        void Start()
        {
            defaultFarClip = VRCCameraSettings.ScreenCamera.FarClipPlane;
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!player.isLocal) return;
            VRCCameraSettings.ScreenCamera.FarClipPlane = outdoorFarClip;
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!player.isLocal) return;
            VRCCameraSettings.ScreenCamera.FarClipPlane = defaultFarClip;
        }
    }

}
