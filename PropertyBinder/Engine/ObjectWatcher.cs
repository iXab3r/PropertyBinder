using System.Collections.Generic;
using System.ComponentModel;

using PropertyBinder.Helpers;

namespace PropertyBinder.Engine;

internal sealed class ObjectWatcher<TParent, TNode> : IObjectWatcher<TParent>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly bool IsValueType;

    static ObjectWatcher()
    {
        IsValueType = typeof(TNode).IsValueType;
    }

    private readonly IReadOnlyDictionary<string, IObjectWatcher<TNode>> _subWatchers;
    private readonly IObjectWatcher<TNode> _collectionWatcher;
    private TNode _target;
    private readonly PropertyChangedEventHandler _handler;
    private readonly BindingNode<TParent, TNode> _bindingNode;

    public ObjectWatcher(BindingNode<TParent, TNode> bindingNode, BindingMap map)
    {
        _bindingNode = bindingNode;
        Map = map;
        if (bindingNode.CollectionNode != null)
        {
            _collectionWatcher = bindingNode.CollectionNode.CreateWatcher(map);
        }
        _subWatchers = bindingNode.SubNodes?.ToReadOnlyDictionary2(x => x.Key, x => x.Value.CreateWatcher(map));
        _handler = _subWatchers == null ? TerminalTargetPropertyChanged : new PropertyChangedEventHandler(TargetPropertyChanged);
    }
    
    public BindingMap Map { get; }

    public void Attach(TParent parent)
    {
        if (!IsValueType)
        {
            if (_target is INotifyPropertyChanged notify)
            {
                notify.PropertyChanged -= _handler;
            }
        }

        _target = parent == null ? default : _bindingNode.TargetSelector(parent);

        if (!IsValueType)
        {
            if (_target is INotifyPropertyChanged notify)
            {
                notify.PropertyChanged += _handler;
            }
        }

        if (_subWatchers != null)
        {
            foreach (var node in _subWatchers.Values)
            {
                node.Attach(_target);
            }
        }

        _collectionWatcher?.Attach(_target);
    }

    public void Dispose()
    {
        Attach(default);
    }

    private void TargetPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        var propertyName = e.PropertyName;
        if (_subWatchers.TryGetValue(propertyName, out var node))
        {
            node.Attach(_target);
        }

        if (_bindingNode.BindingActions.TryGetValue(propertyName, out var bindings))
        {
            BindingExecutor.Execute(Map, bindings);
        }
    }

    private void TerminalTargetPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (_bindingNode.BindingActions.TryGetValue(e.PropertyName, out var bindings))
        {
            BindingExecutor.Execute(Map, bindings);
        }
    }
}