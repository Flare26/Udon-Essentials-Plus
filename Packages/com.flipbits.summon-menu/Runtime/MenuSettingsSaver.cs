using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

namespace flipbits.SummonMenu
{
    /// <summary>
    /// Saves the menu's local toggles in VRChat PlayerData, so each player gets their own choices back on their next
    /// visit. Works with Toggle Plus, Toggle Plus V2 and Toggle Plus Alt, and with any UdonSharp behaviour that has a
    /// public bool "state" and a "ToggleProcedure" event. Leave Is Synced off on them: a synced toggle is everyone's.
    ///
    /// A toggle the player never changed keeps the scene's start state, so changing a start state in a later upload
    /// still reaches returning players. Other scripts can set a toggle through SetState (a preset, a join prompt);
    /// that counts as the player's choice and is saved too. Local only.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MenuSettingsSaver : UdonSharpBehaviour
    {
        [Tooltip("The toggles to save.")]
        public UdonBehaviour[] toggles;
        [Tooltip("PlayerData key per toggle, same order. Keep them stable between uploads: renaming a key forgets that setting.")]
        public string[] keys;
        [Tooltip("Put in front of every key, so two menus in one world don't share saves.")]
        [SerializeField] private string keyPrefix = "menu.";
        [Tooltip("Keep separate saves on PC and on Android/iOS, so a mobile visit keeps its own performance defaults.")]
        [SerializeField] private bool perPlatform = true;
        [Tooltip("Seconds between checks for a changed toggle.")]
        [SerializeField] private float checkInterval = 1f;

        [Header("Restored hook")]
        [Tooltip("Sent Restored Event once the saved settings are back (or there were none).")]
        public UdonBehaviour[] restoredListeners;
        [SerializeField] private string restoredEvent = "_OnMenuSettingsRestored";

        [Header("State (read-only)")]
        [Tooltip("The local player's saved data has arrived and been applied. Nothing is written before this.")]
        public bool restored;

        // Per toggle: 0 not known yet, 1 "state" + ToggleProcedure, 2 Toggle Plus Alt ("isFlipped" + Interact), -1 skipped.
        private int[] _kind;
        private bool[] _known;
        private bool[] _dirty;      // changed before the saved data arrived: that change wins
        private VRCPlayerApi _local;
        private bool _restoreQueued;

        void Start()
        {
            int n = toggles != null ? toggles.Length : 0;
            _kind = new int[n];
            _known = new bool[n];
            _dirty = new bool[n];
            SendCustomEventDelayedSeconds(nameof(_Check), checkInterval);
        }

        // ---- public ----

        /// <summary>Index of a toggle in the list, or -1.</summary>
        public int IndexOf(UdonBehaviour toggle)
        {
            if (toggles == null || toggle == null) return -1;
            for (int i = 0; i < toggles.Length; i++)
                if (toggles[i] == toggle) return i;
            return -1;
        }

        /// <summary>The toggle's saved value: Toggle Plus "state", Toggle Plus Alt "isFlipped".</summary>
        public bool GetState(int i)
        {
            if (!Known(i)) return false;
            return ReadState(i);
        }

        /// <summary>Set a toggle and save it as the player's choice.</summary>
        public void SetState(int i, bool value)
        {
            if (!Known(i)) return;
            if (ReadState(i) != value) Flip(i);
            _known[i] = ReadState(i);
            if (restored) Save(i);
            else _dirty[i] = true;
        }

        /// <summary>The player has a saved value for this toggle (on this platform when Per Platform is on).</summary>
        public bool HasSave(int i)
        {
            if (!restored || !Known(i)) return false;
            return PlayerData.HasKey(_local, Key(i));
        }

        // ---- events ----

        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) || !player.isLocal || _restoreQueued) return;
            _restoreQueued = true;
            _local = player;
            // A couple of frames so every toggle has run its Start before it gets flipped.
            SendCustomEventDelayedFrames(nameof(_Restore), 2);
        }

        public void _Restore()
        {
            for (int i = 0; i < _kind.Length; i++)
            {
                if (!Known(i)) continue;
                if (_dirty[i])
                {
                    _dirty[i] = false;
                    Save(i);
                    continue;
                }
                bool saved;
                if (PlayerData.TryGetBool(_local, Key(i), out saved) && ReadState(i) != saved) Flip(i);
                _known[i] = ReadState(i);
            }
            restored = true;
            if (restoredListeners == null) return;
            foreach (UdonBehaviour b in restoredListeners)
                if (b != null) b.SendCustomEvent(restoredEvent);
        }

        // Catches every change, whichever button or script made it.
        public void _Check()
        {
            SendCustomEventDelayedSeconds(nameof(_Check), checkInterval);
            for (int i = 0; i < _kind.Length; i++)
            {
                if (!Known(i)) continue;
                bool now = ReadState(i);
                if (now == _known[i]) continue;
                _known[i] = now;
                if (restored) Save(i);
                else _dirty[i] = true;
            }
        }

        // ---- toggles ----

        // Works out what kind of toggle it is the first time it is needed: its program may not be loaded at Start.
        private bool Known(int i)
        {
            if (_kind == null || i < 0 || i >= _kind.Length) return false;
            if (_kind[i] != 0) return _kind[i] > 0;
            UdonBehaviour t = toggles[i];
            if (t == null || keys == null || i >= keys.Length || string.IsNullOrEmpty(keys[i]))
            {
                _kind[i] = -1;
                return false;
            }
            // Every UdonSharp program has its type name; null means it isn't loaded yet, so try again next time.
            // (Asking for a symbol a program doesn't have logs an error in the editor, so look before asking.)
            object typeName = t.GetProgramVariable("__refl_typename");
            if (typeName == null) return false;
            string type = (string)typeName;
            if (type.Contains("TogglePlus") && (bool)t.GetProgramVariable("isSynced"))
            {
                Debug.LogWarning("[MenuSettingsSaver] " + t.name + " is synced, so it isn't saved as a setting.", t);
                _kind[i] = -1;
                return false;
            }
            if (type.EndsWith("TogglePlusAlt")) _kind[i] = 2;
            else if (t.GetProgramVariableType("state") != null) _kind[i] = 1;
            else
            {
                Debug.LogWarning("[MenuSettingsSaver] " + t.name + " has no public bool state, so it isn't saved.", t);
                _kind[i] = -1;
                return false;
            }
            _known[i] = ReadState(i);
            return true;
        }

        private bool ReadState(int i)
        {
            object v = toggles[i].GetProgramVariable(_kind[i] == 2 ? "isFlipped" : "state");
            return v != null && (bool)v;
        }

        private void Flip(int i)
        {
            toggles[i].SendCustomEvent(_kind[i] == 2 ? "_interact" : "ToggleProcedure");
        }

        private void Save(int i)
        {
            PlayerData.SetBool(Key(i), ReadState(i));
        }

        private string Key(int i)
        {
            string key = keyPrefix + keys[i];
            if (!perPlatform) return key;
#if UNITY_ANDROID || UNITY_IOS
            return key + ".mobile";
#else
            return key + ".pc";
#endif
        }
    }
}
