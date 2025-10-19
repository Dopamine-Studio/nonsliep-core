using UnityEditor;

namespace Nonsliep.Core.Scene.Editor
{
    [CustomEditor(typeof(SceneNavigator))]
    public class SceneNavigatorEditor : UnityEditor.Editor
    {
        private SerializedProperty _allowConcurrentLoadsProp;
        private SerializedProperty _transitionModeProp;
        private SerializedProperty _fadeCanvasGroupProp;
        private SerializedProperty _fadeOutDurationProp;
        private SerializedProperty _fadeInDurationProp;
        private SerializedProperty _fadeCurveProp;
        private SerializedProperty _fadeIgnoreTimeScaleProp;
        private SerializedProperty _transitionManagerProp;
        private SerializedProperty _defaultTransitionSettingsProp;
        private SerializedProperty _transitionStartDelayProp;

        private void OnEnable()
        {
            _allowConcurrentLoadsProp = serializedObject.FindProperty("allowConcurrentLoads");
            _transitionModeProp = serializedObject.FindProperty("transitionMode");
            _fadeCanvasGroupProp = serializedObject.FindProperty("fadeCanvasGroup");
            _fadeOutDurationProp = serializedObject.FindProperty("fadeOutDuration");
            _fadeInDurationProp = serializedObject.FindProperty("fadeInDuration");
            _fadeCurveProp = serializedObject.FindProperty("fadeCurve");
            _fadeIgnoreTimeScaleProp = serializedObject.FindProperty("fadeIgnoreTimeScale");
            _transitionManagerProp = serializedObject.FindProperty("transitionManager");
            _defaultTransitionSettingsProp = serializedObject.FindProperty("defaultTransitionSettings");
            _transitionStartDelayProp = serializedObject.FindProperty("transitionStartDelay");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((SceneNavigator)target), typeof(SceneNavigator), false);
            }

            EditorGUILayout.PropertyField(_allowConcurrentLoadsProp);
            EditorGUILayout.PropertyField(_transitionModeProp);

            var mode = (SceneNavigator.TransitionMode)_transitionModeProp.enumValueIndex;
            switch (mode)
            {
                case SceneNavigator.TransitionMode.Fade:
                    EditorGUILayout.PropertyField(_fadeCanvasGroupProp);
                    EditorGUILayout.PropertyField(_fadeOutDurationProp);
                    EditorGUILayout.PropertyField(_fadeInDurationProp);
                    EditorGUILayout.PropertyField(_fadeCurveProp);
                    EditorGUILayout.PropertyField(_fadeIgnoreTimeScaleProp);
                    break;
                case SceneNavigator.TransitionMode.EasyTransitions:
                    EditorGUILayout.PropertyField(_transitionManagerProp);
                    EditorGUILayout.PropertyField(_defaultTransitionSettingsProp);
                    EditorGUILayout.PropertyField(_transitionStartDelayProp);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}