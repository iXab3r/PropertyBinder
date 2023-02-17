using System;
using System.Collections.Concurrent;
using PropertyBinder.Engine;

namespace PropertyBinder;

internal interface IWatcherFactory<in TContext>
{
    IWatcherRoot Attach(TContext context);
}
    
internal sealed class DefaultWatcherFactory<TContext> : IWatcherFactory<TContext>
    where TContext : class
{
    private readonly Binder<TContext>.BindingAction[] _actions;
    private readonly IBindingNode<TContext> _rootNode;

    public DefaultWatcherFactory(Binder<TContext>.BindingAction[] actions, IBindingNode<TContext> rootNode)
    {
        _actions = actions;
        _rootNode = rootNode;
    }

    public IWatcherRoot Attach(TContext context)
    {
        var map = new BindingMap<TContext>(_actions);
        map.SetContext(context);
        var watcher = _rootNode.CreateWatcher(map);
        watcher.Attach(context);
        return watcher;
    }
}
    
internal sealed class ReusableWatcherFactory<TContext> : IWatcherFactory<TContext>
    where TContext : class
{
    private readonly Binder<TContext>.BindingAction[] _actions;
    private readonly IBindingNode<TContext> _root;

    private readonly ConcurrentStack<WeakReference> _detachedWatchers = new ConcurrentStack<WeakReference>();

    public ReusableWatcherFactory(Binder<TContext>.BindingAction[] actions, IBindingNode<TContext> root)
    {
        _actions = actions;
        _root = root;
    }

    public IWatcherRoot Attach(TContext context)
    {
        Root root = null;
        while (_detachedWatchers.TryPop(out var reference))
        {
            var target = reference.Target;
            if (reference.IsAlive && (root = target as Root) != null)
            {
                break;
            }
        }

        if (root == null)
        {
            root = new Root(this);
        }

        root.SetContext(context);
        return root;
    }

    private sealed class Root : IWatcherRoot<TContext>
    {
        private readonly ReusableWatcherFactory<TContext> _parent;

        public Root(ReusableWatcherFactory<TContext> parent)
        {
            _parent = parent;
            Map = new BindingMap<TContext>(parent._actions);
            Watcher = _parent._root.CreateWatcher(Map);
        }
        
        public IObjectWatcher<TContext> Watcher { get; }
        
        public BindingMap<TContext> Map { get; }
        
        BindingMap IWatcherRoot.Map => Map;

        public void SetContext(TContext context)
        {
            Map.SetContext(context);
            Watcher.Attach(context);
        }

        public void Dispose()
        {
            Watcher.Attach(null);
            _parent._detachedWatchers.Push(new WeakReference(this));
        }

    }
}

internal interface IWatcherRoot : IDisposable
{
    BindingMap Map { get; }
}

internal interface IWatcherRoot<TContext> : IWatcherRoot
    where TContext : class
{
    new BindingMap<TContext> Map { get; }
        
    IObjectWatcher<TContext> Watcher { get; }
}