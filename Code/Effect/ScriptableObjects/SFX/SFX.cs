using UnityEngine;
using UnityEngine.Audio;

namespace Nonsliep.Core.Effect.SFX
{
    [CreateAssetMenu(fileName = "SFXAsset", menuName = "FX/SFX", order = 1)]
    public class SFXAsset : ScriptableObject
    {
        [Header("Clip Variations")]
        public AudioClip[] clips;

        [Header("Routing (Required)")]
        public AudioMixerGroup group; // must be assigned

        [Header("Playback")] 
        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 pitchRange = new Vector2(1f, 1f);
        public bool loop = false;
        public bool spatial = false; // false for UI; if true, 3D settings apply
        public float spatialBlend = 1f; // 0..1 when spatial
        public float maxDistance = 20f;

        [Header("Limits")]
        public float cooldown = 0.02f;  // prevent spam
        public int maxVoices = 8;        // per-effect voice limit

        [Header("Randomization")] 
        [Range(0f, 1f)] public float randomStartTime = 0f; // 0=no, 1=up to clip length

        public AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0) return null;
            if (clips.Length == 1) return clips[0];
            int idx = Random.Range(0, clips.Length);
            return clips[idx];
        }

        private void OnValidate()
        {
            if (group == null)
            {
                Debug.LogWarning($"[SFX] MixerGroup is not assigned on {name}. This SFX will not play.", this);
            }
        }
    }
}

