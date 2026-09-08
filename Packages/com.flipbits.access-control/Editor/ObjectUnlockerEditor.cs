using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace FlipBits.AccessControl.Editors
{
    /// <summary>
    /// Inspector for <see cref="ObjectUnlocker"/>: pairs each collider/GameObject
    /// with its locked state on one row, can capture the current scene state as
    /// the locked state, and offers Unlock/Relock test buttons in play mode.
    /// </summary>
    [CustomEditor(typeof(ObjectUnlocker))]
    public class ObjectUnlockerEditor : UnityEditor.Editor
    {
        private SerializedProperty _colliders;
        private SerializedProperty _colliderStartStates;
        private SerializedProperty _gameObjects;
        private SerializedProperty _gameObjectStartStates;
        private SerializedProperty _udonBehaviours;
        private SerializedProperty _udonBehaviourEvents;
        private SerializedProperty _buttonsToEnable;
        private SerializedProperty _linkedUnlockers;

        private void OnEnable()
        {
            _colliders = serializedObject.FindProperty("colliders");
            _colliderStartStates = serializedObject.FindProperty("colliderStartStates");
            _gameObjects = serializedObject.FindProperty("gameObjects");
            _gameObjectStartStates = serializedObject.FindProperty("gameObjectStartStates");
            _udonBehaviours = serializedObject.FindProperty("udonBehaviours");
            _udonBehaviourEvents = serializedObject.FindProperty("udonBehaviourEvents");
            _buttonsToEnable = serializedObject.FindProperty("buttonsToEnable");
            _linkedUnlockers = serializedObject.FindProperty("linkedUnlockers");
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            SyncLengths(_colliders, _colliderStartStates);
            SyncLengths(_gameObjects, _gameObjectStartStates);
            SyncLengths(_udonBehaviours, _udonBehaviourEvents);

            DrawPairedSection("Colliders", "Enabled while locked", _colliders, _colliderStartStates);
            EditorGUILayout.Space(8);
            DrawPairedSection("GameObjects", "Active while locked", _gameObjects, _gameObjectStartStates);
            EditorGUILayout.Space(8);
            DrawEventSection();
            EditorGUILayout.Space(8);
            EditorGUILayout.PropertyField(_buttonsToEnable, new GUIContent("UI Buttons (non-interactable while locked)"), true);
            EditorGUILayout.PropertyField(_linkedUnlockers, new GUIContent("Linked Unlockers"), true);

            EditorGUILayout.Space(10);
            DrawTools();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPairedSection(string title, string stateLabel, SerializedProperty objects, SerializedProperty states)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(stateLabel + " — flipped when unlocked", EditorStyles.miniLabel);

            var removeAt = -1;
            for (var i = 0; i < objects.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(objects.GetArrayElementAtIndex(i), GUIContent.none);
                var stateProp = states.GetArrayElementAtIndex(i);
                stateProp.boolValue = EditorGUILayout.ToggleLeft(stateProp.boolValue ? "On" : "Off", stateProp.boolValue, GUILayout.Width(50));
                if (GUILayout.Button("✕", GUILayout.Width(24))) removeAt = i;
                EditorGUILayout.EndHorizontal();
            }

            if (removeAt >= 0) RemoveElement(removeAt, objects, states);

            if (GUILayout.Button($"Add {title.TrimEnd('s')}"))
            {
                objects.arraySize++;
                states.arraySize++;
                objects.GetArrayElementAtIndex(objects.arraySize - 1).objectReferenceValue = null;
            }
        }

        private void DrawEventSection()
        {
            EditorGUILayout.LabelField("Unlock Events", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Sent via SendCustomEvent when this unlocks (not on relock)", EditorStyles.miniLabel);

            var removeAt = -1;
            for (var i = 0; i < _udonBehaviours.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(_udonBehaviours.GetArrayElementAtIndex(i), GUIContent.none);
                var evtProp = _udonBehaviourEvents.GetArrayElementAtIndex(i);
                evtProp.stringValue = EditorGUILayout.TextField(evtProp.stringValue, GUILayout.Width(140));
                if (GUILayout.Button("✕", GUILayout.Width(24))) removeAt = i;
                EditorGUILayout.EndHorizontal();
            }

            if (removeAt >= 0) RemoveElement(removeAt, _udonBehaviours, _udonBehaviourEvents);

            if (GUILayout.Button("Add Unlock Event"))
            {
                _udonBehaviours.arraySize++;
                _udonBehaviourEvents.arraySize++;
                _udonBehaviours.GetArrayElementAtIndex(_udonBehaviours.arraySize - 1).objectReferenceValue = null;
                _udonBehaviourEvents.GetArrayElementAtIndex(_udonBehaviourEvents.arraySize - 1).stringValue = "";
            }
        }

        private void DrawTools()
        {
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

            if (GUILayout.Button(new GUIContent("Capture Current States As Locked",
                "Reads each collider's enabled flag and each GameObject's active flag from the scene and stores them as the locked state.")))
            {
                CaptureLockedStates();
            }

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Test Unlock")) SendEventToBacking("Unlock");
                if (GUILayout.Button("Test Relock")) SendEventToBacking("Relock");
                EditorGUILayout.EndHorizontal();
            }
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("Enter play mode to test Unlock / Relock.", EditorStyles.miniLabel);
            }
        }

        private void CaptureLockedStates()
        {
            for (var i = 0; i < _colliders.arraySize; i++)
            {
                var collider = _colliders.GetArrayElementAtIndex(i).objectReferenceValue as Collider;
                if (collider != null) _colliderStartStates.GetArrayElementAtIndex(i).boolValue = collider.enabled;
            }

            for (var i = 0; i < _gameObjects.arraySize; i++)
            {
                var go = _gameObjects.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                if (go != null) _gameObjectStartStates.GetArrayElementAtIndex(i).boolValue = go.activeSelf;
            }
        }

        private void SendEventToBacking(string eventName)
        {
            var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour((UdonSharpBehaviour)target);
            if (backing != null) backing.SendCustomEvent(eventName);
            else Debug.LogWarning("[FlipBits.ObjectUnlocker] No backing UdonBehaviour found to test with.");
        }

        private static void SyncLengths(params SerializedProperty[] arrays)
        {
            var max = 0;
            foreach (var arr in arrays) max = Mathf.Max(max, arr.arraySize);
            foreach (var arr in arrays) arr.arraySize = max;
        }

        private static void RemoveElement(int index, params SerializedProperty[] arrays)
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
    }
}
