namespace SnowyLib;

public static partial class Utils
{
    [StaticUpdate]
    internal static void UtilsUpdate()
    {
        if (terminal != null && terminal.currentNode != null)
        {
            terminal.currentNode.maxCharactersToType = 50;
        }
    }
}