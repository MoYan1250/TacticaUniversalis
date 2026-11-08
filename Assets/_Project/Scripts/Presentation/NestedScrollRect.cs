using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    /// <summary>
    /// Nested ScrollRect that forwards the non-primary axis to a parent ScrollRect.
    /// Fixes vertical parent scroll being swallowed by a horizontal child bracket scroller.
    /// </summary>
    public sealed class NestedScrollRect : ScrollRect
    {
        private bool _routeToParent;
        private bool _dragDecided;
        private ScrollRect _parentScroll;

        protected override void Awake()
        {
            base.Awake();
            _parentScroll = null;
            Transform t = transform.parent;
            while (t != null)
            {
                ScrollRect found = t.GetComponent<ScrollRect>();
                if (found != null)
                {
                    _parentScroll = found;
                    break;
                }
                t = t.parent;
            }
        }

        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (_parentScroll != null)
                ExecuteEvents.Execute(
                    _parentScroll.gameObject, eventData, ExecuteEvents.initializePotentialDrag);
            base.OnInitializePotentialDrag(eventData);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            _dragDecided = false;
            _routeToParent = ShouldRouteToParent(eventData.delta);
            if (eventData.delta.sqrMagnitude > 0.01f)
                _dragDecided = true;

            if (_routeToParent && _parentScroll != null)
                ExecuteEvents.Execute(_parentScroll.gameObject, eventData, ExecuteEvents.beginDragHandler);
            else
                base.OnBeginDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (!_dragDecided && _parentScroll != null)
            {
                // BeginDrag often has zero delta; decide on the first meaningful move.
                if (eventData.delta.sqrMagnitude > 0.01f)
                {
                    bool wantParent = ShouldRouteToParent(eventData.delta);
                    _dragDecided = true;
                    if (wantParent != _routeToParent)
                    {
                        if (_routeToParent)
                            ExecuteEvents.Execute(
                                _parentScroll.gameObject, eventData, ExecuteEvents.endDragHandler);
                        else
                            base.OnEndDrag(eventData);

                        _routeToParent = wantParent;
                        if (_routeToParent)
                            ExecuteEvents.Execute(
                                _parentScroll.gameObject, eventData, ExecuteEvents.beginDragHandler);
                        else
                            base.OnBeginDrag(eventData);
                    }
                }
            }

            if (_routeToParent && _parentScroll != null)
                ExecuteEvents.Execute(_parentScroll.gameObject, eventData, ExecuteEvents.dragHandler);
            else
                base.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (_routeToParent && _parentScroll != null)
                ExecuteEvents.Execute(_parentScroll.gameObject, eventData, ExecuteEvents.endDragHandler);
            else
                base.OnEndDrag(eventData);
            _routeToParent = false;
            _dragDecided = false;
        }

        public override void OnScroll(PointerEventData data)
        {
            if (_parentScroll == null)
            {
                base.OnScroll(data);
                return;
            }

            // Horizontal-only child: vertical wheel must scroll the parent.
            if (horizontal && !vertical)
            {
                if (Mathf.Abs(data.scrollDelta.x) > Mathf.Abs(data.scrollDelta.y))
                    base.OnScroll(data);
                else
                    ExecuteEvents.Execute(_parentScroll.gameObject, data, ExecuteEvents.scrollHandler);
                return;
            }

            // Vertical-only child: horizontal wheel goes to parent if parent can use it.
            if (vertical && !horizontal)
            {
                if (Mathf.Abs(data.scrollDelta.y) >= Mathf.Abs(data.scrollDelta.x))
                    base.OnScroll(data);
                else
                    ExecuteEvents.Execute(_parentScroll.gameObject, data, ExecuteEvents.scrollHandler);
                return;
            }

            base.OnScroll(data);
        }

        private bool ShouldRouteToParent(Vector2 delta)
        {
            if (_parentScroll == null)
                return false;

            float ax = Mathf.Abs(delta.x);
            float ay = Mathf.Abs(delta.y);

            // Horizontal child: strictly vertical-dominant drag belongs to parent.
            if (horizontal && !vertical)
                return ay > ax;

            // Vertical child: strictly horizontal-dominant drag belongs to parent.
            if (vertical && !horizontal)
                return ax > ay;

            return false;
        }
    }
}
