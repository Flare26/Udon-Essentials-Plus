using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace FlipBits.AccessControl.Editors
{
    /// <summary>
    /// Row-based inspector for <see cref="AccessGate"/>: presents the parallel
    /// entry / keypad arrays as single rows, keeps their lengths in sync, and
    /// surfaces configuration problems inline.
    /// </summary>
    [CustomEditor(typeof(AccessGate))]
    public class AccessGateEditor : UnityEditor.Editor
    {
        private SerializedProperty _entryLabels;
        private SerializedProperty _targetBehaviours;
        private SerializedProperty _targetEvents;
        private SerializedProperty _keypadCodes;
        private SerializedProperty _keypadEntryIndices;
        private SerializedProperty _verboseLogging;

        private void OnEnable()
        {
            _entryLabels = serializedObject.FindProperty("entryLabels");
            _targetBehaviours = serializedObject.FindProperty("targetBehaviours");
            _targetEvents = serializedObject.FindProperty("targetEvents");
            _keypadCodes = serializedObject.FindProperty("keypadCodes");
            _keypadEntryIndices = serializedObject.FindProperty("keypadEntryIndices");
            _verboseLogging = serializedObject.FindProperty("verboseLogging");
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            SyncLengths(_entryLabels, _targetBehaviours, _targetEvents);
            SyncLengths(_keypadCodes, _keypadEntryIndices);

            DrawEntries();
            EditorGUILayout.Space(10);
            DrawKeypadCodes();
            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(_verboseLogging);
            DrawValidation();

            serializedObject.ApplyModifiedProperties();
        }

        // -------------------------------------------------------------- entries

        private void DrawEntries()
        {
            EditorGUILayout.LabelField("Entries", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Identity providers (keypad, VRCLinking, ...) fire these by index or label.", EditorStyles.miniLabel);

            var arrays = new[] { _entryLabels, _targetBehaviours, _targetEvents };
            int removeAt = -1, moveFrom = -1, moveTo = -1;

            for (var i = 0; i < _targetBehaviours.arraySize; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                var label = _entryLabels.GetArrayElementAtIndex(i).stringValue;
                EditorGUILayout.LabelField(string.IsNullOrEmpty(label) ? $"Entry {i}" : $"Entry {i} — {label}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(i == 0))
                {
                    if (GUILayout.Button("▲", GUILayout.Width(24))) { moveFrom = i; moveTo = i - 1; }
                }
                using (new EditorGUI.DisabledScope(i == _targetBehaviours.arraySize - 1))
                {
                    if (GUILayout.Button("▼", GUILayout.Width(24))) { moveFrom = i; moveTo = i + 1; }
                }
                if (GUILayout.Button("✕", GUILayout.Width(24))) removeAt = i;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(_entryLabels.GetArrayElementAtIndex(i), new GUIContent("Label", "Display-only name, shown in labels and logs."));
                var behaviourProp = _targetBehaviours.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(behaviourProp, new GUIContent("Target Behaviour"));
                EditorGUILayout.PropertyField(_targetEvents.GetArrayElementAtIndex(i), new GUIContent("Target Event", "Custom event sent via SendCustomEvent, e.g. Unlock"));

                if (behaviourProp.objectReferenceValue == null
                    && GUILayout.Button(new GUIContent("Create Object Unlocker For This Entry",
                        "Creates an ObjectUnlocker in the scene, assigns it as this entry's target, and sets the event to Unlock.")))
                {
                    var unlocker = AccessControlAuditor.CreateUnlockerObject(null, false);
                    unlocker.name = string.IsNullOrEmpty(label) ? $"Unlocker_Entry{i}" : $"Unlocker_{label}";
                    behaviourProp.objectReferenceValue = unlocker;
                    var evtProp = _targetEvents.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(evtProp.stringValue)) evtProp.stringValue = "Unlock";
                }

                EditorGUILayout.EndVertical();
            }

            if (removeAt >= 0) RemoveElement(removeAt, arrays);
            else if (moveFrom >= 0) MoveElement(moveFrom, moveTo, arrays);

            if (GUILayout.Button("Add Entry"))
            {
                AddElement(arrays);
                _entryLabels.GetArrayElementAtIndex(_entryLabels.arraySize - 1).stringValue = "";
                _targetEvents.GetArrayElementAtIndex(_targetEvents.arraySize - 1).stringValue = "Unlock";
            }
        }

        // --------------------------------------------------------------- keypad

        private void DrawKeypadCodes()
        {
            EditorGUILayout.LabelField("Keypad Codes", EditorStyles.boldLabel);

            if (_targetBehaviours.arraySize == 0 && _keypadCodes.arraySize > 0)
            {
                EditorGUILayout.HelpBox("Keypad codes need at least one entry to point at.", MessageType.Warning);
            }

            var labels = BuildEntryLabels(serializedObject);
            var keypadArrays = new[] { _keypadCodes, _keypadEntryIndices };
            var removeAt = -1;

            for (var i = 0; i < _keypadCodes.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var codeProp = _keypadCodes.GetArrayElementAtIndex(i);
                codeProp.stringValue = EditorGUILayout.TextField(codeProp.stringValue, GUILayout.MinWidth(80));

                var idxProp = _keypadEntryIndices.GetArrayElementAtIndex(i);
                if (labels.Length > 0)
                {
                    var clamped = Mathf.Clamp(idxProp.intValue, 0, labels.Length - 1);
                    idxProp.intValue = EditorGUILayout.Popup(clamped, labels);
                }
                else
                {
                    EditorGUILayout.LabelField("(no entries)", EditorStyles.miniLabel);
                }

                if (GUILayout.Button("✕", GUILayout.Width(24))) removeAt = i;
                EditorGUILayout.EndHorizontal();
            }

            if (removeAt >= 0) RemoveElement(removeAt, keypadArrays);

            using (new EditorGUI.DisabledScope(_targetBehaviours.arraySize == 0))
            {
                if (GUILayout.Button("Add Keypad Code"))
                {
                    AddElement(keypadArrays);
                    _keypadCodes.GetArrayElementAtIndex(_keypadCodes.arraySize - 1).stringValue = "";
                    _keypadEntryIndices.GetArrayElementAtIndex(_keypadEntryIndices.arraySize - 1).intValue = 0;
                }
            }
        }

        /// <summary>"i: label → target.event" for every entry of a gate; shared with the provider inspectors.</summary>
        public static string[] BuildEntryLabels(SerializedObject gate)
        {
            var labelsProp = gate.FindProperty("entryLabels");
            var behavioursProp = gate.FindProperty("targetBehaviours");
            var eventsProp = gate.FindProperty("targetEvents");
            var count = behavioursProp.arraySize;
            var labels = new string[count];
            for (var i = 0; i < count; i++)
            {
                var label = i < labelsProp.arraySize ? labelsProp.GetArrayElementAtIndex(i).stringValue : "";
                var behaviour = behavioursProp.GetArrayElementAtIndex(i).objectReferenceValue;
                var evt = i < eventsProp.arraySize ? eventsProp.GetArrayElementAtIndex(i).stringValue : "";
                labels[i] = $"{i}: {(string.IsNullOrEmpty(label) ? "(no label)" : label)} → " +
                            $"{(behaviour != null ? behaviour.name : "(none)")}.{(string.IsNullOrEmpty(evt) ? "(no event)" : evt)}";
            }
            return labels;
        }

        // ----------------------------------------------------------- validation

        private void DrawValidation()
        {
            for (var i = 0; i < _targetBehaviours.arraySize; i++)
            {
                if (_targetBehaviours.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    EditorGUILayout.HelpBox($"Entry {i}: no target behaviour assigned.", MessageType.Warning);
                if (string.IsNullOrEmpty(_targetEvents.GetArrayElementAtIndex(i).stringValue))
                    EditorGUILayout.HelpBox($"Entry {i}: no target event set.", MessageType.Warning);
            }

            for (var i = 0; i < _keypadCodes.arraySize; i++)
            {
                var codeValue = _keypadCodes.GetArrayElementAtIndex(i).stringValue;
                if (string.IsNullOrEmpty(codeValue))
                {
                    EditorGUILayout.HelpBox($"Keypad code {i} is empty.", MessageType.Warning);
                    continue;
                }
                for (var j = 0; j < i; j++)
                {
                    if (_keypadCodes.GetArrayElementAtIndex(j).stringValue == codeValue)
                    {
                        EditorGUILayout.HelpBox($"Keypad code {i} duplicates code {j} ('{codeValue}'); only the first match fires.", MessageType.Warning);
                        break;
                    }
                }
            }
        }

        // -------------------------------------------------- parallel array utils

        private static void SyncLengths(params SerializedProperty[] arrays)
        {
            var max = 0;
            foreach (var arr in arrays) max = Mathf.Max(max, arr.arraySize);
            foreach (var arr in arrays) arr.arraySize = max;
        }

        private static void AddElement(SerializedProperty[] arrays)
        {
            foreach (var arr in arrays) arr.arraySize++;
        }

        private static void RemoveElement(int index, SerializedProperty[] arrays)
        {
            foreach (var arr in arrays)
            {
                var element = arr.GetArrayElementAtIndex(index);
                if (element.propertyType == SerializedPropertyType.ObjectReference && element.objectReferenceValue != null)
                {
                    element.objectReferenceValue = null;
                }
                arr.DeleteArrayElementAtIndex(index);
            }
        }

        private static void MoveElement(int from, int to, SerializedProperty[] arrays)
        {
            foreach (var arr in arrays) arr.MoveArrayElement(from, to);
        }
    }
}
