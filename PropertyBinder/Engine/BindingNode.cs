using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PropertyBinder.Helpers;

namespace PropertyBinder.Engine;

internal class BindingNodeBuilder<TParent, TNode> : IBindingNodeBuilder<TParent>
{
    protected readonly Func<TParent, TNode> TargetSelector;
    protected readonly IDictionary<string, List<int>> BindingActions;
    protected IDictionary<string, IBindingNodeBuilder<TNode>> SubNodes;
    protected ICollectionBindingNodeBuilder<TNode> CollectionNode;

    protected BindingNodeBuilder(Func<TParent, TNode> targetSelector, IDictionary<string, IBindingNodeBuilder<TNode>> subNodes, IDictionary<string, List<int>> bindingActions, ICollectionBindingNodeBuilder<TNode> collectionNode)
    {
        TargetSelector = targetSelector;
        SubNodes = subNodes;
        BindingActions = bindingActions;
        CollectionNode = collectionNode;
    }

    public BindingNodeBuilder(Func<TParent, TNode> targetSelector)
        : this(targetSelector, null, new Dictionary<string, List<int>>(), null)
    {
        TargetSelector = targetSelector;
    }

    public IBindingNodeBuilder GetSubNode(BindableMember member)
    {
        SubNodes ??= new Dictionary<string, IBindingNodeBuilder<TNode>>();
        if (SubNodes.TryGetValue(member.Name, out var node))
        {
            return node;
        }

        var selector = member.CreateSelector(typeof(TNode));

        MethodInfo nodeMethodInfo;
        try
        {
            nodeMethodInfo = selector.Method;
        }
        catch (Exception e)
        {
            throw new ArgumentException($@"Failed to resolve selector method for member {member} with node of type {typeof(TNode)}", e);
        }
        
        Type nodeType;
        try
        {
            nodeType = typeof(BindingNodeBuilder<,>).MakeGenericType(typeof(TNode), nodeMethodInfo.ReturnType);
        }
        catch (Exception e)
        {
            throw new ArgumentException($@"Failed to create binding node for member {member} with node of type {typeof(TNode)} and selector method {nodeMethodInfo}", e);
        }
        node = (IBindingNodeBuilder<TNode>)Activator.CreateInstance(nodeType, selector);
        SubNodes.Add(member.Name, node);

        return node;
    }

    public ICollectionBindingNodeBuilder GetCollectionNode(Type itemType)
    {
        if (CollectionNode != null)
        {
            return CollectionNode;
        }

        Type collectionNodeType;
        try
        {
            collectionNodeType = typeof(CollectionBindingNodeBuilder<,>).MakeGenericType(typeof(TNode), itemType);
        }
        catch (Exception e)
        {
            throw new ArgumentException($@"Failed to create collection node type for item of type {itemType} with node of type {typeof(TNode)}", e);
        }
        
        CollectionNode = (ICollectionBindingNodeBuilder<TNode>) Activator.CreateInstance(collectionNodeType);
        return CollectionNode;
    }

    public void AddAction(string memberName, int actionIndex)
    {
        if (!BindingActions.TryGetValue(memberName, out var currentAction))
        {
            BindingActions[memberName] = currentAction = new List<int>();
        }
        currentAction.Add(actionIndex);
    }

    public bool HasBindingActions
    {
        get
        {
            if (BindingActions.Count != 0)
            {
                return true;
            }

            if (SubNodes != null && SubNodes.Values.Any(x => x.HasBindingActions))
            {
                return true;
            }

            return CollectionNode is {HasBindingActions: true};
        }
    }

    public IBindingNodeBuilder<TParent> Clone()
    {
        return new BindingNodeBuilder<TParent, TNode>(
            TargetSelector,
            SubNodes?.ToDictionary(x => x.Key, x => x.Value.Clone()),
            BindingActions.ToDictionary(x => x.Key, x => new List<int>(x.Value)),
            CollectionNode?.Clone());
    }

    public virtual IBindingNodeBuilder<TNewParent> CloneForDerivedParentType<TNewParent>()
        where TNewParent : TParent
    {
        return new BindingNodeBuilder<TNewParent, TNode>(
            x => TargetSelector(x),
            SubNodes?.ToDictionary(x => x.Key, x => x.Value.Clone()),
            BindingActions.ToDictionary(x => x.Key, x => new List<int>(x.Value)),
            CollectionNode?.Clone());
    }

    public IBindingNode<TParent> CreateBindingNode(int[] actionRemap)
    {
        return new BindingNode<TParent,TNode>(
            TargetSelector,
            SubNodes?.ToReadOnlyDictionary(x => x.Key, x => x.Value.CreateBindingNode(actionRemap)),
            BindingActions
                .Select(pair => new KeyValuePair<string, int[]>(pair.Key, pair.Value.CompactRemap(actionRemap)))
                .Where(x => x.Value.Length > 0)
                .ToList()
                .ToReadOnlyDictionary(x => x.Key, x => x.Value),
            CollectionNode?.CreateBindingNode(actionRemap));
    }
}

internal sealed class BindingNode<TParent, TNode> : IBindingNode<TParent>
{
    public readonly Func<TParent, TNode> TargetSelector;
    public readonly IReadOnlyDictionary<string, int[]> BindingActions;
    public readonly IReadOnlyDictionary<string, IBindingNode<TNode>> SubNodes;
    public readonly ICollectionBindingNode<TNode> CollectionNode;

    public BindingNode(
        Func<TParent, TNode> targetSelector,
        IReadOnlyDictionary<string, IBindingNode<TNode>> subNodes,
        IReadOnlyDictionary<string, int[]> bindingActions,
        ICollectionBindingNode<TNode> collectionNode)
    {
        TargetSelector = targetSelector;
        SubNodes = subNodes;
        BindingActions = bindingActions;
        CollectionNode = collectionNode;
    }

    public IObjectWatcher<TParent> CreateWatcher(BindingMap map)
    {
        return new ObjectWatcher<TParent, TNode>(this, map);
    }
}