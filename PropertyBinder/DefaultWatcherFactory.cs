using PropertyBinder.Engine;

namespace PropertyBinder;

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