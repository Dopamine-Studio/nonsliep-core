using UnityEngine;
using UnityEngine.Audio;

namespace Nonsliep.Core.Effect.SFX
{
    // Minimal fire-and-forget SFX player. No pooling or voice limits.
    internal class SFXPlayer : MonoBehaviour
    {
        public void Play(Nonsliep.Core.Effect.SFX.SFXAsset sfx, Transform anchor = null, Vector3? pos = null, float? overrideVolume = null)
        {
            if (sfx == null) return;
            var clip = sfx.PickClip();
            if (clip == null) return;

            var go = new GameObject($"SFX_{clip.name}");
            var src = go.AddComponent<AudioSource>();
            ConfigureSource(src, sfx, anchor, pos, sfx.group, 1f, 1f, clip, overrideVolume);

            src.Play();
            if (!src.loop)
            {
                Destroy(go, Mathf.Max(0.01f, src.clip.length + 0.1f));
            }
        }

        public void Play(Nonsliep.Core.Effect.SFX.SFXAsset sfx, Transform anchor, Vector3? pos, AudioMixerGroup groupOverride, float volumeMul, float pitchMul)
        {
            if (sfx == null) return;
            var clip = sfx.PickClip();
            if (clip == null) return;

            var go = new GameObject($"SFX_{clip.name}");
            var src = go.AddComponent<AudioSource>();
            var group = groupOverride != null ? groupOverride : sfx.group;
            ConfigureSource(src, sfx, anchor, pos, group, volumeMul, pitchMul, clip, null);

            src.Play();
            if (!src.loop)
            {
                Destroy(go, Mathf.Max(0.01f, src.clip.length + 0.1f));
            }
        }

        private void ConfigureSource(AudioSource src, Nonsliep.Core.Effect.SFX.SFXAsset sfx, Transform anchor, Vector3? pos, AudioMixerGroup group, float volumeMul, float pitchMul, AudioClip clip, float? overrideVolume)
        {
            if (group == null)
            {
                Debug.LogWarning($"[SfxPlayer] SFX '{sfx.name}' has no MixerGroup assigned. Skipping.");
                Destroy(src.gameObject);
                return;
            }

            src.outputAudioMixerGroup = group;
            src.playOnAwake = false;
            src.clip = clip;
            src.loop = sfx.loop;

            // Spatial setup
            if (sfx.spatial)
            {
                src.spatialBlend = Mathf.Clamp01(sfx.spatialBlend);
                src.dopplerLevel = 0f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.maxDistance = sfx.maxDistance;
                if (anchor != null)
                {
                    src.transform.SetParent(anchor, false);
                    src.transform.localPosition = Vector3.zero;
                }
                else
                {
                    src.transform.SetParent(transform, false);
                    if (pos.HasValue) src.transform.position = pos.Value;
                }
            }
            else
            {
                src.spatialBlend = 0f;
                src.dopplerLevel = 0f;
                src.transform.SetParent(anchor != null ? anchor : transform, false);
                src.transform.localPosition = Vector3.zero;
            }

            // Volume/Pitch
            float baseVol = overrideVolume.HasValue ? Mathf.Clamp01(overrideVolume.Value) : Mathf.Clamp01(sfx.volume);
            src.volume = Mathf.Clamp01(baseVol * Mathf.Max(0f, volumeMul));
            float randPitch = Random.Range(sfx.pitchRange.x, sfx.pitchRange.y);
            src.pitch = randPitch * (pitchMul == 0f ? 1f : pitchMul);

            // Random start offset
            if (sfx.randomStartTime > 0f && src.clip != null && src.clip.length > 0.01f)
            {
                float offs = Random.Range(0f, Mathf.Clamp01(sfx.randomStartTime)) * src.clip.length;
                src.time = Mathf.Clamp(offs, 0f, Mathf.Max(0f, src.clip.length - 0.01f));
            }
        }
    }
}
