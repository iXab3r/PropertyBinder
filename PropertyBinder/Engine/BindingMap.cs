using System;
using System.Diagnostics;
using PropertyBinder.Diagnostics;
using PropertyBinder.Helpers;

namespace PropertyBinder.Engine;

internal abstract class BindingMap
{
    // seems to work faster than BitArray, we don't need to optimize for memory THAT much
    public readonly bool[] Schedule;

    protected BindingMap(int size)
    {
        Schedule = new bool[size];
    }

    public abstract void Execute(int index);

    public abstract string GetStamp(int index);

    public abstract DebugContext GetDebugContext(int index);
}

internal sealed class BindingMap<TContext> : BindingMap
    where TContext : class
{
    public readonly Binder<TContext>.BindingAction[] _actions;

    private TContext _context;

    public BindingMap(Binder<TContext>.BindingAction[] actions)
        : base(actions.Length)
    {
        _actions = actions;
    }

    public void SetContext(TContext context)
    {
        _context = context;
    }

    public override void Execute(int index)
    {
        var context = _context;
        if (context is null)
        {
            //this usually means that Binder is already disposed
            return;
        }
        _actions[index].Action(context);
    }

    public override string GetStamp(int index)
    {
        return _actions[index].GetStamped(_context);
    }

    public override DebugContext GetDebugContext(int index)
    {
        return _actions[index].DebugContext;
    }
    
    public override string ToString()
    {
        return $"BindingMap({_actions.Length}) {_context}";
    }
}