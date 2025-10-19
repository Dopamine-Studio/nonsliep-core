using UnityEngine;
using UnityEngine.Audio;
using Nonsliep.Core.Effect.SFX;
using Nonsliep.Core.Effect.VFX;

namespace Nonsliep.Core.Effect
{
    public static class Effects
    {
        public static bool IsReady => EffectRouter.Instance != null;

        // No group lookup helpers; SFX assets carry their own AudioMixerGroup.

        // Require context for audio playback
        public static void PlayAudio(SFXAsset sfx, in EffectContext ctx)
        {
            var rt = EffectRouter.CreateOrGet();
            Play(sfx, ctx);
        }

        // UI audio via context (typically non-spatial)
        public static void PlayUiAudio(SFXAsset sfx, in EffectContext ctx)
        {
            var rt = EffectRouter.CreateOrGet();
            Play(sfx, ctx);
        }

        public static void PlayVfx(VFXAsset effect, Transform anchor = null, Vector3? pos = null)
        {
            var rt = EffectRouter.CreateOrGet();
            rt.vfxPlayer.Play(effect, anchor, pos);
        }

        public static void PlayHaptics(HapticPreset preset, float scale = 1f)
        {
            var rt = EffectRouter.CreateOrGet();
            rt.hapticsDriver.Play(preset, scale);
        }

        // New overloads that accept EffectContext for per-channel overrides
        public static void Play(SFXAsset sfx, in EffectContext ctx)
        {
            if (sfx == null) return;
            var rt = EffectRouter.CreateOrGet();
            // Compute anchor/pos priority: SFX-specific > common > null
            Transform anchor = null;
            Vector3? pos = null;
            AudioMixerGroup group = null;
            float volMul = 1f;
            float pitchMul = 1f;

            if (ctx.sfx.HasValue)
            {
                var a = ctx.sfx.Value;
                anchor = a.anchor != null ? a.anchor : anchor;
                pos = a.position.HasValue ? a.position : pos;
                group = a.groupOverride != null ? a.groupOverride : group;
                if (a.volumeMul.HasValue) volMul = a.volumeMul.Value;
                if (a.pitchMul.HasValue) pitchMul = a.pitchMul.Value;
            }


            rt.sfxPlayer.Play(sfx, anchor, pos, group, volMul, pitchMul);
        }

        public static void Play(VFXAsset vfx, in EffectContext ctx)
        {
            if (vfx == null) return;
            var rt = EffectRouter.CreateOrGet();
            Transform anchor = null;
            Vector3? pos = null;
            if (ctx.vfx.HasValue)
            {
                var v = ctx.vfx.Value;
                anchor = v.anchor != null ? v.anchor : anchor;
                pos = v.position.HasValue ? v.position : pos;
            }

            rt.vfxPlayer.Play(vfx, anchor, pos);
        }

        public static void Play(EffectBundle bundle, in EffectContext ctx)
        {
            if (bundle == null) return;
            // Audio
            if (bundle.audio != null)
            {
                Play(bundle.audio, ctx);
            }
            // VFX
            if (bundle.vfx != null)
            {
                Play(bundle.vfx, ctx);
            }
            // Haptics (no context for now except potential scaling via settings)
            if (bundle.haptics != null)
            {
                var rt = EffectRouter.CreateOrGet();
                rt.hapticsDriver.Play(bundle.haptics, 1f);
            }
        }

    }
}
