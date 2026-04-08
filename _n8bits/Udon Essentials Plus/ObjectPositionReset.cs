
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ObjectPositionReset : UdonSharpBehaviour
{

    [SerializeField] GameObject[] toBeReset;
    Rigidbody[] rbs;
    Vector3[] OGpos;
    Quaternion[] OGrot;

    void Start()
    {
        OGpos = new Vector3[toBeReset.Length];
        rbs = new Rigidbody[toBeReset.Length];
        OGrot = new Quaternion[toBeReset.Length];
        bool t;
        for (int i = 0; i < toBeReset.Length; i++)
        {
            OGpos[i] = toBeReset[i].transform.position;

            rbs[i] = toBeReset[i].GetComponent<Rigidbody>();

            OGrot[i] = toBeReset[i].transform.rotation;
        }
    }

    public override void Interact()
    {
        for (int i = 0; i < toBeReset.Length; i++)
        {
            //Debug.Log("resetting " + toBeReset[i].name);
            Networking.SetOwner(Networking.LocalPlayer, toBeReset[i]);

            toBeReset[i].transform.position = OGpos[i];

            if (rbs[i])
                rbs[i].velocity = Vector3.zero;

            toBeReset[i].transform.rotation = OGrot[i];
        }
    }


}
