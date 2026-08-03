namespace SnowyLib.BehaviorTrees.Main
{
    public class Sequence : CompositeNode
    {
        private int currentChild;

        public Sequence(params Node[] children) : base(children) { }

        public override NodeResult Tick()
        {
            while (currentChild < children.Count)
            {
                NodeResult result = children[currentChild].Tick();

                if (result == NodeResult.Failure)
                {
                    currentChild = 0;
                    return NodeResult.Failure;
                }

                if (result == NodeResult.Running)
                {
                    return NodeResult.Running;
                }

                currentChild++;
            }

            currentChild = 0;
            return NodeResult.Success;
        }
    }
}
