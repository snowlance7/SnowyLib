using System;
using System.Collections.Generic;
using System.Linq;
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

            Assembly snowyLibAssembly = typeof(StaticUpdateManager).Assembly;
            string snowyLibName = snowyLibAssembly.GetName().Name;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == snowyLibAssembly)
                {
                    ScanAssembly(assembly);
                    continue;
                }

                if (!assembly.GetReferencedAssemblies().Any(a => a.Name == snowyLibName))
                    continue;

                ScanAssembly(assembly);
            }
        }

        private static void ScanAssembly(Assembly assembly)
        {
            foreach (Type type in GetLoadableTypes(assembly))
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!method.IsDefined(typeof(StaticUpdateAttribute), false))
                        continue;

                    if (method.ReturnType != typeof(void))
                        throw new Exception($"{method.DeclaringType.FullName}.{method.Name} must return void.");

                    if (method.GetParameters().Length != 0)
                        throw new Exception($"{method.DeclaringType.FullName}.{method.Name} cannot have parameters.");

                    updates.Add((Action)Delegate.CreateDelegate(typeof(Action), method));
                }
            }
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null)!;
            }
        }

        public static void Update()
        {
            foreach (Action update in updates)
                update();
        }
    }
}
