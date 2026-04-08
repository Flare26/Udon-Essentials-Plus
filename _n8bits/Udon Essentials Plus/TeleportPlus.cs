using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]

public class TeleportPlus : UdonSharpBehaviour
{
    [Header("Teleport+ by n8")]

    [SerializeField]
    [Tooltip("Location where the palyer will be sent after interacting with this object.")]
    Transform destinationWaypoint;

    [SerializeField]
    [Tooltip("If checked, player rotation wil be overridden by the destination waypoint's rotation when teleporting.")]
    bool useWaypointRotation;

    public override void Interact()
    {
        TeleportPlayer();
    }

    public void TeleportPlayer()
    {
        if (useWaypointRotation)
            Networking.LocalPlayer.TeleportTo(destinationWaypoint.position, destinationWaypoint.rotation);
        else
            Networking.LocalPlayer.TeleportTo(destinationWaypoint.position, Networking.LocalPlayer.GetRotation());
    }
}