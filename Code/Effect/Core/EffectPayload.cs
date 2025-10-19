using Nonsliep.Core.Effect.SFX;
using Nonsliep.Core.Effect.VFX;

namespace Nonsliep.Core.Effect
{
    public readonly struct EffectPayload
    {
        public readonly EffectPayloadItem[] items;
        public readonly EffectContext? context;

        public EffectPayload(EffectPayloadItem[] items, EffectContext? context = null)
        {
            this.items = items;
            this.context = context;
        }

        public bool HasAnyData => (items != null && items.Length > 0) || context.HasValue;

        public EffectContext ResolveContext()
        {
            return context.HasValue ? context.Value : default;
        }

        public EffectPayload WithContext(in EffectContext newContext)
        {
            return new EffectPayload(items, newContext);
        }

        public static EffectPayload Empty => default;
    }

    public readonly struct EffectPayloadItem
    {
        public readonly EffectType[] keys;
        public readonly SFXAsset sfxOverride;
        public readonly VFXAsset vfxOverride;
        public readonly HapticPreset hapticsOverride;
        public readonly EffectBundle bundleOverride;
        public readonly bool suppressDefaultPlayback;

        public EffectPayloadItem(
            EffectType[] keys,
            SFXAsset sfxOverride = null,
            VFXAsset vfxOverride = null,
            HapticPreset hapticsOverride = null,
            EffectBundle bundleOverride = null,
            bool suppressDefaultPlayback = false)
        {
            this.keys = keys;
            this.sfxOverride = sfxOverride;
            this.vfxOverride = vfxOverride;
            this.hapticsOverride = hapticsOverride;
            this.bundleOverride = bundleOverride;
            this.suppressDefaultPlayback = suppressDefaultPlayback;
        }

        public bool HasOverrides => sfxOverride != null || vfxOverride != null || hapticsOverride != null || bundleOverride != null;

        public bool HasKeys => keys != null && keys.Length > 0;

        public bool SuppressDefault => suppressDefaultPlayback;
    }
}
