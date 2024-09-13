
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class TouchMe : UdonSharpBehaviour
{
    [Header("TouchMe Unlocker by n8bits")]
    [Header("V1.0.0")]
    [SerializeField] GameObject[] objs;
    [SerializeField] Collider[] colliders;
    [SerializeField] Button[] buttons;
    [SerializeField] bool[] objSetBool;
    [SerializeField] bool[] colliderSetBool;
    [SerializeField] bool locked;
    [SerializeField] Canvas canvas1;
    [SerializeField] Canvas canvas2;
    [SerializeField] Canvas[] canvases;
    [SerializeField] Collider thisCollider;
    void Start()
    {
        locked = true;
        gameObject.SetActive(false);

        for (int i = 0; i < objs.Length; i++)
        {
            if (objs[i] != null)
            {
                objs[i].SetActive(false);
            }
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = false;
        }

        if (canvas1)
            canvas1.enabled = false;
        if (canvas2)
            canvas1.enabled = false;
    }

    public void OnEnable()
    {
        Vector3 myVec = Networking.LocalPlayer.GetPosition();

        if (thisCollider.bounds.Contains(myVec))
        {
            Unlock();
        }
    }

    private void Unlock()
    {
        locked = false;
        for (int i = 0; i < objs.Length; i++)
        {
            if (objs[i] != null)
            {
                objs[i].SetActive(objSetBool[i]);
            }
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = (colliderSetBool[i]);
            }
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = true;
        }

        if (canvas1)
            canvas1.enabled = true;
        if (canvas2)
            canvas1.enabled = true;
    }
    public override void OnPlayerTriggerEnter(VRCPlayerApi api)
    {
        if (Networking.LocalPlayer.Equals(api))
        {
            if (locked == true)
            {
                Unlock();
            }
        }
    }
}
