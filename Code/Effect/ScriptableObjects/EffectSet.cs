using System;
using System.Collections.Generic;
using UnityEngine;
using Nonsliep.Core.Effect.SFX;
using Nonsliep.Core.Effect.VFX;


namespace Nonsliep.Core.Effect
{
    [CreateAssetMenu(fileName = "EffectSet", menuName = "FX/Effect Set", order = 10)]
    public class EffectSet : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public EffectType key;
            [Header("Direct Assignment (optional)")]
            public SFXAsset sfx;
            public VFXAsset vfx;
            public HapticPreset haptics;

        }

        [SerializeField] private List<Entry> entries = new();
        private Dictionary<EffectType, Entry> _cache;

        private void OnEnable()
        {
            BuildCache();
        }

        private void BuildCache()
        {
            _cache = new Dictionary<EffectType, Entry>();
            foreach (var e in entries)
            {
                if (e == null) continue;
                _cache[e.key] = e;
            }
        }

        public Entry Get(EffectType key)
        {
            if (_cache == null || _cache.Count != entries.Count) BuildCache();
            return _cache != null && _cache.TryGetValue(key, out var e) ? e : null;
        }
    }
}
