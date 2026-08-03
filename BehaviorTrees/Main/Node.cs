namespace SnowyLib.BehaviorTrees.Main
{
    public abstract class Node
    {
        public enum NodeResult
        {
            Success,
            Failure,
            Running
        }

        public abstract NodeResult Tick();
    }
}
