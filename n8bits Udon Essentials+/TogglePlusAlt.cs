
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TogglePlusAlt : UdonSharpBehaviour
{
    [Header("Toggle+ Alt by n8")]

    [SerializeField] bool isSynced;
    [UdonSynced] bool [] objStates;
    [UdonSynced] bool[] collStates;
    [SerializeField] GameObject[] togObjs;
    [SerializeField] Collider[] togColliders;
    void Start()
    {
        objStates = new bool[togObjs.Length];
        collStates = new bool[togColliders.Length];

    }

    public override void Interact()
    {
        // On interact, check if this toggle is supposed to be global or not and set ownership if so.
        // After setting ownership, request serializaiton if supposed to be synced

        if (!Networking.IsOwner(Networking.LocalPlayer, gameObject) && isSynced)
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        
        //Debug.Log("Changed state bool");

        SetState();
        Toggle();

        if (isSynced)
            RequestSerialization();
    }

    void SetState()
    {
        if (togObjs.Length > 0)
        {
            for (int i = 0; i < objStates.Length; i++)
            {
                objStates[i] = !togObjs[i].activeSelf;
            }
        }

        if (togColliders.Length > 0)
        {
            for (int i = 0; i < togColliders.Length; i++)
            {
                collStates[i] = !togColliders[i].enabled;
            }
        }

        //Debug.Log("Set state to " + state);
    }

    void Toggle()
    {
        if (togObjs.Length > 0)
        {
            for (int i = 0; i < objStates.Length; i++)
            {
                togObjs[i].SetActive(objStates[i]);
            }
        }

        if (togColliders.Length > 0)
        {
            for (int i = 0; i < togColliders.Length; i++)
            {
                togColliders[i].enabled = collStates[i];
            }
        }
    }
    public override void OnDeserialization()
    {
        // Make sure it doesnt double execute if its not supposed to be synced

        if (Networking.GetOwner(gameObject) == Networking.LocalPlayer)
            return;

        if (isSynced == false)
            return;

        Toggle();
        //Debug.Log("Set state from deserialize");
    }
}
