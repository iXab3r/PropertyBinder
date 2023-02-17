using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace PropertyBinder.Engine;

internal class CollectionWatcher<TCollection, TItem> : IObjectWatcher<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly CollectionBindingNode<TCollection, TItem> _node;
    private readonly IDictionary<TItem, IObjectWatcher<TItem>> _attachedItems = new Dictionary<TItem, IObjectWatcher<TItem>>();

    private TCollection _target;

    public CollectionWatcher(CollectionBindingNode<TCollection, TItem> node, BindingMap map)
    {
        _node = node;
        Map = map;
    }

    public BindingMap Map { get; }

    public void Attach(TCollection parent)
    {
        DetachItems();

        var notify = _target as INotifyCollectionChanged;
        if (notify != null)
        {
            notify.CollectionChanged -= TargetCollectionChanged;
        }

        _target = parent;

        notify = _target as INotifyCollectionChanged;
        if (notify != null)
        {
            notify.CollectionChanged += TargetCollectionChanged;
        }

        if (_target != null && _node.ItemNode != null)
        {
            AttachItems();
        }
    }

    public void Dispose()
    {
        Attach(default);
    }

    protected virtual void TargetCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (_node.ItemNode == null)
        {
            if (_node.Indexes.Length > 0)
            {
                BindingExecutor.Execute(Map, _node.Indexes);
            }
        }
        else
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    AttachItem((TItem) e.NewItems[0]);
                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                {
                    DetachItem((TItem) e.OldItems[0]);
                    break;
                }

                case NotifyCollectionChangedAction.Replace:
                {
                    DetachItem((TItem) e.OldItems[0]);
                    AttachItem((TItem) e.NewItems[0]);
                    break;
                }

                case NotifyCollectionChangedAction.Reset:
                {
                    DetachItems();
                    AttachItems();
                    break;
                }
            }

            if (_node.Indexes.Length > 0)
            {
                BindingExecutor.Execute(Map, _node.Indexes);
            }
        }
    }

    private void DetachItems()
    {
        foreach (var watcher in _attachedItems.Values)
        {
            watcher.Dispose();
        }

        _attachedItems.Clear();
    }

    private void DetachItem(TItem item)
    {
        if (item == null || _target == null || _target.Contains(item) || !_attachedItems.TryGetValue(item, out var watcher))
        {
            return;
        }

        watcher.Dispose();
        _attachedItems.Remove(item);
    }

    private void AttachItems()
    {
        foreach (var item in _target)
        {
            AttachItem(item);
        }
    }

    private void AttachItem(TItem item)
    {
        if (item == null || _attachedItems.ContainsKey(item))
        {
            return;
        }

        var watcher = _node.ItemNode.CreateWatcher(Map);
        watcher.Attach(item);
        _attachedItems.Add(item, watcher);
    }
}