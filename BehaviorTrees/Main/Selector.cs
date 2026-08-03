namespace SnowyLib.BehaviorTrees.Main
{
    public class Selector : CompositeNode
    {
        private int currentChild;

        public Selector(params Node[] children) : base(children) { }

        public override NodeResult Tick()
        {
            while (currentChild < children.Count)
            {
                NodeResult result = children[currentChild].Tick();

                if (result == NodeResult.Success)
                {
                    currentChild = 0;
                    return NodeResult.Success;
                }

                if (result == NodeResult.Running)
                {
                    return NodeResult.Running;
                }

                currentChild++;
            }

            currentChild = 0;
            return NodeResult.Failure;
        }
    }
}
