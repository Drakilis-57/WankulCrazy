using System;
using System.Collections.Generic;
using Xunit;
using WankulCrazyPlugin.utils;

namespace WankulCrazyPlugin.Tests
{
    public class EnumExtensionsTests
    {
        [Fact]
        public void TryToInt32_SupportsMultipleTypes()
        {
            Assert.True(EnumExtensions.TryToInt32(42, out int res1));
            Assert.Equal(42, res1);

            Assert.True(EnumExtensions.TryToInt32(DayOfWeek.Wednesday, out int res2));
            Assert.Equal((int)DayOfWeek.Wednesday, res2);

            Assert.False(EnumExtensions.TryToInt32("notAnInt", out int res3));
            Assert.Equal(0, res3);

            Assert.False(EnumExtensions.TryToInt32(null, out int res4));
            Assert.Equal(0, res4);
        }

        [Fact]
        public void Patch_Enum_IsDefined_DoesNotThrowOnStringOrInvalidType()
        {
            bool result = false;

            // Test string value for custom enum
            bool handled = Patch_Enum_IsDefined.Prefix(typeof(global::EMonsterType), "WankulMonster001", ref result);
            Assert.False(handled); // Handled by prefix
            Assert.True(result);

            // Test string value for native enum name (passes through)
            bool handledNativeString = Patch_Enum_IsDefined.Prefix(typeof(global::EMonsterType), "EarlyPlayer", ref result);
            Assert.True(handledNativeString); // Laisse passer a l'original

            // Test integer value for custom enum
            bool handledCustomInt = Patch_Enum_IsDefined.Prefix(typeof(global::EMonsterType), 50000, ref result);
            Assert.False(handledCustomInt);
            Assert.True(result);

            // Test unhandled type/value
            bool handledNull = Patch_Enum_IsDefined.Prefix(typeof(global::EMonsterType), null, ref result);
            Assert.True(handledNull);
        }

        [Fact]
        public void Patch_Enum_GetName_ReturnsCustomName()
        {
            string? result = null;

            // Custom EItemType dans la plage sécurisée 200..216
            bool handled = Patch_Enum_GetName.Prefix(typeof(global::EItemType), 200, ref result);
            Assert.False(handled);
            Assert.Equal("BoosterStellar", result);

            string? resultLegacy = null;
            bool handledLegacy = Patch_Enum_GetName.Prefix(typeof(global::EItemType), 216, ref resultLegacy);
            Assert.False(handledLegacy);
            Assert.Equal("BoosterGoldLegacy", resultLegacy);

            // Custom ECollectionPackType dans la plage 30..33
            string? resultPack = null;
            bool handledPack = Patch_Enum_GetName.Prefix(typeof(global::ECollectionPackType), 30, ref resultPack);
            Assert.False(handledPack);
            Assert.Equal("Stellar", resultPack);

            string? resultLegacyPack = null;
            bool handledLegacyPack = Patch_Enum_GetName.Prefix(typeof(global::ECollectionPackType), 33, ref resultLegacyPack);
            Assert.False(handledLegacyPack);
            Assert.Equal("LegacyTaux", resultLegacyPack);

            // Native or unmapped int passes through
            bool handledNative = Patch_Enum_GetName.Prefix(typeof(global::EItemType), 0, ref result);
            Assert.True(handledNative);
        }

        [Fact]
        public void Patch_Enum_Parse_ParsesCustomValuesCaseInsensitively()
        {
            object? result = null;

            bool handled = Patch_Enum_Parse.Prefix(typeof(global::EItemType), "boosterstellar", true, ref result);
            Assert.False(handled);
            Assert.NotNull(result);
            Assert.Equal((global::EItemType)200, (global::EItemType)result!);

            object? packResult = null;
            bool handledPack = Patch_Enum_Parse.Prefix(typeof(global::ECollectionPackType), "stellar", true, ref packResult);
            Assert.False(handledPack);
            Assert.NotNull(packResult);
            Assert.Equal((global::ECollectionPackType)30, (global::ECollectionPackType)packResult!);

            // Native values pass through to base Enum.Parse
            bool handledNative = Patch_Enum_Parse.Prefix(typeof(global::EItemType), "None", false, ref result);
            Assert.True(handledNative);
        }

