namespace Nonsliep.Core.Effect
{
    /// <summary>
    /// Optional interface for effect receivers that need to be notified of payloads
    /// before their GameObject is activated (e.g., to defer default lifecycle playback).
    /// </summary>
    public interface IEffectPayloadPreparer
    {
        void PrepareEffect(in EffectPayload payload);
    }
}
