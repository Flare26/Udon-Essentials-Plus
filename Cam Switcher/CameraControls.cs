
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

// N8bits desktop camera controller for changing cam angles and recording.
public class CameraControls : UdonSharpBehaviour
{

    [SerializeField] Camera podCam;
    [SerializeField] Transform[] camPoints;
    [SerializeField]  Slider FOVSlider;
    [SerializeField] TextMeshProUGUI FOVtext;
    float defaultFOV;
    float[] pointFOVs;
    int point;
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
        pointFOVs[point] = FOVSlider.value;
        podCam.fieldOfView = pointFOVs[point];
        FOVtext.text = pointFOVs[point].ToString();
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
        podCam.depth = 1;
    }

    void DisablePodCam()
    {
        podCam.depth = -1;
        camIndicator.transform.position = new Vector3(1000, 1000, 1000);
    }

    public void SwitchCamPosition(int idx)
    {
        podCam.transform.position = camPoints[idx].transform.position;
        podCam.transform.rotation = camPoints[idx].transform.rotation;

        if (!Networking.LocalPlayer.Equals(Networking.GetOwner(camIndicator)))
            Networking.SetOwner(Networking.LocalPlayer, camIndicator);

        // UpdateCamIndicator
        if (!Networking.GetOwner(camIndicator).Equals(Networking.LocalPlayer))
            Networking.SetOwner(Networking.LocalPlayer, camIndicator);

        camIndicator.transform.position = camPoints[idx].transform.position;
        camIndicator.transform.rotation = camPoints[idx].transform.rotation;


        // Update point based FOV

        podCam.fieldOfView = pointFOVs[idx];
        FOVSlider.value = pointFOVs[idx];
        FOVtext.text = pointFOVs[idx].ToString();

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
            SwitchCamPosition(point);
        } else

        {
            Debug.Log("==SYSTEM DISABLING==");
            podCam.depth = -100f;
        }
    }

    public void TestUpCam()
    {
        if (point + 1 <= camPoints.Length-1)
        {
            point++;
            SwitchCamPosition(point);
        }
    }

    public void TestDownCam()
    {
        if (point - 1 >= 0)
        {
            point--;
            SwitchCamPosition(point);
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

            // keep adding more for each number on keyboard

            if (camPoints[1] != null)
            {
                SwitchCamPosition(1); // label the physical cam markers 1-9, where marker 1 is idx 0.
            }
        }
    }
}
