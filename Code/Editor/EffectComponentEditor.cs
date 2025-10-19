using UnityEditor;
using UnityEngine;

namespace Nonsliep.Core.Effect.Editor
{
    [CustomEditor(typeof(EffectComponent))]
    public class EffectComponentEditor : UnityEditor.Editor
    {
        private bool _showOverrides = true;
        private bool _showLifecycle = true;
        private bool _showUi = true;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Effect Set", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseSet"));

            var overridesProp = serializedObject.FindProperty("overrides");
            _showOverrides = EditorGUILayout.Foldout(_showOverrides, "Overrides", true);
            if (_showOverrides)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawOverridesList(overridesProp);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Triggers", EditorStyles.boldLabel);

            _showLifecycle = EditorGUILayout.BeginFoldoutHeaderGroup(_showLifecycle, "Lifecycle Triggers");
            if (_showLifecycle)
            {
                var lifecycleProp = serializedObject.FindProperty("lifecycle");
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawLifecycleSection(lifecycleProp);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _showUi = EditorGUILayout.BeginFoldoutHeaderGroup(_showUi, "UI Triggers");
            if (_showUi)
            {
                var uiProp = serializedObject.FindProperty("uiTriggers");
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawUiSection(uiProp);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOverridesList(SerializedProperty overridesProp)
        {
            if (overridesProp == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Count", GUILayout.MaxWidth(50f));
            EditorGUILayout.PropertyField(overridesProp.FindPropertyRelative("Array.size"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < overridesProp.arraySize; i++)
            {
                var element = overridesProp.GetArrayElementAtIndex(i);
                if (element == null) continue;

                EditorGUILayout.BeginVertical(GUI.skin.box);
                bool removed = DrawOverrideElement(element, overridesProp, i);
                EditorGUILayout.EndVertical();
                if (removed)
                {
                    i--;
                    continue;
                }
            }

            if (GUILayout.Button("Add Override"))
            {
                overridesProp.arraySize++;
                var element = overridesProp.GetArrayElementAtIndex(overridesProp.arraySize - 1);
                if (element != null)
                {
                    var keyProp = element.FindPropertyRelative("key");
                    if (keyProp != null) keyProp.enumValueIndex = 0;
                    var placementProp = element.FindPropertyRelative("placement");
                    if (placementProp != null)
                    {
                        placementProp.FindPropertyRelative("mode").enumValueIndex = (int)EffectComponent.AttachmentMode.Self;
                        placementProp.FindPropertyRelative("follow").boolValue = true;
                        var targetsProp = placementProp.FindPropertyRelative("targets");
                        if (targetsProp != null) targetsProp.arraySize = 0;
                    }
                }
            }
        }

        private bool DrawOverrideElement(SerializedProperty element, SerializedProperty collection, int index)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(element.FindPropertyRelative("key"), GUIContent.none);
            if (GUILayout.Button("X", GUILayout.Width(20f)))
            {
                collection.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                return true;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(element.FindPropertyRelative("sfx"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("vfx"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("haptics"));

            var placementProp = element.FindPropertyRelative("placement");
            if (placementProp != null)
            {
                DrawPlacementConfig(placementProp);
            }
            EditorGUI.indentLevel--;
            return false;
        }

        private void DrawPlacementConfig(SerializedProperty placementProp)
        {
            EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            var modeProp = placementProp.FindPropertyRelative("mode");
            var followProp = placementProp.FindPropertyRelative("follow");
            var targetsProp = placementProp.FindPropertyRelative("targets");

            if (modeProp != null)
            {
                EditorGUILayout.PropertyField(modeProp);
            }

            if (followProp != null)
            {
                EditorGUILayout.PropertyField(followProp);
            }

            if (modeProp != null && targetsProp != null)
            {
                if ((EffectComponent.AttachmentMode)modeProp.enumValueIndex == EffectComponent.AttachmentMode.Custom)
                {
                    EditorGUILayout.PropertyField(targetsProp, new GUIContent("Targets"), true);
                }
            }
            EditorGUI.indentLevel--;
        }

        private static void DrawLifecycleSection(SerializedProperty lifecycleProp)
        {
            DrawLifecycleTrigger(lifecycleProp.FindPropertyRelative("onAwake"), "On Awake");
            DrawLifecycleTrigger(lifecycleProp.FindPropertyRelative("onEnable"), "On Enable");
            DrawLifecycleTrigger(lifecycleProp.FindPropertyRelative("onDisable"), "On Disable");
            DrawLifecycleTrigger(lifecycleProp.FindPropertyRelative("onDestroy"), "On Destroy");
        }

        private static void DrawLifecycleTrigger(SerializedProperty triggerProp, string label)
        {
            var enabledProp = triggerProp.FindPropertyRelative("enabled");
            enabledProp.boolValue = EditorGUILayout.ToggleLeft(label, enabledProp.boolValue, EditorStyles.boldLabel);
            if (enabledProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(triggerProp.FindPropertyRelative("key"), GUIContent.none);
                EditorGUI.indentLevel--;
            }
        }

        private static void DrawUiSection(SerializedProperty uiProp)
        {
            DrawButtonTrigger(uiProp.FindPropertyRelative("button"));
            DrawToggleTrigger(uiProp.FindPropertyRelative("toggle"));
            DrawSliderTrigger(uiProp.FindPropertyRelative("slider"));
            DrawPointerTrigger(uiProp.FindPropertyRelative("pointer"));
        }

        private static void DrawButtonTrigger(SerializedProperty buttonProp)
        {
            var enabled = buttonProp.FindPropertyRelative("enabled");
            enabled.boolValue = EditorGUILayout.ToggleLeft("Button", enabled.boolValue, EditorStyles.boldLabel);
            if (!enabled.boolValue) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(buttonProp.FindPropertyRelative("playOnClick"));
            EditorGUILayout.PropertyField(buttonProp.FindPropertyRelative("clickEffect"));
            EditorGUILayout.PropertyField(buttonProp.FindPropertyRelative("playOnPointerDown"));
            EditorGUILayout.PropertyField(buttonProp.FindPropertyRelative("pointerDownEffect"));
            EditorGUILayout.PropertyField(buttonProp.FindPropertyRelative("playOnPointerUp"));
            EditorGUILayout.PropertyField(buttonProp.FindPropertyRelative("pointerUpEffect"));
            EditorGUI.indentLevel--;
        }

        private static void DrawToggleTrigger(SerializedProperty toggleProp)
        {
            var enabled = toggleProp.FindPropertyRelative("enabled");
            enabled.boolValue = EditorGUILayout.ToggleLeft("Toggle", enabled.boolValue, EditorStyles.boldLabel);
            if (!enabled.boolValue) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(toggleProp.FindPropertyRelative("playOnOn"));
            EditorGUILayout.PropertyField(toggleProp.FindPropertyRelative("onEffect"));
            EditorGUILayout.PropertyField(toggleProp.FindPropertyRelative("playOnOff"));
            EditorGUILayout.PropertyField(toggleProp.FindPropertyRelative("offEffect"));
            EditorGUI.indentLevel--;
        }

        private static void DrawSliderTrigger(SerializedProperty sliderProp)
        {
            var enabled = sliderProp.FindPropertyRelative("enabled");
            enabled.boolValue = EditorGUILayout.ToggleLeft("Slider", enabled.boolValue, EditorStyles.boldLabel);
            if (!enabled.boolValue) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(sliderProp.FindPropertyRelative("playOnPointerDown"));
            EditorGUILayout.PropertyField(sliderProp.FindPropertyRelative("pointerDownEffect"));
            EditorGUILayout.PropertyField(sliderProp.FindPropertyRelative("playOnPointerUp"));
            EditorGUILayout.PropertyField(sliderProp.FindPropertyRelative("pointerUpEffect"));
            EditorGUILayout.PropertyField(sliderProp.FindPropertyRelative("requireValueChangeBeforePointerUp"));
            EditorGUILayout.PropertyField(sliderProp.FindPropertyRelative("playOnValueChanged"));
            EditorGUILayout.PropertyField(sliderProp.FindPropertyRelative("changeEffect"));
            EditorGUI.indentLevel--;
        }

        private static void DrawPointerTrigger(SerializedProperty pointerProp)
        {
            var enabled = pointerProp.FindPropertyRelative("enabled");
            enabled.boolValue = EditorGUILayout.ToggleLeft("Pointer", enabled.boolValue, EditorStyles.boldLabel);
            if (!enabled.boolValue) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(pointerProp.FindPropertyRelative("playOnPointerDown"));
            EditorGUILayout.PropertyField(pointerProp.FindPropertyRelative("pointerDownEffect"));
            EditorGUILayout.PropertyField(pointerProp.FindPropertyRelative("playOnPointerUp"));
            EditorGUILayout.PropertyField(pointerProp.FindPropertyRelative("pointerUpEffect"));
            EditorGUILayout.PropertyField(pointerProp.FindPropertyRelative("requireValueChangeBeforePointerUp"));
            EditorGUILayout.PropertyField(pointerProp.FindPropertyRelative("playOnDragEnd"));
            EditorGUILayout.PropertyField(pointerProp.FindPropertyRelative("pointerDragEndEffect"));
            EditorGUILayout.PropertyField(pointerProp.FindPropertyRelative("requireValueChangeBeforeDragEnd"));
            EditorGUI.indentLevel--;
        }
    }
}
