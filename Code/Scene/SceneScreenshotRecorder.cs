using System;
#if UNITY_EDITOR
using System.IO;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Nonsliep.Core.Scene
{
    /// <summary>
    /// Scene-scoped screenshot helper that wraps Unity Recorder in a simple singleton.
    /// Works in the editor only; safely no-ops in player builds.
    /// </summary>
    public sealed class SceneScreenshotRecorder : SceneSingleton<SceneScreenshotRecorder>
    {
#if UNITY_EDITOR
        [Header("Output")]
        [SerializeField] string outputFolderName = "SampleRecordings";

        [Header("Screenshot")]
        [SerializeField] string outputFilePrefix = "image_";
        [SerializeField] bool matchGameViewResolution = true;
        [SerializeField] int outputWidth = 1920;
        [SerializeField] int outputHeight = 1080;
        [SerializeField] KeyCode captureKey = KeyCode.F12;

        [Header("Video")]
        [SerializeField] bool enableVideoRecording = true;
        [SerializeField] string videoFilePrefix = "video_";
        [SerializeField] KeyCode videoToggleKey = KeyCode.F11;
        [SerializeField] float videoFrameRate = 30f;
        [SerializeField] bool captureVideoAudio = true;

        RecorderController screenshotRecorderController;
        RecorderController videoRecorderController;
        bool isScreenshotInitialized;
        bool isVideoInitialized;
        bool isVideoRecording;
#endif

        protected override void OnSingletonReady()
        {
#if UNITY_EDITOR
            InitializeScreenshotRecorder();
            if (enableVideoRecording)
            {
                InitializeVideoRecorder();
            }
#else
            enabled = false;
#endif
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying || !isScreenshotInitialized)
            {
                return;
            }

            if (ShouldTriggerCapture())
            {
                CaptureScreenshot();
            }

            if (enableVideoRecording && ShouldToggleVideo())
            {
                ToggleVideoRecording();
            }
#endif
        }

        public void CaptureScreenshot()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[SceneScreenshotRecorder] Screenshot capture works in Play Mode only.");
                return;
            }

            if (!EnsureScreenshotRecorderReady())
            {
                Debug.LogWarning("[SceneScreenshotRecorder] Recorder failed to initialize.");
                return;
            }

            screenshotRecorderController.PrepareRecording();
            screenshotRecorderController.StartRecording();
#else
            Debug.LogWarning("[SceneScreenshotRecorder] Screenshot capture is editor-only.");
#endif
        }

#if UNITY_EDITOR
        public bool ToggleVideoRecording()
        {
            if (!enableVideoRecording)
            {
                Debug.LogWarning("[SceneScreenshotRecorder] Video recording disabled.");
                return false;
            }

            if (isVideoRecording)
            {
                StopVideoRecording();
                return false;
            }

            return StartVideoRecording();
        }

        public bool StartVideoRecording()
        {
            if (!enableVideoRecording)
            {
                Debug.LogWarning("[SceneScreenshotRecorder] Video recording disabled.");
                return false;
            }

            if (isVideoRecording)
            {
                Debug.LogWarning("[SceneScreenshotRecorder] Video recording already running.");
                return false;
            }

            if (!Application.isPlaying)
            {
                Debug.LogWarning("[SceneScreenshotRecorder] Video recording works in Play Mode only.");
                return false;
            }

            if (!EnsureVideoRecorderReady())
            {
                Debug.LogWarning("[SceneScreenshotRecorder] Video recorder failed to initialize.");
                return false;
            }

            videoRecorderController.PrepareRecording();
            var started = videoRecorderController.StartRecording();
            isVideoRecording = started && videoRecorderController.IsRecording();
            return started;
        }

        public void StopVideoRecording()
        {
            if (!isVideoRecording || videoRecorderController == null)
            {
                return;
            }

            videoRecorderController.StopRecording();
            isVideoRecording = false;
        }

        bool ShouldTriggerCapture()
        {
            return WasKeyReleased(captureKey);
        }

        bool ShouldToggleVideo()
        {
            return WasKeyReleased(videoToggleKey);
        }

        bool WasKeyReleased(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return WasKeyReleasedInputSystem(keyCode);
#else
            return Input.GetKeyUp(keyCode);
#endif
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        bool WasKeyReleasedInputSystem(KeyCode keyCode)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            if (!TryGetInputSystemKey(keyCode, out var targetKey))
            {
                return false;
            }

            var keyControl = keyboard[targetKey];
            return keyControl != null && keyControl.wasReleasedThisFrame;
        }

        bool TryGetInputSystemKey(KeyCode keyCode, out Key mappedKey)
        {
            if (Enum.TryParse(keyCode.ToString(), out mappedKey))
            {
                return true;
            }

            switch (keyCode)
            {
                case KeyCode.Alpha0: mappedKey = Key.Digit0; return true;
                case KeyCode.Alpha1: mappedKey = Key.Digit1; return true;
                case KeyCode.Alpha2: mappedKey = Key.Digit2; return true;
                case KeyCode.Alpha3: mappedKey = Key.Digit3; return true;
                case KeyCode.Alpha4: mappedKey = Key.Digit4; return true;
                case KeyCode.Alpha5: mappedKey = Key.Digit5; return true;
                case KeyCode.Alpha6: mappedKey = Key.Digit6; return true;
                case KeyCode.Alpha7: mappedKey = Key.Digit7; return true;
                case KeyCode.Alpha8: mappedKey = Key.Digit8; return true;
                case KeyCode.Alpha9: mappedKey = Key.Digit9; return true;
                default:
                    mappedKey = Key.None;
                    return false;
            }
        }
