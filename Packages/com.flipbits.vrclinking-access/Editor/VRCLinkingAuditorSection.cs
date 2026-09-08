using System.Collections.Generic;
using FlipBits.AccessControl.Editors;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRCLinking;

namespace FlipBits.AccessControl.VRCLinking.Editors
{
    /// <summary>
    /// Adds the VRCLinking rows (downloader status, whitelists, "Add Whitelist")
    /// to the Access Control Auditor. Registered at load, so the base package
    /// never references this one.
    /// </summary>
    [InitializeOnLoad]
    public static class VRCLinkingAuditorSection
    {
        static VRCLinkingAuditorSection()
        {
            AccessControlAuditor.ExtraSections.Add(Draw);
        }

        private static void Draw()
        {
            var downloader = Object.FindObjectOfType<VrcLinkingDownloader>(true);
            var whitelists = Object.FindObjectsOfType<VRCLinkingWhitelist>(true);

            if (downloader == null)
            {
                EditorGUILayout.HelpBox("No VrcLinkingDownloader in the open scenes — no whitelist will ever fire.", MessageType.Error);
                if (GUILayout.Button("Set Up VRCLinking Object")) CreateDownloaderObject();
            }
            else
            {
                AccessControlAuditor.DrawRow("VRCLinking Downloader", downloader.gameObject, null);
            }

            if (GUILayout.Button(new GUIContent("Add VRCLinking Whitelist",
                "Creates a VRCLinkingWhitelist as a child of the VRCLinking object (creating that too if missing) and points it at the scene's access gate.")))
            {
                CreateWhitelistObject();
            }

            foreach (var whitelist in whitelists)
            {
                AccessControlAuditor.DrawRow(whitelist.name, whitelist.gameObject, GetWhitelistIssues(whitelist));
            }
        }

        /// <summary>
        /// Creates a "VRCLinking" GameObject with a VrcLinkingDownloader on it —
        /// the downloader is a plain U# behaviour, no prefab required. The SDK's
        /// build hook wires up the compressor, URLs, and module list automatically.
        /// </summary>
        public static VrcLinkingDownloader CreateDownloaderObject()
        {
            var go = new GameObject("VRCLinking");
            Undo.RegisterCreatedObjectUndo(go, "Create VRCLinking Downloader");
            var downloader = UdonSharpUndo.AddComponent<VrcLinkingDownloader>(go);
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            Debug.Log("[FlipBits] Created 'VRCLinking' object — set the World ID on the VrcLinkingDownloader before building.");
            return downloader;
        }

        /// <summary>Creates a VRCLinkingWhitelist under the VRCLinking object, creating the downloader first if the scene has none.</summary>
        public static VRCLinkingWhitelist CreateWhitelistObject()
        {
            var downloader = Object.FindObjectOfType<VrcLinkingDownloader>(true);
            if (downloader == null) downloader = CreateDownloaderObject();

            var go = new GameObject("VRCLinkingWhitelist");
            Undo.RegisterCreatedObjectUndo(go, "Create VRCLinking Whitelist");
            Undo.SetTransformParent(go.transform, downloader.transform, "Create VRCLinking Whitelist");
            var whitelist = UdonSharpUndo.AddComponent<VRCLinkingWhitelist>(go);

            var gate = Object.FindObjectOfType<AccessGate>(true);
            if (gate != null)
            {
                var so = new SerializedObject(whitelist);
                so.FindProperty("gate").objectReferenceValue = gate;
                so.ApplyModifiedProperties();
            }

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            return whitelist;
        }

        private static List<string> GetWhitelistIssues(VRCLinkingWhitelist whitelist)
        {
            var issues = new List<string>();

            if (whitelist.gate == null) issues.Add("No access gate assigned — roles have nothing to fire.");

            var count = whitelist.roleValues != null ? whitelist.roleValues.Length : 0;
            if (count == 0)
            {
                issues.Add("No roles configured.");
                return issues;
            }
            if (whitelist.roleTypes.Length != count || whitelist.gateEntryIndices.Length != count)
            {
                issues.Add("Role entry arrays are out of sync — open the component and re-save it.");
                return issues;
            }

            var entryCount = whitelist.gate != null && whitelist.gate.targetBehaviours != null ? whitelist.gate.targetBehaviours.Length : 0;
            for (var i = 0; i < count; i++)
            {
                if (string.IsNullOrEmpty(whitelist.roleValues[i])) issues.Add($"Role {i}: role ID / name is empty.");
                var idx = whitelist.gateEntryIndices[i];
                if (whitelist.gate != null && (idx < 0 || idx >= entryCount)) issues.Add($"Role {i}: points at gate entry {idx}, which does not exist.");
            }

            return issues;
        }
    }
}
