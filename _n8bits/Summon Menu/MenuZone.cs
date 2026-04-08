using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace n8bits.SummonMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuZone : UdonSharpBehaviour
    {
        [SerializeField] private ODMenuCtrl menu;
        [SerializeField] private Transform waypoint;

        void Start()
        {
            if (menu == null)
                menu = GameObject.Find("Summon Menu").GetComponent<ODMenuCtrl>();
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            base.OnPlayerTriggerEnter(player);
            if (player.isLocal)
            {
                Transform origin = waypoint != null ? waypoint : transform;
                menu.SetMenuOrigin(origin);
            }
        }
    }
}