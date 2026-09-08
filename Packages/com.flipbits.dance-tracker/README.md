# Dance Tracker

Dance counters for club worlds. Every player gets a little number floating over their head. Staff can see it, managers can click it to bump it. Counts save between visits and reset themselves if they're older than a day, so you start each event clean.

## Setup

Drop the **Dance Count System** prefab in your scene. That's it for the basics.

- `DanceTrackerManager` has all the settings (bounds, colours, reset hours). The trackers find it by GameObject name, so if you rename it, update `managerObjectName` on the tracker too.
- `IndividualTracker` is the PlayerObject. VRChat spawns one per player. It already has VRCPlayerObject and VRCEnablePersistence on it, leave those alone.

Needs Worlds SDK 3.7.4 or newer (persistence) and TMP essentials.

## How counting works

Goes from `lowBound` up to `highBound`, then `F` (floater, optional), then `N/D`, then wraps back. Colour ramps low -> median -> high.

## Permissions

Nothing here is synced. Each client decides what it shows itself, and only the owner of a tracker ever writes to it.

Two flags, granted by whatever you already use to identify staff:

- `GrantStaff` / `RevokeStaff`: can see counters
- `GrantManager` / `RevokeManager`: can see and click

`RevokeAll` clears both. Manager counts as staff.

If you have a whitelist (VRCLinking, keypad, whatever), fire `GrantStaff` or `GrantManager` on the manager when someone verifies, then hook your UI buttons to `ToggleStaffView` and `ToggleManagerInteract`. Those do nothing for people without the flag.

If you don't, point a hidden interact cube at `EnableStaffView` or `EnableManagerInteract`. Those grant and unlock in one click, so hide the cube somewhere only staff can reach.

There are also `TurnOn…` / `TurnOff…` / `Set…(bool)` versions of every toggle for button bindings.

## Testing on your own

You can't see your own tracker normally. Tick `showSelfTracker` on the manager (or call `ToggleSelfView` once you have staff) to see and click it in Build & Test. Untick before you publish, it logs a warning while it's on.
