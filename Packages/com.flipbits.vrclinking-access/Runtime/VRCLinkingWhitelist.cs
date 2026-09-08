using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRCLinking.Modules;
using VRCLinking.Modules.SupporterBoard;

namespace FlipBits.AccessControl.VRCLinking
{
    /// <summary>
    /// VRCLinking identity provider for the FlipBits access-control system.
    /// When the local player holds a whitelisted Discord role (resolved through
    /// VRCLinking), fires the matching entry on an AccessGate. The gate owns the
    /// targets and the keypad codes, so a world without VRCLinking uses the same
    /// gate with just a keypad.
    ///
    /// The four role arrays are parallel (index i describes one role). Use the
    /// custom inspector, which keeps them in sync automatically.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("FlipBits/VRCLinking Whitelist")]
    public class VRCLinkingWhitelist : VrcLinkingModuleBase
    {
        public override string ModuleName => "FlipBits.VRCLinkingWhitelist";

        [Header("Gate")]
        [Tooltip("The access gate whose entries this whitelist fires.")]
        public AccessGate gate;

        [Header("Role Entries")]
        [Tooltip("How each role looks up its members: by ID, name, or link.")]
        public RoleType[] roleTypes = new RoleType[0];

        [Tooltip("The role ID or name to match against.")]
        public string[] roleValues = new string[0];

        [Tooltip("Optional display name for each role - used in inspector labels and logs only, never for matching.")]
        public string[] roleAliases = new string[0];

        [Tooltip("For each role, the index of the gate entry that fires when the local player holds it.")]
        public int[] gateEntryIndices = new int[0];

        [Header("Debugging")]
        [Tooltip("Log every lookup and comparison. Grants and configuration errors are always logged.")]
        public bool verboseLogging;

        public override void OnDataLoaded()
        {
            var localPlayer = Networking.LocalPlayer;
            if (localPlayer == null) return;

            if (downloader == null)
            {
                LogError("Downloader is not assigned. Is the VRCLinking object in the scene?");
                return;
            }

            if (gate == null)
            {
                LogError("No access gate assigned - roles have nothing to fire.");
                return;
            }

            if (roleValues == null || roleValues.Length == 0)
            {
                LogVerbose("No whitelist roles configured.");
                return;
            }

            if (roleTypes.Length != roleValues.Length || gateEntryIndices.Length != roleValues.Length)
            {
                LogError("Role entry arrays are out of sync (types=" + roleTypes.Length + ", values=" + roleValues.Length + ", entries=" + gateEntryIndices.Length + "). Re-save the component with the FlipBits inspector.");
                return;
            }

            var displayName = localPlayer.displayName;
            LogVerbose("Checking whitelist for player '" + displayName + "'...");

            for (var i = 0; i < roleValues.Length; i++)
            {
                var roleValue = roleValues[i];
                if (string.IsNullOrEmpty(roleValue))
                {
                    LogError("Role " + i + " has no role ID / name. Skipping.");
                    continue;
                }

                DataList members = null;
                var found = false;

                if (roleTypes[i] == RoleType.RoleId)
                {
                    found = downloader.TryGetGuildMembersByRoleId(roleValue, out members);
                }
                else // RoleName and RoleLink both resolve by name
                {
                    found = downloader.TryGetGuildMembersByRoleName(roleValue, out members);
                }

                if (!found)
                {
                    LogVerbose("No members found for role '" + RoleLabel(i) + "'.");
                    continue;
                }

                for (var m = 0; m < members.Count; m++)
                {
                    if (members[m].String != displayName) continue;

                    Log("Player '" + displayName + "' matched role '" + RoleLabel(i) + "'. Firing gate entry " + gateEntryIndices[i] + ".");
                    gate.Fire(gateEntryIndices[i]);
                    break;
                }
            }
        }

        /// <summary>"Alias (roleValue)" when an alias is set, otherwise just the role value.</summary>
        private string RoleLabel(int index)
        {
            var value = roleValues[index];
            if (roleAliases == null || index >= roleAliases.Length) return value;
            var alias = roleAliases[index];
            return string.IsNullOrEmpty(alias) ? value : alias + " (" + value + ")";
        }

        private void Log(string message)
        {
            Debug.Log("[" + ModuleName + "] " + message);
        }

        private void LogVerbose(string message)
        {
            if (verboseLogging) Debug.Log("[" + ModuleName + "] " + message);
        }

        private void LogError(string message)
        {
            Debug.LogError("[" + ModuleName + "] " + message);
        }
    }
}
