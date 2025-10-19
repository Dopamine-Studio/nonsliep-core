#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Nonsliep.Core.Tween.Editor
{
    [CustomEditor(typeof(TweenPlayer))]
    public class TweenPlayerEditor : UnityEditor.Editor
    {
        private ReorderableList _presetsList;
        private SerializedProperty _presetsProperty;
        private SerializedProperty _repeatEnabledProperty;
        private SerializedProperty _repeatReturnProperty;
        private SerializedProperty _repeatInfiniteProperty;
        private SerializedProperty _repeatCountProperty;
        private SerializedProperty _repeatDelayProperty;
        private SerializedProperty _repeatUseUnscaledProperty;

        private void OnEnable()
        {
            _presetsProperty = serializedObject.FindProperty(nameof(TweenPlayer.presets));
            _repeatEnabledProperty = serializedObject.FindProperty(nameof(TweenPlayer.repeatEnabled));
            _repeatReturnProperty = serializedObject.FindProperty(nameof(TweenPlayer.repeatReturnToInitial));
            _repeatInfiniteProperty = serializedObject.FindProperty(nameof(TweenPlayer.repeatInfinite));
            _repeatCountProperty = serializedObject.FindProperty(nameof(TweenPlayer.repeatCount));
            _repeatDelayProperty = serializedObject.FindProperty(nameof(TweenPlayer.repeatDelay));
            _repeatUseUnscaledProperty = serializedObject.FindProperty(nameof(TweenPlayer.repeatUseUnscaledTime));

            _presetsList = new ReorderableList(serializedObject, _presetsProperty, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Presets"),
                drawElementCallback = (rect, index, active, focused) =>
                {
                    SerializedProperty element = _presetsProperty.GetArrayElementAtIndex(index);
                    rect.y += EditorGUIUtility.standardVerticalSpacing * 0.5f;
                    EditorGUI.PropertyField(rect, element, GUIContent.none, true);
                },
                elementHeightCallback = index =>
                {
                    SerializedProperty element = _presetsProperty.GetArrayElementAtIndex(index);
                    float height = EditorGUI.GetPropertyHeight(element, true) + EditorGUIUtility.standardVerticalSpacing;
                    return Mathf.Max(EditorGUIUtility.singleLineHeight * 1.2f, height);
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (_repeatEnabledProperty != null)
            {
                EditorGUILayout.LabelField("반복 옵션", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_repeatEnabledProperty);
                using (new EditorGUI.DisabledScope(!_repeatEnabledProperty.boolValue))
                {
                    if (_repeatReturnProperty != null)
                    {
                        EditorGUILayout.PropertyField(_repeatReturnProperty);
                    }

                    if (_repeatInfiniteProperty != null)
                    {
                        EditorGUILayout.PropertyField(_repeatInfiniteProperty);
                    }

                    using (new EditorGUI.DisabledScope(_repeatInfiniteProperty != null && _repeatInfiniteProperty.boolValue))
                    {
                        EditorGUILayout.PropertyField(_repeatCountProperty);
                    }
                    EditorGUILayout.PropertyField(_repeatDelayProperty);
                    EditorGUILayout.PropertyField(_repeatUseUnscaledProperty);
                }

                EditorGUILayout.Space();
            }

            if (_presetsList != null)
            {
                _presetsList.DoLayoutList();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomPropertyDrawer(typeof(TweenPlayer.Preset))]
    public class TweenPlayerPresetDrawer : PropertyDrawer
    {
        private readonly struct Section
        {
            public readonly string Label;
            public readonly string[] Fields;

            public Section(string label, string[] fields)
            {
                Label = label;
                Fields = fields;
            }
        }

        private static readonly float VerticalSpacing = EditorGUIUtility.standardVerticalSpacing;
        private static readonly Section[] Sections =
        {
            new("기본 정보", new[] { "trigger", "type", "playMode" }),
            new("타이밍", new[] { "duration", "delay", "ease" }),
            new("공통 옵션", new[] { "useUnscaledTime", "deactivateOnComplete", "restartFromInitial" }),
            new("Slide 옵션", new[] { "slideDistance" }),
            new("Pop 옵션", new[] { "popFromScale" }),
            new("Punch 옵션", new[] { "punchStrength", "punchVibrato", "punchElasticity" }),
            new("Fade/Pop 옵션", new[] { "autoAddCanvasGroup" }),
        new("ColorTo 옵션", new[] { "targetColor", "rendererColorProperty", "colorToggleWithInitial" }),
            new("Move 옵션", new[] { "movePosition", "moveRelative", "moveUseLocal", "moveUseAnchoredPosition", "moveCurve" }),
            new("Rotate 옵션", new[] { "rotateEuler", "rotateRelative", "rotateUseLocal", "rotateCurve" }),
            new("PathMove 옵션", new[] { "pathAsset", "pathClampToOrigin", "pathEndAtOrigin" }),
        new("TMP 글자 옵션", new[] { "tmpCharIndices", "tmpCharUseVisibleIndex", "tmpCharTargetScale", "tmpCharHoldDuration", "tmpCharReturnDuration", "tmpCharReturnEase", "tmpCharForceRefresh" })
        };

        private static readonly Dictionary<string, bool> FoldoutStates = new Dictionary<string, bool>();
        private static readonly Dictionary<string, bool> SectionFoldoutStates = new Dictionary<string, bool>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            bool foldout = GetFoldout(property);
            if (!foldout)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight + VerticalSpacing; // main fold header + spacer

            foreach (Section section in Sections)
            {
                if (!TryGetSectionHeight(property, section, out float sectionHeight))
                {
                    continue;
                }

                height += EditorGUIUtility.singleLineHeight; // section fold header

                if (GetSectionFoldout(property, section.Label))
                {
                    height += VerticalSpacing + sectionHeight;
                }

                height += VerticalSpacing;
            }

            float longPressHeight = GetLongPressHeight(property);
            if (longPressHeight > 0f)
            {
                height += longPressHeight;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            string header = GetHeader(property);

            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            bool foldout = EditorGUI.BeginFoldoutHeaderGroup(foldoutRect, GetFoldout(property), header);
            SetFoldout(property, foldout);
            EditorGUI.EndFoldoutHeaderGroup();

            if (!foldout)
            {
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Rect contentRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + VerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.indentLevel++;

            foreach (Section section in Sections)
            {
                if (!TryGetSectionProperties(property, section, out List<SerializedProperty> fields))
                {
                    continue;
                }

                Rect sectionFoldRect = new Rect(contentRect.x, contentRect.y, contentRect.width, EditorGUIUtility.singleLineHeight);
                bool sectionFold = EditorGUI.Foldout(sectionFoldRect, GetSectionFoldout(property, section.Label), section.Label, true);
                SetSectionFoldout(property, section.Label, sectionFold);
                contentRect.y += EditorGUIUtility.singleLineHeight + VerticalSpacing;

                if (!sectionFold)
                {
                    continue;
                }

                EditorGUI.indentLevel++;
                foreach (SerializedProperty field in fields)
                {
                    float propertyHeight = EditorGUI.GetPropertyHeight(field, true);
                    Rect propertyRect = new Rect(contentRect.x, contentRect.y, contentRect.width, propertyHeight);
                    EditorGUI.PropertyField(propertyRect, field, true);
                    contentRect.y += propertyHeight + VerticalSpacing;
                }
                EditorGUI.indentLevel--;
            }

            DrawLongPressSettings(property, ref contentRect);

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private static string GetHeader(SerializedProperty property)
        {
            SerializedProperty typeProp = property.FindPropertyRelative("type");
            string typeLabel = null;
            if (typeProp != null && typeProp.propertyType == SerializedPropertyType.Enum)
            {
                int index = typeProp.enumValueIndex;
                if (index >= 0 && index < typeProp.enumDisplayNames.Length)
                {
                    typeLabel = typeProp.enumDisplayNames[index];
                }
            }

            SerializedProperty playModeProp = property.FindPropertyRelative("playMode");
            string modeLabel = null;
            if (playModeProp != null && playModeProp.propertyType == SerializedPropertyType.Enum)
            {
                int index = playModeProp.enumValueIndex;
                if (index >= 0 && index < playModeProp.enumDisplayNames.Length)
                {
                    modeLabel = playModeProp.enumDisplayNames[index] switch
                    {
                        "Sequential" => "Seq",
                        "Parallel" => "Join",
                        string other => other
                    };
                }
            }

            if (string.IsNullOrEmpty(typeLabel))
            {
                typeLabel = property.displayName;
            }

            return string.IsNullOrEmpty(modeLabel) ? typeLabel : $"{typeLabel} [{modeLabel}]";
        }

        private static bool TryGetSectionHeight(SerializedProperty root, Section section, out float height)
        {
            height = 0f;
            bool hasField = false;

            foreach (string fieldName in section.Fields)
            {
                SerializedProperty field = root.FindPropertyRelative(fieldName);
                if (field == null)
                {
                    continue;
                }

                if (!ShouldDisplay(field, root))
                {
                    continue;
                }

                hasField = true;
                height += EditorGUI.GetPropertyHeight(field, true) + VerticalSpacing;
            }

            if (hasField)
            {
                height -= VerticalSpacing; // remove last spacing
            }

            return hasField;
        }

        private static bool TryGetSectionProperties(SerializedProperty root, Section section, out List<SerializedProperty> properties)
        {
            properties = new List<SerializedProperty>();
            foreach (string fieldName in section.Fields)
            {
                SerializedProperty field = root.FindPropertyRelative(fieldName);
                if (field == null)
                {
                    continue;
                }

                if (!ShouldDisplay(field, root))
                {
                    continue;
                }

                properties.Add(field);
            }

            return properties.Count > 0;
        }

        private static bool ShouldDisplay(SerializedProperty property, SerializedProperty root)
        {
            string name = property.name;
            TweenPlayer.TweenAnimation type = (TweenPlayer.TweenAnimation)root.FindPropertyRelative("type").enumValueIndex;

            static bool IsSlide(TweenPlayer.TweenAnimation t) => t is TweenPlayer.TweenAnimation.SlideInLeft
                or TweenPlayer.TweenAnimation.SlideInRight
                or TweenPlayer.TweenAnimation.SlideInUp
                or TweenPlayer.TweenAnimation.SlideInDown
                or TweenPlayer.TweenAnimation.SlideOutLeft
                or TweenPlayer.TweenAnimation.SlideOutRight
                or TweenPlayer.TweenAnimation.SlideOutUp
                or TweenPlayer.TweenAnimation.SlideOutDown;

            switch (name)
            {
                case "slideDistance":
                    return IsSlide(type);
                case "popFromScale":
                    return type is TweenPlayer.TweenAnimation.PopIn or TweenPlayer.TweenAnimation.PopOut;
                case "punchStrength":
                case "punchVibrato":
                case "punchElasticity":
                    return type == TweenPlayer.TweenAnimation.PunchScale;
                case "autoAddCanvasGroup":
                    return type is TweenPlayer.TweenAnimation.FadeIn
                        or TweenPlayer.TweenAnimation.FadeOut
                        or TweenPlayer.TweenAnimation.PopIn
                        or TweenPlayer.TweenAnimation.PopOut;
                case "targetColor":
                    return type == TweenPlayer.TweenAnimation.ColorTo;
                case "rendererColorProperty":
                    return type == TweenPlayer.TweenAnimation.ColorTo;
                case "colorToggleWithInitial":
                    return type == TweenPlayer.TweenAnimation.ColorTo;
                case "movePosition":
                case "moveRelative":
                case "moveUseLocal":
                case "moveUseAnchoredPosition":
                case "moveCurve":
                    return type == TweenPlayer.TweenAnimation.Move;
                case "rotateEuler":
                case "rotateRelative":
                case "rotateUseLocal":
                case "rotateCurve":
                    return type == TweenPlayer.TweenAnimation.Rotate;
                case "pathAsset":
                case "pathClampToOrigin":
                case "pathEndAtOrigin":
                    return type == TweenPlayer.TweenAnimation.PathMove;
                case "tmpCharIndices":
                case "tmpCharUseVisibleIndex":
                case "tmpCharTargetScale":
                case "tmpCharHoldDuration":
                case "tmpCharReturnDuration":
                case "tmpCharReturnEase":
                case "tmpCharForceRefresh":
                    return type == TweenPlayer.TweenAnimation.TmpCharPulse;
                default:
                    return true;
            }
        }

        private static float GetLongPressHeight(SerializedProperty property)
        {
            SerializedProperty triggerProp = property.FindPropertyRelative("trigger");
            if (triggerProp == null || triggerProp.enumValueIndex != (int)TweenPlayer.TweenTrigger.OnLongPress)
            {
                return 0f;
            }

            SerializedObject root = property.serializedObject;
            SerializedProperty enableProp = root.FindProperty("enableLongPress");
            SerializedProperty durationProp = root.FindProperty("longPressDuration");
            SerializedProperty useUnscaledProp = root.FindProperty("longPressUseUnscaledTime");
            SerializedProperty blockProp = root.FindProperty("blockClickWhenLongPressed");
            SerializedProperty toggleProp = root.FindProperty("toggleImageOnLongPress");
            SerializedProperty targetProp = root.FindProperty("longPressTargetImage");
            SerializedProperty startHiddenProp = root.FindProperty("longPressTargetStartsHidden");
            SerializedProperty toggleGoProp = root.FindProperty("longPressToggleGameObject");

            if (enableProp == null || durationProp == null || useUnscaledProp == null || blockProp == null || toggleProp == null)
            {
                return 0f;
            }

            float height = EditorGUIUtility.singleLineHeight + VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(enableProp, true) + VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(durationProp, true) + VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(useUnscaledProp, true) + VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(blockProp, true) + VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(toggleProp, true) + VerticalSpacing;

            if (toggleProp.boolValue && targetProp != null && startHiddenProp != null && toggleGoProp != null)
            {
                height += EditorGUI.GetPropertyHeight(targetProp, true) + VerticalSpacing;
                height += EditorGUI.GetPropertyHeight(startHiddenProp, true) + VerticalSpacing;
                height += EditorGUI.GetPropertyHeight(toggleGoProp, true) + VerticalSpacing;
            }

            return height;
        }

        private static void DrawLongPressSettings(SerializedProperty property, ref Rect contentRect)
        {
            SerializedProperty triggerProp = property.FindPropertyRelative("trigger");
            if (triggerProp == null || triggerProp.enumValueIndex != (int)TweenPlayer.TweenTrigger.OnLongPress)
            {
                return;
            }

            SerializedObject root = property.serializedObject;
            SerializedProperty enableProp = root.FindProperty("enableLongPress");
            SerializedProperty durationProp = root.FindProperty("longPressDuration");
            SerializedProperty useUnscaledProp = root.FindProperty("longPressUseUnscaledTime");
            SerializedProperty blockProp = root.FindProperty("blockClickWhenLongPressed");
            SerializedProperty toggleProp = root.FindProperty("toggleImageOnLongPress");
            SerializedProperty targetProp = root.FindProperty("longPressTargetImage");
            SerializedProperty startHiddenProp = root.FindProperty("longPressTargetStartsHidden");
            SerializedProperty toggleGoProp = root.FindProperty("longPressToggleGameObject");

            if (enableProp == null || durationProp == null || useUnscaledProp == null || blockProp == null || toggleProp == null)
            {
                return;
            }

            EditorGUI.LabelField(contentRect, "Long Press", EditorStyles.boldLabel);
            contentRect.y += EditorGUIUtility.singleLineHeight + VerticalSpacing;

            float height = EditorGUI.GetPropertyHeight(enableProp, true);
            Rect fieldRect = new Rect(contentRect.x, contentRect.y, contentRect.width, height);
            EditorGUI.PropertyField(fieldRect, enableProp, new GUIContent("Enable Long Press"));
            contentRect.y += height + VerticalSpacing;

            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledScope(!enableProp.boolValue))
            {
                height = EditorGUI.GetPropertyHeight(durationProp, true);
                fieldRect = new Rect(contentRect.x, contentRect.y, contentRect.width, height);
                EditorGUI.PropertyField(fieldRect, durationProp);
                contentRect.y += height + VerticalSpacing;

                height = EditorGUI.GetPropertyHeight(useUnscaledProp, true);
                fieldRect = new Rect(contentRect.x, contentRect.y, contentRect.width, height);
                EditorGUI.PropertyField(fieldRect, useUnscaledProp);
                contentRect.y += height + VerticalSpacing;

                height = EditorGUI.GetPropertyHeight(blockProp, true);
                fieldRect = new Rect(contentRect.x, contentRect.y, contentRect.width, height);
                EditorGUI.PropertyField(fieldRect, blockProp);
                contentRect.y += height + VerticalSpacing;

                height = EditorGUI.GetPropertyHeight(toggleProp, true);
                fieldRect = new Rect(contentRect.x, contentRect.y, contentRect.width, height);
                EditorGUI.PropertyField(fieldRect, toggleProp);
                contentRect.y += height + VerticalSpacing;

                if (toggleProp.boolValue && targetProp != null && startHiddenProp != null && toggleGoProp != null)
                {
                    EditorGUI.indentLevel++;

                    height = EditorGUI.GetPropertyHeight(targetProp, true);
                    fieldRect = new Rect(contentRect.x, contentRect.y, contentRect.width, height);
                    EditorGUI.PropertyField(fieldRect, targetProp);
                    contentRect.y += height + VerticalSpacing;

                    height = EditorGUI.GetPropertyHeight(startHiddenProp, true);
                    fieldRect = new Rect(contentRect.x, contentRect.y, contentRect.width, height);
                    EditorGUI.PropertyField(fieldRect, startHiddenProp);
                    contentRect.y += height + VerticalSpacing;

                    height = EditorGUI.GetPropertyHeight(toggleGoProp, true);
                    fieldRect = new Rect(contentRect.x, contentRect.y, contentRect.width, height);
                    EditorGUI.PropertyField(fieldRect, toggleGoProp);
                    contentRect.y += height + VerticalSpacing;

                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.indentLevel--;
        }
        private static bool GetFoldout(SerializedProperty property)
        {
            if (!FoldoutStates.TryGetValue(property.propertyPath, out bool foldout))
            {
                foldout = true;
                FoldoutStates[property.propertyPath] = foldout;
            }

            return foldout;
        }

        private static void SetFoldout(SerializedProperty property, bool value)
        {
            FoldoutStates[property.propertyPath] = value;
        }

        private static bool GetSectionFoldout(SerializedProperty property, string section)
        {
            string key = $"{property.propertyPath}.{section}";
            if (!SectionFoldoutStates.TryGetValue(key, out bool foldout))
            {
                foldout = true;
                SectionFoldoutStates[key] = foldout;
            }

            return foldout;
        }

        private static void SetSectionFoldout(SerializedProperty property, string section, bool value)
        {
            string key = $"{property.propertyPath}.{section}";
            SectionFoldoutStates[key] = value;
        }
    }
#endif
}