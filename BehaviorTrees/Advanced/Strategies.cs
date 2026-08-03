using System;

namespace SnowyLib.BehaviorTrees.Advanced
{
    public interface IStrategy
    {
        Node.Status Tick();

        void Reset()
        {
            // Noop
        }
    }

    public class ActionStrategy : IStrategy
    {
        readonly Action doSomething;

        public ActionStrategy(Action doSomething)
        {
            this.doSomething = doSomething;
        }

        public Node.Status Tick()
        {
            doSomething();
            return Node.Status.Success;
        }
    }

    public class Condition : IStrategy
    {
        readonly Func<bool> predicate;

        public Condition(Func<bool> predicate)
        {
            this.predicate = predicate;
        }

        public Node.Status Tick() => predicate() ? Node.Status.Success : Node.Status.Failure;
    }
}