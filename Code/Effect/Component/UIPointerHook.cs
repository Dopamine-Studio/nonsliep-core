using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nonsliep.Core.Effect
{
    [DisallowMultipleComponent]
    public class UIPointerHook : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IEndDragHandler
    {
        public event Action PointerDown;
        public event Action PointerUp;
        public event Action DragEnd;

        public void OnPointerDown(PointerEventData eventData)
        {
            PointerDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            PointerUp?.Invoke();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            DragEnd?.Invoke();
        }
    }
}
