namespace flipbits.DanceTracker
{
    using UdonSharp;
    using UnityEngine;
    using VRC.SDKBase;

    // Config + local permission state shared by all IndividualTracker player objects.
    // Counts themselves live on the trackers (persisted). This just holds the
    // display settings, the reset window, and who is allowed to see/click.
    //
    // Permissions are two flags (isStaff, isManager) that something external
    // grants, and three toggles the user flips from UI. Toggles only take effect
    // if the matching flag is set. None of it is synced; each client works out
    // what it shows on its own.
    //
    // Hooking it up:
    //  - whitelist (VRCLinking etc): SendCustomEvent GrantStaff / GrantManager
    //    when a user verifies, then bind buttons to ToggleStaffView /
    //    ToggleManagerInteract. Those no-op for anyone without the flag.
    //  - no whitelist: hidden interact cube -> EnableStaffView or
    //    EnableManagerInteract. Grants and unlocks in one go, so put the cube
    //    somewhere only staff can get to.
    public class DanceTrackerManager : UdonSharpBehaviour
    {
        [Header("Bounds")]
        [Tooltip("Lowest count. Counts up to medianBound use the low->median colour ramp.")]
        public int lowBound = 0;
        [Tooltip("Where the colour ramp switches from low->median to median->high.")]
        public int medianBound = 3;
        [Tooltip("Highest normal count. After this comes F (if enabled), then N/D, then wraps to lowBound.")]
        public int highBound = 6;

        [Header("Colors")]
        public Color lowBoundColor = Color.red;
        public Color medianBoundColor = Color.green;
        public Color highBoundColor = Color.blue;
        public Color noDanceColor = Color.gray;
        [Tooltip("Adds an F state between the top count and N/D.")]
        public bool useFloaterState = true;
        [Tooltip("Colour for the F state.")]
        public Color floaterStateColor = Color.green;

        [Header("Persistence")]
        [Tooltip("If a player's count hasn't changed in this many hours it resets to 0 next time they join. 0 disables.")]
        public float resetThresholdHours = 24f;

        [Header("Debug")]
        [Tooltip("Show your own tracker to yourself. For solo testing only, turn it off before publishing.")]
        public bool showSelfTracker = false;

        // Permission flags. Set from outside via Grant/Revoke.
        [HideInInspector] public bool isStaff = false;
        [HideInInspector] public bool isManager = false;

        // Effective unlock state, read by the trackers.
        [HideInInspector] public bool staffViewUnlocked = false;
        [HideInInspector] public bool managerInteractUnlocked = false;
        [HideInInspector] public bool selfViewUnlocked = false;

        // What the user asked for. Kept separate so a revoke/re-grant or a
        // parent toggle going off doesn't lose their preference.
        bool staffViewRequested = false;
        bool managerInteractRequested = false;
        bool selfViewRequested = false;

        void Start()
        {
            selfViewRequested = showSelfTracker;
            RefreshUnlockState();

            if (selfViewRequested)
            {
                Debug.LogWarning("[DanceTrackerManager] showSelfTracker is on, turn it off before publishing.");
            }
        }

        public long GetResetThresholdSeconds()
        {
            return (long)(resetThresholdHours * 3600f);
        }

        // Manager implies staff.
        public bool HasStaffPermission()
        {
            return isStaff || isManager;
        }

        public bool HasManagerPermission()
        {
            return isManager;
        }

        // Grants. Idempotent. Granting also turns the matching view on.
        public void GrantStaff()
        {
            isStaff = true;
            staffViewRequested = true;
            RefreshUnlockState();
            Debug.Log("[DanceTrackerManager] GrantStaff");
        }

        public void GrantManager()
        {
            isManager = true;
            staffViewRequested = true;
            managerInteractRequested = true;
            RefreshUnlockState();
            Debug.Log("[DanceTrackerManager] GrantManager");
        }

        public void RevokeStaff()
        {
            isStaff = false;
            ReconcileLocksAfterRevoke();
            Debug.Log("[DanceTrackerManager] RevokeStaff");
        }

        public void RevokeManager()
        {
            isManager = false;
            ReconcileLocksAfterRevoke();
            Debug.Log("[DanceTrackerManager] RevokeManager");
        }

        public void RevokeAll()
        {
            isStaff = false;
            isManager = false;
            staffViewRequested = false;
            managerInteractRequested = false;
            selfViewRequested = false;
            RefreshUnlockState();
            Debug.Log("[DanceTrackerManager] RevokeAll");
        }

