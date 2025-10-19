using UnityEngine;
using Nonsliep.Core.Effect.Haptics;

namespace Nonsliep.Core.Effect
{
    // Routes effect assets to concrete players. One per app.
    public class EffectRouter : Scene.SceneSingleton<EffectRouter>
    {
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] internal SFX.SFXPlayer sfxPlayer;
        [SerializeField] internal VFX.VFXPlayer vfxPlayer;
        [SerializeField] internal IHapticsDriver hapticsDriver;

        public static EffectRouter CreateOrGet()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var go = new GameObject("EffectRouter");
            return go.AddComponent<EffectRouter>();
        }

        protected override bool ShouldPersistAcrossScenes => dontDestroyOnLoad;

        protected override void OnSingletonReady()
        {
            Bootstrap();
        }

        private void Bootstrap()
        {
            if (sfxPlayer == null)
            {
                sfxPlayer = gameObject.GetComponent<SFX.SFXPlayer>() ?? gameObject.AddComponent<SFX.SFXPlayer>();
            }

            if (vfxPlayer == null)
            {
                vfxPlayer = gameObject.GetComponent<VFX.VFXPlayer>() ?? gameObject.AddComponent<VFX.VFXPlayer>();
            }

            if (hapticsDriver == null)
            {
#if UNITY_IOS && !UNITY_EDITOR
                hapticsDriver = new IOSCoreHapticsDriver();
#else
                hapticsDriver = new MobileHapticsDriver(this);
#endif
            }

            // Mixer settings are bound by UI Settings SoundSettingsApplier.
        }
    }
}

