using UnityEngine;

namespace Nonsliep.Core.Effect.VFX
{
    [CreateAssetMenu(fileName = "VFXAsset", menuName = "FX/VFX", order = 2)]
    public class VFXAsset : ScriptableObject
    {
        [Header("Prefab to spawn (ParticleSystem/VisualEffect/GameObject)")]
        public GameObject prefab;

        [Header("Lifetime")]
        public float fallbackLifetime = 1.5f; // used if we can't auto-detect

        [Header("Pooling")]
        public int prewarm = 0; // optional: initial instances
        public int maxPool = 16;
    }
}


