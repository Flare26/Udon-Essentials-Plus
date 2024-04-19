
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ODMenuCtrl : UdonSharpBehaviour
{
    bool isMenuActive;
    public float menuDist;
    [SerializeField] GameObject menu;
    [SerializeField, Range(0,1)] float flickDecay;
    [SerializeField] float activateTime;
    [SerializeField] float deactivateTime;
    [SerializeField] float timerActivate;
    [SerializeField] float timerDeactivate;

    bool isWaitingForDown;
    bool isVr;

    private void Start()
    {
        isVr = Networking.LocalPlayer.IsUserInVR();
       

    }
    void ActivateMenu(Vector3 pos, Quaternion rot)
    {
        isMenuActive = true;
        menu.transform.position = pos;
        menu.transform.rotation = rot;
        menu.SetActive(true);
        timerActivate = activateTime;
    }

    void DeactivateMenu()
    {
        isMenuActive = false;
        menu.SetActive(false);
        timerDeactivate = deactivateTime;
    }

    
    private void Update()
    {
        if (isVr)
        {
            //since using Time.DeltaTime accures 1f per second, a fast flick would complete somewhere in the range of 0.5f per sec.
            //Test first by making decay .5 and seeing.

            //float rightThumbstickHorizontal = Input.GetAxis("RightThumbstickHorizontal");
            float rightThumbstickVertical = Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickVertical");
            float rightTrigger = Input.GetAxis("Oculus_CrossPlatform_SecondaryHandTrigger");
            //Debug.Log("Right trigger = " + rightTrigger.ToString());
            //Debug.Log("Right Vertical = " + rightThumbstickVertical.ToString());

            if (rightThumbstickVertical >= 0.8f && rightTrigger >= 0.8f)
            {
                timerActivate -= Time.deltaTime;

                if (timerActivate <= 0 && !isMenuActive)
                {
                    
                    Vector3 ppos = Networking.LocalPlayer.GetPosition();
                    Quaternion prot = Networking.LocalPlayer.GetRotation();
                    float ph = Networking.LocalPlayer.GetAvatarEyeHeightAsMeters();

                    Vector3 forwardDirection = prot * Vector3.forward; // calculate direction player is facing
                    Vector3 menuPosition = ppos + new Vector3(0, ph, 0) + forwardDirection * menuDist;

                    ActivateMenu(menuPosition, prot);
                }
            } else if (rightThumbstickVertical <= -0.8f && rightTrigger >= 0.8f)
            {
                timerDeactivate -= Time.deltaTime;

                if (timerDeactivate <= 0 && isMenuActive)
                {
                    DeactivateMenu();
                }
            }


            return;
        } 

        //if check was false, so user on desktop

        if (Input.GetKeyDown(KeyCode.I))
        {

            if (!Networking.LocalPlayer.IsUserInVR() && !isMenuActive)
            {
                // get player position then offset it based on player height
                Vector3 ppos = Networking.LocalPlayer.GetPosition();
                Quaternion prot = Networking.LocalPlayer.GetRotation();
                float ph = Networking.LocalPlayer.GetAvatarEyeHeightAsMeters();

                Vector3 forwardDirection = prot * Vector3.forward; // calculate direction player is facing
                Vector3 menuPosition = ppos + new Vector3(0, ph, 0) + forwardDirection * menuDist;

                ActivateMenu(menuPosition, prot);

            } else if (!Networking.LocalPlayer.IsUserInVR() && isMenuActive)
            {
                DeactivateMenu();
            }
            
        }
    }
}
