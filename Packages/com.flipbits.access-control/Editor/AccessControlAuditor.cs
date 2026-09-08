using System;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace FlipBits.AccessControl.Editors
{
    /// <summary>
    /// Overview window for the FlipBits access-control system. Lists every
    /// AccessGate, ObjectUnlocker and keypad in the open scenes, flags
    /// configuration problems, and shows which unlockers are actually reachable
    /// from a gate entry or a linked unlocker.
    ///
    /// Identity-provider packages (VRCLinking, ...) add their own sections via
    /// <see cref="ExtraSections"/> without this package depending on them.
    /// </summary>
    public class AccessControlAuditor : EditorWindow
    {
        /// <summary>Drawn under "Identity Providers". Register from an [InitializeOnLoad] static constructor.</summary>
        public static readonly List<Action> ExtraSections = new List<Action>();

        private Vector2 _scroll;

        [MenuItem("Tools/FlipBits/Access Control Auditor")]
        private static void Open()
        {
            var window = GetWindow<AccessControlAuditor>("Access Control");
            window.minSize = new Vector2(380, 300);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            var gates = FindObjectsOfType<AccessGate>(true);
            var unlockers = FindObjectsOfType<ObjectUnlocker>(true);
            var keypads = FindObjectsOfType<FlipBitsKeypad>(true);

            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Add Access Gate", "Creates an AccessGate GameObject (under the current selection, if any).")))
            {
                CreateGateObject(Selection.activeGameObject != null ? Selection.activeGameObject.transform : null, true);
            }
            if (GUILayout.Button(new GUIContent("Add Object Unlocker", "Creates an ObjectUnlocker GameObject (under the current selection, if any).")))
            {
                CreateUnlockerObject(Selection.activeGameObject != null ? Selection.activeGameObject.transform : null, true);
            }
            if (GUILayout.Button(new GUIContent("Add Keypad", "Creates a FlipBitsKeypad with a ready-made button canvas, wired to the scene's access gate.")))
            {
                FlipBitsKeypadEditor.CreateKeypadObject();
            }
            EditorGUILayout.EndHorizontal();

            if (ExtraSections.Count > 0)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Identity Providers", EditorStyles.boldLabel);
                foreach (var section in ExtraSections) section();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"Access Gates ({gates.Length})", EditorStyles.boldLabel);
            foreach (var gate in gates) DrawRow(gate.name, gate.gameObject, GetGateIssues(gate));
            if (gates.Length == 0) EditorGUILayout.LabelField("None in open scenes.", EditorStyles.miniLabel);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"Object Unlockers ({unlockers.Length})", EditorStyles.boldLabel);
            var reachable = GetReachableUnlockers(gates);
            foreach (var unlocker in unlockers)
            {
                var issues = GetUnlockerIssues(unlocker);
                if (!reachable.Contains(unlocker))
                {
                    issues.Add("Not targeted by any gate entry or linked unlocker — nothing ever unlocks it.");
                }
                DrawRow(unlocker.name, unlocker.gameObject, issues);
            }
            if (unlockers.Length == 0) EditorGUILayout.LabelField("None in open scenes.", EditorStyles.miniLabel);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"Keypads ({keypads.Length})", EditorStyles.boldLabel);
            foreach (var keypad in keypads)
            {
                var issues = new List<string>();
                if (keypad.gate == null) issues.Add("No access gate assigned — submitted codes go nowhere.");
                if (keypad.display == null) issues.Add("No display assigned — no visual feedback.");
                DrawRow(keypad.name, keypad.gameObject, issues);
            }
            if (keypads.Length == 0) EditorGUILayout.LabelField("None in open scenes.", EditorStyles.miniLabel);

            EditorGUILayout.EndScrollView();
        }

        /// <summary>Creates an AccessGate GameObject, optionally parented.</summary>
        public static AccessGate CreateGateObject(Transform parent, bool select)
        {
            var go = new GameObject("AccessGate");
            Undo.RegisterCreatedObjectUndo(go, "Create Access Gate");
            if (parent != null) Undo.SetTransformParent(go.transform, parent, "Create Access Gate");
            var gate = UdonSharpUndo.AddComponent<AccessGate>(go);
            if (select) Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            return gate;
        }

        /// <summary>Creates an ObjectUnlocker GameObject, optionally parented.</summary>
        public static ObjectUnlocker CreateUnlockerObject(Transform parent, bool select)
        {
            var go = new GameObject("ObjectUnlocker");
            Undo.RegisterCreatedObjectUndo(go, "Create Object Unlocker");
            if (parent != null) Undo.SetTransformParent(go.transform, parent, "Create Object Unlocker");
            var unlocker = UdonSharpUndo.AddComponent<ObjectUnlocker>(go);
            if (select) Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            return unlocker;
        }

        /// <summary>One boxed row with a status glyph, a Select button and any issues. Shared with provider sections.</summary>
        public static void DrawRow(string label, GameObject go, List<string> issues)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            var hasIssues = issues != null && issues.Count > 0;
            EditorGUILayout.LabelField(hasIssues ? $"⚠ {label}" : $"✓ {label}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
            }
            EditorGUILayout.EndHorizontal();

            if (hasIssues)
            {
                foreach (var issue in issues) EditorGUILayout.HelpBox(issue, MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
        }

        public static List<string> GetGateIssues(AccessGate gate)
        {
            var issues = new List<string>();

            var count = gate.targetBehaviours != null ? gate.targetBehaviours.Length : 0;
            if (count == 0)
            {
                issues.Add("No entries configured.");
            }
            else if (gate.targetEvents == null || gate.targetEvents.Length != count)
            {
                issues.Add("Entry arrays are out of sync — open the component and re-save it.");
                return issues;
            }

            for (var i = 0; i < count; i++)
            {
                if (gate.targetBehaviours[i] == null) issues.Add($"Entry {i}: no target behaviour.");
                if (string.IsNullOrEmpty(gate.targetEvents[i])) issues.Add($"Entry {i}: no target event.");
            }

            var codeCount = gate.keypadCodes != null ? gate.keypadCodes.Length : 0;
            if (gate.keypadEntryIndices == null || gate.keypadEntryIndices.Length != codeCount)
            {
                issues.Add("Keypad arrays are out of sync — open the component and re-save it.");
                return issues;
            }

            for (var i = 0; i < codeCount; i++)
            {
                if (string.IsNullOrEmpty(gate.keypadCodes[i])) issues.Add($"Keypad code {i} is empty.");
                var idx = gate.keypadEntryIndices[i];
                if (idx < 0 || idx >= count) issues.Add($"Keypad code {i} points at entry {idx}, which does not exist.");
            }

            return issues;
        }

        private static List<string> GetUnlockerIssues(ObjectUnlocker unlocker)
        {
            var issues = new List<string>();
            var so = new SerializedObject(unlocker);

            CheckPair(so, issues, "colliders", "colliderStartStates", "collider");
            CheckPair(so, issues, "gameObjects", "gameObjectStartStates", "GameObject");
            CheckPair(so, issues, "udonBehaviours", "udonBehaviourEvents", "unlock event");

            var hasAnything = so.FindProperty("colliders").arraySize > 0
                              || so.FindProperty("gameObjects").arraySize > 0
                              || so.FindProperty("udonBehaviours").arraySize > 0
                              || so.FindProperty("buttonsToEnable").arraySize > 0
                              || so.FindProperty("linkedUnlockers").arraySize > 0;
            if (!hasAnything) issues.Add("Controls nothing — no colliders, GameObjects, events, buttons, or links assigned.");

            return issues;
        }

        private static void CheckPair(SerializedObject so, List<string> issues, string arrayName, string pairName, string label)
        {
            var array = so.FindProperty(arrayName);
            var pair = so.FindProperty(pairName);

            if (array.arraySize != pair.arraySize)
            {
                issues.Add($"{arrayName} and {pairName} lengths differ — open the component and re-save it.");
                return;
            }

            for (var i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == null) issues.Add($"Empty {label} slot at index {i}.");
            }
        }

        private static HashSet<ObjectUnlocker> GetReachableUnlockers(AccessGate[] gates)
        {
            var reachable = new HashSet<ObjectUnlocker>();

            foreach (var gate in gates)
            {
                if (gate.targetBehaviours == null) continue;
                foreach (var behaviour in gate.targetBehaviours)
                {
                    if (behaviour is ObjectUnlocker unlocker) MarkReachable(unlocker, reachable);
                }
            }

            return reachable;

            void MarkReachable(ObjectUnlocker unlocker, HashSet<ObjectUnlocker> set)
            {
                if (unlocker == null || !set.Add(unlocker)) return;
                var linked = new SerializedObject(unlocker).FindProperty("linkedUnlockers");
                for (var i = 0; i < linked.arraySize; i++)
                {
                    MarkReachable(linked.GetArrayElementAtIndex(i).objectReferenceValue as ObjectUnlocker, set);
                }
            }
        }
    }
}
