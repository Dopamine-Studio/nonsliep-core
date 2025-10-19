namespace Nonsliep.Core.Effect
{
    /// <summary>
    /// Implemented by components that can react to externally dispatched effect payloads.
    /// </summary>
    public interface IEffectReceiver
    {
        void ReceiveEffect(in EffectPayload payload);
    }
}
