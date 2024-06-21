
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ListManager : UdonSharpBehaviour
{

    [SerializeField] string[] blackListed;
    [SerializeField] string[] masterWhitelist; // admin
    [SerializeField] string[] djWhitelist;
    [SerializeField] string[] dancerWhitelist;

    [SerializeField] GameObject[] djObjs;
    [SerializeField] GameObject[] dancerObjs;
    [SerializeField] GameObject[] adminObjs;

    [SerializeField] Transform banTP;

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        // if it's not the local player that joined, don't execute any checks otherwise objects will toggle.
        if (!player.displayName.Equals(Networking.LocalPlayer.displayName))
            return;

        //Blacklist handling
        foreach(string name in blackListed)
        {
                // check the player who just joined
            if (Networking.LocalPlayer.displayName.Equals(name,System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("Blackisted Player joined ... " + player);
                player.TeleportTo(banTP.position, banTP.rotation);
            }
        }

        foreach (string name in djWhitelist)
        {
            // check the player who just joined
            if (Networking.LocalPlayer.displayName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("Whitelisted DJ joined ... " + player);
                ToggleDJObjs();
            }
        }

        foreach (string name in dancerWhitelist)
        {
            // check the player who just joined
            if (Networking.LocalPlayer.displayName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("Whitelisted dancer joined ... " + player);
                ToggleDancerObjs();
            }
        }

        foreach (string name in masterWhitelist)
        {
            // check the player who just joined
            if (Networking.LocalPlayer.displayName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("Whitelisted DJ joined ... " + player);
                ToggleAdminObjs();
            }
        }
    }

    private void ToggleAdminObjs()
    {
        ToggleDJObjs();
        ToggleDancerObjs();
        foreach (GameObject o in adminObjs)
        {
            o.SetActive(!o.activeSelf);
        }
    }

    private void ToggleDJObjs()
    {
        foreach (GameObject o in djObjs)
        {
            o.SetActive(!o.activeSelf);
        }
    }

    private void ToggleDancerObjs()
    {
        foreach (GameObject o in dancerObjs)
        {
            o.SetActive(!o.activeSelf);
        }
    }
}
