namespace n8bits.UEPlus
{
// Toggle Plus by N8bits
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TogglePlusV2 : UdonSharpBehaviour
    {
        [Header("||Toggle+ by n8 v2.0.0||")]

        [SerializeField] bool isSynced;
        [SerializeField, UdonSynced] public bool state;
        [SerializeField] GameObject[] togObjs;
        [SerializeField] GameObject[] colliderContainers;
        [SerializeField] ReflectionProbe[] reflProbes;
        [SerializeField] Camera[] cams;
        Collider[] colChildren;
        bool initialized;

        [Header("--Toggle Mode--")]
        [SerializeField, Tooltip("When enabled, each object flips relative to its initial state instead of being set to a uniform on/off. Use this when your objects start in mixed states (some on, some off) and you want them all to invert on toggle.")]
        bool useRelativeToggle;

        // Snapshots for relative toggle mode
        bool[] initialObjStates;
        bool[] initialCollStates;

        [Header("--Trigger Options--")]
        [SerializeField, Tooltip("Toggle on player trigger enter instead of interact.")]
        bool triggerEnter;
        [SerializeField, Tooltip("Toggle on player trigger exit instead of interact.")]
        bool triggerExit;
        [SerializeField, Tooltip("Once toggled, it cannot be toggled again for this play session.")]
        bool oneShot;
        bool spent;

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

        [Header("--Optional UdonBehaviour Events--")]
        [SerializeField] bool useUdonEvents;
        [SerializeField, Tooltip("The UdonBehaviour to send events to.")]
        UdonBehaviour targetUdonBehaviour;
        [SerializeField, Tooltip("Event sent to the target UdonBehaviour when toggled ON.")]
        string onEventName;
        [SerializeField, Tooltip("Event sent to the target UdonBehaviour when toggled OFF.")]
        string offEventName;

        [Header("--Optional Button Interactable--")]
        [SerializeField] bool useButtonInteractable;
        [SerializeField] Button[] interactableButtons;

        [Header("--Optional Mat Swaps--")]
        [SerializeField, Tooltip("Do not check unless you have matSwapGameObj assigned.")]
        bool useMatSwap;
        [SerializeField] GameObject matSwapGameObj;
        MeshRenderer swapMR;
        [SerializeField] Material onMat;
        [SerializeField] Material offMat;

        [Header("--Optional Component Toggles--")]
        [SerializeField] MeshRenderer[] togRenderers;
        [SerializeField] Animator[] togAnimators;

        // One-time programmatic grant flag (for keypad integration)
        bool progGranted;

        void Start()
        {
            // Material swap renderer setup
            if (useMatSwap && matSwapGameObj)
                swapMR = matSwapGameObj.GetComponent<MeshRenderer>();

            // Gather child colliders from containers
            colChildren = new Collider[] { };

            if (colliderContainers != null)
            {
                for (int i = 0; i < colliderContainers.Length; i++)
                {
                    if (!colliderContainers[i]) continue;

                    Collider[] newComponents = colliderContainers[i].GetComponentsInChildren<Collider>(true);
                    Collider[] replacement = new Collider[colChildren.Length + newComponents.Length];
                    Array.Copy(colChildren, replacement, colChildren.Length);
                    Array.Copy(newComponents, 0, replacement, colChildren.Length, newComponents.Length);
                    colChildren = replacement;
                }
            }

            // Snapshot initial states for relative toggle mode
            if (useRelativeToggle)
            {
                if (togObjs != null)
                {
                    initialObjStates = new bool[togObjs.Length];
                    for (int i = 0; i < togObjs.Length; i++)
                    {
                        if (togObjs[i])
                            initialObjStates[i] = togObjs[i].activeSelf;
                    }
                }

                if (colChildren != null)
                {
                    initialCollStates = new bool[colChildren.Length];
                    for (int i = 0; i < colChildren.Length; i++)
                    {
                        if (colChildren[i])
                            initialCollStates[i] = colChildren[i].enabled;
                    }
                }
            }

            initialized = true;
            ApplyState();
        }

        void OnEnable()
        {
            ApplyState();
        }

        public override void Interact()
        {
            if (spent) return;
            if (triggerEnter || triggerExit) return;

            ToggleProcedure();
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (spent) return;
            if (!triggerEnter) return;
            if (!player.Equals(Networking.LocalPlayer)) return;

            ToggleProcedure();
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (spent) return;
            if (!triggerExit) return;
            if (!player.Equals(Networking.LocalPlayer)) return;

            ToggleProcedure();
        }

        /// <summary>
        /// Public toggle entry point — callable from Interact, triggers, or external scripts via SendCustomEvent.
        /// </summary>
        public void ToggleProcedure()
        {
            if (!Networking.IsOwner(Networking.LocalPlayer, gameObject) && isSynced)
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            state = !state;

            if (oneShot)
                spent = true;

            ApplyState();

            if (isSynced)
                RequestSerialization();
        }

        /// <summary>
        /// One-time programmatic toggle for keypad / access-grant integration.
        /// Ensures the toggle fires only once per play session.
        /// </summary>
        public void ProgramGranted()
        {
            if (!progGranted)
            {
                progGranted = true;
                ToggleProcedure();
            }
        }

        // ── State Application ──────────────────────────────────────

        void ApplyState()
        {
            if (useRelativeToggle)
                ApplyRelativeState();
            else
                ApplyUniformState();

            // Optionals (shared by both modes)

            ApplyReflectionProbeState();
            ApplyCameraState();

            if (isAnimated && optionalAnim)
                SetAnimatorState();

            if (useUiImage && buttonImg)
                CheckUpdateButtonColor();

            if (useMatSwap && swapMR)
                SetMatSwaps();

            if (useButtonInteractable && interactableButtons != null && interactableButtons.Length > 0)
                SetButtonInteractable();

            if (initialized && useUdonEvents && targetUdonBehaviour)
                SendUdonEvent();
        }

        /// <summary>
        /// Uniform mode: all objects are set to the current state bool.
        /// </summary>
        void ApplyUniformState()
        {
            // GameObjects
            if (togObjs != null && togObjs.Length > 0)
            {
                foreach (GameObject o in togObjs)
                {
                    if (!o) continue;
                    o.SetActive(state);
                }
            }

            // Colliders
            if (colChildren != null && colChildren.Length > 0)
            {
                foreach (Collider c in colChildren)
                {
                    if (!c) continue;
                    c.enabled = state;
                }
            }

            // MeshRenderers
            if (togRenderers != null && togRenderers.Length > 0)
            {
                foreach (MeshRenderer r in togRenderers)
                {
                    if (!r) continue;
                    r.enabled = state;
                }
            }

            // Animators (enable/disable)
            if (togAnimators != null && togAnimators.Length > 0)
            {
                foreach (Animator a in togAnimators)
                {
                    if (!a) continue;
                    a.enabled = state;
                }
            }
        }

        /// <summary>
        /// Relative mode: each object is flipped relative to its initial state.
        /// When state is false (default), objects keep their initial states.
        /// When state is true (flipped), every object/collider inverts from its initial state.
        /// </summary>
        void ApplyRelativeState()
        {
            // GameObjects
            if (togObjs != null && initialObjStates != null)
            {
                for (int i = 0; i < togObjs.Length; i++)
                {
                    if (!togObjs[i]) continue;
                    togObjs[i].SetActive(state ? !initialObjStates[i] : initialObjStates[i]);
                }
            }

            // Colliders
            if (colChildren != null && initialCollStates != null)
            {
                for (int i = 0; i < colChildren.Length; i++)
                {
                    if (!colChildren[i]) continue;
                    colChildren[i].enabled = state ? !initialCollStates[i] : initialCollStates[i];
                }
            }

            // MeshRenderers & Animators still use uniform logic in relative mode
            // (they don't have initial state snapshots — add if needed in the future)
            if (togRenderers != null && togRenderers.Length > 0)
            {
                foreach (MeshRenderer r in togRenderers)
                {
                    if (!r) continue;
                    r.enabled = state;
                }
            }

            if (togAnimators != null && togAnimators.Length > 0)
            {
                foreach (Animator a in togAnimators)
                {
                    if (!a) continue;
                    a.enabled = state;
                }
            }
        }

        // ── Optional Feature Helpers ───────────────────────────────

        void SetAnimatorState()
        {
            if (optionalAnim)
                optionalAnim.Play(state ? onStateName : offStateName);
        }

        void CheckUpdateButtonColor()
        {
            buttonImg.color = state ? onColor : offColor;
        }

        void SetMatSwaps()
        {
            swapMR.material = state ? onMat : offMat;
        }

        void SetButtonInteractable()
        {
            foreach (Button b in interactableButtons)
            {
                if (!b) continue;
                b.interactable = state;
            }
        }

        void ApplyReflectionProbeState()
        {
            if (reflProbes == null || reflProbes.Length == 0)
                return;

            foreach (ReflectionProbe p in reflProbes)
            {
                if (!p) continue;
                p.gameObject.SetActive(state);
            }
        }

        void ApplyCameraState()
        {
            if (cams == null || cams.Length == 0)
                return;

            foreach (Camera c in cams)
            {
                if (!c) continue;
                c.enabled = state;
            }
        }

        void SendUdonEvent()
        {
            string eventName = state ? onEventName : offEventName;

            if (!string.IsNullOrEmpty(eventName))
                targetUdonBehaviour.SendCustomEvent(eventName);
        }

        // ── Network Sync ──────────────────────────────────────────

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