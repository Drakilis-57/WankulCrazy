using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.importer;
using WankulCrazyPlugin.utils;

namespace WankulCrazyPlugin.patch;

public class GameStarting
{
    private static Texture2D s_BillboardTex;
    private static readonly Dictionary<Material, Material> s_BillboardMaterialCache = new Dictionary<Material, Material>();
    public static void OnLevelFinishedLoading(CGameManager __instance)
    {
        WankulCardsData.Instance.EnsureInitialized();

        if (__instance.m_IsGameLevel)
        {
            PatchTexturesImporter.ReplaceGameTextures("shared1");
            OBJImporter.DoReplace();
            CustomItemsImporter.ImportCustomItems();
            PatchTexturesImporter.ApplyCustomItemMeshMaterials();
            ApplyBillboardTexture();
            LogItemDiagnostics();
        }
        else
        {
            PatchTexturesImporter.ReplaceGameTextures("shared0");
            OBJImporter.InitFiles();
            ExpansionScreen.inited = false;
            SceneLifecyclePatches.ApplyWankulLoadingCard(CSingleton<LoadingScreen>.Instance);
        }

        SceneLifecyclePatches.ApplyToAllLoadingObjects();

        // Clean up unreferenced assets and perform garbage collection at level transition
        UnityEngine.Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    private static void LogItemDiagnostics()
    {
        try
        {
            EItemType[] typesToDiag = new EItemType[]
            {
                EItemType.BasicCardBox,
                EnumExtensions.SafeParseEItemType("DisplayStellar"),
                EItemType.D20DiceBox,
                EItemType.D20DiceBox2,
                EItemType.BasicCardPack
            };

            foreach (var type in typesToDiag)
            {
                ItemMeshData data = InventoryBase.GetItemMeshData(type);
                if (data == null)
                {
                    Plugin.Logger.LogWarning($"[ItemDiag] {type}: ItemMeshData NULL");
                    continue;
                }

                string matListName = data.materialList != null ? string.Join(", ", data.materialList.Select(m => m != null ? $"{m.name}({m.shader?.name})" : "null")) : "none";
                Plugin.Logger.LogInfo(
                    $"[ItemDiag] {type}: mesh={data.mesh?.name}(submeshes={data.mesh?.subMeshCount}), " +
                    $"meshSec={data.meshSecondary?.name}(submeshes={data.meshSecondary?.subMeshCount}), " +
                    $"mat={data.material?.name}({data.material?.shader?.name}), " +
                    $"matSec={data.materialSecondary?.name}({data.materialSecondary?.shader?.name}), " +
                    $"matList=[{matListName}], arrType={data.arrayMaterialType}, texIdx={data.textureIndex}, secTexIdx={data.secondTextureIndex}"
                );
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"[ItemDiag] Erreur diag items : {ex.Message}");
        }
    }

    public static void ApplyBillboardTexture()
    {
        try
        {
            if (s_BillboardTex == null)
            {
                string billboardPath = System.IO.Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "Billboard.png");
                if (!System.IO.File.Exists(billboardPath))
                {
                    billboardPath = System.IO.Path.Combine(Plugin.GetPluginPath(), "data", "patchtextures", "shared1", "mcp_building_32_billboards_d.png");
                }
                if (!System.IO.File.Exists(billboardPath)) return;

                s_BillboardTex = PatchTexturesImporter.GetOrLoadDiskTexture(billboardPath, tex => tex.name = "WankulBillboard");
                if (s_BillboardTex == null) return;
            }
            Texture2D billboardTex = s_BillboardTex;

            if (s_BillboardMaterialCache.Count > 0)
            {
                var deadKeys = s_BillboardMaterialCache.Where(kvp => kvp.Key == null || kvp.Value == null).Select(kvp => kvp.Key).ToList();
                foreach (var k in deadKeys)
                {
                    s_BillboardMaterialCache.Remove(k);
                }
            }

            // 1. Chercher tous les GameObjects liés au Billboard
            GameObject[] allGos = UnityEngine.Object.FindObjectsOfType<GameObject>();
            int foundCount = 0;
            foreach (var go in allGos)
            {
                if (go == null) continue;
                string goName = go.name ?? "";
                if (goName.IndexOf("Billboard", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    goName.IndexOf("Light", StringComparison.OrdinalIgnoreCase) < 0 &&
                    goName.IndexOf("Text", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    Renderer r = go.GetComponent<Renderer>();
                    if (r != null)
                    {
                        Material[] mats = r.sharedMaterials;
                        if (mats != null && mats.Length > 0)
                        {
                            bool changed = false;
                            for (int i = 0; i < mats.Length; i++)
                            {
                                Material m = mats[i];
                                if (m == null) continue;
                                if (!s_BillboardMaterialCache.TryGetValue(m, out Material safeMat) || safeMat == null)
                                {
                                    safeMat = ShaderUtils.CreateSafeMaterial(ShaderUtils.EnsureNotBrokenStandard(m));
                                    if (safeMat.HasProperty("_MainTex")) safeMat.SetTexture("_MainTex", billboardTex);
                                    if (safeMat.HasProperty("_BaseMap")) safeMat.SetTexture("_BaseMap", billboardTex);
                                    if (safeMat.HasProperty("_BaseColorMap")) safeMat.SetTexture("_BaseColorMap", billboardTex);
                                    if (safeMat.HasProperty("_EmissionTex")) safeMat.SetTexture("_EmissionTex", billboardTex);
                                    safeMat.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                                    UnityEngine.Object.DontDestroyOnLoad(safeMat);
                                    s_BillboardMaterialCache[m] = safeMat;
                                    s_BillboardMaterialCache[safeMat] = safeMat;
                                }
                                else if (safeMat.HasProperty("_MainTex") && safeMat.GetTexture("_MainTex") != billboardTex)
                                {
                                    safeMat.SetTexture("_MainTex", billboardTex);
                                    if (safeMat.HasProperty("_BaseMap")) safeMat.SetTexture("_BaseMap", billboardTex);
                                    if (safeMat.HasProperty("_BaseColorMap")) safeMat.SetTexture("_BaseColorMap", billboardTex);
                                    if (safeMat.HasProperty("_EmissionTex")) safeMat.SetTexture("_EmissionTex", billboardTex);
                                }
                                if (mats[i] != safeMat)
                                {
                                    mats[i] = safeMat;
                                    changed = true;
                                }
                            }
                            if (changed)
                            {
                                r.sharedMaterials = mats;
                                foundCount++;
                                Plugin.Logger.LogInfo($"[BillboardPatch] Texture Wankul Billboard appliquée sur GameObject '{go.name}'.");
                            }
                        }
                    }
                }
            }

            // 2. Mettre à jour tous les matériaux BillboardOptimized en mémoire
            Material[] allMats = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var m in allMats)
            {
                if (m != null && (m.name == "BillboardOptimized" || m.name == "Billboard"))
                {
                    if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", billboardTex);
                    if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", billboardTex);
                    if (m.HasProperty("_BaseColorMap")) m.SetTexture("_BaseColorMap", billboardTex);
                    if (m.HasProperty("_EmissionTex")) m.SetTexture("_EmissionTex", billboardTex);
                    Plugin.Logger.LogInfo($"[BillboardPatch] Propriétés textures mises à jour sur Material '{m.name}'.");
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"[BillboardPatch] Erreur lors de l'application du Billboard : {ex.Message}");
        }
    }
}
