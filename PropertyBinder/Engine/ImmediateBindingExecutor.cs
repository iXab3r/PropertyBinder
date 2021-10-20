using System;
using PropertyBinder.Helpers;

namespace PropertyBinder.Engine
{
    /// <summary>
    ///  Binder that supports nested bindings execution without rescheduling. Similar to how RX behaves without using schedulers
    /// </summary>
    internal sealed class ImmediateBindingExecutor : BindingExecutor
    {
        private readonly LiteQueue<BindingReference> _scheduledBindings = new LiteQueue<BindingReference>();

        protected override void ExecuteInternal(BindingMap map, int[] bindings)
        {
            _scheduledBindings.Reserve(bindings.Length);
            foreach (var i in bindings)
            {
                if (map.Schedule[i])
                {
                    // already scheduled
                    continue;
                }

                map.Schedule[i] = true;
                _scheduledBindings.EnqueueUnsafe(new BindingReference(map, i));
            }

            try
            {
                while (_scheduledBindings.Count > 0)
                {
                    ref BindingReference binding = ref _scheduledBindings.DequeueRef();
                    binding.UnSchedule();
                    try
                    {
                        binding.Execute();
                    }
                    catch (Exception ex)
                    {
                        HandleExecutionException(ex, binding);
                    }
                }
            }
            catch (Exception)
            {
                while (_scheduledBindings.Count > 0)
                {
                    _scheduledBindings.DequeueRef().UnSchedule();
                }
                throw;
            }
        }

        protected override void SuspendInternal()
        {
            throw new NotSupportedException($"Enable {nameof(Binder.SupportTransactions)} to support transactions");
        }

        protected override void ResumeInternal()
        {
            throw new NotSupportedException($"Enable {nameof(Binder.SupportTransactions)} to support transactions");
        }
    }
}