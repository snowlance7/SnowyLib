using Dawn;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static SnowyLib.Plugin;

namespace SnowyLib;

[AttributeUsage(AttributeTargets.Method)]
public sealed class OnResetShipAttribute : Attribute
{
    private static readonly List<Action> methods = new();

    [StaticInit]
    internal static void Initialize()
    {
        methods.Clear();

        Assembly snowyLibAssembly = typeof(OnResetShipAttribute).Assembly;
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
                try
                {
                    if (!method.IsDefined(typeof(OnResetShipAttribute), false))
                        continue;

                    if (method.ReturnType != typeof(void))
                        throw new Exception($"{method.DeclaringType.FullName}.{method.Name} must return void.");

                    if (method.GetParameters().Length != 0)
                        throw new Exception($"{method.DeclaringType.FullName}.{method.Name} cannot have parameters.");

                    var action = (Action)Delegate.CreateDelegate(typeof(Action), method);
                    methods.Add(action);
                    action.Invoke();
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

    internal static void OnResetShip()
    {
        foreach (Action method in methods)
        {
            try
            {
                method();
            }
            catch (Exception e)
            {
                logger.LogError(e);
                continue;
            }
        }
    }
}



[HarmonyPatch]
internal static class OnResetShipAttributePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetShip))]
    private static void StartOfRound_ResetShip_Postfix()
    {
        try
        {
            OnResetShipAttribute.OnResetShip();
        }
        catch (Exception e)
        {
            logger.LogError(e);
            return;
        }
    }
}