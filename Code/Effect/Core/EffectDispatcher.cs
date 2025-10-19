using System.Collections.Generic;
using UnityEngine;

namespace Nonsliep.Core.Effect
{
    /// <summary>
    /// Broadcasts an EffectPayload to a GameObject and its children, optionally activating the root first.
    /// </summary>
    public static class EffectDispatcher
    {
        private static readonly List<Component> ComponentBuffer = new(16);
        private static readonly List<Component> PrepBuffer = new(16);

        public static void Dispatch(GameObject target, in EffectPayload payload, bool activateTarget = true, bool includeInactiveChildren = true)
        {
            if (target == null)
            {
                return;
            }

            if (activateTarget && !target.activeSelf)
            {
                if (payload.HasAnyData)
                {
                    PrepareReceivers(target, payload, includeInactiveChildren);
                }
                target.SetActive(true);
            }

            if (!payload.HasAnyData)
            {
                return;
            }

            ComponentBuffer.Clear();
            target.GetComponentsInChildren(includeInactiveChildren, ComponentBuffer);
            for (int i = 0; i < ComponentBuffer.Count; i++)
            {
                if (ComponentBuffer[i] is IEffectReceiver receiver)
                {
                    receiver.ReceiveEffect(payload);
                }
            }
        }

        public static void Dispatch(IEnumerable<GameObject> targets, in EffectPayload payload, bool activateTargets = true, bool includeInactiveChildren = true)
        {
            if (targets == null)
            {
                return;
            }

            foreach (var go in targets)
            {
                Dispatch(go, payload, activateTargets, includeInactiveChildren);
            }
        }

        private static void PrepareReceivers(GameObject target, in EffectPayload payload, bool includeInactiveChildren)
        {
            PrepBuffer.Clear();
            target.GetComponentsInChildren(includeInactiveChildren, PrepBuffer);
            for (int i = 0; i < PrepBuffer.Count; i++)
            {
                if (PrepBuffer[i] is IEffectPayloadPreparer preparer)
                {
                    preparer.PrepareEffect(payload);
                }
            }
            PrepBuffer.Clear();
        }
    }
}
