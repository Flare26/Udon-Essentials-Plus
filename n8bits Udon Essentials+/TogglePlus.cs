// Toggle Plus by N8bits
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UEPlus
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TogglePlus : UdonSharpBehaviour
    {
        [Header("Toggle+ by n8")]

        [SerializeField] bool isSynced;
        [SerializeField, UdonSynced] bool state;
        [SerializeField] GameObject[] togObjs;
        [SerializeField] Collider[] togColliders;
        void Start()
        {
            SetState();
        }

        public override void Interact()
        {
            // On interact, check if this toggle is supposed to be global or not and set ownership if so.
            // After setting ownership, request serializaiton if supposed to be synced

            if (!Networking.IsOwner(Networking.LocalPlayer, gameObject) && isSynced)
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            state = !state;
            //Debug.Log("Changed state bool");

            SetState();

            if (isSynced)
                RequestSerialization();
        }

        void SetState()
        {
            if (togObjs.Length > 0)
            {
                foreach (GameObject o in togObjs)
                {
                    o.SetActive(state);
                }
            }

            if (togColliders.Length > 0)
            {
                foreach (Collider c in togColliders)
                {
                    c.enabled = (state);
                }
            }

            //Debug.Log("Set state to " + state);
        }

        public override void OnDeserialization()
        {
            // On synced variable change, make sure its not double executing
            // Make sure it doesnt double execute if its not supposed to be synced

            if (Networking.GetOwner(gameObject) == Networking.LocalPlayer)
                return;

            if (isSynced == false)
                return;

            SetState();
            //Debug.Log("Set state from deserialize");
        }
    }
}
