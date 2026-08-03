using System.Collections.Generic;

namespace SnowyLib.BehaviorTrees.Main
{
    public abstract class CompositeNode : Node
    {
        protected List<Node> children = new();

        public CompositeNode(params Node[] children)
        {
            this.children.AddRange(children);
        }
    }
}
