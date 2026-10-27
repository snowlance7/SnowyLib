using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SnowyLib
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class StaticUpdateAttribute : Attribute
    {
        private static readonly List<Action> updates = new();

        internal static void Initialize()
        {
            updates.Clear();

            Assembly snowyLibAssembly = typeof(StaticUpdateAttribute).Assembly;
            string snowyLibName = snowyLibAssembly.GetName().Name;

            ScanAssembly(snowyLibAssembly);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == snowyLibAssembly) { continue; }

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
                    try
                    {
                        if (!method.IsDefined(typeof(StaticUpdateAttribute), false))
                            continue;

                        if (method.ReturnType != typeof(void))
                            throw new Exception($"{method.DeclaringType.FullName}.{method.Name} must return void.");

                        if (method.GetParameters().Length != 0)
                            throw new Exception($"{method.DeclaringType.FullName}.{method.Name} cannot have parameters.");

                        updates.Add((Action)Delegate.CreateDelegate(typeof(Action), method));
                    }
                    catch (Exception e)
                    {
                        Plugin.logger.LogError(e);
                        continue;
                    }
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

        internal static void Update()
        {
            foreach (Action update in updates)
            {
                try
                {
                    update();
                }
                catch (Exception e)
                {
                    Plugin.logger.LogError(e);
                    continue;
                }
            }
        }
    }
}
