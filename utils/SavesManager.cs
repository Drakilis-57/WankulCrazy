using System.Collections.Generic;
using WankulCrazyPlugin.cards;
using Newtonsoft.Json;
using WankulCrazyPlugin.inventory;
using System.Reflection;
using System;

using SortUI = WankulCrazyPlugin.patch.SortUI;
using CardPrice = WankulCrazyPlugin.patch.CardPrice;
namespace WankulCrazyPlugin.utils
{

    public class OldSave
    {
        public Dictionary<string, int> associations;
        public Dictionary<int, (int WankulCardIndex, string cardkey, int amount, List<float> pastPrices)> wankulCards;
        public bool savedebug = false;
    }
    public class Save
    {
        public Dictionary<string, (int WankulCardIndex, List<float> pastPercent, float generatedMarketPrice)> associationsWithPercents;
        public Dictionary<int, (int WankulCardIndex, string cardkey, int amount)> wankulCards;
        public bool savedebug = false;
        public string version = SavesManager.SaveVersion;

        // Constructeur pour la nouvelle version
        [JsonConstructor]
        public Save(Dictionary<string, (int WankulCardIndex, List<float> pastPercent, float generatedMarketPrice)> associationsWithPercents, Dictionary<int, (int WankulCardIndex, string cardkey, int amount)> wankulCards, string version, bool savedebug = false)
        {
            this.associationsWithPercents = associationsWithPercents;
            this.wankulCards = wankulCards;
            this.savedebug = savedebug;
            this.version = version;
        }