        [Fact]
        public void SafeParse_ValidatesRanges200And30()
        {
            // Vérifier les EItemType dans 200..216
            Assert.Equal((EItemType)200, EnumExtensions.SafeParseEItemType("BoosterStellar"));
            Assert.Equal((EItemType)201, EnumExtensions.SafeParseEItemType("DisplayStellar"));
            Assert.Equal((EItemType)202, EnumExtensions.SafeParseEItemType("BoosterStellarTaux"));
            Assert.Equal((EItemType)203, EnumExtensions.SafeParseEItemType("DisplayStellarTaux"));
            Assert.Equal((EItemType)204, EnumExtensions.SafeParseEItemType("CaleconStellar"));
            Assert.Equal((EItemType)205, EnumExtensions.SafeParseEItemType("StarterApocalypse"));
            Assert.Equal((EItemType)206, EnumExtensions.SafeParseEItemType("StarterShowtime"));
            Assert.Equal((EItemType)207, EnumExtensions.SafeParseEItemType("TapisS41"));
            Assert.Equal((EItemType)208, EnumExtensions.SafeParseEItemType("TapisS42"));
            Assert.Equal((EItemType)209, EnumExtensions.SafeParseEItemType("ClasseurS4"));
            Assert.Equal((EItemType)210, EnumExtensions.SafeParseEItemType("BoosterGoldBattle"));
            Assert.Equal((EItemType)211, EnumExtensions.SafeParseEItemType("BoosterGoldStellar"));
            Assert.Equal((EItemType)212, EnumExtensions.SafeParseEItemType("BoosterLegacy"));
            Assert.Equal((EItemType)213, EnumExtensions.SafeParseEItemType("DisplayLegacy"));
            Assert.Equal((EItemType)214, EnumExtensions.SafeParseEItemType("BoosterLegacyTaux"));
            Assert.Equal((EItemType)215, EnumExtensions.SafeParseEItemType("DisplayLegacyTaux"));
            Assert.Equal((EItemType)216, EnumExtensions.SafeParseEItemType("BoosterGoldLegacy"));

            // Vérifier les ECollectionPackType dans 30..33
            Assert.Equal((ECollectionPackType)30, EnumExtensions.SafeParseECollectionPackType("Stellar"));
            Assert.Equal((ECollectionPackType)31, EnumExtensions.SafeParseECollectionPackType("StellarTaux"));
            Assert.Equal((ECollectionPackType)32, EnumExtensions.SafeParseECollectionPackType("Legacy"));
            Assert.Equal((ECollectionPackType)33, EnumExtensions.SafeParseECollectionPackType("LegacyTaux"));

            // remappedEnumValues ne contient plus de mapping sur ECollectionPackType (pas de conflit avec FoodieGO 15)
            if (EnumExtensions.remappedEnumValues.TryGetValue(typeof(ECollectionPackType), out var dict))
            {
                Assert.False(dict.ContainsKey(15), "Le mapping ECollectionPackType 15 ne doit plus être présent");
            }
        }

