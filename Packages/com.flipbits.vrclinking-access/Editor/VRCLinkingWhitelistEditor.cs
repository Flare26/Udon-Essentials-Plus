using FlipBits.AccessControl.Editors;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRCLinking;

namespace FlipBits.AccessControl.VRCLinking.Editors
{
    /// <summary>
    /// Row-based inspector for <see cref="VRCLinkingWhitelist"/>: one row per
    /// Discord role, each pointing at an entry of the assigned AccessGate.
    /// </summary>
    [CustomEditor(typeof(VRCLinkingWhitelist))]
    public class VRCLinkingWhitelistEditor : UnityEditor.Editor
    {
        private SerializedProperty _gate;
        private SerializedProperty _roleTypes;
        private SerializedProperty _roleValues;
        private SerializedProperty _roleAliases;
        private SerializedProperty _gateEntryIndices;
        private SerializedProperty _verboseLogging;

        private void OnEnable()
        {
            _gate = serializedObject.FindProperty("gate");
            _roleTypes = serializedObject.FindProperty("roleTypes");
            _roleValues = serializedObject.FindProperty("roleValues");
            _roleAliases = serializedObject.FindProperty("roleAliases");
            _gateEntryIndices = serializedObject.FindProperty("gateEntryIndices");
            _verboseLogging = serializedObject.FindProperty("verboseLogging");
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            SyncLengths(_roleTypes, _roleValues, _roleAliases, _gateEntryIndices);

            DrawGate();
            EditorGUILayout.Space(10);
            DrawRoleEntries();
            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(_verboseLogging);
            DrawValidation();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGate()
        {
            EditorGUILayout.LabelField("Gate", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_gate, new GUIContent("Access Gate", "Owns the targets and keypad codes; this whitelist only decides which entry a role fires."));

            if (_gate.objectReferenceValue != null) return;

            var existing = FindObjectOfType<AccessGate>(true);
            EditorGUILayout.HelpBox("No access gate assigned — roles have nothing to fire.", MessageType.Warning);
            EditorGUILayout.BeginHorizontal();
            if (existing != null && GUILayout.Button($"Use '{existing.name}'"))
            {
                _gate.objectReferenceValue = existing;
            }
            if (GUILayout.Button("Create Access Gate"))
            {
                var whitelist = (Component)target;
                _gate.objectReferenceValue = AccessControlAuditor.CreateGateObject(whitelist.transform, false);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRoleEntries()
        {
            EditorGUILayout.LabelField("Role Entries", EditorStyles.boldLabel);

            var gate = _gate.objectReferenceValue as AccessGate;
            var entryLabels = gate != null ? AccessGateEditor.BuildEntryLabels(new SerializedObject(gate)) : new string[0];

            var arrays = new[] { _roleTypes, _roleValues, _roleAliases, _gateEntryIndices };
            int removeAt = -1, moveFrom = -1, moveTo = -1;

            for (var i = 0; i < _roleValues.arraySize; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                var alias = _roleAliases.GetArrayElementAtIndex(i).stringValue;
                EditorGUILayout.LabelField(string.IsNullOrEmpty(alias) ? $"Role {i}" : $"Role {i} — {alias}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(i == 0))
                {
                    if (GUILayout.Button("▲", GUILayout.Width(24))) { moveFrom = i; moveTo = i - 1; }
                }
                using (new EditorGUI.DisabledScope(i == _roleValues.arraySize - 1))
                {
                    if (GUILayout.Button("▼", GUILayout.Width(24))) { moveFrom = i; moveTo = i + 1; }
                }
                if (GUILayout.Button("✕", GUILayout.Width(24))) removeAt = i;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(_roleTypes.GetArrayElementAtIndex(i), new GUIContent("Role Type"));
                EditorGUILayout.PropertyField(_roleValues.GetArrayElementAtIndex(i), new GUIContent("Role ID / Name"));
                EditorGUILayout.PropertyField(_roleAliases.GetArrayElementAtIndex(i), new GUIContent("Alias", "Display-only name for this role, shown in labels and logs — never used for matching."));

                var idxProp = _gateEntryIndices.GetArrayElementAtIndex(i);
                if (entryLabels.Length > 0)
                {
                    var clamped = Mathf.Clamp(idxProp.intValue, 0, entryLabels.Length - 1);
                    idxProp.intValue = EditorGUILayout.Popup("Gate Entry", clamped, entryLabels);
                }
                else
                {
                    EditorGUILayout.LabelField("Gate Entry", gate == null ? "(assign a gate first)" : "(gate has no entries)", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
            }

            if (removeAt >= 0) RemoveElement(removeAt, arrays);
            else if (moveFrom >= 0) MoveElement(moveFrom, moveTo, arrays);

            if (GUILayout.Button("Add Role"))
            {
                AddElement(arrays);
                _roleValues.GetArrayElementAtIndex(_roleValues.arraySize - 1).stringValue = "";
                _roleAliases.GetArrayElementAtIndex(_roleAliases.arraySize - 1).stringValue = "";
                _gateEntryIndices.GetArrayElementAtIndex(_gateEntryIndices.arraySize - 1).intValue = 0;
            }
        }

        private void DrawValidation()
        {
            var gate = _gate.objectReferenceValue as AccessGate;
            var entryCount = gate != null && gate.targetBehaviours != null ? gate.targetBehaviours.Length : 0;

            for (var i = 0; i < _roleValues.arraySize; i++)
            {
                if (string.IsNullOrEmpty(_roleValues.GetArrayElementAtIndex(i).stringValue))
                    EditorGUILayout.HelpBox($"Role {i}: role ID / name is empty.", MessageType.Warning);
                var idx = _gateEntryIndices.GetArrayElementAtIndex(i).intValue;
                if (gate != null && (idx < 0 || idx >= entryCount))
                    EditorGUILayout.HelpBox($"Role {i}: points at gate entry {idx}, which does not exist.", MessageType.Warning);
            }

            var component = (Component)target;
            if (component != null && component.gameObject.scene.IsValid()
                && Object.FindObjectOfType<VrcLinkingDownloader>(true) == null)
            {
                EditorGUILayout.HelpBox("No VrcLinkingDownloader found in the open scene — role checks will never run.", MessageType.Error);
                if (GUILayout.Button("Set Up VRCLinking Object")) VRCLinkingAuditorSection.CreateDownloaderObject();
            }
        }

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
