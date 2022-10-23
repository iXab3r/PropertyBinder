using System.Collections.Generic;
using System.Linq;

namespace PropertyBinder.Engine;

internal sealed class BindingNodeRootBuilder<TContext> : BindingNodeBuilder<TContext, TContext>
{
    private BindingNodeRootBuilder(IDictionary<string, IBindingNodeBuilder<TContext>> subNodes, IDictionary<string, List<int>> bindingActions, ICollectionBindingNodeBuilder<TContext> collectionNode)
        : base(_ => _, subNodes, bindingActions, collectionNode)
    {
    }

    public BindingNodeRootBuilder()
        : base(_ => _)
    {
    }

    public override IBindingNodeBuilder<TNewParent> CloneForDerivedParentType<TNewParent>()
    {
        return new BindingNodeRootBuilder<TNewParent>(
            SubNodes?.ToDictionary(x => x.Key, x => x.Value.CloneForDerivedParentType<TNewParent>()),
            BindingActions.ToDictionary(x => x.Key, x => new List<int>(x.Value)),
            CollectionNode?.CloneForDerivedParentType<TNewParent>());
    }
}