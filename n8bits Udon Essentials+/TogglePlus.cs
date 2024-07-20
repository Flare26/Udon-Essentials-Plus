// Toggle Plus by N8bits
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
namespace UEPlus
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TogglePlus : UdonSharpBehaviour
    {
        [Header("||Toggle+ by n8 v1.0.3||")]

        [SerializeField] bool isSynced;
        [SerializeField, UdonSynced] public bool state;
        [SerializeField] GameObject[] togObjs;
        [SerializeField] Collider[] togColliders;
        
        [Header("--Optional Animation--")]
        [SerializeField, 
            Tooltip("If using an animator, it is recommended to put your toggle actions into the animations instead of using the object arrays.")]
            bool isAnimated;
        [SerializeField] Animator optionalAnim;
        [SerializeField] string onStateName;
        [SerializeField] string offStateName;

        [Header("--Optional UI Button--")]
        [SerializeField] bool useUiImage;
        [SerializeField] Image buttonImg;
        [SerializeField] Color onColor;
        [SerializeField] Color offColor;

        [Header("--Optional Mat Swaps--")]
        [SerializeField, Tooltip("Do not check unless you have matSwapGameObj assigned. It WILL crash the script.")] 
        bool useMatSwap;
        [SerializeField] GameObject matSwapGameObj;
        MeshRenderer swapMR;    
        [SerializeField] Material onMat;
        [SerializeField] Material offMat;
        void Start()
        {
            if (useMatSwap)
                swapMR = matSwapGameObj.GetComponent<MeshRenderer>();

            ApplyState();
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

            ApplyState();
            

            if (isSynced)
                RequestSerialization();
        }

        void SetMatSwaps()
        {
            switch (state)
            {
                case true:
                    buttonImg.color = onColor;
                    break;
                case false:
                    buttonImg.color = offColor;
                    break;
            }
        }
        void CheckUpdateButtonColor()
        {
            switch (state)
            {
                case true:
                    buttonImg.color = onColor;
                    break;
                case false:
                    buttonImg.color = offColor;
                    break;
            }
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

        void ApplyState()
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

            if (isAnimated && optionalAnim)
                SetAnimatorState();

            if (buttonImg && useUiImage)
                CheckUpdateButtonColor();

            if (swapMR && useMatSwap)
                SetMatSwaps();

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

            ApplyState();
            //Debug.Log("Set state from deserialize");
        }
    }
}
