using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace WankulCrazyPlugin.patch
{
    /// <summary>
    /// Remplace la distribution de récompenses vanilla (Tetramon) à la fin d'un duel
    /// par des Boosters Wankul appropriés au niveau du magasin et à la performance.
    ///
    /// Logique de la méthode originale (PlayTableGame.EvaluateEndGameGift) :
    ///   - Construit une pool depuis m_StockItemData_SO.m_CardPackItemTypeList (Tetramon vanilla).
    ///   - Booste les items proches du niveau actuel de shop (+ AscensionCardPack).
    ///   - Tire entre 1 et 4 packs aléatoires dans la pool.
    ///   - Spawn les objets 3D sur la table.
    ///
    /// Notre Prefix remplace la construction de la pool par des Boosters Wankul
    /// (S1 → S5, Taux +, Gold) pondérés selon le niveau et la performance.
    /// On laisse le code de spawn physique (ItemMeshData / Item.SetMesh) intact.
    /// </summary>
    public static class DuelRewardsPatch
    {
        /// <summary>
        /// Préfixe qui court-circuite la constitution de la pool de récompenses vanilla
        /// et la remplace par une pool Wankul. On retourne true pour laisser le spawn
        /// physique s'exécuter normalement avec notre liste modifiée.
        /// </summary>
        public static bool EvaluateEndGameGiftPrefix(
            PlayTableGame __instance,
            ref List<EItemType> ___m_EndGameGiftItemTypeList,
            ref List<Item> ___m_SpawnedItemList,
            int ___m_PlayerWinCount,
            int ___m_EnemyWinCount)
        {
            try
            {
                int shopLevel = CPlayerData.m_ShopLevel;
                bool isVictory = ___m_PlayerWinCount > ___m_EnemyWinCount;
                bool isDraw = ___m_PlayerWinCount == ___m_EnemyWinCount && ___m_PlayerWinCount > 0;
                bool hasPlayed = ___m_PlayerWinCount + ___m_EnemyWinCount > 0;

                // Aucun round joué → aucune récompense
                if (!hasPlayed)
                {
                    ___m_EndGameGiftItemTypeList.Clear();
                    Plugin.LogInfo("[DuelRewardsPatch] Aucun round joué, aucune récompense.");
                    return false;
                }

                // Construire la pool de boosters Wankul selon le niveau
                var pool = BuildWankulRewardPool(shopLevel, isVictory, ___m_PlayerWinCount);

                if (pool.Count == 0)
                {
                    // Fallback sécurisé : on laisse l'original s'exécuter
                    Plugin.Logger.LogWarning("[DuelRewardsPatch] Pool Wankul vide ! Fallback sur récompenses vanilla.");
                    return true;
                }

                // Nombre de packs à donner :
                // - Défaite complète (0 victoire) : 1 pack, 50% de chance d'en avoir
                // - Match nul / défaite partielle : 1-2 packs
                // - Victoire : 1-4 packs (2-5 si whitewash 3-0)
                int packCount;
                if (!isVictory && ___m_PlayerWinCount == 0)
                {
                    packCount = Random.Range(0, 100) < 50 ? 1 : 0;
                }
                else if (isDraw || ___m_PlayerWinCount < ___m_EnemyWinCount)
                {
                    packCount = Random.Range(1, 3); // 1 ou 2
                }
                else if (___m_PlayerWinCount >= 3 && ___m_EnemyWinCount == 0)
                {
                    packCount = Random.Range(2, 6); // victoire parfaite : 2-5 packs
                }
                else
                {
                    packCount = Random.Range(1, 5); // victoire standard : 1-4 packs
                }

                ___m_EndGameGiftItemTypeList.Clear();
                for (int i = 0; i < packCount; i++)
                {
                    EItemType picked = pool[Random.Range(0, pool.Count)];
                    ___m_EndGameGiftItemTypeList.Add(picked);
                    Plugin.LogInfo($"[DuelRewardsPatch] Récompense [{i + 1}/{packCount}] : {EnumExtensions.GetEnumName(typeof(EItemType), (int)(object)picked)}");
                }

                Plugin.Logger.LogInfo($"[DuelRewardsPatch] {___m_EndGameGiftItemTypeList.Count} booster(s) Wankul attribué(s) (ShopLevel={shopLevel}, PlayerWins={___m_PlayerWinCount}).");

                // Spawn physique 3D sur la table
                if (__instance.m_ItemSpawnPhysicsBlocker != null)
                {
                    __instance.m_ItemSpawnPhysicsBlocker.SetActive(true);
                }
                if (__instance.m_ItemSpawnParentGrp != null)
                {
                    __instance.m_ItemSpawnParentGrp.gameObject.SetActive(true);
                    if (__instance.m_PlayCardSetEnemy != null && __instance.m_PlayCardSetEnemy.m_ItemSpawnPos != null)
                    {
                        __instance.m_ItemSpawnParentGrp.position = __instance.m_PlayCardSetEnemy.m_ItemSpawnPos.position;
                        __instance.m_ItemSpawnParentGrp.rotation = __instance.m_PlayCardSetEnemy.m_ItemSpawnPos.rotation;
                    }
                }

                for (int l = 0; l < ___m_EndGameGiftItemTypeList.Count; l++)
                {
                    EItemType giftType = ___m_EndGameGiftItemTypeList[l];
                    ItemMeshData itemMeshData = InventoryBase.GetItemMeshData(giftType);
                    Item item = ItemSpawnManager.GetItem(__instance.m_ItemSpawnParentGrp);
                    if (item != null && itemMeshData != null)
                    {
                        item.SetMesh(itemMeshData.mesh, itemMeshData.material, giftType, itemMeshData.meshSecondary, itemMeshData.materialSecondary, itemMeshData.materialList);
                        item.transform.localPosition = Vector3.up * 0.025f * (float)(l + 1) + Vector3.right * 0.005f * (float)(l + 1) + Vector3.back * -0.005f * (float)(l + 1);
                        item.transform.localRotation = Quaternion.identity;
                        if (item.m_Collider != null)
                        {
                            item.m_Collider.enabled = true;
                        }
                        if (item.m_Rigidbody != null)
                        {
                            item.m_Rigidbody.isKinematic = false;
                        }
                        item.gameObject.SetActive(true);
                        ___m_SpawnedItemList?.Add(item);
                    }
                }

                // Retourne false pour court-circuiter la méthode vanilla qui écraserait nos récompenses
                return false;
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"[DuelRewardsPatch] Exception dans EvaluateEndGameGiftPrefix : {ex}");
                // En cas d'erreur, laisser l'original s'exécuter en fallback
                return true;
            }
        }

        /// <summary>
        /// Construit la pool de boosters Wankul pondérée selon le niveau du magasin
        /// et la performance du joueur.
        ///
        /// Paliers :
        ///   Niv 1-10  : S1 (BasicCardPack vanilla) ×5, S2 (RareCardPack) ×3
        ///   Niv 11-25 : S1 ×2, S2 ×4, S3 (EpicCardPack) ×4
        ///   Niv 26-40 : S2 ×2, S3 ×4, Stellar ×4, StellarTaux ×1
        ///   Niv 41-60 : S3 ×2, Stellar ×4, StellarTaux ×2, Legacy ×2
        ///   Niv 61+   : Stellar ×2, StellarTaux ×2, Legacy ×4, LegacyTaux ×2, GoldBattle ×1
        ///
        /// Si victoire parfaite (ex: winCount >= 3 et ennemi à 0),
        /// ajoute un Gold supplémentaire en bonus.
        /// </summary>
        private static List<EItemType> BuildWankulRewardPool(int shopLevel, bool isVictory, int playerWinCount)
        {
            var pool = new List<EItemType>();

            // EItemTypes vanilla Tetramon (toujours accessibles)
            EItemType s1 = EItemType.BasicCardPack;
            EItemType s2 = EItemType.RareCardPack;
            EItemType s3 = EItemType.EpicCardPack;

            // EItemTypes Wankul custom (via SafeParse pour éviter tout crash si absent)
            EItemType stellar = EnumExtensions.SafeParseEItemType("BoosterStellar");
            EItemType stellarTaux = EnumExtensions.SafeParseEItemType("BoosterStellarTaux");
            EItemType legacy = EnumExtensions.SafeParseEItemType("BoosterLegacy");
            EItemType legacyTaux = EnumExtensions.SafeParseEItemType("BoosterLegacyTaux");
            EItemType goldBattle = EnumExtensions.SafeParseEItemType("BoosterGoldBattle");
            EItemType goldStellar = EnumExtensions.SafeParseEItemType("BoosterGoldStellar");
            EItemType goldLegacy = EnumExtensions.SafeParseEItemType("BoosterGoldLegacy");

            // Valeur EItemType.None si le SafeParse échoue
            EItemType none = EItemType.None;

            if (shopLevel <= 10)
            {
                // Débutant : Saison 1 & 2
                AddToPool(pool, s1, 5);
                AddToPool(pool, s2, 3);
            }
            else if (shopLevel <= 25)
            {
                // Intermédiaire bas : S1-S3
                AddToPool(pool, s1, 2);
                AddToPool(pool, s2, 4);
                AddToPool(pool, s3, 4);
            }
            else if (shopLevel <= 40)
            {
                // Intermédiaire haut : S2-S4 + chance Taux
                AddToPool(pool, s2, 2);
                AddToPool(pool, s3, 4);
                AddToPool(pool, stellar, 4, none);
                AddToPool(pool, stellarTaux, 1, none);
            }
            else if (shopLevel <= 60)
            {
                // Avancé : S3-S5 + Taux
                AddToPool(pool, s3, 2);
                AddToPool(pool, stellar, 4, none);
                AddToPool(pool, stellarTaux, 2, none);
                AddToPool(pool, legacy, 2, none);
            }
            else
            {
                // Expert : Stellar & Legacy + Taux + Gold
                AddToPool(pool, stellar, 2, none);
                AddToPool(pool, stellarTaux, 2, none);
                AddToPool(pool, legacy, 4, none);
                AddToPool(pool, legacyTaux, 2, none);
                AddToPool(pool, goldBattle, 1, none);
            }

            // Bonus victoire parfaite : +1 Gold dans la pool
            if (isVictory && playerWinCount >= 3)
            {
                if (shopLevel <= 40)
                    AddToPool(pool, goldBattle, 1, none);
                else if (shopLevel <= 60)
                    AddToPool(pool, goldStellar, 1, none);
                else
                    AddToPool(pool, goldLegacy, 1, none);
            }

            return pool;
        }

        /// <summary>
        /// Ajoute count fois un EItemType à la pool, en ignorant l'entrée si elle vaut fallback (typiquement EItemType.None).
        /// </summary>
        private static void AddToPool(List<EItemType> pool, EItemType itemType, int count, EItemType fallback = EItemType.BasicCardPack)
        {
            if (itemType == fallback)
            {
                Plugin.LogInfo($"[DuelRewardsPatch] Booster ignoré (type non chargé, fallback={fallback}), on ignore.");
                return;
            }
            for (int i = 0; i < count; i++)
                pool.Add(itemType);
        }
    }
}
