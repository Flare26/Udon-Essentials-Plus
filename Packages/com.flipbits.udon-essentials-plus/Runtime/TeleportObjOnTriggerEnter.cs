namespace flipbits.UEPlus
{

    using UdonSharp;
    using UnityEngine;
    using VRC.SDKBase;
    using VRC.Udon;

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TeleportObjOnTriggerEnter : UdonSharpBehaviour
    {
        [SerializeField] GameObject obj;
        [SerializeField] Transform waypoint;
        public override void OnPlayerTriggerEnter(VRCPlayerApi api)
        {
            if (api.isLocal)
            {
                obj.transform.position = waypoint.position;
                obj.transform.rotation = waypoint.rotation;
            }
        }
    }

}
