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
                Vector2 center = (layout.StartPosition + layout.ExitPosition) * 0.5f + Vector2.up * 2f;
                float width = Mathf.Max(12f, Mathf.Abs(layout.ExitPosition.x - layout.StartPosition.x) + 10f);
                view.in2DMode = true;
                view.Frame(new Bounds(center, new Vector3(width, 16f, 1f)), false);
            }
            if (GUILayout.Button("Generate Fresh Course Revision (Preserve Existing)"))
            {
                StageAuthoring.CreateRevisionCopies();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.Space();
            DrawDefaultInspector();
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
                Handles.color = Color.white;
                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.PositionHandle(position.vector2Value, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    position.vector2Value = new Vector2(moved.x, moved.y);
                    serializedObject.ApplyModifiedProperties(); // SerializedProperty records Undo and dirties the asset.
                    Repaint();
                }
            }
            finally { Handles.color = previous; }
        }

        private void DrawLayout(StageDefinition layout)
        {
            Box(layout.StartPosition, 0.7f, 0.9f, Color.cyan, "START");
            Box(layout.ExitPosition, 1f, 2f, Color.green, "EXIT");
            for (int i = 0; i < layout.Platforms.Length; i++)
            {
                StageDefinition.RectPlacement item = layout.Platforms[i];
                Box(item.Position, item.Width, 0.6f, new Color(0.6f, 0.7f, 0.8f), "P" + (i + 1));
            }
            for (int i = 0; i < layout.MovingPlatforms.Length; i++)
            {
                StageDefinition.MovingPlacement item = layout.MovingPlatforms[i];
                Box(item.Position, item.Width, 0.6f, Color.yellow, "M" + (i + 1));
                Handles.DrawLine(item.Position, item.Position + item.Travel);
                Box(item.Position + item.Travel, item.Width, 0.6f, new Color(1f, 0.8f, 0.3f, 0.4f), string.Empty);
            }
            for (int i = 0; i < layout.Spikes.Length; i++)
            {
                StageDefinition.RectPlacement item = layout.Spikes[i];
                Box(item.Position + Vector2.down * 0.1f, item.Width, 0.5f, Color.red, "SPIKES " + (i + 1));
            }
            for (int i = 0; i < layout.Saws.Length; i++)
            {
                StageDefinition.SawPlacement item = layout.Saws[i];
                Handles.color = new Color(1f, 0.4f, 0.3f);
                Handles.DrawWireDisc(item.Position, Vector3.forward, 0.45f);
                Handles.DrawLine(item.Position - item.Travel, item.Position + item.Travel);
                Handles.Label(item.Position + Vector2.up * 0.5f, "SAW " + (i + 1));
            }
            for (int i = 0; i < layout.Magnets.Length; i++)
            {
                StageDefinition.MagnetPlacement item = layout.Magnets[i];
                Handles.color = new Color(0.35f, 0.95f, 0.85f, 0.4f);
                Handles.DrawWireDisc(item.Position, Vector3.forward, item.Range);
                Box(item.Position, 1f, 1f, Color.cyan, "MAGNET " + (i + 1));
            }
            for (int i = 0; i < layout.Checkpoints.Length; i++)
            {
                StageDefinition.CheckpointPlacement item = layout.Checkpoints[i];
                Box(item.Position, 0.5f, 1.2f, Color.green, "CP " + (i + 1));
                Handles.DrawLine(item.Position, item.Position + item.SpawnOffset);
                Box(item.Position + item.SpawnOffset, 0.7f, 0.9f, new Color(0.3f, 1f, 0.5f, 0.4f), string.Empty);
            }
            if (!hintRanges) return;
            for (int i = 0; i < layout.Hints.Length; i++)
            {
                StageDefinition.HintPlacement item = layout.Hints[i];
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
