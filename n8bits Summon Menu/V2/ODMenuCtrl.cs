using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ODMenuCtrl : UdonSharpBehaviour
{
    bool isMenuActive;
    public float wristDist;
    public float pcDist;
    [SerializeField] GameObject menu;
    //[SerializeField, Range(0, 1)] float flickDecay;
    [SerializeField] float activateTime;
    //[SerializeField] float deactivateTime;
     float timerActivate;
    //[SerializeField] float timerDeactivate;
    [SerializeField] Vector3 menuWristScale; // Smaller scale when attached to wrist

    Vector3 menuOriginPos;
    Quaternion menuOriginRot;
    Vector3 menuOriginScale;
    bool isVr;

    private void Start()
    {
        isVr = Networking.LocalPlayer.IsUserInVR();

        // Save the original static menu position, rotation, and scale
        menuOriginPos = menu.transform.position;
        menuOriginRot = menu.transform.rotation;
        menuOriginScale = menu.transform.localScale; // Ensure you're using local scale

        // Set initial state - keep the menu active but in its default position
        menu.transform.position = menuOriginPos;
        menu.transform.rotation = menuOriginRot;
        menu.transform.localScale = menuOriginScale;
    }

    void ActivateMenuOnWrist(Vector3 wristPos, Quaternion wristRot)
    {
        isMenuActive = true;

        // Move the menu to the wrist
        menu.transform.position = wristPos;
        menu.transform.rotation = wristRot * Quaternion.Euler(0, 180f, 180f); // Adjust rotation if upside down

        // Set the smaller wrist scale
        menu.transform.localScale = menuWristScale;

        timerActivate = activateTime;
    }

    void ActivateMenuOnDesktop()
    {
        isMenuActive = true;

        // Get player position in desktop mode (use head data for better accuracy)
        VRCPlayerApi.TrackingData headData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        Vector3 headPos = headData.position;
        Quaternion headRot = headData.rotation;

        // Calculate the menu position slightly in front of the player's head
        Vector3 forwardDirection = headRot * Vector3.forward; // Use head rotation to get forward direction
        Vector3 menuPosition = headPos + forwardDirection * pcDist; // Offset the menu forward

        // Set the menu position
        menu.transform.position = menuPosition;

        // Make the menu look at the player's head (so it faces the player)
        Vector3 playerHeadPos = headPos; // The player's head position
        Vector3 directionToLook = playerHeadPos - menuPosition; // Calculate direction from menu to player's head
        menu.transform.rotation = Quaternion.LookRotation(directionToLook); // Rotate the menu to face the player
    }


    void DeactivateMenuToStatic()
    {
        isMenuActive = false;

        // Move the menu back to the original position, rotation, and scale
        menu.transform.position = menuOriginPos;
        menu.transform.rotation = menuOriginRot;
        menu.transform.localScale = menuOriginScale;

        // No need to deactivate, just move it back to the world space position
    }

    private void Update()
    {
        if (isVr)
        {
            VRCPlayerApi.TrackingData handData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
            Vector3 handPos = handData.position;
            Quaternion handRot = handData.rotation;
            bool leftStickClick = Input.GetButton("Oculus_CrossPlatform_SecondaryThumbstick");

            if (isMenuActive)
            {
                // Calculate the offset to the wrist
                Vector3 wristOffset = handRot * Vector3.down * wristDist; // Adjust this offset if needed
                menu.transform.position = handPos + wristOffset;

                // Correct the rotation to avoid the menu being upside down
                menu.transform.rotation = handRot * Quaternion.Euler(0, 180f, 180f); // Adjust rotation

                // Make the menu face towards the player
                Vector3 playerHeadPos = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                menu.transform.LookAt(playerHeadPos);
            }

            // Handle menu activation/deactivation logic
            if (leftStickClick)
            {
                timerActivate -= Time.deltaTime;
                if (timerActivate <= 0)
                {
                    if (!isMenuActive)
                    {
                        // Activate the menu and attach it to the wrist
                        ActivateMenuOnWrist(handPos, handRot);
                    }
                    else
                    {
                        // Move the menu back to its original static position
                        DeactivateMenuToStatic();
                    }
                    timerActivate = activateTime;  // Reset the timer
                }
            }
            else
            {
                // Reset the activation timer when the stick is not being clicked
                timerActivate = activateTime;
            }
        }
        else
        {
            // Handle desktop mode with keyboard input
            if (Input.GetKeyDown(KeyCode.I))
            {
                if (!isMenuActive)
                {
                    ActivateMenuOnDesktop();
                }
                else
                {
                    // Move the menu back to the static position
                    DeactivateMenuToStatic();
                }
            }
        }
    }
}
