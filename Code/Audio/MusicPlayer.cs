using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace Nonsliep.Core.Audio
{
    [DisallowMultipleComponent]
    public class MusicPlayer : Scene.SceneSingleton<MusicPlayer>
    {
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool autoPlayOnStart = true;
        [SerializeField] private float startFadeDuration = 0f;
        [SerializeField, Range(0f, 1f)] private float defaultVolume = 1f;
        [SerializeField] private AudioClip startClip;
        [SerializeField] private AudioMixerGroup defaultOutputGroup;

        private AudioSource musicSource;
        private Coroutine _fadeRoutine;

        protected override bool ShouldPersistAcrossScenes => dontDestroyOnLoad;

        protected override void OnSingletonReady()
        {
            EnsureSource();
        }

        private void Start()
        {
            TryPlayStartClip();
        }

        private void TryPlayStartClip()
        {
            if (autoPlayOnStart && startClip != null)
            {
                Play(startClip, loop: true, fadeDuration: startFadeDuration, targetVolume: defaultVolume, outputGroup: defaultOutputGroup);
            }
        }

        public void Play(AudioClip clip, bool loop = true, float fadeDuration = 0f, float targetVolume = -1f, AudioMixerGroup outputGroup = null)
        {
            if (clip == null)
            {
                Stop(fadeDuration);
                return;
            }

            EnsureSource();

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            musicSource.loop = loop;
            musicSource.clip = clip;

            var group = outputGroup != null ? outputGroup : defaultOutputGroup;
            if (group != null)
            {
                musicSource.outputAudioMixerGroup = group;
            }

            float volume = targetVolume >= 0f ? Mathf.Clamp01(targetVolume) : defaultVolume;

            if (fadeDuration <= 0f)
            {
                musicSource.volume = volume;
                musicSource.Play();
            }
            else
            {
                musicSource.volume = 0f;
                musicSource.Play();
                _fadeRoutine = StartCoroutine(CoFadeTo(volume, fadeDuration));
            }
        }

        public void Stop(float fadeDuration = 0f)
        {
            if (musicSource == null || !musicSource.isPlaying)
            {
                return;
            }

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            if (fadeDuration <= 0f)
            {
                musicSource.Stop();
                return;
            }

            _fadeRoutine = StartCoroutine(CoFadeTo(0f, fadeDuration, stopAfter: true));
        }

        public void SetVolume(float volume, float fadeDuration = 0f)
        {
            EnsureSource();
            volume = Mathf.Clamp01(volume);

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            if (fadeDuration <= 0f)
            {
                musicSource.volume = volume;
            }
            else
            {
                _fadeRoutine = StartCoroutine(CoFadeTo(volume, fadeDuration));
            }
        }

        public bool IsPlaying => musicSource != null && musicSource.isPlaying;

        private void EnsureSource()
        {
            if (musicSource != null)
            {
                return;
            }

            musicSource = GetComponent<AudioSource>();
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }

            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = Mathf.Clamp01(defaultVolume);

            if (defaultOutputGroup != null)
            {
                musicSource.outputAudioMixerGroup = defaultOutputGroup;
            }
        }

        private IEnumerator CoFadeTo(float targetVolume, float duration, bool stopAfter = false)
        {
            targetVolume = Mathf.Clamp01(targetVolume);
            float startVolume = musicSource.volume;
            float time = 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = duration > 0f ? time / duration : 1f;
                musicSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
                yield return null;
            }

            musicSource.volume = targetVolume;

            if (stopAfter)
            {
                musicSource.Stop();
            }

            _fadeRoutine = null;
        }
    }
}
