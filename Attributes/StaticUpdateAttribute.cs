using System;
using System.Collections.Generic;
using System.Reflection;

namespace SnowyLib
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class StaticUpdateAttribute : Attribute { }

    public static class StaticUpdateManager
    {
        private static readonly List<Action> updates = new();

        public static void Initialize()
        {
            updates.Clear();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    foreach (MethodInfo method in type.GetMethods(
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic))
                    {
                        if (!method.IsDefined(typeof(StaticUpdateAttribute), false))
                            continue;

                        // Validate signature
                        if (method.ReturnType != typeof(void))
                            throw new Exception($"{type.FullName}.{method.Name} must return void.");

                        if (method.GetParameters().Length != 0)
                            throw new Exception($"{type.FullName}.{method.Name} cannot have parameters.");

                        updates.Add((Action)Delegate.CreateDelegate(typeof(Action), method));
                    }
                }
            }
        }

        public static void Update()
        {
            foreach (Action update in updates)
                update();
        }
    }
}
