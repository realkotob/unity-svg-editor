using System;
using UnityEditor;

namespace SvgEditor.Shared
{
    /// <summary>
    /// Small reusable gate for coalescing one editor deferred callback.
    /// </summary>
    internal sealed class DeferredActionGate
    {
        private readonly Action _callback;
        private readonly Action<Action> _schedule;
        private readonly Action<Action> _unschedule;

        public DeferredActionGate(
            Action callback,
            Action<Action> schedule = null,
            Action<Action> unschedule = null)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _schedule = schedule ?? ScheduleWithDelayCall;
            _unschedule = unschedule ?? UnscheduleWithDelayCall;
        }

        public bool IsScheduled { get; private set; }

        public void Schedule()
        {
            if (IsScheduled)
            {
                return;
            }

            IsScheduled = true;
            _schedule(_callback);
        }

        public void Cancel()
        {
            if (!IsScheduled)
            {
                return;
            }

            IsScheduled = false;
            _unschedule(_callback);
        }

        private static void ScheduleWithDelayCall(Action callback)
        {
            if (callback == null)
            {
                return;
            }

            EditorApplication.delayCall += callback.Invoke;
        }

        private static void UnscheduleWithDelayCall(Action callback)
        {
            if (callback == null)
            {
                return;
            }

            EditorApplication.delayCall -= callback.Invoke;
        }
    }
}
