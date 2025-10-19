using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Coffee.UIExtensions;

namespace Nonsliep.Core.Effect.VFX
{
    internal class VFXPlayer : MonoBehaviour
    {
        private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();

        public void Play(VFXAsset effect, Transform anchor = null, Vector3? pos = null)
        {
            if (effect == null || effect.prefab == null) return;
            var go = GetOrCreate(effect);
            if (anchor != null)
            {
                go.transform.SetParent(anchor, false);
                go.transform.localPosition = pos.HasValue ? pos.Value : Vector3.zero;
            }
            else
            {
                go.transform.SetParent(transform, false);
                if (pos.HasValue) go.transform.position = pos.Value;
            }

            RefreshUiParticle(go, anchor);
            go.SetActive(true);
            var releaser = go.GetComponent<VfxAutoRelease>();
            if (releaser == null) releaser = go.AddComponent<VfxAutoRelease>();
            releaser.Setup(this, effect);
            releaser.Begin();
        }

        private static void RefreshUiParticle(GameObject instance, Transform anchor)
        {
            if (instance != null)
            {
                var uiParticles = instance.GetComponentsInChildren<UIParticle>(true);
                if (uiParticles != null && uiParticles.Length > 0)
                {
                    for (int i = 0; i < uiParticles.Length; i++)
                    {
                        uiParticles[i]?.RefreshParticles();
                    }
                    return;
                }
            }

            if (anchor == null) return;

            var anchorParticles = anchor.GetComponentsInChildren<UIParticle>(true);
            if (anchorParticles == null || anchorParticles.Length == 0)
            {
                anchorParticles = anchor.GetComponentsInParent<UIParticle>(true);
            }

            if (anchorParticles != null)
            {
                for (int i = 0; i < anchorParticles.Length; i++)
                {
                    anchorParticles[i]?.RefreshParticles();
                }
            }
        }

        private GameObject GetOrCreate(VFXAsset effect)
        {
            if (!_pools.TryGetValue(effect.prefab, out var q))
            {
                q = new Queue<GameObject>();
                _pools[effect.prefab] = q;
            }

            while (q.Count > 0)
            {
                var obj = q.Dequeue();
                if (obj != null) return obj;
            }

            var go = Instantiate(effect.prefab);
            go.name = effect.prefab.name + "_VFX";
            go.SetActive(false);
            return go;
        }

        internal void Release(VFXAsset effect, GameObject instance)
        {
            if (instance == null) return;
            instance.SetActive(false);
            instance.transform.SetParent(transform, false);
            if (!_pools.TryGetValue(effect.prefab, out var q))
            {
                q = new Queue<GameObject>();
                _pools[effect.prefab] = q;
            }
            q.Enqueue(instance);
        }
    }

    internal class VfxAutoRelease : MonoBehaviour
    {
        private VFXPlayer _player;
        private VFXAsset _effect;
        private ParticleSystem[] _particles;
        private float _fallbackLifetime;

        public void Setup(VFXPlayer player, VFXAsset effect)
        {
            _player = player;
            _effect = effect;
            _particles = GetComponentsInChildren<ParticleSystem>(true);
            _fallbackLifetime = Mathf.Max(0.01f, effect.fallbackLifetime);
        }

        public void Begin()
        {
            StopAllCoroutines();
            // Restart particles
            if (_particles != null && _particles.Length > 0)
            {
                foreach (var p in _particles)
                {
                    p.Clear(true);
                    p.Simulate(0f, true, true);
                    p.Play(true);
                }
                StartCoroutine(CoWatchParticles());
            }
            else
            {
                StartCoroutine(CoFallback());
            }
        }

        private IEnumerator CoWatchParticles()
        {
            // Wait while any particle is alive
            while (true)
            {
                bool anyAlive = false;
                foreach (var p in _particles)
                {
                    if (p != null && p.IsAlive(true)) { anyAlive = true; break; }
                }
                if (!anyAlive) break;
                yield return null;
            }
            _player.Release(_effect, gameObject);
        }

        private IEnumerator CoFallback()
        {
            yield return new WaitForSeconds(_fallbackLifetime);
            _player.Release(_effect, gameObject);
        }
    }
}
