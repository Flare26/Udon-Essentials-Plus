namespace flipbits.DanceTracker
{
    using TMPro;
    using UdonSharp;
    using UnityEngine;
    using UnityEngine.UI;
    using VRC.SDKBase;
    using VRC.Udon.Common.Interfaces;

    // One of these per player, on a PlayerObject prefab with VRCEnablePersistence
    // on the root so danceCount/lastUpdatedUnix survive between visits.
    //
    // Only the owner writes the count. Clicking someone else's tracker sends a
    // network event to the owner, who does the increment. Whether you can see
    // or click anything is decided locally from the manager's unlock flags.
    //
    // Keep the visuals on child objects. The root has to stay active or the
    // network events stop arriving when the tracker is hidden.
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class IndividualTracker : UdonSharpBehaviour
    {
        [Header("Manager Lookup")]
        [Tooltip("Name of the GameObject with the DanceTrackerManager. PlayerObjects can't hold scene refs, so it's found by name.")]
        [SerializeField] string managerObjectName = "DanceTrackerManager";

        [Header("Visual")]
        [Tooltip("Metres above the player's head.")]
        [SerializeField] float headOffset = 0.3f;
        [SerializeField] TextMeshProUGUI[] numberTexts;
        [SerializeField] Image[] visualizationObjs;
        [Tooltip("Face the local player each frame.")]
        [SerializeField] bool billboard = true;

        [Header("Interaction")]
        [Tooltip("Collider for Interact. Leave empty to grab the one on the root.")]
        [SerializeField] Collider interactCollider;

        [UdonSynced] int danceCount = 0;
        [UdonSynced] long lastUpdatedUnix = 0;

        DanceTrackerManager manager;
        VRCPlayerApi owner;
        bool initialized = false;
        Color currentColor = Color.white;

        // Owner never changes for a PlayerObject, so cache it once.
        bool isLocalOwner = false;
        bool ownerCached = false;

        // Start "true" so the first ApplyState(false) actually runs.
        bool lastVisibleApplied = true;
        bool lastColliderEnabled = true;

        void Start()
        {
            GameObject managerObj = GameObject.Find(managerObjectName);
            if (managerObj != null)
            {
                // Generic GetComponent<T> here. The Type overload gives back a
                // raw Component that breaks when U# bridges to the proxy.
                manager = managerObj.GetComponent<DanceTrackerManager>();
            }

            if (manager == null)
            {
                Debug.LogError($"[IndividualTracker] No DanceTrackerManager on a GameObject named '{managerObjectName}'. Tracker disabled.");
                ApplyVisibility(false);
                if (interactCollider != null) interactCollider.enabled = false;
                return;
            }

            if (interactCollider == null)
            {
                interactCollider = GetComponent<Collider>();
            }

            CacheOwner();

            ApplyVisibility(false);
            lastVisibleApplied = false;
            if (interactCollider != null) interactCollider.enabled = false;
            lastColliderEnabled = false;

            initialized = true;
            UpdateDisplay();
        }

        private void CacheOwner()
        {
            if (ownerCached) return;

            VRCPlayerApi resolved = Networking.GetOwner(gameObject);
            if (resolved == null || !Utilities.IsValid(resolved)) return;

            owner = resolved;
            isLocalOwner = owner.isLocal;
            ownerCached = true;
        }

        // Fires once persistence has loaded. Owner doesn't get OnDeserialization
        // for its own data, so the stale reset happens here.
        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (player == null) return;

            // Owner may not have been assigned yet when Start ran.
            CacheOwner();

            if (player != owner) return;

            UpdateDisplay();

            if (!isLocalOwner) return;
            if (manager == null) return;

            long threshold = manager.GetResetThresholdSeconds();
            if (threshold > 0 && lastUpdatedUnix > 0 && danceCount > 0)
            {
                long elapsed = NowUnix() - lastUpdatedUnix;
                if (elapsed > threshold)
                {
                    Debug.Log($"[IndividualTracker] Auto-reset for {player.displayName}: {elapsed}s since last update (count was {danceCount})");
                    danceCount = 0;
                    // Leave lastUpdatedUnix alone, the next real click sets it.
                    UpdateDisplay();
                }
            }

            // Push to late joiners right away.
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            UpdateDisplay();
        }

        public override void Interact()
        {
            // Collider is only on when interact is unlocked, so we're allowed.
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(OwnerIncrement));
        }

        // Runs on the owner. Wraps after N/D (highBound+2 with F, +1 without).
        public void OwnerIncrement()
        {
            if (!Networking.IsOwner(gameObject)) return;
            if (manager == null) return;

            int next = danceCount + 1;
            int maxState = manager.highBound + (manager.useFloaterState ? 2 : 1);
            if (next > maxState) next = manager.lowBound;

            danceCount = next;
            lastUpdatedUnix = NowUnix();
            UpdateDisplay();
            RequestSerialization();
        }

        // Children only, root stays active.
        private void ApplyVisibility(bool visible)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(visible);
            }
        }

        private void ApplyState()
        {
            if (manager == null) return;

            bool shouldBeVisible;
            bool shouldBeInteractable;

            if (isLocalOwner)
            {
                // Your own tracker is only shown via selfView, not staff view.
                shouldBeVisible = manager.selfViewUnlocked;
                shouldBeInteractable = manager.selfViewUnlocked && manager.managerInteractUnlocked;
            }
            else
            {
                shouldBeVisible = manager.staffViewUnlocked;
                shouldBeInteractable = manager.managerInteractUnlocked;
            }

            if (shouldBeVisible != lastVisibleApplied)
            {
                ApplyVisibility(shouldBeVisible);
                lastVisibleApplied = shouldBeVisible;
            }

            if (interactCollider != null && shouldBeInteractable != lastColliderEnabled)
            {
                interactCollider.enabled = shouldBeInteractable;
                lastColliderEnabled = shouldBeInteractable;
            }
        }

        void Update()
        {
            if (!initialized) return;

            ApplyState();

            if (!lastVisibleApplied) return;
            if (owner == null || !Utilities.IsValid(owner)) return;

            VRCPlayerApi.TrackingData head = owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            transform.position = head.position + Vector3.up * headOffset;

            if (billboard)
            {
                VRCPlayerApi localPlayer = Networking.LocalPlayer;
                if (localPlayer != null && Utilities.IsValid(localPlayer))
                {
                    VRCPlayerApi.TrackingData localHead = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                    Vector3 lookDir = transform.position - localHead.position;
                    lookDir.y = 0f; // no vertical tilt
                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
                    }
                }
            }
        }

        // Wall clock, since server time doesn't carry across sessions.
        private long NowUnix()
        {
            return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        private void UpdateDisplay()
        {
            if (manager == null) return;

            int low = manager.lowBound;
            int median = manager.medianBound;
            int high = manager.highBound;

            bool isFloating = manager.useFloaterState && danceCount == high + 1;
            bool isNoDance = danceCount > high + (manager.useFloaterState ? 1 : 0);
            string countText = isNoDance ? "N/D" : isFloating ? "F" : danceCount.ToString();

            if (numberTexts != null)
            {
                for (int i = 0; i < numberTexts.Length; i++)
                {
                    if (numberTexts[i] != null) numberTexts[i].text = countText;
                }
            }

            if (isNoDance)
            {
                currentColor = manager.noDanceColor;
            }
            else if (isFloating)
            {
                currentColor = manager.floaterStateColor;
            }
            else if (danceCount <= median)
            {
                float t = (median == low) ? 0f : (float)(danceCount - low) / (median - low);
                currentColor = LerpColorHSV(manager.lowBoundColor, manager.medianBoundColor, t);
            }
            else
            {
                float t = (high == median) ? 1f : (float)(danceCount - median) / (high - median);
                currentColor = LerpColorHSV(manager.medianBoundColor, manager.highBoundColor, t);
            }

            if (visualizationObjs != null)
            {
                for (int i = 0; i < visualizationObjs.Length; i++)
                {
                    if (visualizationObjs[i] != null) visualizationObjs[i].color = currentColor;
                }
            }

            if (numberTexts != null)
            {
                for (int i = 0; i < numberTexts.Length; i++)
                {
                    if (numberTexts[i] != null) numberTexts[i].color = currentColor;
                }
            }
        }

        private Color LerpColorHSV(Color a, Color b, float t)
        {
            float hA, sA, vA, hB, sB, vB;
            Color.RGBToHSV(a, out hA, out sA, out vA);
            Color.RGBToHSV(b, out hB, out sB, out vB);

            // go the short way round the hue wheel
            float hDiff = hB - hA;
            if (hDiff > 0.5f) hA += 1f;
            else if (hDiff < -0.5f) hB += 1f;

            float h = Mathf.Lerp(hA, hB, t) % 1f;
            float s = Mathf.Lerp(sA, sB, t);
            float v = Mathf.Lerp(vA, vB, t);

            Color result = Color.HSVToRGB(h, s, v);
            result.a = Mathf.Lerp(a.a, b.a, t);
            return result;
        }
    }
}
