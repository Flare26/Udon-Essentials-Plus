# Access Control

Staff-only doors, rooms and controls for club worlds, without tying you to one way of proving who someone is.

## Pieces

- **AccessGate** — the hub. A table of entries (label + target behaviour + event) plus a list of keypad codes that map to entries. Anything that identifies a player fires an entry: the keypad, a VRCLinking whitelist (see `com.flipbits.vrclinking-access`), a hidden interact, or your own script calling `Fire(index)` / `FireByLabel("Staff")`.
- **ObjectUnlocker** — the usual target. Holds colliders and GameObjects in a locked state until `Unlock` arrives, enables UI buttons, fires events on other behaviours, cascades to linked unlockers. `Relock` reverses it.
- **FlipBitsKeypad** — world-space keypad canvas (built for you by the inspector) that submits codes to a gate and shows GRANTED / DENIED.

Anything with a public event can be a target, e.g. `DanceTrackerManager.GrantStaff` from the Dance Tracker package.

## Setup

1. `Tools > FlipBits > Access Control Auditor` → **Add Access Gate**.
2. On the gate, **Add Entry**; use **Create Object Unlocker For This Entry** or point it at any behaviour + event.
3. Fill the unlocker: colliders / GameObjects with their *locked* state (the state flips on unlock).
4. **Add Keypad Code** on the gate, then **Add Keypad** from the auditor — it builds the canvas and points itself at the gate.

The auditor lists every gate, unlocker and keypad in the open scenes and flags anything unwired.

Needs Worlds SDK 3.7.4 or newer and TMP essentials.
