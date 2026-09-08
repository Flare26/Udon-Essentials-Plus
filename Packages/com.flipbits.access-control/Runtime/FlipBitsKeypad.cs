using TMPro;
using UdonSharp;
using UnityEngine;

namespace FlipBits.AccessControl
{
    /// <summary>
    /// FlipBits first-party keypad. Collects a digit code and hands it to an
    /// AccessGate via TryKeypadCode(), which owns all code->entry routing (one
    /// list of truth) and reports back whether the code matched, so the display
    /// can show accurate GRANTED / DENIED feedback.
    ///
    /// UI buttons call the public events (Digit0-Digit9, Clear, Backspace,
    /// Submit) on the backing UdonBehaviour; the editor builder wires a
    /// ready-made canvas for you.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("FlipBits/Keypad")]
    public class FlipBitsKeypad : UdonSharpBehaviour
    {
        [Header("Wiring")]
        [Tooltip("The access gate that owns the code list and routing.")]
        public AccessGate gate;

        [Tooltip("Display for the entered code and feedback text.")]
        public TextMeshProUGUI display;

        [Header("Behaviour")]
        [Tooltip("Maximum digits accepted; extra presses are ignored.")]
        public int maxLength = 8;

        [Tooltip("Show * instead of the entered digits.")]
        public bool maskInput = true;

        [Tooltip("How long GRANTED / DENIED stays on the display.")]
        public float feedbackSeconds = 2f;

        [Header("Display Text")]
        public string idleText = "ENTER CODE";
        public string grantedText = "GRANTED";
        public string deniedText = "DENIED";

        private string entry = "";

        private void Start()
        {
            ShowIdle();
        }

        public void Digit0() { Append("0"); }
        public void Digit1() { Append("1"); }
        public void Digit2() { Append("2"); }
        public void Digit3() { Append("3"); }
        public void Digit4() { Append("4"); }
        public void Digit5() { Append("5"); }
        public void Digit6() { Append("6"); }
        public void Digit7() { Append("7"); }
        public void Digit8() { Append("8"); }
        public void Digit9() { Append("9"); }

        public void Backspace()
        {
            if (entry.Length == 0) return;
            entry = entry.Substring(0, entry.Length - 1);
            ShowEntry();
        }

        public void Clear()
        {
            entry = "";
            ShowIdle();
        }

        public void Submit()
        {
            if (entry.Length == 0) return;

            if (gate == null)
            {
                Debug.LogError("[FlipBits.Keypad] No access gate assigned - nothing to submit codes to.");
                return;
            }

            var granted = gate.TryKeypadCode(entry);
            entry = "";

            if (display != null) display.text = granted ? grantedText : deniedText;
            SendCustomEventDelayedSeconds(nameof(ShowIdle), feedbackSeconds);
        }

        /// <summary>Returns the display to the idle prompt unless a new entry is in progress.</summary>
        public void ShowIdle()
        {
            if (entry.Length == 0 && display != null) display.text = idleText;
        }

        private void Append(string digit)
        {
            if (entry.Length >= maxLength) return;
            entry += digit;
            ShowEntry();
        }

        private void ShowEntry()
        {
            if (display == null) return;

            if (entry.Length == 0)
            {
                display.text = idleText;
                return;
            }

            if (!maskInput)
            {
                display.text = entry;
                return;
            }

            var masked = "";
            for (var i = 0; i < entry.Length; i++) masked += "*";
            display.text = masked;
        }
    }
}
