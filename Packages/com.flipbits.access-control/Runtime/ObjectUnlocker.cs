using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.Udon;

namespace FlipBits.AccessControl
{
    /// <summary>
    /// FlipBits object unlocker.
    /// Holds a set of scene objects in a "locked" state until Unlock() is called
    /// (typically as the target of an AccessGate entry). Unlocking
    /// flips colliders and GameObjects out of their locked states, enables UI
    /// buttons, fires custom events on other behaviours, and cascades to any
    /// linked unlockers. Relock() reverses everything except the fired events.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("FlipBits/Object Unlocker")]
    public class ObjectUnlocker : UdonSharpBehaviour
    {
        [Header("Colliders")]
        [Tooltip("Colliders driven by this unlocker.")]
        [SerializeField] private Collider[] colliders = new Collider[0];

        [Tooltip("Enabled state of each collider while locked; flipped when unlocked.")]
        [SerializeField] private bool[] colliderStartStates = new bool[0];

        [Header("GameObjects")]
        [Tooltip("GameObjects driven by this unlocker.")]
        [SerializeField] private GameObject[] gameObjects = new GameObject[0];

        [Tooltip("Active state of each GameObject while locked; flipped when unlocked.")]
        [SerializeField] private bool[] gameObjectStartStates = new bool[0];

        [Header("Unlock Events")]
        [Tooltip("Behaviours that receive a custom event when this unlocks (not fired on relock).")]
        [SerializeField] private UdonBehaviour[] udonBehaviours = new UdonBehaviour[0];

        [Tooltip("Custom event name sent to each behaviour above.")]
        [SerializeField] private string[] udonBehaviourEvents = new string[0];

        [Header("UI Buttons")]
        [Tooltip("Buttons that are non-interactable while locked.")]
        [SerializeField] private Button[] buttonsToEnable = new Button[0];

        [Header("Linked Unlockers")]
        [Tooltip("Other unlockers that unlock and relock together with this one.")]
        [SerializeField] private ObjectUnlocker[] linkedUnlockers = new ObjectUnlocker[0];

        private bool isUnlocked;

        private void Start()
        {
            ApplyStates(false);
        }

        /// <summary>Unlocks this unlocker and everything linked to it. Safe to call repeatedly.</summary>
        public void Unlock()
        {
            if (isUnlocked) return;
            isUnlocked = true;

            ApplyStates(true);

            if (linkedUnlockers == null) return;
            for (var i = 0; i < linkedUnlockers.Length; i++)
            {
                if (linkedUnlockers[i] != null) linkedUnlockers[i].Unlock();
            }
        }

        /// <summary>Returns this unlocker and everything linked to it to the locked state.</summary>
        public void Relock()
        {
            if (!isUnlocked) return;
            isUnlocked = false;

            ApplyStates(false);

            if (linkedUnlockers == null) return;
            for (var i = 0; i < linkedUnlockers.Length; i++)
            {
                if (linkedUnlockers[i] != null) linkedUnlockers[i].Relock();
            }
        }

        private void ApplyStates(bool unlocked)
        {
            if (buttonsToEnable != null)
            {
                for (var i = 0; i < buttonsToEnable.Length; i++)
                {
                    if (buttonsToEnable[i] != null) buttonsToEnable[i].interactable = unlocked;
                }
            }

            if (colliders != null && colliderStartStates != null)
            {
                var count = Mathf.Min(colliders.Length, colliderStartStates.Length);
                for (var i = 0; i < count; i++)
                {
                    if (colliders[i] != null)
                    {
                        colliders[i].enabled = unlocked ? !colliderStartStates[i] : colliderStartStates[i];
                    }
                }
            }

            if (gameObjects != null && gameObjectStartStates != null)
            {
                var count = Mathf.Min(gameObjects.Length, gameObjectStartStates.Length);
                for (var i = 0; i < count; i++)
                {
                    if (gameObjects[i] != null)
                    {
                        gameObjects[i].SetActive(unlocked ? !gameObjectStartStates[i] : gameObjectStartStates[i]);
                    }
                }
            }

            if (!unlocked || udonBehaviours == null || udonBehaviourEvents == null) return;

            var eventCount = Mathf.Min(udonBehaviours.Length, udonBehaviourEvents.Length);
            for (var i = 0; i < eventCount; i++)
            {
                if (udonBehaviours[i] != null && !string.IsNullOrEmpty(udonBehaviourEvents[i]))
                {
                    udonBehaviours[i].SendCustomEvent(udonBehaviourEvents[i]);
                }
            }
        }
    }
}
