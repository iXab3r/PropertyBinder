using System;
using System.Collections.Generic;
using System.Linq;

namespace PropertyBinder.Engine;

internal sealed class DebugModeBindingExecutor : BindingExecutor
{
    private readonly Queue<ScheduledBinding> _scheduledBindings = new Queue<ScheduledBinding>();
    private ScheduledBinding _executingBinding;

    protected override void SuspendInternal()
    {
        _executingBinding = new ScheduledBinding(new BindingReference(new TransactionBindingMap<ScheduledBinding>(_executingBinding), 0), _executingBinding);
    }

    protected override void ResumeInternal()
    {
        _executingBinding = _executingBinding?.Parent;
        ExecuteInternal(null, new int[0]);
    }

    private sealed class ScheduledBinding
    {
        public ScheduledBinding(BindingReference binding, ScheduledBinding parent)
        {
            Binding = binding;
            Parent = parent;
        }

        public readonly BindingReference Binding;

        public readonly ScheduledBinding Parent;
    }

    protected override void ExecuteInternal(BindingMap map, int[] bindings)
    {
        foreach (var i in bindings)
        {
            var binding = new BindingReference(map, i);
            if (binding.Schedule())
            {
                _scheduledBindings.Enqueue(new ScheduledBinding(binding, _executingBinding));
                _tracer?.OnScheduled(map.GetDebugContext(i).Description);
            }
            else
            {
                _tracer?.OnIgnored(map.GetDebugContext(i).Description);
            }
        }

        if (_executingBinding != null)
        {
            return;
        }

        try
        {
            while (_scheduledBindings.Count > 0)
            {
                _executingBinding = _scheduledBindings.Dequeue();
                _executingBinding.Binding.UnSchedule();
                var description = _executingBinding.Binding.DebugContext?.Description;
                _tracer?.OnStarted(description);

                try
                {
                    if (Binder.DebugMode)
                    {
                        var tracedBindings = TraceBindings().ToArray();
                        tracedBindings[0].DebugContext.VirtualFrame(tracedBindings, 0);
                    }
                    else
                    {
                        _executingBinding.Binding.Execute();
                    }
                }
                catch (Exception ex)
                {
                    _tracer?.OnException(ex);
                    HandleExecutionException(ex, _executingBinding.Binding);
                }

                _tracer?.OnEnded(description);
            }
        }
        catch (Exception)
        {
            foreach (var binding in _scheduledBindings)
            {
                binding.Binding.UnSchedule();
            }

            _scheduledBindings.Clear();
            throw;
        }
        finally
        {
            _executingBinding = null;
        }
    }

    public IEnumerable<BindingReference> TraceBindings()
    {
        var bindings = new List<BindingReference>();
        var current = _executingBinding;

        while (current != null)
        {
            bindings.Add(current.Binding);
            current = current.Parent;
        }

        bindings.Reverse();

        return bindings;
    }
}