#endif

        void InitializeScreenshotRecorder()
        {
            if (isScreenshotInitialized)
            {
                return;
            }

            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            screenshotRecorderController = new RecorderController(controllerSettings);

            var mediaOutputFolder = EnsureOutputDirectory();

            var imageRecorder = ScriptableObject.CreateInstance<ImageRecorderSettings>();
            imageRecorder.name = "Scene Screenshot Recorder";
            imageRecorder.Enabled = true;
            imageRecorder.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
            imageRecorder.CaptureAlpha = false;
            imageRecorder.OutputFile = Path.Combine(mediaOutputFolder, outputFilePrefix) + DefaultWildcard.Take;
            imageRecorder.imageInputSettings = CreateGameViewInputSettings();

            controllerSettings.AddRecorderSettings(imageRecorder);
            controllerSettings.SetRecordModeToSingleFrame(0);

            isScreenshotInitialized = true;
        }

        void InitializeVideoRecorder()
        {
            if (isVideoInitialized)
            {
                return;
            }

            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            videoRecorderController = new RecorderController(controllerSettings);
            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRate = Mathf.Max(1f, videoFrameRate);
            controllerSettings.ExitPlayMode = false;
            controllerSettings.CapFrameRate = false;

            var mediaOutputFolder = EnsureOutputDirectory();

            var movieRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movieRecorder.name = "Scene Video Recorder";
            movieRecorder.Enabled = true;
            movieRecorder.CaptureAlpha = false;
            movieRecorder.CaptureAudio = captureVideoAudio;
            movieRecorder.AudioInputSettings.PreserveAudio = captureVideoAudio;
            movieRecorder.FrameRate = Mathf.Max(1f, videoFrameRate);
            movieRecorder.OutputFile = Path.Combine(mediaOutputFolder, videoFilePrefix) + DefaultWildcard.Take;
            movieRecorder.ImageInputSettings = CreateGameViewInputSettings();
            movieRecorder.RecordMode = RecordMode.Manual;
            movieRecorder.FrameRatePlayback = FrameRatePlayback.Constant;
            movieRecorder.CapFrameRate = false;

            controllerSettings.AddRecorderSettings(movieRecorder);

            isVideoInitialized = true;
        }

        GameViewInputSettings CreateGameViewInputSettings()
        {
            var settings = new GameViewInputSettings();

            if (!matchGameViewResolution)
            {
                settings.OutputWidth = Mathf.Max(1, outputWidth);
                settings.OutputHeight = Mathf.Max(1, outputHeight);
            }

            return settings;
        }

        string EnsureOutputDirectory()
        {
            var mediaOutputFolder = Path.Combine(Application.dataPath, "..", outputFolderName);
            if (!Directory.Exists(mediaOutputFolder))
            {
                Directory.CreateDirectory(mediaOutputFolder);
            }

            return mediaOutputFolder;
        }

        bool EnsureScreenshotRecorderReady()
        {
            if (!isScreenshotInitialized || screenshotRecorderController == null)
            {
                InitializeScreenshotRecorder();
            }

            return isScreenshotInitialized && screenshotRecorderController != null;
        }

        bool EnsureVideoRecorderReady()
        {
            if (!enableVideoRecording)
            {
                return false;
            }

            if (!isVideoInitialized || videoRecorderController == null)
            {
                InitializeVideoRecorder();
            }

            return isVideoInitialized && videoRecorderController != null;
        }

        protected override void OnSingletonDestroyed()
        {
            StopVideoRecording();
            screenshotRecorderController = null;
            videoRecorderController = null;
            isScreenshotInitialized = false;
            isVideoInitialized = false;
            isVideoRecording = false;
        }
#else
        public bool ToggleVideoRecording() => false;
        public bool StartVideoRecording() => false;
        public void StopVideoRecording() { }
#endif
    }
}