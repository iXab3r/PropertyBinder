using System;
using System.Collections.Generic;
using System.Diagnostics;
using PropertyBinder.Helpers;

namespace PropertyBinder.Engine;

/// <summary>
///  Binder that supports nested bindings execution without rescheduling. Similar to how RX behaves without using schedulers
/// </summary>
internal sealed class ImmediateBindingExecutor : BindingExecutor
{
    private readonly LiteQueue<BindingReference> _scheduledBindings = new LiteQueue<BindingReference>();

    protected override void ExecuteInternal(BindingMap map, IReadOnlyList<int> bindings)
    {
        lock (map)
        {
            _scheduledBindings.Reserve(bindings.Count);
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
                while (true)
                {
                    if (_scheduledBindings.Count <= 0)
                    {
                        break;
                    }

                    ref var binding = ref _scheduledBindings.DequeueRef();
                    binding.UnSchedule();

                    try
                    {
                        Log.WriteLine($" Executing binding #{binding.Index} in {binding.Map}");
                        binding.Execute();
                        Log.WriteLine($" Executed binding #{binding.Index} in {binding.Map}");
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