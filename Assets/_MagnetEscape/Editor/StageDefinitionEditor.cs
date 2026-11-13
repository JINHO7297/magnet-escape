using UnityEditor;
using UnityEngine;

namespace MagnetEscape.Editor
{
    [CustomEditor(typeof(StageDefinition))]
    public sealed class StageDefinitionEditor : UnityEditor.Editor
    {
        private static readonly string[] Fields =
        {
            "StartPosition", "ExitPosition", "Platforms", "MovingPlatforms", "Spikes",
            "Saws", "Magnets", "Checkpoints", "Hints"
        };
        private static readonly string[] Labels =
        {
            "Start", "Exit", "Platforms", "Moving platforms", "Spikes", "Saws", "Magnets", "Checkpoints", "Hints"
        };
        private bool preview = true;
        private bool hintRanges;
        private bool secondaryOnly;
        private int group;
        private int element;

        private void OnEnable() => SceneView.duringSceneGui += DrawScene;
        private void OnDisable() => SceneView.duringSceneGui -= DrawScene;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Source courses read this asset on Play. Existing editable scene copies are preserved. "
                + "The Scene view drawing is an authoring preview, not a physics simulation.", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            preview = EditorGUILayout.Toggle("Scene preview", preview);
            hintRanges = EditorGUILayout.Toggle("Hint ranges", hintRanges);
            group = EditorGUILayout.Popup("Move in Scene", group, Labels);
            if (HasSecondaryHandle()) secondaryOnly = EditorGUILayout.Toggle("Edit end / radius only", secondaryOnly);
            serializedObject.Update();
            if (group >= 2)
            {
                int count = serializedObject.FindProperty(Fields[group]).arraySize;
                if (count > 0) element = EditorGUILayout.IntSlider("Element", Mathf.Clamp(element, 0, count - 1), 0, count - 1);
                else EditorGUILayout.LabelField("This group has no placements.");
            }
            if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();
            if (GUILayout.Button("Frame Entire Course"))
            {
                StageDefinition layout = (StageDefinition)target;
                SceneView view = SceneView.lastActiveSceneView;
                if (view == null) view = EditorWindow.GetWindow<SceneView>();
                view.in2DMode = true;
                view.Frame(CourseBounds(layout), false);
            }
            if (GUILayout.Button("Generate Fresh Course Revision (Preserve Existing)"))
            {
                StageAuthoring.CreateRevisionCopies();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.Space();
            DrawDefaultInspector();
            string prefabError = ((StageDefinition)target).PlayerPrefabError();
            if (prefabError != null) EditorGUILayout.HelpBox(prefabError, MessageType.Error);
        }

        private SerializedProperty SelectedPosition()
        {
            SerializedProperty property = serializedObject.FindProperty(Fields[group]);
            if (group < 2) return property;
            if (property.arraySize == 0) return null;
            element = Mathf.Clamp(element, 0, property.arraySize - 1);
            return property.GetArrayElementAtIndex(element).FindPropertyRelative("Position");
        }

        private void DrawScene(SceneView view)
        {
            if (!preview || target == null || Selection.activeObject != target || Application.isPlaying) return;
            StageDefinition layout = (StageDefinition)target;
            Color previous = Handles.color;
            try
            {
                DrawLayout(layout);
                serializedObject.Update();
                SerializedProperty position = SelectedPosition();
                if (position == null) return;
                if (!secondaryOnly || !HasSecondaryHandle())
                {
                    Handles.color = Color.white;
                    EditorGUI.BeginChangeCheck();
                    Vector3 moved = Handles.PositionHandle(position.vector2Value, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        position.vector2Value = new Vector2(moved.x, moved.y);
                        serializedObject.ApplyModifiedProperties();
                        Repaint();
                    }
                }
                DrawSecondaryHandle(position.vector2Value);
            }
            finally { Handles.color = previous; }
        }

        private bool HasSecondaryHandle()
        {
            string field = Fields[group];
            return field == "MovingPlatforms" || field == "Saws" || field == "Checkpoints" || field == "Magnets" || field == "Hints";
        }