        [Fact]
        public void SavesManager_MigratesOldIdsToNewRanges()
        {
            // Vérifier la migration unitaire EItemType depuis 125..143
            Assert.Equal((EItemType)200, SavesManager.MigrateItemType((EItemType)125));
            Assert.Equal((EItemType)201, SavesManager.MigrateItemType((EItemType)126));
            Assert.Equal((EItemType)202, SavesManager.MigrateItemType((EItemType)127));
            Assert.Equal((EItemType)203, SavesManager.MigrateItemType((EItemType)128));
            Assert.Equal((EItemType)204, SavesManager.MigrateItemType((EItemType)129));
            Assert.Equal((EItemType)205, SavesManager.MigrateItemType((EItemType)130));
            Assert.Equal((EItemType)206, SavesManager.MigrateItemType((EItemType)131));
            Assert.Equal((EItemType)207, SavesManager.MigrateItemType((EItemType)132));
            Assert.Equal((EItemType)208, SavesManager.MigrateItemType((EItemType)133));
            Assert.Equal((EItemType)209, SavesManager.MigrateItemType((EItemType)134));
            Assert.Equal((EItemType)210, SavesManager.MigrateItemType((EItemType)135));
            Assert.Equal((EItemType)211, SavesManager.MigrateItemType((EItemType)136));
            Assert.Equal((EItemType)212, SavesManager.MigrateItemType((EItemType)139));
            Assert.Equal((EItemType)213, SavesManager.MigrateItemType((EItemType)140));
            Assert.Equal((EItemType)214, SavesManager.MigrateItemType((EItemType)141));
            Assert.Equal((EItemType)215, SavesManager.MigrateItemType((EItemType)142));
            Assert.Equal((EItemType)216, SavesManager.MigrateItemType((EItemType)143));

            // Vérifier la migration unitaire EItemType depuis 5000..5016
            Assert.Equal((EItemType)200, SavesManager.MigrateItemType((EItemType)5000));
            Assert.Equal((EItemType)201, SavesManager.MigrateItemType((EItemType)5001));
            Assert.Equal((EItemType)202, SavesManager.MigrateItemType((EItemType)5002));
            Assert.Equal((EItemType)203, SavesManager.MigrateItemType((EItemType)5003));
            Assert.Equal((EItemType)204, SavesManager.MigrateItemType((EItemType)5004));
            Assert.Equal((EItemType)205, SavesManager.MigrateItemType((EItemType)5005));
            Assert.Equal((EItemType)206, SavesManager.MigrateItemType((EItemType)5006));
            Assert.Equal((EItemType)207, SavesManager.MigrateItemType((EItemType)5007));
            Assert.Equal((EItemType)208, SavesManager.MigrateItemType((EItemType)5008));
            Assert.Equal((EItemType)209, SavesManager.MigrateItemType((EItemType)5009));
            Assert.Equal((EItemType)210, SavesManager.MigrateItemType((EItemType)5010));
            Assert.Equal((EItemType)211, SavesManager.MigrateItemType((EItemType)5011));
            Assert.Equal((EItemType)212, SavesManager.MigrateItemType((EItemType)5012));
            Assert.Equal((EItemType)213, SavesManager.MigrateItemType((EItemType)5013));
            Assert.Equal((EItemType)214, SavesManager.MigrateItemType((EItemType)5014));
            Assert.Equal((EItemType)215, SavesManager.MigrateItemType((EItemType)5015));
            Assert.Equal((EItemType)216, SavesManager.MigrateItemType((EItemType)5016));

            // Les items vanilla ne doivent pas être affectés
            Assert.Equal(EItemType.BasicCardPack, SavesManager.MigrateItemType(EItemType.BasicCardPack));

            // Vérifier la migration unitaire ECollectionPackType depuis 15..20
            Assert.Equal((ECollectionPackType)30, SavesManager.MigrateCollectionPackType((ECollectionPackType)15));
            Assert.Equal((ECollectionPackType)31, SavesManager.MigrateCollectionPackType((ECollectionPackType)16));
            Assert.Equal((ECollectionPackType)32, SavesManager.MigrateCollectionPackType((ECollectionPackType)19));
            Assert.Equal((ECollectionPackType)33, SavesManager.MigrateCollectionPackType((ECollectionPackType)20));

            // Vérifier la migration unitaire ECollectionPackType depuis 100..103
            Assert.Equal((ECollectionPackType)30, SavesManager.MigrateCollectionPackType((ECollectionPackType)100));
            Assert.Equal((ECollectionPackType)31, SavesManager.MigrateCollectionPackType((ECollectionPackType)101));
            Assert.Equal((ECollectionPackType)32, SavesManager.MigrateCollectionPackType((ECollectionPackType)102));
            Assert.Equal((ECollectionPackType)33, SavesManager.MigrateCollectionPackType((ECollectionPackType)103));

            // Les packs vanilla ne doivent pas être affectés
            Assert.Equal(ECollectionPackType.BasicCardPack, SavesManager.MigrateCollectionPackType(ECollectionPackType.BasicCardPack));

            // Test de migration complète sur un CGameData de test (couvrant 125, 5001, 15, 101)
            CGameData testData = new CGameData();
            testData.m_ShelfSaveDataList = new List<ShelfSaveData>
            {
                new ShelfSaveData
                {
                    itemTypeAmountList = new List<ItemTypeAmount>
                    {
                        new ItemTypeAmount { itemType = (EItemType)125, amount = 10 },
                        new ItemTypeAmount { itemType = (EItemType)5000, amount = 5 }
                    }
                }
            };
            testData.m_HoldItemTypeList = new List<EItemType> { (EItemType)126, (EItemType)5001 };
            testData.m_CollectionCardPackCountList = new List<int>();
            for (int i = 0; i < 110; i++) testData.m_CollectionCardPackCountList.Add(0);
            testData.m_CollectionCardPackCountList[15] = 42;
            testData.m_CollectionCardPackCountList[101] = 12;

            testData.m_CurrentTotalItemCountList = new List<int>();
            for (int i = 0; i < 5010; i++) testData.m_CurrentTotalItemCountList.Add(0);
            testData.m_CurrentTotalItemCountList[125] = 8;
            testData.m_CurrentTotalItemCountList[5002] = 3;

            bool migrated = SavesManager.MigrateOldSaveData(testData);
            Assert.True(migrated);

            // Vérifications post-migration
            Assert.Equal((EItemType)200, testData.m_ShelfSaveDataList[0].itemTypeAmountList[0].itemType);
            Assert.Equal((EItemType)200, testData.m_ShelfSaveDataList[0].itemTypeAmountList[1].itemType);
            Assert.Equal((EItemType)201, testData.m_HoldItemTypeList[0]);
            Assert.Equal((EItemType)201, testData.m_HoldItemTypeList[1]);

            Assert.Equal(42, testData.m_CollectionCardPackCountList[30]);
            Assert.Equal(0, testData.m_CollectionCardPackCountList[15]);
            Assert.Equal(12, testData.m_CollectionCardPackCountList[31]);
            Assert.Equal(0, testData.m_CollectionCardPackCountList[101]);

            Assert.Equal(8, testData.m_CurrentTotalItemCountList[200]);
            Assert.Equal(0, testData.m_CurrentTotalItemCountList[125]);
            Assert.Equal(3, testData.m_CurrentTotalItemCountList[202]);
            Assert.Equal(0, testData.m_CurrentTotalItemCountList[5002]);
        }
    }
}
