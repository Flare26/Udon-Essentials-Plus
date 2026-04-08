
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ToggleCycler : UdonSharpBehaviour
{

    [SerializeField] bool isGlobal;
    [SerializeField] GameObject[] cycleObjs;
    [SerializeField] int startIdx;
    [UdonSynced] int cIdx; // internal curent idx
    void Start()
    {
        if (startIdx != cIdx)
            CycleTo(cIdx);
        else
            CycleTo(startIdx);
    }


    public override void Interact()
    {
        if (isGlobal)
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

        cIdx = (cIdx + 1) % cycleObjs.Length; // update udon sync var
        CycleTo(cIdx); // cycle to the new var

        if (isGlobal)
            RequestSerialization(); // serialize cIdx increase if global
    }


    void CycleTo(int idx)
    {
        for (int i = 0; i < cycleObjs.Length; i++)
        {
            if (i == idx)
                cycleObjs[i].SetActive(true);
            else
                cycleObjs[i].SetActive(false);
        }

        cIdx = idx;
    }

    void CycleUp()
    {

    }

    void CycleDown()
    {

    }

    public override void OnDeserialization()
    {
        // on sync var changed then cycle to the value

        CycleTo(cIdx);
    }
}