        private void DrawSecondaryHandle(Vector2 center)
        {
            if (group < 2) return;
            SerializedProperty array = serializedObject.FindProperty(Fields[group]);
            if (array.arraySize == 0) return;
            SerializedProperty item = array.GetArrayElementAtIndex(element);
            string field = Fields[group];
            if (field == "MovingPlatforms" || field == "Saws" || field == "Checkpoints")
            {
                string offsetName = field == "Checkpoints" ? "SpawnOffset" : "Travel";
                SerializedProperty offset = item.FindPropertyRelative(offsetName);
                Vector2 end = center + offset.vector2Value;
                Handles.color = field == "Checkpoints" ? Color.green : Color.yellow;
                Handles.DrawLine(center, end);
                Handles.Label(end + Vector2.up * 0.3f, field == "Checkpoints" ? "RESPAWN" : "TRAVEL END");
                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.PositionHandle(end, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    offset.vector2Value = new Vector2(moved.x, moved.y) - center;
                    serializedObject.ApplyModifiedProperties();
                    Repaint();
                }
            }
            else if (field == "Magnets" || field == "Hints")
            {
                SerializedProperty radius = item.FindPropertyRelative("Range");
                Vector3 end = (Vector3)center + Vector3.right * radius.floatValue;
                Handles.color = Color.cyan;
                Handles.DrawWireDisc(center, Vector3.forward, radius.floatValue);
                Handles.Label(end + Vector3.up * 0.3f, "RANGE");
                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.Slider(end, Vector3.right, HandleUtility.GetHandleSize(end) * 0.07f, Handles.DotHandleCap, 0.1f);
                if (EditorGUI.EndChangeCheck())
                {
                    radius.floatValue = Mathf.Max(0.5f, moved.x - center.x);
                    serializedObject.ApplyModifiedProperties();
                    Repaint();
                }
            }
        }

        private Bounds CourseBounds(StageDefinition layout)
        {
            Bounds bounds = new Bounds(layout.StartPosition, Vector3.one);
            IncludeBox(ref bounds, layout.ExitPosition, 1f, 2f);
            foreach (StageDefinition.RectPlacement item in layout.Platforms)
                if (item != null) IncludeBox(ref bounds, item.Position, item.Width, 0.6f);
            foreach (StageDefinition.RectPlacement item in layout.Spikes)
                if (item != null) IncludeBox(ref bounds, item.Position, item.Width, 0.7f);
            foreach (StageDefinition.MovingPlacement item in layout.MovingPlatforms)
            {
                if (item == null) continue;
                IncludeBox(ref bounds, item.Position, item.Width, 0.6f);
                IncludeBox(ref bounds, item.Position + item.Travel, item.Width, 0.6f);
            }
            foreach (StageDefinition.SawPlacement item in layout.Saws)
            {
                if (item == null) continue;
                IncludeBox(ref bounds, item.Position - item.Travel, 1f, 1f);
                IncludeBox(ref bounds, item.Position + item.Travel, 1f, 1f);
            }
            foreach (StageDefinition.MagnetPlacement item in layout.Magnets)
                if (item != null) IncludeBox(ref bounds, item.Position, item.Range * 2f, item.Range * 2f);
            foreach (StageDefinition.CheckpointPlacement item in layout.Checkpoints)
            {
                if (item == null) continue;
                IncludeBox(ref bounds, item.Position, 0.5f, 1.2f);
                IncludeBox(ref bounds, item.Position + item.SpawnOffset, 0.7f, 0.9f);
            }
            foreach (StageDefinition.HintPlacement item in layout.Hints)
                if (item != null) IncludeBox(ref bounds, item.Position, hintRanges ? item.Range * 2f : 1f, hintRanges ? item.Range * 2f : 1f);
            bounds.Expand(new Vector3(6f, 4f, 1f));
            bounds.size = new Vector3(Mathf.Max(12f, bounds.size.x), Mathf.Max(10f, bounds.size.y), 1f);
            return bounds;
        }

