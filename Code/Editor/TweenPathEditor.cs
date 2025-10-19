#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Nonsliep.Core.Tween.Editor
{
    [CustomEditor(typeof(TweenPath))]
    public class TweenPathEditor : UnityEditor.Editor
    {
        private const float PreviewPadding = 16f;
        private const float HandleRadius = 4f;

        private static bool _editInSceneView;
        private static GUIStyle _sceneLabelStyle;

        private static GUIStyle SceneLabelStyle
        {
            get
            {
                if (_sceneLabelStyle == null)
                {
                    _sceneLabelStyle = new GUIStyle(EditorStyles.whiteMiniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter
                    };
                }

                return _sceneLabelStyle;
            }
        }

        private SerializedProperty _pointsProp;
        private SerializedProperty _closedProp;
        private SerializedProperty _interpolationProp;
        private SerializedProperty _lengthSamplingProp;
        private ReorderableList _pointsList;

        private void OnEnable()
        {
            _pointsProp = serializedObject.FindProperty("points");
            _closedProp = serializedObject.FindProperty("closed");
            _interpolationProp = serializedObject.FindProperty("interpolation");
            _lengthSamplingProp = serializedObject.FindProperty("lengthSampling");

            _pointsList = new ReorderableList(serializedObject, _pointsProp, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Points (UI Anchored Position Offsets)"),
                drawElementCallback = (rect, index, active, focused) =>
                {
                    SerializedProperty element = _pointsProp.GetArrayElementAtIndex(index);
                    rect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(rect, element, GUIContent.none);
                },
                elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
            };

            SceneView.duringSceneGui += DuringSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPreview();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_interpolationProp);
            using (new EditorGUI.DisabledScope(_interpolationProp.enumValueIndex != (int)TweenPath.PathInterpolation.CatmullRom))
            {
                EditorGUILayout.PropertyField(_lengthSamplingProp);
            }

            EditorGUILayout.PropertyField(_closedProp);

            EditorGUILayout.Space();
            _pointsList.DoLayoutList();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Symmetric Point"))
                {
                    AddSymmetricPoint();
                }

                if (GUILayout.Button("Normalize First Point"))
                {
                    NormalizeFirstPoint();
                }
            }

            EditorGUILayout.Space();
            bool editScene = EditorGUILayout.ToggleLeft("Edit in Scene View", _editInSceneView);
            if (editScene != _editInSceneView)
            {
                _editInSceneView = editScene;
                SceneView.RepaintAll();
            }

            serializedObject.ApplyModifiedProperties();

            foreach (TweenPath path in targets)
            {
                path.RebuildCache();
            }

            SceneView.RepaintAll();
        }

        private void DrawPreview()
        {
            Rect previewRect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, 220f);
            EditorGUI.DrawRect(previewRect, new Color(0.12f, 0.12f, 0.12f));

            List<Vector2> points = GetPoints();
            if (points.Count < 2)
            {
                GUI.Label(previewRect, "Add at least two points to preview.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            bool closed = _closedProp.boolValue;
            TweenPath.PathInterpolation interpolation = (TweenPath.PathInterpolation)_interpolationProp.enumValueIndex;
            int samples = Mathf.Clamp(_lengthSamplingProp.intValue, 4, 64);

            List<Vector2> curve = BuildCurve(points, closed, interpolation, samples);
            if (curve.Count < 2)
            {
                GUI.Label(previewRect, "Add at least two points to preview.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            Rect contentRect = new Rect(previewRect.x + PreviewPadding, previewRect.y + PreviewPadding,
                previewRect.width - PreviewPadding * 2f, previewRect.height - PreviewPadding * 2f);
            EditorGUI.DrawRect(contentRect, new Color(0.18f, 0.18f, 0.18f));

            Vector2 min = curve[0];
            Vector2 max = curve[0];
            foreach (Vector2 p in curve)
            {
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            // Expand bounds slightly to avoid zero size
            if (Mathf.Approximately(min.x, max.x))
            {
                max.x += 0.5f;
                min.x -= 0.5f;
            }
            if (Mathf.Approximately(min.y, max.y))
            {
                max.y += 0.5f;
                min.y -= 0.5f;
            }

            Handles.BeginGUI();
            Handles.color = new Color(1f, 1f, 1f, 0.15f);
            if (min.x <= 0f && max.x >= 0f)
            {
                float normalized = Mathf.InverseLerp(min.x, max.x, 0f);
                float x = Mathf.Lerp(contentRect.xMin, contentRect.xMax, normalized);
                Handles.DrawLine(new Vector3(x, contentRect.yMin), new Vector3(x, contentRect.yMax));
            }
            if (min.y <= 0f && max.y >= 0f)
            {
                float normalized = Mathf.InverseLerp(min.y, max.y, 0f);
                float y = Mathf.Lerp(contentRect.yMax, contentRect.yMin, normalized);
                Handles.DrawLine(new Vector3(contentRect.xMin, y), new Vector3(contentRect.xMax, y));
            }

            // Draw curve
            Handles.color = new Color(0.3f, 0.78f, 1f, 0.9f);
            Vector3[] polyline = new Vector3[curve.Count];
            for (int i = 0; i < curve.Count; i++)
            {
                Vector2 p = curve[i];
                float nx = Mathf.InverseLerp(min.x, max.x, p.x);
                float ny = Mathf.InverseLerp(min.y, max.y, p.y);
                float sx = Mathf.Lerp(contentRect.xMin, contentRect.xMax, nx);
                float sy = Mathf.Lerp(contentRect.yMax, contentRect.yMin, ny);
                polyline[i] = new Vector3(sx, sy, 0f);
            }
            Handles.DrawAAPolyLine(3f, polyline);
            if (closed && polyline.Length > 1)
            {
                Handles.DrawLine(polyline[polyline.Length - 1], polyline[0]);
            }

            // Draw control points
            Handles.color = new Color(1f, 0.55f, 0.2f, 0.9f);
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 cp = points[i];
                float nx = Mathf.InverseLerp(min.x, max.x, cp.x);
                float ny = Mathf.InverseLerp(min.y, max.y, cp.y);
                Vector3 sp = new Vector3(
                    Mathf.Lerp(contentRect.xMin, contentRect.xMax, nx),
                    Mathf.Lerp(contentRect.yMax, contentRect.yMin, ny),
                    0f);
                Handles.DrawSolidDisc(sp, Vector3.forward, HandleRadius);
            }
            Handles.EndGUI();

            GUI.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, 18f),
                "Preview (XY plane, Shift+Click SceneView to add point)", EditorStyles.centeredGreyMiniLabel);
        }

        private void AddSymmetricPoint()
        {
            if (_pointsProp.arraySize == 0)
            {
                Undo.RecordObjects(targets, "Add Symmetric Point");
                _pointsProp.InsertArrayElementAtIndex(0);
                _pointsProp.GetArrayElementAtIndex(0).vector2Value = Vector2.zero;
                return;
            }

            Undo.RecordObjects(targets, "Add Symmetric Point");
            SerializedProperty last = _pointsProp.GetArrayElementAtIndex(_pointsProp.arraySize - 1);
            Vector2 mirrored = -last.vector2Value;
            int newIndex = _pointsProp.arraySize;
            _pointsProp.InsertArrayElementAtIndex(newIndex);
            _pointsProp.GetArrayElementAtIndex(newIndex).vector2Value = mirrored;
            SceneView.RepaintAll();
        }

        private void NormalizeFirstPoint()
        {
            if (_pointsProp.arraySize == 0)
            {
                return;
            }

            SerializedProperty first = _pointsProp.GetArrayElementAtIndex(0);
            Vector2 delta = first.vector2Value;
            if (delta == Vector2.zero)
            {
                return;
            }

            Undo.RecordObjects(targets, "Normalize Path Start");
            for (int i = 0; i < _pointsProp.arraySize; i++)
            {
                SerializedProperty element = _pointsProp.GetArrayElementAtIndex(i);
                element.vector2Value -= delta;
            }
            SceneView.RepaintAll();
        }

        private List<Vector2> GetPoints()
        {
            List<Vector2> list = new List<Vector2>(_pointsProp.arraySize);
            for (int i = 0; i < _pointsProp.arraySize; i++)
            {
                list.Add(_pointsProp.GetArrayElementAtIndex(i).vector2Value);
            }
            return list;
        }

        private List<Vector2> BuildCurve(List<Vector2> points, bool closed, TweenPath.PathInterpolation interpolation, int samplesPerSegment)
        {
            List<Vector2> result = new List<Vector2>();
            if (points.Count < 2)
            {
                return result;
            }

            if (interpolation != TweenPath.PathInterpolation.CatmullRom || points.Count < 4)
            {
                // Linear
                int segmentCount = closed ? points.Count : points.Count - 1;
                for (int i = 0; i < segmentCount; i++)
                {
                    Vector2 a = points[i];
                    Vector2 b = GetSegmentEnd(points, closed, i);
                    if (i == 0)
                    {
                        result.Add(a);
                    }
                    result.Add(b);
                }
                return result;
            }

            int segments = closed ? points.Count : points.Count - 1;
            samplesPerSegment = Mathf.Clamp(samplesPerSegment, 4, 128);

            for (int i = 0; i < segments; i++)
            {
                for (int s = 0; s <= samplesPerSegment; s++)
                {
                    float t = s / (float)samplesPerSegment;
                    result.Add(EvaluateCatmull(points, closed, i, t));
                }
            }

            return result;
        }

        private static Vector2 GetSegmentEnd(List<Vector2> points, bool closed, int segmentIndex)
        {
            if (closed)
            {
                int next = (segmentIndex + 1) % points.Count;
                return points[next];
            }

            int clamped = Mathf.Min(segmentIndex + 1, points.Count - 1);
            return points[clamped];
        }

        private static Vector2 EvaluateCatmull(List<Vector2> points, bool closed, int segmentIndex, float t)
        {
            int count = points.Count;

            int p1 = ClampIndex(points, closed, segmentIndex);
            int p2 = ClampIndex(points, closed, segmentIndex + 1);
            int p0 = ClampIndex(points, closed, segmentIndex - 1);
            int p3 = ClampIndex(points, closed, segmentIndex + 2);

            Vector2 P0 = points[p0];
            Vector2 P1 = points[p1];
            Vector2 P2 = points[p2];
            Vector2 P3 = points[p3];

            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * ((2f * P1)
                + (-P0 + P2) * t
                + (2f * P0 - 5f * P1 + 4f * P2 - P3) * t2
                + (-P0 + 3f * P1 - 3f * P2 + P3) * t3);
        }

        private static int ClampIndex(List<Vector2> points, bool closed, int index)
        {
            int count = points.Count;
            if (closed)
            {
                int mod = index % count;
                if (mod < 0)
                {
                    mod += count;
                }
                return mod;
            }

            return Mathf.Clamp(index, 0, count - 1);
        }

        private void DuringSceneGUI(SceneView sceneView)
        {
            if (!_editInSceneView)
            {
                return;
            }

            if (Selection.activeObject != target)
            {
                return;
            }

            serializedObject.Update();

            List<Vector2> points = GetPoints();
            if (points.Count == 0)
            {
                return;
            }

            Event e = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlId);
            }

            bool closed = _closedProp.boolValue;
            TweenPath.PathInterpolation interpolation = (TweenPath.PathInterpolation)_interpolationProp.enumValueIndex;
            int samples = Mathf.Clamp(_lengthSamplingProp.intValue, 4, 64);
            List<Vector2> curve = BuildCurve(points, closed, interpolation, samples);

            Handles.color = new Color(0.3f, 0.78f, 1f, 0.9f);
            if (curve.Count >= 2)
            {
                Vector3[] pathVerts = new Vector3[curve.Count];
                for (int i = 0; i < curve.Count; i++)
                {
                    Vector2 c = curve[i];
                    pathVerts[i] = new Vector3(c.x, c.y, 0f);
                }

                Handles.DrawAAPolyLine(4f, pathVerts);
                if (closed)
                {
                    Handles.DrawLine(pathVerts[pathVerts.Length - 1], pathVerts[0]);
                }
            }

            Handles.color = new Color(1f, 0.55f, 0.2f, 0.95f);
            bool changed = false;
            for (int i = 0; i < _pointsProp.arraySize; i++)
            {
                SerializedProperty element = _pointsProp.GetArrayElementAtIndex(i);
                Vector3 pos = new Vector3(element.vector2Value.x, element.vector2Value.y, 0f);
                float size = HandleUtility.GetHandleSize(pos) * 0.075f;

                EditorGUI.BeginChangeCheck();
                var fmh_415_58_638941800094185900 = Quaternion.identity; Vector3 newPos = Handles.FreeMoveHandle(pos, size, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Move Path Point");
                    element.vector2Value = new Vector2(newPos.x, newPos.y);
                    changed = true;
                }

                Handles.Label(pos + Vector3.up * (size * 1.6f), i.ToString(), SceneLabelStyle);
            }

            if (changed)
            {
                serializedObject.ApplyModifiedProperties();
                ((TweenPath)target).RebuildCache();
                EditorUtility.SetDirty(target);
                Repaint();
                SceneView.RepaintAll();
            }

            if (e.type == EventType.MouseDown && e.button == 0 && e.shift)
            {
                Plane plane = new Plane(Vector3.forward, Vector3.zero);
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (plane.Raycast(ray, out float enter))
                {
                    Vector3 hit = ray.GetPoint(enter);
                    Undo.RecordObject(target, "Add Path Point");
                    int newIndex = _pointsProp.arraySize;
                    _pointsProp.InsertArrayElementAtIndex(newIndex);
                    _pointsProp.GetArrayElementAtIndex(newIndex).vector2Value = new Vector2(hit.x, hit.y);
                    serializedObject.ApplyModifiedProperties();
                    ((TweenPath)target).RebuildCache();
                    EditorUtility.SetDirty(target);
                    Repaint();
                    SceneView.RepaintAll();
                    e.Use();
                }
            }
        }
    }
#endif
}