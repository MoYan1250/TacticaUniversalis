using UnityEngine;
using UnityEngine.EventSystems;

namespace TacticaUniversalis.Presentation
{
    /// <summary>
    /// Drag a RectTransform by shifting its normalized anchors (works with SetRect layouts).
    /// </summary>
    public sealed class UiWindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public RectTransform Target;
        public bool CanDrag;

        Canvas _canvas;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDrag || Target == null)
                return;
            if (_canvas == null)
                _canvas = Target.GetComponentInParent<Canvas>();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!CanDrag || Target == null || eventData == null)
                return;

            RectTransform parent = Target.parent as RectTransform;
            if (parent == null)
                return;

            Vector2 parentSize = parent.rect.size;
            if (parentSize.x < 1f || parentSize.y < 1f)
                return;

            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            if (scale < 0.01f)
                scale = 1f;

            Vector2 nd = new Vector2(
                eventData.delta.x / scale / parentSize.x,
                eventData.delta.y / scale / parentSize.y);

            Vector2 min = Target.anchorMin + nd;
            Vector2 max = Target.anchorMax + nd;
            Vector2 size = max - min;

            // Keep most of the window on-screen while allowing partial overhang.
            min.x = Mathf.Clamp(min.x, -size.x * 0.45f, 1f - size.x * 0.55f);
            min.y = Mathf.Clamp(min.y, -size.y * 0.45f, 1f - size.y * 0.55f);
            max = min + size;

            Target.anchorMin = min;
            Target.anchorMax = max;
            Target.offsetMin = Vector2.zero;
            Target.offsetMax = Vector2.zero;
        }
    }
}
