using System.Linq;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using static SnowyLib.Plugin;

namespace SnowyLib;

public class ItemRetrievalShip : NetworkBehaviour // TODO: Make sure this actually works
{
    private static ItemRetrievalShip

    [OnFinishGeneratingLevel]
    internal static void Init()
    {
        var systemsRoot = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(obj => obj.name == "Systems");
        var itemDropshipTransform = systemsRoot.transform.Find("ItemShipAnimContainer");
        if (itemDropshipTransform == null) { logger.LogWarning("Item dropship not found"); return; }

        var itemDropshipObj = itemDropshipTransform.gameObject;

    }
}