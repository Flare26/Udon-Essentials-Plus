
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class WalkInTeleporter : UdonSharpBehaviour
{
    [SerializeField] Transform WP;
    [SerializeField] bool fixedExitRot;
    public override void OnPlayerTriggerEnter(VRCPlayerApi api)
    {
        if (Networking.LocalPlayer.Equals(api))
        {
            switch (fixedExitRot)
            {
                case true:
                    Networking.LocalPlayer.TeleportTo(WP.position, WP.rotation);
                    break;

                case false:
                    Networking.LocalPlayer.TeleportTo(WP.position, Networking.LocalPlayer.GetBoneRotation(HumanBodyBones.Hips));
                    break;

            }

            
        }
    }
}
