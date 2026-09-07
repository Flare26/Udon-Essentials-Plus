
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TripodLevel : UdonSharpBehaviour
{
    [SerializeField] Transform tripodParent;
    void Start()
    {
        
    }

    public void LevelTripod()
    {
        if (!Networking.GetOwner(tripodParent.gameObject).Equals(Networking.LocalPlayer))
            Networking.SetOwner(Networking.LocalPlayer, tripodParent.gameObject);

        Vector3 newAngles = new Vector3(0f, tripodParent.eulerAngles.y, 0f);
        tripodParent.eulerAngles = newAngles;
    }
}
