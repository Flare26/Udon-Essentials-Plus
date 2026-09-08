using TMPro;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.Udon;

namespace FlipBits.AccessControl.Editors
{
    /// <summary>
    /// Inspector for <see cref="FlipBitsKeypad"/> with wiring validation, plus
    /// the builder that constructs a ready-to-use world-space keypad canvas
    /// (display + 0–9/CLR/OK buttons wired to the backing UdonBehaviour).
    /// </summary>
    [CustomEditor(typeof(FlipBitsKeypad))]
    public class FlipBitsKeypadEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");

            var keypad = (FlipBitsKeypad)target;
            EditorGUILayout.Space(8);

            if (serializedObject.FindProperty("gate").objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No access gate assigned — submitted codes will go nowhere.", MessageType.Warning);
                if (GUILayout.Button("Auto-Assign Access Gate")) AutoAssignGate(keypad);
            }

            if (serializedObject.FindProperty("display").objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No display assigned. Build the keypad UI, or assign a TextMeshProUGUI manually.", MessageType.Warning);
            }

            if (GUILayout.Button(new GUIContent("Build Keypad UI",
                "Creates a world-space canvas under this keypad: display + 0-9/CLR/OK buttons wired to the backing UdonBehaviour.")))
            {
                BuildUi(keypad);
            }

            serializedObject.ApplyModifiedProperties();
        }

        // ------------------------------------------------------------ builder

        /// <summary>Creates a keypad GameObject with a built UI, auto-assigning the scene's access gate.</summary>
        public static FlipBitsKeypad CreateKeypadObject()
        {
            var go = new GameObject("FlipBitsKeypad");
            Undo.RegisterCreatedObjectUndo(go, "Create FlipBits Keypad");
            var keypad = UdonSharpUndo.AddComponent<FlipBitsKeypad>(go);
            BuildUi(keypad);
            AutoAssignGate(keypad);
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            return keypad;
        }

        public static void AutoAssignGate(FlipBitsKeypad keypad)
        {
            var gate = FindObjectOfType<AccessGate>(true);
            if (gate == null)
            {
                Debug.LogWarning("[FlipBits] No AccessGate in the open scenes to assign — add one via the Access Control Auditor first.");
                return;
            }
            var so = new SerializedObject(keypad);
            so.FindProperty("gate").objectReferenceValue = gate;
            so.ApplyModifiedProperties();
        }

        /// <summary>Builds the world-space canvas: background, display, and a 3×4 button grid.</summary>
        public static void BuildUi(FlipBitsKeypad keypad)
        {
            var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(keypad);
            if (backing == null)
            {
                Debug.LogError("[FlipBits] Keypad has no backing UdonBehaviour — cannot wire buttons.");
                return;
            }

            var canvasGo = new GameObject("Keypad Canvas", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Build Keypad UI");
            canvasGo.transform.SetParent(keypad.transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGo.AddComponent<VRCUiShape>();

            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400, 560);
            canvasRect.localScale = Vector3.one * 0.001f; // 0.4 m × 0.56 m in world space

            var box = canvasGo.GetComponent<BoxCollider>();
            if (box == null) box = canvasGo.AddComponent<BoxCollider>();
            box.size = new Vector3(400, 560, 1);
            box.isTrigger = true;

            // Background panel
            var bg = CreateRect("Background", canvasRect);
            bg.anchorMin = Vector2.zero;
            bg.anchorMax = Vector2.one;
            bg.offsetMin = Vector2.zero;
            bg.offsetMax = Vector2.zero;
            var bgImage = bg.gameObject.AddComponent<Image>();
            bgImage.color = new Color(0.07f, 0.07f, 0.09f, 1f);
            bgImage.raycastTarget = false;

            // Display
            var displayRect = CreateRect("Display", canvasRect);
            displayRect.sizeDelta = new Vector2(360, 100);
            displayRect.anchoredPosition = new Vector2(0, 210);
            var displayTmp = displayRect.gameObject.AddComponent<TextMeshProUGUI>();
            displayTmp.text = keypad.idleText;
            displayTmp.fontSize = 52;
            displayTmp.alignment = TextAlignmentOptions.Center;
            displayTmp.color = Color.white;
            displayTmp.raycastTarget = false;

            // Button grid: 3 columns × 4 rows
            string[] labels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "CLR", "0", "OK" };
            string[] events = { "Digit1", "Digit2", "Digit3", "Digit4", "Digit5", "Digit6", "Digit7", "Digit8", "Digit9", "Clear", "Digit0", "Submit" };
            float[] columns = { -125f, 0f, 125f };
            float[] rows = { 110f, 10f, -90f, -190f };

            for (var i = 0; i < labels.Length; i++)
            {
                var pos = new Vector2(columns[i % 3], rows[i / 3]);
                CreateButton(canvasRect, labels[i], events[i], pos, backing);
            }

            var so = new SerializedObject(keypad);
            so.FindProperty("display").objectReferenceValue = displayTmp;
            so.ApplyModifiedProperties();
        }

        private static RectTransform CreateRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void CreateButton(RectTransform parent, string label, string eventName, Vector2 position, UdonBehaviour backing)
        {
            var rect = CreateRect($"Btn_{label}", parent);
            rect.sizeDelta = new Vector2(115, 90);
            rect.anchoredPosition = position;

            var image = rect.gameObject.AddComponent<Image>();
            image.color = label == "OK" ? new Color(0.16f, 0.32f, 0.2f, 1f)
                : label == "CLR" ? new Color(0.32f, 0.16f, 0.16f, 1f)
                : new Color(0.18f, 0.18f, 0.22f, 1f);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            UnityEventTools.AddStringPersistentListener(button.onClick, backing.SendCustomEvent, eventName);

            var labelRect = CreateRect("Label", rect);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var tmp = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 42;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }
    }
}