        // Constructeur pour l'ancienne version
        public Save(Dictionary<string, int> associations, Dictionary<int, (int WankulCardIndex, string cardkey, int amount, List<float> pastPrices)> oldWankulCards, bool savedebug = false)
        {
            this.associationsWithPercents = new Dictionary<string, (int WankulCardIndex, List<float> pastPercent, float generatedMarketPrice)>();
            if (associations != null)
            {
                foreach (var association in associations)
                {
                    this.associationsWithPercents[association.Key] = (association.Value, new List<float>(), 0f);
                }
            }

            this.wankulCards = new Dictionary<int, (int WankulCardIndex, string cardkey, int amount)>();
            if (oldWankulCards != null)
            {
                foreach (var card in oldWankulCards)
                {
                    this.wankulCards[card.Key] = (card.Value.WankulCardIndex, card.Value.cardkey, card.Value.amount);
                }
            }

            this.savedebug = savedebug;
        }
    }
    public class SavesManager
    {
        public static void SafeLogInfo(string message)
        {
            try
            {
                SafeLogInfoInternal(message);
            }
            catch
            {
                Console.WriteLine("[WankulCrazy] " + message);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void SafeLogInfoInternal(string message)
        {
            Plugin.LogInfo(message);
        }

        public static readonly Dictionary<int, int> LegacyItemTypeMigration = new Dictionary<int, int>
        {
            // Anciens IDs pré-1.07 (125..143) -> 200..216
            { 125, 200 }, // BoosterStellar
            { 126, 201 }, // DisplayStellar
            { 127, 202 }, // BoosterStellarTaux
            { 128, 203 }, // DisplayStellarTaux
            { 129, 204 }, // CaleconStellar
            { 130, 205 }, // StarterApocalypse
            { 131, 206 }, // StarterShowtime
            { 132, 207 }, // TapisS41
            { 133, 208 }, // TapisS42
            { 134, 209 }, // ClasseurS4
            { 135, 210 }, // BoosterGoldBattle
            { 136, 211 }, // BoosterGoldStellar
            { 139, 212 }, // BoosterLegacy
            { 140, 213 }, // DisplayLegacy
            { 141, 214 }, // BoosterLegacyTaux
            { 142, 215 }, // DisplayLegacyTaux
            { 143, 216 }, // BoosterGoldLegacy

            // Tentative précédente 5000..5016 -> 200..216
            { 5000, 200 }, // BoosterStellar
            { 5001, 201 }, // DisplayStellar
            { 5002, 202 }, // BoosterStellarTaux
            { 5003, 203 }, // DisplayStellarTaux
            { 5004, 204 }, // CaleconStellar
            { 5005, 205 }, // StarterApocalypse
            { 5006, 206 }, // StarterShowtime
            { 5007, 207 }, // TapisS41
            { 5008, 208 }, // TapisS42
            { 5009, 209 }, // ClasseurS4
            { 5010, 210 }, // BoosterGoldBattle
            { 5011, 211 }, // BoosterGoldStellar
            { 5012, 212 }, // BoosterLegacy
            { 5013, 213 }, // DisplayLegacy
            { 5014, 214 }, // BoosterLegacyTaux
            { 5015, 215 }, // DisplayLegacyTaux
            { 5016, 216 }  // BoosterGoldLegacy
        };

        public static readonly Dictionary<int, int> LegacyCollectionPackTypeMigration = new Dictionary<int, int>
        {
            // Anciennes valeurs 15..20 -> 30..33
            { 15, 30 }, // Stellar
            { 16, 31 }, // StellarTaux
            { 19, 32 }, // Legacy
            { 20, 33 }, // LegacyTaux

            // Tentative précédente 100..103 -> 30..33
            { 100, 30 }, // Stellar
            { 101, 31 }, // StellarTaux
            { 102, 32 }, // Legacy
            { 103, 33 }  // LegacyTaux
        };

        public static EItemType MigrateItemType(EItemType itemType)
        {
            int val = (int)itemType;
            if (LegacyItemTypeMigration.TryGetValue(val, out int newVal))
            {
                return (EItemType)newVal;
            }
            return itemType;
        }

        public static ECollectionPackType MigrateCollectionPackType(ECollectionPackType packType)
        {
            int val = (int)packType;
            if (LegacyCollectionPackTypeMigration.TryGetValue(val, out int newVal))
            {
                return (ECollectionPackType)newVal;
            }
            return packType;
        }

        public static void EnsureGameDataListsCapacity(CGameData gameData, int requiredItemCount = 300, int requiredPackCount = 60)
        {
            if (gameData == null) return;

            if (gameData.m_CurrentTotalItemCountList != null)
            {
                while (gameData.m_CurrentTotalItemCountList.Count < requiredItemCount)
                    gameData.m_CurrentTotalItemCountList.Add(0);
            }
            if (gameData.m_SetItemPriceList != null)
            {
                while (gameData.m_SetItemPriceList.Count < requiredItemCount)
                    gameData.m_SetItemPriceList.Add(0f);
            }
            if (gameData.m_AverageItemCostList != null)
            {
                while (gameData.m_AverageItemCostList.Count < requiredItemCount)
                    gameData.m_AverageItemCostList.Add(0f);
            }
            if (gameData.m_GeneratedCostPriceList != null)
            {
                while (gameData.m_GeneratedCostPriceList.Count < requiredItemCount)
                    gameData.m_GeneratedCostPriceList.Add(0f);
            }
            if (gameData.m_GeneratedMarketPriceList != null)
            {
                while (gameData.m_GeneratedMarketPriceList.Count < requiredItemCount)
                    gameData.m_GeneratedMarketPriceList.Add(0f);
            }
            if (gameData.m_ItemPricePercentChangeList != null)
            {
                while (gameData.m_ItemPricePercentChangeList.Count < requiredItemCount)
                    gameData.m_ItemPricePercentChangeList.Add(0f);
            }
            if (gameData.m_ItemPricePercentPastChangeList != null)
            {
                while (gameData.m_ItemPricePercentPastChangeList.Count < requiredItemCount)
                {
                    FloatList fl = new FloatList();
                    fl.floatDataList = new List<float>();
                    gameData.m_ItemPricePercentPastChangeList.Add(fl);
                }
            }
            if (gameData.m_StockSoldList != null)
            {
                while (gameData.m_StockSoldList.Count < requiredItemCount)
                    gameData.m_StockSoldList.Add(0);
            }
            if (gameData.m_CollectionCardPackCountList != null)
            {
                while (gameData.m_CollectionCardPackCountList.Count < requiredPackCount)
                    gameData.m_CollectionCardPackCountList.Add(0);
            }
        }

        public static void EnsurePlayerDataListsCapacity(int requiredItemCount = 300, int requiredPackCount = 60)
        {
            try
            {
                var method = typeof(SavesManager).GetMethod(nameof(EnsurePlayerDataListsCapacityInternal), BindingFlags.NonPublic | BindingFlags.Static);
                method?.Invoke(null, new object[] { requiredItemCount, requiredPackCount });
            }
            catch
            {
                // Dans l'environnement de test unitaire (.NET 8), CPlayerData a des méthodes InternalCall avec non-NULL RVA
                // qui provoquent un TypeLoadException propre au CLR .NET Core. Ignoré hors runtime Unity Mono.
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void EnsurePlayerDataListsCapacityInternal(int requiredItemCount, int requiredPackCount)
        {
            if (CPlayerData.m_CurrentTotalItemCountList != null)
            {
                while (CPlayerData.m_CurrentTotalItemCountList.Count < requiredItemCount)
                    CPlayerData.m_CurrentTotalItemCountList.Add(0);
            }
            if (CPlayerData.m_SetItemPriceList != null)
            {
                while (CPlayerData.m_SetItemPriceList.Count < requiredItemCount)
                    CPlayerData.m_SetItemPriceList.Add(0f);
            }
            if (CPlayerData.m_AverageItemCostList != null)
            {
                while (CPlayerData.m_AverageItemCostList.Count < requiredItemCount)
                    CPlayerData.m_AverageItemCostList.Add(0f);
            }
            if (CPlayerData.m_GeneratedCostPriceList != null)
            {
                while (CPlayerData.m_GeneratedCostPriceList.Count < requiredItemCount)
                    CPlayerData.m_GeneratedCostPriceList.Add(0f);
            }
            if (CPlayerData.m_GeneratedMarketPriceList != null)
            {
                while (CPlayerData.m_GeneratedMarketPriceList.Count < requiredItemCount)
                    CPlayerData.m_GeneratedMarketPriceList.Add(0f);
            }
            if (CPlayerData.m_ItemPricePercentChangeList != null)
            {
                while (CPlayerData.m_ItemPricePercentChangeList.Count < requiredItemCount)
                    CPlayerData.m_ItemPricePercentChangeList.Add(0f);
            }
            if (CPlayerData.m_ItemPricePercentPastChangeList != null)
            {
                while (CPlayerData.m_ItemPricePercentPastChangeList.Count < requiredItemCount)
                {
                    FloatList fl = new FloatList();
                    fl.floatDataList = new List<float>();
                    CPlayerData.m_ItemPricePercentPastChangeList.Add(fl);
                }
            }
            if (CPlayerData.m_StockSoldList != null)
            {
                while (CPlayerData.m_StockSoldList.Count < requiredItemCount)
                    CPlayerData.m_StockSoldList.Add(0);
            }
            if (CPlayerData.m_CollectionCardPackCountList != null)
            {
                while (CPlayerData.m_CollectionCardPackCountList.Count < requiredPackCount)
                    CPlayerData.m_CollectionCardPackCountList.Add(0);
            }
        }

        public static bool MigrateOldSaveData(CGameData gameData = null)
        {
            if (gameData == null)
            {
                try
                {
                    gameData = CSaveLoad.m_SavedGame;
                }
                catch
                {
                    gameData = null;
                }
            }

            bool migratedAny = false;

            if (gameData != null)
            {
                // Shelves
                if (gameData.m_ShelfSaveDataList != null)
                {
                    foreach (var shelf in gameData.m_ShelfSaveDataList)
                    {
                        if (shelf?.itemTypeAmountList == null) continue;
                        foreach (var ita in shelf.itemTypeAmountList)
                        {
                            if (ita == null) continue;
                            var migrated = MigrateItemType(ita.itemType);
                            if (migrated != ita.itemType)
                            {
                                ita.itemType = migrated;
                                migratedAny = true;
                            }
                        }
                    }
                }

                // Warehouse shelves
                if (gameData.m_WarehouseShelfSaveDataList != null)
                {
                    foreach (var wh in gameData.m_WarehouseShelfSaveDataList)
                    {
                        if (wh?.compartmentItemType == null) continue;
                        for (int i = 0; i < wh.compartmentItemType.Count; i++)
                        {
                            var migrated = MigrateItemType(wh.compartmentItemType[i]);
                            if (migrated != wh.compartmentItemType[i])
                            {
                                wh.compartmentItemType[i] = migrated;
                                migratedAny = true;
                            }
                        }
                    }
                }

                // Package box items
                if (gameData.m_PackageBoxItemSaveDataList != null)
                {
                    foreach (var box in gameData.m_PackageBoxItemSaveDataList)
                    {
                        if (box?.itemTypeAmount == null) continue;
                        var migrated = MigrateItemType(box.itemTypeAmount.itemType);
                        if (migrated != box.itemTypeAmount.itemType)
                        {
                            box.itemTypeAmount.itemType = migrated;
                            migratedAny = true;
                        }
                    }
                }

                // CardItemCombiShelf
                if (gameData.m_CardItemCombiShelfSaveDataList != null)
                {
                    foreach (var combi in gameData.m_CardItemCombiShelfSaveDataList)
                    {
                        if (combi?.itemTypeAmountList == null) continue;
                        foreach (var ita in combi.itemTypeAmountList)
                        {
                            if (ita == null) continue;
                            var migrated = MigrateItemType(ita.itemType);
                            if (migrated != ita.itemType)
                            {
                                ita.itemType = migrated;
                                migratedAny = true;
                            }
                        }
                    }
                }

                // AutoPackOpener
                if (gameData.m_AutoPackOpenerSaveDataList != null)
                {
                    foreach (var opener in gameData.m_AutoPackOpenerSaveDataList)
                    {
                        if (opener?.itemTypeList == null) continue;
                        for (int i = 0; i < opener.itemTypeList.Count; i++)
                        {
                            var migrated = MigrateItemType(opener.itemTypeList[i]);
                            if (migrated != opener.itemTypeList[i])
                            {
                                opener.itemTypeList[i] = migrated;
                                migratedAny = true;
                            }
                        }
                    }
                }

                // Workbench
                if (gameData.m_WorkbenchSaveDataList != null)
                {
                    foreach (var wb in gameData.m_WorkbenchSaveDataList)
                    {
                        if (wb?.itemTypeList == null) continue;
                        for (int i = 0; i < wb.itemTypeList.Count; i++)
                        {
                            var migrated = MigrateItemType(wb.itemTypeList[i]);
                            if (migrated != wb.itemTypeList[i])
                            {
                                wb.itemTypeList[i] = migrated;
                                migratedAny = true;
                            }
                        }
                    }
                }

                // Hold item type
                if (gameData.m_HoldItemTypeList != null)
                {
                    for (int i = 0; i < gameData.m_HoldItemTypeList.Count; i++)
                    {
                        var migrated = MigrateItemType(gameData.m_HoldItemTypeList[i]);
                        if (migrated != gameData.m_HoldItemTypeList[i])
                        {
                            gameData.m_HoldItemTypeList[i] = migrated;
                            migratedAny = true;
                        }
                    }
                }

                // Target buy item
                if (gameData.m_TargetBuyItemList != null)
                {
                    for (int i = 0; i < gameData.m_TargetBuyItemList.Count; i++)
                    {
                        var migrated = MigrateItemType(gameData.m_TargetBuyItemList[i]);
                        if (migrated != gameData.m_TargetBuyItemList[i])
                        {
                            gameData.m_TargetBuyItemList[i] = migrated;
                            migratedAny = true;
                        }
                    }
                }

                // Customers
                if (gameData.m_CustomerSaveDataList != null)
                {
                    foreach (var cust in gameData.m_CustomerSaveDataList)
                    {
                        if (cust?.itemInBagList != null)
                        {
                            for (int i = 0; i < cust.itemInBagList.Count; i++)
                            {
                                var migrated = MigrateItemType(cust.itemInBagList[i]);
                                if (migrated != cust.itemInBagList[i])
                                {
                                    cust.itemInBagList[i] = migrated;
                                    migratedAny = true;
                                }
                            }
                        }
                        if (cust?.itemInHandList != null)
                        {
                            for (int i = 0; i < cust.itemInHandList.Count; i++)
                            {
                                var migrated = MigrateItemType(cust.itemInHandList[i]);
                                if (migrated != cust.itemInHandList[i])
                                {
                                    cust.itemInHandList[i] = migrated;
                                    migratedAny = true;
                                }
                            }
                        }
                    }
                }

                // Capacity check and indexed lists migration
                EnsureGameDataListsCapacity(gameData, 300, 60);

                foreach (var kvp in LegacyItemTypeMigration)
                {
                    int oldId = kvp.Key;
                    int newId = kvp.Value;

                    if (gameData.m_CurrentTotalItemCountList != null && oldId < gameData.m_CurrentTotalItemCountList.Count && gameData.m_CurrentTotalItemCountList[oldId] > 0)
                    {
                        gameData.m_CurrentTotalItemCountList[newId] = gameData.m_CurrentTotalItemCountList[oldId];
                        gameData.m_CurrentTotalItemCountList[oldId] = 0;
                        migratedAny = true;
                    }

                    if (gameData.m_SetItemPriceList != null && oldId < gameData.m_SetItemPriceList.Count && gameData.m_SetItemPriceList[oldId] > 0f)
                    {
                        gameData.m_SetItemPriceList[newId] = gameData.m_SetItemPriceList[oldId];
                        gameData.m_SetItemPriceList[oldId] = 0f;
                        migratedAny = true;
                    }

                    if (gameData.m_AverageItemCostList != null && oldId < gameData.m_AverageItemCostList.Count && gameData.m_AverageItemCostList[oldId] > 0f)
                    {
                        gameData.m_AverageItemCostList[newId] = gameData.m_AverageItemCostList[oldId];
                        gameData.m_AverageItemCostList[oldId] = 0f;
                        migratedAny = true;
                    }

                    if (gameData.m_GeneratedCostPriceList != null && oldId < gameData.m_GeneratedCostPriceList.Count && gameData.m_GeneratedCostPriceList[oldId] > 0f)
                    {
                        gameData.m_GeneratedCostPriceList[newId] = gameData.m_GeneratedCostPriceList[oldId];
                        gameData.m_GeneratedCostPriceList[oldId] = 0f;
                        migratedAny = true;
                    }

                    if (gameData.m_GeneratedMarketPriceList != null && oldId < gameData.m_GeneratedMarketPriceList.Count && gameData.m_GeneratedMarketPriceList[oldId] > 0f)
                    {
                        gameData.m_GeneratedMarketPriceList[newId] = gameData.m_GeneratedMarketPriceList[oldId];
                        gameData.m_GeneratedMarketPriceList[oldId] = 0f;
                        migratedAny = true;
                    }

                    if (gameData.m_ItemPricePercentChangeList != null && oldId < gameData.m_ItemPricePercentChangeList.Count && gameData.m_ItemPricePercentChangeList[oldId] != 0f)
                    {
                        gameData.m_ItemPricePercentChangeList[newId] = gameData.m_ItemPricePercentChangeList[oldId];
                        gameData.m_ItemPricePercentChangeList[oldId] = 0f;
                        migratedAny = true;
                    }

                    if (gameData.m_StockSoldList != null && oldId < gameData.m_StockSoldList.Count && gameData.m_StockSoldList[oldId] > 0)
                    {
                        gameData.m_StockSoldList[newId] = gameData.m_StockSoldList[oldId];
                        gameData.m_StockSoldList[oldId] = 0;
                        migratedAny = true;
                    }
                }

                // Collection pack counts
                if (gameData.m_CollectionCardPackCountList != null)
                {
                    foreach (var kvp in LegacyCollectionPackTypeMigration)
                    {
                        int oldPack = kvp.Key;
                        int newPack = kvp.Value;
                        if (oldPack < gameData.m_CollectionCardPackCountList.Count && gameData.m_CollectionCardPackCountList[oldPack] > 0)
                        {
                            gameData.m_CollectionCardPackCountList[newPack] = gameData.m_CollectionCardPackCountList[oldPack];
                            gameData.m_CollectionCardPackCountList[oldPack] = 0;
                            migratedAny = true;
                        }
                    }
                }
            }

            EnsurePlayerDataListsCapacity(300, 60);

            if (migratedAny)
            {
                SafeLogInfo("[Migration] Sauvegarde migrée avec succès : anciens IDs d'items (125..143 et 5000..5016 -> 200..216) et de packs (15..20 et 100..103 -> 30..33) mis à jour.");
            }

            return migratedAny;
        }

        public static bool DebuggingSave = false;
        public static string SaveVersion = "1.1.0";
        public static void ModSave()
        {
            Plugin.LogInfo("Saving cards associations");
            Dictionary<string, WankulCardData> associations = WankulCardsData.Instance.association;
            string pluginPath = Plugin.GetPluginPath();

            int saveIndex = CGameManager.Instance.m_CurrentSaveLoadSlotSelectedIndex;

            Plugin.LogInfo("Save index: " + saveIndex);

            string path = pluginPath + "/data/save_" + saveIndex + ".json";

            Dictionary<string, (int WankulCardIndex, List<float> pastPercent, float generatedMarketPrice)> knewAssociations = new Dictionary<string, (int WankulCardIndex, List<float> pastPercent, float generatedMarketPrice)>();
            foreach (var association in associations)
            {
                //Plugin.LogInfo("Saving association: " + association.Key + " => " + association.Value.Index);
                knewAssociations.Add(association.Key, (association.Value.Index, association.Value.PastPercent, association.Value.generatedMarketPrice));
            }

            Dictionary<int, (int WankulCardIndex, string cardkey, int amount)> wankulCards = [];
            foreach (var item in WankulInventory.Instance.wankulCards)
            {
                string cardkey = $"{item.Value.card.monsterType}_{item.Value.card.borderType}_{item.Value.card.expansionType}";
                wankulCards[item.Key] = (item.Value.wankulcard.Index, cardkey, item.Value.amount);
            }

            Save save = new(knewAssociations, wankulCards, SaveVersion, false);

            string json = JsonConvert.SerializeObject(save);
            System.IO.File.WriteAllText(path, json);
        }

        public static void ModLoad()
        {
            SortUI.inited = false;
            WankulCardsData.Instance.EnsureInitialized();

            try
            {
                MigrateOldSaveData();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[ModLoad] Erreur lors de la migration de la sauvegarde : {ex.Message}");
            }

            Plugin.LogInfo("Loading cards associations");
            string pluginPath = Plugin.GetPluginPath();
            int saveIndex = CGameManager.Instance.m_CurrentSaveLoadSlotSelectedIndex;
            string path = $"{pluginPath}/data/save_{saveIndex}.json";

            Plugin.LogInfo($"Save index: {saveIndex}");
            Plugin.LogInfo($"Path: {path}");

            if (!System.IO.File.Exists(path))
            {
                Plugin.Logger.LogError("Save file does not exist.");
                return;
            }

            string json = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
            Save save = DeserializeSave(json);

            if (save.associationsWithPercents == null)
            {
                Plugin.Logger.LogError("Failed to deserialize save file.");
                Plugin.LogInfo("Trying to deserialize old save file.");
                save = DeserializeOldSave(json);
            }

            if (save == null)
            {
                Plugin.Logger.LogError("Failed to deserialize save file.");
                return;
            }

            Plugin.LogInfo("Deserialized Save object successfully.");
            //Plugin.LogInfo($"Associations count: {save.associationsWithPercents.Count}");

            if (save.savedebug)
            {
                HandleDebugSave(save);
            }
            else if (save.version == null || save.version != SaveVersion)
            {
                LoadAssociations(save);
                LoadWankulCards(save);
                foreach (var wankulCardData in WankulCardsData.Instance.cards)
                {
                    UpdateCardPriceIfNeeded(wankulCardData);
                }
            }
            else
            {
                LoadAssociations(save);
                LoadWankulCards(save);
            }
        }

        private static Save DeserializeSave(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<Save>(json);
            }
            catch (JsonSerializationException ex)
            {
                Plugin.Logger.LogError($"Failed to deserialize Save object: {ex.Message}");
                return DeserializeOldSave(json);
            }
        }

        private static Save DeserializeOldSave(string json)
        {
            try
            {
                var oldSave = JsonConvert.DeserializeObject<OldSave>(json);
                Plugin.LogInfo("Deserialized OldSave object successfully.");
                return new Save(oldSave.associations, oldSave.wankulCards, oldSave.savedebug);
            }
            catch (JsonSerializationException ex)
            {
                Plugin.Logger.LogError($"Failed to deserialize OldSave object: {ex.Message}");
                return null;
            }
        }

        private static void HandleDebugSave(Save save)
        {
            DebuggingSave = true;
            Plugin.Logger.LogWarning("Debugging Save");
            Plugin.LogInfo("Clearing save");

            ClearPlayerData();

            foreach (var wankulCardData in WankulCardsData.Instance.cards)
            {
                var cardData = WankulCardsData.Instance.GetUnassciatedCardData();
                WankulCardsData.Instance.association[$"{cardData.monsterType}_{cardData.borderType}_{cardData.expansionType}"] = wankulCardData;
                UpdateCardPriceIfNeeded(wankulCardData);
            }

            var cardsByIndex = BuildCardIndex();
            foreach (var item in save.wankulCards)
            {
                AddDebugCard(item, cardsByIndex);
            }

            DebuggingSave = false;
        }

        private static void ClearPlayerData()
        {
            CPlayerData.m_CardCollectedList.Clear();
            CPlayerData.m_CardCollectedListDestiny.Clear();
            CPlayerData.m_CardCollectedListGhost.Clear();
            CPlayerData.m_CardCollectedListGhostBlack.Clear();
            CPlayerData.m_CardCollectedListMegabot.Clear();
            CPlayerData.m_CardCollectedListFantasyRPG.Clear();
            CPlayerData.m_CardCollectedListCatJob.Clear();
        }

        private static void AddDebugCard(KeyValuePair<int, (int WankulCardIndex, string cardkey, int amount)> item, Dictionary<int, WankulCardData> cardsByIndex)
        {
            if (cardsByIndex.TryGetValue(item.Value.WankulCardIndex, out var wankulCardData))
            {
                var cardData = WankulCardsData.Instance.GetCardDataFromWankulCardData(wankulCardData);
                if (cardData != null)
                {
                    WankulInventory.Instance.wankulCards[item.Key] = (wankulCardData, cardData, item.Value.amount);
                }
                else
                {
                    Plugin.Logger.LogError($"CardData not found for index: {item.Value.WankulCardIndex}");
                }
            }
            else
            {
                Plugin.Logger.LogError($"WankulCardData not found for index: {item.Value.WankulCardIndex}");
            }
        }

        // Index -> carte, construit une fois : remplace les List.Find en O(N) par entree.
        private static Dictionary<int, WankulCardData> BuildCardIndex()
        {
            var byIndex = new Dictionary<int, WankulCardData>(WankulCardsData.Instance.cards.Count);
            foreach (var card in WankulCardsData.Instance.cards)
            {
                byIndex.TryAdd(card.Index, card); // garde la premiere, comme List.Find
            }
            return byIndex;
        }

        private static void LoadAssociations(Save save)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var cardsByIndex = BuildCardIndex();
            var associatedIndexes = new HashSet<int>();
            foreach (var existing in WankulCardsData.Instance.association.Values)
            {
                if (existing != null) associatedIndexes.Add(existing.Index);
            }

            foreach (var association in save.associationsWithPercents)
            {
                if (save.version == "1.0.0" && (association.Value.WankulCardIndex == 546 || association.Value.WankulCardIndex == 547))
                {
                    continue;
                }
                if (WankulCardsData.Instance.association.ContainsKey(association.Key) || associatedIndexes.Contains(association.Value.WankulCardIndex))
                {
                    //Plugin.LogInfo($"Association already exists: {association.Key}");
                    continue;
                }

                cardsByIndex.TryGetValue(association.Value.WankulCardIndex, out var card);
                if (card != null)
                {
                    if (association.Value.pastPercent != null && association.Value.pastPercent.Count > 0)
                    {
                        card.PastPercent = association.Value.pastPercent;
                        card.Percentage = card.PastPercent[^1];
                    }
                    if (association.Value.generatedMarketPrice != 0)
                    {
                        card.MarketPrice = association.Value.generatedMarketPrice;

                        // Migration : réinitialise le prix si une Légendaire avait un prix de Commune
                        // (< 1€) dû au bug du fallback par Drop — force le recalcul par Rarity
                        if (card is EffigyCardData effigyCardMigration &&
                            (effigyCardMigration.Rarity == Rarity.LB ||
                             effigyCardMigration.Rarity == Rarity.LA ||
                             effigyCardMigration.Rarity == Rarity.LO) &&
                            card.generatedMarketPrice < 1f)
                        {
                            Plugin.Logger.LogInfo($"[Migration] Prix Légendaire bugué détecté ({card.Title} → {card.generatedMarketPrice:F4}€), régénération forcée.");
                            card.generatedMarketPrice = 0;
                        }
                    }

                    UpdateCardPriceIfNeeded(card);
                    associatedIndexes.Add(card.Index);

                    if (WankulCardsData.IsKeyValid(association.Key))
                    {
                        WankulCardsData.Instance.association[association.Key] = card;
                    }
                    else
                    {
                        CardData cardData = WankulCardsData.Instance.GetUnassciatedCardData();
                        WankulCardsData.Instance.association[$"{cardData.monsterType}_{cardData.borderType}_{cardData.expansionType}"] = card;
                    }
                }
                else
                {
                    Plugin.Logger.LogWarning($"Obsolete or missing WankulCardData index {association.Value.WankulCardIndex} in save associations; ignoring.");
                }
            }

            Plugin.LogInfo($"Associations loaded: {WankulCardsData.Instance.association.Count} in {sw.ElapsedMilliseconds} ms");
        }

        private static void UpdateCardPriceIfNeeded(WankulCardData card)
        {
            int currentDay = CSaveLoad.m_SavedGame.m_CurrentDay + 1;
            int maxDayHistory = 30;
            while (card.PastPercent.Count < currentDay && card.PastPercent.Count < maxDayHistory)
            {
                CardPrice.UpdateCardPricePercent(card);
            }
        }

        private static void LoadWankulCards(Save save)
        {
            var cardsByIndex = BuildCardIndex();
            foreach (var item in save.wankulCards)
            {
                string cardkey = item.Value.cardkey;
                cardsByIndex.TryGetValue(item.Value.WankulCardIndex, out var wankulCardData);
                if (wankulCardData != null)
                {
                    if (WankulCardsData.IsKeyValid(cardkey))
                    {
                        var cardData = WankulCardsData.Instance.GetCardDataFromKey(item.Value.cardkey);
                        WankulInventory.Instance.wankulCards[item.Key] = (wankulCardData, cardData, item.Value.amount);
                    }
                    else
                    {
                        if (WankulInventory.Instance.wankulCards.TryGetValue(item.Key, out var existingCard))
                        {
                            existingCard.amount += item.Value.amount;
                        }
                        else
                        {
                            CardData cardData = WankulCardsData.Instance.GetCardDataFromWankulCardData(wankulCardData);
                            if (cardData != null)
                            {
                                WankulInventory.Instance.wankulCards[item.Key] = (wankulCardData, cardData, item.Value.amount);
                            }
                            else
                            {
                                Plugin.Logger.LogError($"CardData not found for index: {item.Value.WankulCardIndex}");
                            }
                        }
                    }
                }
                else
                {
                    Plugin.Logger.LogWarning($"Obsolete or missing WankulCardData index {item.Value.WankulCardIndex} in saved cards; ignoring.");
                }
            }
        }

        public static List<CardData> debubPackContent()
        {
            Type type = typeof(CardOpeningSequence);
            MethodInfo methodInfo = type.GetMethod("GetPackContent", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo rolledCardDataList = type.GetField("m_RolledCardDataList", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo cardValueList = type.GetField("m_CardValueList", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo collectionPackType = type.GetField("m_CollectionPackType", BindingFlags.NonPublic | BindingFlags.Instance);

            if (collectionPackType != null)
            {
                Plugin.LogInfo("Setting m_CollectionPackType");
                collectionPackType.SetValue(CardOpeningSequence.Instance, ECollectionPackType.BasicCardPack);
                Plugin.LogInfo("Set m_CollectionPackType");
            }
            else
            {
                Plugin.Logger.LogError($"Le champ m_CollectionPackType n'a pas été trouvé.");
            }

            if (methodInfo != null)
            {
                Plugin.LogInfo("Calling GetPackContent");
                methodInfo.Invoke(CardOpeningSequence.Instance, new object[] { true, false, false, ECollectionPackType.BasicCardPack });
                Plugin.LogInfo("GetPackContent called");
            }
            else
            {
                Plugin.Logger.LogError($"Le champ GetPackContent n'a pas été trouvé.");
            }

            if (rolledCardDataList != null)
            {
                Plugin.LogInfo("Getting m_RolledCardDataList");
                List<CardData> m_RolledCardDataList = (List<CardData>)rolledCardDataList.GetValue(CardOpeningSequence.Instance);
                Plugin.LogInfo($"Get m_RolledCardDataList count: {m_RolledCardDataList.Count}");
                return m_RolledCardDataList;
            }
            else
            {
                Plugin.Logger.LogError($"Le champ m_RolledCardDataList n'a pas été trouvé.");
            }

            return null;
        }
    }
}
