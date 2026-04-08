using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UEPlus
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TriggerExitDisabler : UdonSharpBehaviour
    {
        [Header("Trigger Exit Disabler by n8")]
        [Header("v1.0.1")]
        [SerializeField] GameObject[] disables;
        [Header("Alternative method")]
        [Tooltip("Fires interact() event on behaviours array instead of using disables array."), SerializeField] bool useEvents;
        [SerializeField] string methodName;
        [Tooltip("Will fire the Interact() event on these objects"), SerializeField] UdonBehaviour[] behaviours;

        public override void OnPlayerTriggerExit(VRCPlayerApi api)
        {
            Debug.Log("OnPlayerTriggerExit called");

            if (Networking.LocalPlayer.Equals(api))
            {
                if (!useEvents)
                {
                    foreach (GameObject o in disables)
                    {
                        if (o.activeSelf == true)
                        {
                            o.SetActive(false);
                            Debug.Log($"Disabling GameObject: {o.name}");
                        }
                    }
                }
                else
                {
                    foreach (UdonBehaviour o in behaviours)
                    {
                        Debug.Log($"Checking program var on {o.name}");
                        bool state = (bool) o.GetProgramVariable("state");
                        if (state)
                        {
                            Debug.Log($"State true. Sending custom event on {o.name}");
                            o.SendCustomEvent(methodName);
                        }
                    }
                }
            }
        }
    }
}
