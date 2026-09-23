using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.importer;

namespace WankulCrazyPlugin.patch;

public class GameStarting
{
    public static void OnLevelFinishedLoading(CGameManager __instance)
    {
        WankulCardsData.Instance.EnsureInitialized();

        if (__instance.m_IsGameLevel) {
            PatchTexturesImporter.ReplaceGameTextures("shared1");
            OBJImporter.DoReplace();
            CustomItemsImporter.ImportCustomItems();
        }
        else
        {
            PatchTexturesImporter.ReplaceGameTextures("shared0");
            OBJImporter.InitFiles();
            ExpansionScreen.inited = false;
        }

        // Clean up unreferenced assets and perform garbage collection at level transition
        UnityEngine.Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}
