using UdonSharp;
using UnityEngine;

namespace FlipBits.AccessControl
{
    /// <summary>
    /// The hub of the FlipBits access-control system. Holds a table of entries
    /// (label + target behaviour + custom event) and fires one on request.
    ///
    /// Anything that can prove who a player is drives a gate: the FlipBits
    /// keypad (via TryKeypadCode), a VRCLinking whitelist module (via Fire),
    /// or any other behaviour that calls Fire / FireByLabel. The gate never
    /// needs to know which of those it was.
    ///
    /// Targets are usually ObjectUnlockers, but any UdonSharpBehaviour with a
    /// public event works (for example DanceTrackerManager.GrantStaff).
    ///
    /// The three entry arrays are parallel (index i is one entry); so are the
    /// two keypad arrays. Use the custom inspector, which keeps them in sync.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("FlipBits/Access Gate")]
    public class AccessGate : UdonSharpBehaviour
    {
        [Header("Entries")]
        [Tooltip("Display-only name for each entry, used in inspector labels and logs.")]
        public string[] entryLabels = new string[0];

        [Tooltip("The behaviour that receives the event when this entry fires.")]
        public UdonSharpBehaviour[] targetBehaviours = new UdonSharpBehaviour[0];

        [Tooltip("The custom event name sent via SendCustomEvent, e.g. Unlock.")]
        public string[] targetEvents = new string[0];

        [Header("Keypad Codes")]
        [Tooltip("Passcodes accepted by keypads pointed at this gate.")]
        public string[] keypadCodes = new string[0];

        [Tooltip("For each passcode, the index of the entry that fires.")]
        public int[] keypadEntryIndices = new int[0];

        [Header("Debugging")]
        [Tooltip("Log every lookup. Fires and configuration errors are always logged.")]
        public bool verboseLogging;

        /// <summary>Number of entries in the table.</summary>
        public int GetEntryCount()
        {
            return targetBehaviours == null ? 0 : targetBehaviours.Length;
        }

        /// <summary>The entry's label, or "Entry N" when it has none.</summary>
        public string GetEntryLabel(int index)
        {
            if (entryLabels == null || index < 0 || index >= entryLabels.Length) return "Entry " + index;
            var label = entryLabels[index];
            return string.IsNullOrEmpty(label) ? "Entry " + index : label;
        }

        /// <summary>Fires entry <paramref name="index"/>. Returns true if the event was sent.</summary>
        public bool Fire(int index)
        {
            if (targetBehaviours == null || targetEvents == null || targetBehaviours.Length != targetEvents.Length)
            {
                LogError("Entry arrays are out of sync. Re-save the component with the FlipBits inspector.");
                return false;
            }

            if (index < 0 || index >= targetBehaviours.Length)
            {
                LogError("Entry " + index + " does not exist.");
                return false;
            }

            if (!ValidateEntry(index)) return false;

            Log("Firing '" + GetEntryLabel(index) + "' -> " + targetBehaviours[index].name + "." + targetEvents[index]);
            targetBehaviours[index].SendCustomEvent(targetEvents[index]);
            return true;
        }

        /// <summary>Fires the first entry whose label matches. Returns true if one fired.</summary>
        public bool FireByLabel(string label)
        {
            if (entryLabels == null || string.IsNullOrEmpty(label)) return false;
            for (var i = 0; i < entryLabels.Length; i++)
            {
                if (entryLabels[i] == label) return Fire(i);
            }
            LogVerbose("No entry labelled '" + label + "'.");
            return false;
        }

        /// <summary>
        /// Keypad entry point: routes the code and reports whether it matched, so
        /// the keypad can show accurate GRANTED / DENIED feedback.
        /// </summary>
        public bool TryKeypadCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                LogVerbose("Empty keypad code.");
                return false;
            }

            if (keypadCodes == null || keypadEntryIndices == null || keypadCodes.Length != keypadEntryIndices.Length)
            {
                LogError("Keypad arrays are out of sync. Re-save the component with the FlipBits inspector.");
                return false;
            }

            for (var i = 0; i < keypadCodes.Length; i++)
            {
                if (code != keypadCodes[i]) continue;
                Log("Keypad code matched code " + i + ".");
                return Fire(keypadEntryIndices[i]);
            }

            LogVerbose("Keypad code did not match any entry.");
            return false;
        }

        private bool ValidateEntry(int index)
        {
            if (targetBehaviours[index] == null)
            {
                LogError("Entry " + index + " ('" + GetEntryLabel(index) + "') has no target behaviour.");
                return false;
            }

            if (string.IsNullOrEmpty(targetEvents[index]))
            {
                LogError("Entry " + index + " ('" + GetEntryLabel(index) + "') has no target event.");
                return false;
            }

            return true;
        }

        private void Log(string message)
        {
            Debug.Log("[FlipBits.AccessGate] " + message);
        }

        private void LogVerbose(string message)
        {
            if (verboseLogging) Debug.Log("[FlipBits.AccessGate] " + message);
        }

        private void LogError(string message)
        {
            Debug.LogError("[FlipBits.AccessGate] " + message);
        }
    }
}
