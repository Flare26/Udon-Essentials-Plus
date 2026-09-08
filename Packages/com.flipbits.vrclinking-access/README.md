# VRCLinking Access

Plugs VRCLinking into Access Control. When the local player holds a whitelisted Discord role, the whitelist fires the matching entry on your AccessGate.

## Setup

1. Install Access Control and set up a gate (see that package's README).
2. `Tools > FlipBits > Access Control Auditor` → under **Identity Providers**, **Set Up VRCLinking Object** (creates the downloader; set its World ID) and **Add VRCLinking Whitelist**.
3. On the whitelist, **Add Role**: role ID or name, an optional alias for logs, and which gate entry it fires.

The VRCLinking SDK wires the downloader and module list at build time — no prefab needed.