        // Drop any unlocks the user no longer has a flag for.
        private void ReconcileLocksAfterRevoke()
        {
            if (!HasManagerPermission())
            {
                managerInteractRequested = false;
            }
            if (!HasStaffPermission())
            {
                staffViewRequested = false;
                selfViewRequested = false;
            }
            RefreshUnlockState();
        }

        private void RefreshUnlockState()
        {
            bool hasStaffPermission = HasStaffPermission();
            bool hasManagerPermission = HasManagerPermission();

            staffViewUnlocked = hasStaffPermission && staffViewRequested;
            managerInteractUnlocked = hasManagerPermission && staffViewUnlocked && managerInteractRequested;
            selfViewUnlocked = hasStaffPermission && selfViewRequested;
        }

        // Toggles for UI buttons. No-op without the matching flag.
        // Staff view off also turns interact off (can't click what you can't see).
        // Interact on also turns staff view on.
        public void ToggleStaffView()
        {
            if (!HasStaffPermission()) return;
            staffViewRequested = !staffViewRequested;
            RefreshUnlockState();
            Debug.Log($"[DanceTrackerManager] staffViewUnlocked={staffViewUnlocked}");
        }

        public void ToggleManagerInteract()
        {
            if (!HasManagerPermission()) return;
            managerInteractRequested = !managerInteractRequested;
            if (managerInteractRequested) staffViewRequested = true;
            RefreshUnlockState();
            Debug.Log($"[DanceTrackerManager] managerInteractUnlocked={managerInteractUnlocked} staffViewUnlocked={staffViewUnlocked}");
        }

        public void SetStaffView(bool unlocked)
        {
            if (!HasStaffPermission()) return;
            staffViewRequested = unlocked;
            RefreshUnlockState();
        }

        public void SetManagerInteract(bool unlocked)
        {
            if (!HasManagerPermission()) return;
            managerInteractRequested = unlocked;
            if (unlocked) staffViewRequested = true;
            RefreshUnlockState();
        }

        public void ToggleSelfView()
        {
            if (!HasStaffPermission()) return;
            selfViewRequested = !selfViewRequested;
            RefreshUnlockState();
            Debug.Log($"[DanceTrackerManager] selfViewUnlocked={selfViewUnlocked}");
        }

        public void SetSelfView(bool unlocked)
        {
            if (!HasStaffPermission()) return;
            selfViewRequested = unlocked;
            RefreshUnlockState();
            Debug.Log($"[DanceTrackerManager] selfViewUnlocked={selfViewUnlocked}");
        }

        // Parameterless versions of the setters, for SendCustomEvent bindings.
        public void TurnOnStaffView()
        {
            SetStaffView(true);
            Debug.Log($"[DanceTrackerManager] staffViewUnlocked={staffViewUnlocked}");
        }

        public void TurnOffStaffView()
        {
            SetStaffView(false);
            Debug.Log($"[DanceTrackerManager] staffViewUnlocked={staffViewUnlocked} managerInteractUnlocked={managerInteractUnlocked} selfViewUnlocked={selfViewUnlocked}");
        }

        public void TurnOnManagerInteract()
        {
            SetManagerInteract(true);
            Debug.Log($"[DanceTrackerManager] managerInteractUnlocked={managerInteractUnlocked} staffViewUnlocked={staffViewUnlocked}");
        }

        public void TurnOffManagerInteract()
        {
            SetManagerInteract(false);
            Debug.Log($"[DanceTrackerManager] managerInteractUnlocked={managerInteractUnlocked} staffViewUnlocked={staffViewUnlocked}");
        }

        public void TurnOnSelfView()
        {
            SetSelfView(true);
        }

        public void TurnOffSelfView()
        {
            SetSelfView(false);
        }

        // Old names, kept so existing button bindings don't break.
        public void EnableStaffViewUnlock() { TurnOnStaffView(); }
        public void DisableStaffViewUnlock() { TurnOffStaffView(); }
        public void EnableManagerInteractUnlock() { TurnOnManagerInteract(); }
        public void DisableManagerInteractUnlock() { TurnOffManagerInteract(); }
        public void EnableSelfViewUnlock() { TurnOnSelfView(); }
        public void DisableSelfViewUnlock() { TurnOffSelfView(); }

        // Grant + unlock in one call, for the hidden-button setup.
        public void EnableStaffView()
        {
            GrantStaff();
            staffViewRequested = true;
            RefreshUnlockState();
            Debug.Log("[DanceTrackerManager] EnableStaffView");
        }

        public void EnableManagerInteract()
        {
            GrantManager();
            staffViewRequested = true;
            managerInteractRequested = true;
            RefreshUnlockState();
            Debug.Log("[DanceTrackerManager] EnableManagerInteract");
        }
    }
}
