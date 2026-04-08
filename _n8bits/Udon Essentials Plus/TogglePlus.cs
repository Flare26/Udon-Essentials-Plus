// Toggle Plus by N8bits
using System;
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
        [Header("||Toggle+ by n8 v1.0.4||")]

        [SerializeField] bool isSynced;
        [SerializeField, UdonSynced] public bool state;
        [SerializeField] GameObject[] togObjs;
        [SerializeField] GameObject[] colliderContainers;
        Collider[] colChildren;

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

            // new grab colliders logic    

            // Initialize the colChildren array to be empty
            colChildren = new Collider[] { };

            // Iterate over each collider in togColliders
            for (int i = 0; i < colliderContainers.Length; i++)
            {
                // Get all colliders in the children of the current togCollider (including itself)
                Collider[] newComponents = colliderContainers[i].GetComponentsInChildren<Collider>();

                // Create a new array with size of colChildren + newComponents
                Collider[] replacement = new Collider[colChildren.Length + newComponents.Length];

                // Copy the old colliders into the new array
                Array.Copy(colChildren, replacement, colChildren.Length);

                // Copy the new colliders found in children to the new array
                Array.Copy(newComponents, 0, replacement, colChildren.Length, newComponents.Length);

                // Update colChildren with the new array
                colChildren = replacement;
            }

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
                    swapMR.material = onMat;
                    break;
                case false:
                    swapMR.material = offMat;
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
            // apply state to game objects active state
            if (togObjs.Length > 0)
            {
                foreach (GameObject o in togObjs)
                {
                    o.SetActive(state);
                }

            }

            // apply state to colliders enabled
            if (colliderContainers.Length > 0)
            {
                foreach (Collider c in colChildren)
                {

                    c.enabled = (state);
                }
            }

            //apply state to the optionals

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