        private static void IncludeBox(ref Bounds bounds, Vector2 center, float width, float height)
        {
            Vector2 half = new Vector2(Mathf.Max(0.1f, width), Mathf.Max(0.1f, height)) * 0.5f;
            bounds.Encapsulate(center - half);
            bounds.Encapsulate(center + half);
        }

        private void DrawLayout(StageDefinition layout)
        {
            Box(layout.StartPosition, 0.7f, 0.9f, Color.cyan, "START");
            Box(layout.ExitPosition, 1f, 2f, Color.green, "EXIT");
            for (int i = 0; i < layout.Platforms.Length; i++)
            {
                StageDefinition.RectPlacement item = layout.Platforms[i];
                if (item == null) continue;
                Box(item.Position, item.Width, 0.6f, new Color(0.6f, 0.7f, 0.8f), "P" + (i + 1));
            }
            for (int i = 0; i < layout.MovingPlatforms.Length; i++)
            {
                StageDefinition.MovingPlacement item = layout.MovingPlatforms[i];
                if (item == null) continue;
                Box(item.Position, item.Width, 0.6f, Color.yellow, "M" + (i + 1));
                Handles.DrawLine(item.Position, item.Position + item.Travel);
                Box(item.Position + item.Travel, item.Width, 0.6f, new Color(1f, 0.8f, 0.3f, 0.4f), string.Empty);
            }
            for (int i = 0; i < layout.Spikes.Length; i++)
            {
                StageDefinition.RectPlacement item = layout.Spikes[i];
                if (item == null) continue;
                Box(item.Position + Vector2.down * 0.1f, item.Width, 0.5f, Color.red, "SPIKES " + (i + 1));
            }
            for (int i = 0; i < layout.Saws.Length; i++)
            {
                StageDefinition.SawPlacement item = layout.Saws[i];
                if (item == null) continue;
                Handles.color = new Color(1f, 0.4f, 0.3f);
                Handles.DrawWireDisc(item.Position, Vector3.forward, 0.45f);
                Handles.DrawLine(item.Position - item.Travel, item.Position + item.Travel);
                Handles.Label(item.Position + Vector2.up * 0.5f, "SAW " + (i + 1));
            }
            for (int i = 0; i < layout.Magnets.Length; i++)
            {
                StageDefinition.MagnetPlacement item = layout.Magnets[i];
                if (item == null) continue;
                Handles.color = new Color(0.35f, 0.95f, 0.85f, 0.4f);
                Handles.DrawWireDisc(item.Position, Vector3.forward, item.Range);
                Box(item.Position, 1f, 1f, Color.cyan, "MAGNET " + (i + 1));
            }
            for (int i = 0; i < layout.Checkpoints.Length; i++)
            {
                StageDefinition.CheckpointPlacement item = layout.Checkpoints[i];
                if (item == null) continue;
                Box(item.Position, 0.5f, 1.2f, Color.green, "CP " + (i + 1));
                Handles.DrawLine(item.Position, item.Position + item.SpawnOffset);
                Box(item.Position + item.SpawnOffset, 0.7f, 0.9f, new Color(0.3f, 1f, 0.5f, 0.4f), string.Empty);
            }
            if (!hintRanges) return;
            for (int i = 0; i < layout.Hints.Length; i++)
            {
                StageDefinition.HintPlacement item = layout.Hints[i];
                if (item == null) continue;
                Handles.color = new Color(1f, 1f, 1f, 0.3f);
                Handles.DrawWireDisc(item.Position, Vector3.forward, item.Range);
                Handles.Label(item.Position, "HINT " + (i + 1));
            }
        }

        private static void Box(Vector2 position, float width, float height, Color color, string label)
        {
            Handles.color = color;
            Handles.DrawWireCube(position, new Vector3(width, height, 0f));
            if (!string.IsNullOrEmpty(label)) Handles.Label(position + Vector2.up * (height * 0.5f + 0.1f), label);
        }
    }
}
