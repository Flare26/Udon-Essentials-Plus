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
        [Header("Toggle+ by n8 v1.0.1")]

        [SerializeField] bool isSynced;
        [SerializeField] bool isAnimated;
        [SerializeField, UdonSynced] public bool state;
        [SerializeField] GameObject[] togObjs;
        [SerializeField] Collider[] togColliders;
        
        [Header("Optional"), SerializeField] Animator optionalAnim;
        [SerializeField] string onStateName;
        [SerializeField] string offStateName;
        void Start()
        {
            SetState();
        }

        public override void Interact()
        {
            ToggleProcedure();
        }

        public void ToggleProcedure()
        {
            // On interact, check if this toggle is supposed to be global or not and set ownership if so.
            // After setting ownership, request serializaiton if supposed to be synced

            if (!Networking.IsOwner(Networking.LocalPlayer, gameObject) && isSynced)
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            Debug.Log("ToggleProcedure invoked, changed state bool");
            state = !state;

            SetState();

            if (isSynced)
                RequestSerialization();
        }

        void SetAnimatorState()
        {
            if (optionalAnim)
            {
                if (state)
                    optionalAnim.Play($"{onStateName}");
                else
                    optionalAnim.Play($"{offStateName}");
            }
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

            if (isAnimated)
                SetAnimatorState();
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
