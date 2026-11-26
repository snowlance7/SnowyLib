using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static SnowyLib.Plugin;

namespace SnowyLib;

[AttributeUsage(AttributeTargets.Property)]
public sealed class StaticConfigAttribute : Attribute
{
    internal static void Initialize()
    {
        Assembly snowyLibAssembly = typeof(StaticConfigAttribute).Assembly;
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
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                try
                {
                    if (!property.IsDefined(typeof(StaticConfigAttribute), false))
                        continue;

                    if (property.PropertyType == typeof(void))
                        throw new Exception($"{property.DeclaringType.FullName}.{property.Name} cannot be void.");

                    if (!property.CanRead)
                        throw new Exception($"{property.DeclaringType.FullName}.{property.Name} must have a getter.");

                    if (property.GetIndexParameters().Length != 0)
                        throw new Exception($"{property.DeclaringType.FullName}.{property.Name} cannot be an indexer.");

                    _ = property.GetValue(null);
                }
                catch (Exception e)
                {
                    logger.LogError(e);
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
}
