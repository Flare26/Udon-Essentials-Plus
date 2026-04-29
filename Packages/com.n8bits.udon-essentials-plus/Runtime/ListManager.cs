
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ListManager : UdonSharpBehaviour
{
    [Header("List Manager v1.0.7 by n8bits")]
    [Header("--Lists--")]
    [SerializeField] string[] blackListed;
    [SerializeField] string[] masterWhitelist; // admin
    [SerializeField] string[] djWhitelist;
    [SerializeField] string[] dancerWhitelist;
    [SerializeField] string[] remoteVIPs;
    [SerializeField] string[] remoteAdmins;
    [Header("--Object Groups--")]
    [SerializeField] GameObject[] djObjs;
    [SerializeField] GameObject[] dancerObjs;
    [SerializeField] GameObject[] adminObjs;
    [SerializeField] GameObject[] vipObjs;
    [SerializeField] BoxCollider vipRoomLock;

    [SerializeField] Transform banTP;

    [SerializeField] private bool isAdmin;
    [SerializeField] private bool isVIP;
    private bool isDancer;
    private bool isDJ;

    [Header("--Remote Loading--")]
    [SerializeField] bool useStringFile;
    [SerializeField] VRCUrl vipUrl;
    [SerializeField] VRCUrl adminUrl;
    [SerializeField] float refreshTime;
    private float timer = 0;
//choomba

    private void Start()
    {
        //LoadList(vipUrl);
        //LoadList(adminUrl);
    }
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
                EnableDJObjects();
            }
        }

        foreach (string name in dancerWhitelist)
        {
            // check the player who just joined
            if (Networking.LocalPlayer.displayName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("Whitelisted dancer joined ... " + player);
                EnableDancerObjects();
            }
        }

        foreach (string name in masterWhitelist)
        {
            // check the player who just joined
            if (Networking.LocalPlayer.displayName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("Whitelisted DJ joined ... " + player);
                UpdateAdminObjects();
            }
        }
    }

    private void LoadList(VRCUrl url)
    {
        VRCStringDownloader.LoadUrl(url, (IUdonEventReceiver)this);
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        Debug.Log("[ListManager] String Load Success");
        string resultAsUTF8 = result.Result;
        string[] elements = resultAsUTF8.Split('\n');

        Debug.Log("[ListManager] Length = " + elements.Length);

        int pointer = 0;
        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i].Contains("--LIST_") && elements[i].Contains("_END--"))
            {
                // see what list is ending
                string[] split = elements[i].Split('_');
                string list = split[1];
                Debug.Log("[ListManager] List ending is " + list);

                switch (list)
                {
                    case "VIP":
                        remoteVIPs = GetSubArray(elements, pointer, i);
                        Debug.Log($"[ListManager] VIPs: {string.Join(",", remoteVIPs)}");
                        break;

                    case "1":
                        remoteAdmins = GetSubArray(elements, pointer, i);
                        Debug.Log($"[ListManager] VIPs: {string.Join(",", remoteVIPs)}");
                        break;

                }
                pointer = i + 1;
            }
        }
        Debug.Log("[ListManager] Checking Local Player");
        CheckLocalPlayer();
    }

    private void CheckLocalPlayer()
    {
        string localName = Networking.LocalPlayer.displayName.ToLower();

        //reset flags for the check. They will stay disabled if the player has been removed during gameplay
        isVIP = false;
        isAdmin = false;

        // Check VIP list
        foreach (string name in remoteVIPs)
        {
            //Debug.Log($"[ListManager] {name.Trim().ToLower()}={localName.Trim().ToLower()}?");
            if (localName.Trim().ToLower().Equals(name.Trim().ToLower()))
            {
                Debug.Log("[ListManager] IsVIP in CheckLocalPlayer");
                isVIP = true; // flag is set to true, objects updated via Update call

                break;
            }
        }


        foreach (string name in remoteAdmins)
        {
            //Debug.Log($"[ListManager] {name.Trim().ToLower()}={localName.Trim().ToLower()}?");
            if (localName.Trim().ToLower().Equals(name.Trim().ToLower()))
            {
                Debug.Log("[ListManager] IsAdmin in CheckLocalPlayer");
                isAdmin = true;// flag is set to true, objects updated via Update call
                isVIP = true;// flag is set to true, objects updated via Update call
                Debug.Log("Set isAdmin true");
                break;
            }
        }

        // Do the updates below after all potential flags have been set
        UpdateVIPObjects();
        UpdateAdminObjects();
    }

    private string[] GetSubArray(string[] array, int start, int end)
    {
        int length = end - start;
        string[] subArray = new string[length];
        System.Array.Copy(array, start, subArray, 0, length);
        return subArray;
    }
    public override void OnStringLoadError(IVRCStringDownload result)
    {
        Debug.LogError($"[ListManager] Error loading string: {result.ErrorCode} - {result.Error}");
    }

    private void UpdateVIPObjects()
    {
        Debug.Log("[ListManager] Toggling VIP");

        foreach (GameObject o in vipObjs)
        {
            o.SetActive(isVIP);
        }

        if (isVIP)
            vipRoomLock.enabled = true;
        else
            vipRoomLock.enabled = false;
    }
    private void UpdateAdminObjects()
    {
        if (isAdmin)
        {

            foreach (GameObject o in adminObjs)
            {
                o.SetActive(isAdmin);
            }
        }
    }

    private void EnableDJObjects()
    {
        foreach (GameObject o in djObjs)
        {
            o.SetActive(true);
        }
    }

    private void EnableDancerObjects()
    {
        foreach (GameObject o in dancerObjs)
        {
            o.SetActive(true);
        }
    }

    private void FixedUpdate()
    {
        if (timer <= 0)
        {
            Debug.Log("Attempting loading list file");
            if (vipUrl != null)
                LoadList(vipUrl);
            if (adminUrl != null)
                LoadList(adminUrl);
            timer = refreshTime;
            return;
        }
        else
            timer -= Time.fixedDeltaTime;    
    }
}
