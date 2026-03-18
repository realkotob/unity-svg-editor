using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.Shared
{
    internal abstract class PointerDragManipulatorBase : PointerManipulator
    {
        protected readonly PointerDragSession DragSession = new();

        protected virtual float DragThreshold => 0f;
        protected bool IsThresholdMet { get; private set; }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDownInternal);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMoveInternal);
            target.RegisterCallback<PointerUpEvent>(OnPointerUpInternal);
            target.RegisterCallback<PointerCancelEvent>(OnPointerCancelInternal);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOutInternal);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDownInternal);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMoveInternal);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUpInternal);
            target.UnregisterCallback<PointerCancelEvent>(OnPointerCancelInternal);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOutInternal);
        }

        protected virtual bool CanStartDrag(PointerDownEvent evt)
        {
            return evt.button == 0;
        }

        protected virtual void HandlePointerDown(PointerDownEvent evt)
        {
        }

        protected virtual void HandleDragStarted(PointerMoveEvent evt)
        {
        }

        protected abstract void HandleDragMove(PointerMoveEvent evt, Vector2 delta);

        protected virtual void HandleDragEnd(PointerUpEvent evt, Vector2 delta)
        {
        }

        protected virtual void HandleDragCancel()
        {
        }

        protected virtual void OnStopped()
        {
        }

        protected void ResetDrag()
        {
            IsThresholdMet = false;
            DragSession.Reset();
        }

        protected Vector2 GetDelta(Vector2 currentPosition)
        {
            return currentPosition - DragSession.StartPosition;
        }

        protected bool TryInitializeDrag(PointerDownEvent evt, bool canContinue, Action onInitialized = null)
        {
            if (!canContinue)
            {
                ResetDrag();
                return false;
            }

            onInitialized?.Invoke();
            evt.StopPropagation();
            return true;
        }

        protected bool TryCancelActiveDrag()
        {
            if (!DragSession.IsActive)
            {
                return false;
            }

            CancelDrag(releasePointer: true);
            return true;
        }

        private void OnPointerDownInternal(PointerDownEvent evt)
        {
            if (!CanStartDrag(evt))
            {
                return;
            }

            DragSession.Begin(target, evt.pointerId, evt.position);
            IsThresholdMet = DragThreshold <= 0f;
            HandlePointerDown(evt);
        }

        private void OnPointerMoveInternal(PointerMoveEvent evt)
        {
            if (!DragSession.Matches(evt.pointerId))
            {
                return;
            }

            Vector2 delta = GetDelta(evt.position);
            if (!IsThresholdMet)
            {
                if (delta.magnitude < DragThreshold)
                {
                    return;
                }

                IsThresholdMet = true;
                HandleDragStarted(evt);
            }

            HandleDragMove(evt, delta);
        }

        private void OnPointerUpInternal(PointerUpEvent evt)
        {
            if (!DragSession.Matches(evt.pointerId))
            {
                return;
            }

            CompleteDrag(evt);
        }

        private void OnPointerCancelInternal(PointerCancelEvent evt)
        {
            if (!DragSession.Matches(evt.pointerId))
            {
                return;
            }

            CancelDrag(releasePointer: true);
        }

        private void OnPointerCaptureOutInternal(PointerCaptureOutEvent evt)
        {
            if (!DragSession.IsActive)
            {
                return;
            }

            CancelDrag(releasePointer: false);
        }

        private void CompleteDrag(PointerUpEvent evt)
        {
            Vector2 delta = GetDelta(evt.position);
            DragSession.End(target);
            HandleDragEnd(evt, delta);
            StopDragLifecycle();
        }

        private void CancelDrag(bool releasePointer)
        {
            if (releasePointer)
            {
                DragSession.End(target);
            }
            else
            {
                DragSession.Reset();
            }

            HandleDragCancel();
            StopDragLifecycle();
        }

        private void StopDragLifecycle()
        {
            OnStopped();
            IsThresholdMet = false;
        }
    }
}
