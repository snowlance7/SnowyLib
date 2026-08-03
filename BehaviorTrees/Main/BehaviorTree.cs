namespace SnowyLib.BehaviorTrees.Main
{
    public class BehaviorTree
    {
        private Node root;

        public BehaviorTree(Node root)
        {
            this.root = root;
        }

        public void Tick()
        {
            root.Tick();
        }
    }
}
