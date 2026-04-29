namespace n8bits.UEPlus
{
    // Toggle Plus Alt by N8bits
    // DEPRECATED — Use TogglePlus with "Use Relative Toggle" enabled instead.
    // This script is kept for backward compatibility with existing projects.
    using System;
    using UdonSharp;
    using UnityEngine;
    using UnityEngine.UI;
    using VRC.SDKBase;
    using VRC.Udon;

    [System.Obsolete("Use TogglePlus with useRelativeToggle enabled instead. This script is maintained for backward compatibility only.")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TogglePlusAlt : UdonSharpBehaviour
    {
        [Header("Toggle+ Alt by n8 v2.0.0 [DEPRECATED]")]
        [Header("Use TogglePlus with Relative Toggle instead")]

        [SerializeField] bool isSynced;
        [SerializeField, UdonSynced] public bool isFlipped;
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

        // Initial states captured at Start so we can flip relative to them
        bool[] initialObjStates;
        bool[] initialCollStates;

        void Start()
        {
            // Snapshot whatever each object/collider starts as
            if (togObjs != null)
            {
                initialObjStates = new bool[togObjs.Length];
                for (int i = 0; i < togObjs.Length; i++)
                {
                    if (togObjs[i])
                        initialObjStates[i] = togObjs[i].activeSelf;
                }
            }

            if (togColliders != null)
            {
                initialCollStates = new bool[togColliders.Length];
                for (int i = 0; i < togColliders.Length; i++)
                {
                    if (togColliders[i])
                        initialCollStates[i] = togColliders[i].enabled;
                }
            }

            ApplyState();
        }

        public override void Interact()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, gameObject) && isSynced)
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            isFlipped = !isFlipped;
            ApplyState();

            if (isSynced)
                RequestSerialization();
        }

        /// <summary>
        /// Applies the current toggle state.
        /// When isFlipped is false (default), objects keep their initial states.
        /// When isFlipped is true, every object/collider is set to the inverse of its initial state.
        /// </summary>
        void ApplyState()
        {
            if (togObjs != null && initialObjStates != null)
            {
                for (int i = 0; i < togObjs.Length; i++)
                {
                    if (!togObjs[i]) continue;
                    togObjs[i].SetActive(isFlipped ? !initialObjStates[i] : initialObjStates[i]);
                }
            }

            if (togColliders != null && initialCollStates != null)
            {
                for (int i = 0; i < togColliders.Length; i++)
                {
                    if (!togColliders[i]) continue;
                    togColliders[i].enabled = isFlipped ? !initialCollStates[i] : initialCollStates[i];
                }
            }

            if (isAnimated && optionalAnim)
                optionalAnim.Play(isFlipped ? onStateName : offStateName);

            if (useUiImage && buttonImg)
                buttonImg.color = isFlipped ? onColor : offColor;
        }

        public override void OnDeserialization()
        {
            if (Networking.GetOwner(gameObject) == Networking.LocalPlayer)
                return;

            if (!isSynced)
                return;

            ApplyState();
        }
    }
}