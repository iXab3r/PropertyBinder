using System;
using PropertyBinder.Helpers;

namespace PropertyBinder.Engine;

internal sealed class ProductionModeBindingExecutor : BindingExecutor
{
    private readonly LiteQueue<BindingReference> _scheduledBindings = new LiteQueue<BindingReference>();
    private int _executeLock;

    protected override void SuspendInternal()
    {
        ++_executeLock;
    }

    protected override void ResumeInternal()
    {
        if (_executeLock == 0)
        {
            throw new InvalidOperationException("Binder in not currently in transaction mode");
        }

        --_executeLock;
        ExecuteInternal(null, new int[0]);
    }

    protected override void ExecuteInternal(BindingMap map, int[] bindings)
    {
        _scheduledBindings.Reserve(bindings.Length);
        foreach (var i in bindings)
        {
            if (map.Schedule[i])
            {
                continue;
            }

            map.Schedule[i] = true;
            _scheduledBindings.EnqueueUnsafe(new BindingReference(map, i));
        }

        if (_executeLock != 0)
        {
            return;
        }

        ++_executeLock;
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
        finally
        {
            --_executeLock;
        }
    }
}