
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
// N8bits desktop camera controller for changing cam angles and recording.
public class CameraControls : UdonSharpBehaviour
{

    [SerializeField] Camera podCam;
    [SerializeField] Transform[] camPoints;
    [SerializeField]  Slider FOVSlider;
    [SerializeField] TextMeshProUGUI FOVtext;
    float defaultFOV;
    float[] pointFOVs;
    int triIdx;
    [SerializeField] GameObject camIndicator; // sync by set ownership and vrc object sync script

    bool isEnabled;

    void Start()
    {
        defaultFOV = podCam.fieldOfView;
        FOVtext.text = defaultFOV.ToString();

        pointFOVs = new float[camPoints.Length];

        for (int i = 0; i < pointFOVs.Length; i++)
        {
            pointFOVs[i] = defaultFOV;
        }

        FOVSlider.value = defaultFOV;
    }

    public void SetFOV()
    {
        pointFOVs[triIdx] = FOVSlider.value;
        podCam.fieldOfView = pointFOVs[triIdx];
        FOVtext.text = pointFOVs[triIdx].ToString();
    }

    public void SetFOV(float newFov)
    {
        pointFOVs[triIdx] = newFov;
        podCam.fieldOfView = pointFOVs[triIdx];
        FOVtext.text = pointFOVs[triIdx].ToString();
        FOVSlider.value = newFov;
        Debug.Log($"Updated point FOV for {triIdx} with {newFov}");
    }

    public void ResetFOV()
    {
        // override fov for each camera angle
        for (int i = 0; i < pointFOVs.Length; i++)
        {
            pointFOVs[i] = defaultFOV;
        }

        // reset slider and cam

        FOVSlider.value = defaultFOV;
        FOVtext.text = defaultFOV.ToString();

        podCam.fieldOfView = defaultFOV;
    }



    void EnablePodCam()
    {
        podCam.depth = 100;
    }

    void DisablePodCam()
    {
        podCam.depth = -100;
        camIndicator.transform.position = new Vector3(1000, 1000, 1000);
    }

    public void SwitchCamPosition(int idx)
    {
        triIdx = idx;

        Debug.Log("PodCam: Swapping camera position to idx " + triIdx);
        podCam.transform.position = camPoints[triIdx].transform.position;
        podCam.transform.rotation = camPoints[triIdx].transform.rotation;

        if (!Networking.LocalPlayer.Equals(Networking.GetOwner(camIndicator)))
            Networking.SetOwner(Networking.LocalPlayer, camIndicator);

        // UpdateCamIndicator
        if (!Networking.GetOwner(camIndicator).Equals(Networking.LocalPlayer))
            Networking.SetOwner(Networking.LocalPlayer, camIndicator);

        camIndicator.transform.position = camPoints[triIdx].transform.position;
        camIndicator.transform.rotation = camPoints[triIdx].transform.rotation;


        // Update point based FOV

        podCam.fieldOfView = pointFOVs[triIdx];
        FOVSlider.value = pointFOVs[triIdx];
        FOVtext.text = pointFOVs[triIdx].ToString();
        Debug.Log($"PodCam: Updated cam fov with value from pointFOVs[{triIdx}] which is {pointFOVs[triIdx]}");
    }

    public void ToggleSystem()
    {
        Debug.Log("=SYSTEM TOGGLED=");
        isEnabled = !isEnabled;
        podCam.enabled = isEnabled;
        camIndicator.SetActive(isEnabled);

        if (podCam.enabled == true)
        {
            Debug.Log("==SYSTEM ENABLING==");
            podCam.depth = 100f;
            SwitchCamPosition(triIdx);
        } else

        {
            Debug.Log("==SYSTEM DISABLING==");
            podCam.depth = -100f;
        }
    }

    public void TestUpCam()
    {
        if (triIdx + 1 <= camPoints.Length-1)
        {
            triIdx++;
            SwitchCamPosition(triIdx);
        }
    }

    public void TestDownCam()
    {
        if (triIdx - 1 >= 0)
        {
            triIdx--;
            SwitchCamPosition(triIdx);
        }
    }

    public void Update()
    {
        //SYSTEM ENABLE
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleSystem();
        }


        // CAMERA SWITCHING
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            Debug.Log("Alpha1");
            if(camPoints[0] != null)
            {
                SwitchCamPosition(0); // label the physical cam markers 1-9, where marker 1 is idx 0.
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            Debug.Log("Alpha2");
            if (camPoints[1] != null)
            {
                SwitchCamPosition(1); // label the physical cam markers 1-9, where marker 1 is idx 0.
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            Debug.Log("Alpha3");
            if (camPoints[2] != null)
            {
                SwitchCamPosition(2); // label the physical cam markers 1-9, where marker 1 is idx 0.
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            Debug.Log("Alpha4");
            if (camPoints[2] != null)
            {
                SwitchCamPosition(3); // label the physical cam markers 1-9, where marker 1 is idx 0.
            }
        }

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadEquals))
        {
            float oldFov = podCam.fieldOfView;
            float newFov = oldFov; // unmodified yet

            if (oldFov + 5 <= 120)
            {
                newFov = oldFov + 5;
            }

            SetFOV(newFov);
        }

        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            float oldFov = podCam.fieldOfView;
            float newFov = oldFov; // unmodified yet

            if (oldFov - 5 >= 20)
            {
                newFov = oldFov - 5;
            }
            SetFOV(newFov);
        }

        if ((podCam.transform.position != camPoints[triIdx].transform.position) && isEnabled)
        {
            podCam.transform.position = camPoints[triIdx].transform.position; // lock the camera to the currently active point
            podCam.transform.rotation = camPoints[triIdx].transform.rotation;
        }
    }
}
