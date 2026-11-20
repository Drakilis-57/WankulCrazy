using System.Collections.Generic;

namespace WankulCrazyPlugin.patch
{
    /// <summary>
    /// Source de vérité unique pour les valeurs EItemType / ECollectionPackType custom
    /// et les tables unifiées de packs ouvrables, displays et correspondances de rareté/extension.
    /// </summary>
    public static class CustomItemTypes
    {
        // --- Types EItemType custom (plage 200..216) ---
        public static readonly EItemType BoosterStellar = EnumExtensions.SafeParseEItemType("BoosterStellar");
        public static readonly EItemType DisplayStellar = EnumExtensions.SafeParseEItemType("DisplayStellar");
        public static readonly EItemType BoosterStellarTaux = EnumExtensions.SafeParseEItemType("BoosterStellarTaux");
        public static readonly EItemType DisplayStellarTaux = EnumExtensions.SafeParseEItemType("DisplayStellarTaux");
        public static readonly EItemType CaleconStellar = EnumExtensions.SafeParseEItemType("CaleconStellar");
        public static readonly EItemType StarterApocalypse = EnumExtensions.SafeParseEItemType("StarterApocalypse");
        public static readonly EItemType StarterShowtime = EnumExtensions.SafeParseEItemType("StarterShowtime");
        public static readonly EItemType TapisS41 = EnumExtensions.SafeParseEItemType("TapisS41");
        public static readonly EItemType TapisS42 = EnumExtensions.SafeParseEItemType("TapisS42");
        public static readonly EItemType ClasseurS4 = EnumExtensions.SafeParseEItemType("ClasseurS4");
        public static readonly EItemType BoosterGoldBattle = EnumExtensions.SafeParseEItemType("BoosterGoldBattle");
        public static readonly EItemType BoosterGoldStellar = EnumExtensions.SafeParseEItemType("BoosterGoldStellar");
        public static readonly EItemType BoosterLegacy = EnumExtensions.SafeParseEItemType("BoosterLegacy");
        public static readonly EItemType DisplayLegacy = EnumExtensions.SafeParseEItemType("DisplayLegacy");
        public static readonly EItemType BoosterLegacyTaux = EnumExtensions.SafeParseEItemType("BoosterLegacyTaux");
        public static readonly EItemType DisplayLegacyTaux = EnumExtensions.SafeParseEItemType("DisplayLegacyTaux");
        public static readonly EItemType BoosterGoldLegacy = EnumExtensions.SafeParseEItemType("BoosterGoldLegacy");

        // --- Types ECollectionPackType custom (plage 30..33) ---
        public static readonly ECollectionPackType PackStellar = EnumExtensions.SafeParseECollectionPackType("Stellar");
        public static readonly ECollectionPackType PackStellarTaux = EnumExtensions.SafeParseECollectionPackType("StellarTaux");
        public static readonly ECollectionPackType PackLegacy = EnumExtensions.SafeParseECollectionPackType("Legacy");
        public static readonly ECollectionPackType PackLegacyTaux = EnumExtensions.SafeParseECollectionPackType("LegacyTaux");
        public static readonly ECollectionPackType PackAscension = EnumExtensions.SafeParseECollectionPackType("AscensionCardPack");

        // --- Groupes pour le catalogue boutique & licences ---
        public static readonly EItemType[] ShopCustomItems =
        {
            BoosterStellar, DisplayStellar, BoosterStellarTaux, DisplayStellarTaux,
            BoosterLegacy, DisplayLegacy, BoosterLegacyTaux, DisplayLegacyTaux,
            StarterApocalypse, StarterShowtime, CaleconStellar, TapisS41, TapisS42, ClasseurS4
        };

        public static readonly EItemType[] NormalCustomPacks =
        {
            BoosterStellar, DisplayStellar, BoosterLegacy, DisplayLegacy
        };

        public static readonly EItemType[] StarterCustomItems =
        {
            StarterApocalypse, StarterShowtime
        };

        public static readonly EItemType[] TauxCustomPacks =
        {
            BoosterStellarTaux, DisplayStellarTaux, BoosterLegacyTaux, DisplayLegacyTaux
        };

        public static readonly EItemType[] AccessoryCustomItems =
        {
            TapisS41, TapisS42, ClasseurS4
        };

        public static readonly EItemType[] CustomCardPacksForWorkers =
        {
            BoosterStellar, BoosterStellarTaux, BoosterLegacy, BoosterLegacyTaux
        };

        public static readonly HashSet<EItemType> TauxCustomTypes = new HashSet<EItemType>
        {
            BoosterStellarTaux, DisplayStellarTaux, BoosterLegacyTaux, DisplayLegacyTaux
        };

        public static readonly HashSet<EItemType> CustomDisplays = new HashSet<EItemType>
        {
            DisplayStellar, DisplayStellarTaux, DisplayLegacy, DisplayLegacyTaux
        };

        public static readonly HashSet<EItemType> CustomBoosters = new HashSet<EItemType>
        {
            BoosterStellar, BoosterStellarTaux, BoosterLegacy, BoosterLegacyTaux
        };

        public static readonly HashSet<EItemType> GoldBoosters = new HashSet<EItemType>
        {
            BoosterGoldBattle, BoosterGoldStellar, BoosterGoldLegacy
        };

        public static readonly HashSet<ECollectionPackType> CustomPackTypes = new HashSet<ECollectionPackType>
        {
            PackStellar, PackStellarTaux, PackLegacy, PackLegacyTaux
        };

        // --- Tables uniques partagées ---
        public static readonly HashSet<EItemType> OpenablePacks = new HashSet<EItemType>
        {
            EItemType.BasicCardPack,
            EItemType.RareCardPack,
            EItemType.EpicCardPack,
            EItemType.LegendaryCardPack,
            EItemType.DestinyBasicCardPack,
            EItemType.DestinyRareCardPack,
            EItemType.DestinyEpicCardPack,
            EItemType.DestinyLegendaryCardPack,
            EItemType.GhostPack,
            EItemType.MegabotPack,
            EItemType.FantasyRPGPack,
            EItemType.CatJobPack,
            EItemType.AscensionCardPack,
            BoosterStellar,
            BoosterStellarTaux,
            BoosterLegacy,
            BoosterLegacyTaux,
            BoosterGoldBattle,
            BoosterGoldStellar,
            BoosterGoldLegacy
        };

        public static readonly Dictionary<EItemType, EItemType> DisplayToBooster = new Dictionary<EItemType, EItemType>
        {
            { EItemType.BasicCardBox, EItemType.BasicCardPack },
            { EItemType.RareCardBox, EItemType.RareCardPack },
            { EItemType.EpicCardBox, EItemType.EpicCardPack },
            { EItemType.LegendaryCardBox, EItemType.LegendaryCardPack },
            { EItemType.DestinyBasicCardBox, EItemType.DestinyBasicCardPack },
            { EItemType.DestinyRareCardBox, EItemType.DestinyRareCardPack },
            { EItemType.DestinyEpicCardBox, EItemType.DestinyEpicCardPack },
            { EItemType.DestinyLegendaryCardBox, EItemType.DestinyLegendaryCardPack },
            { EItemType.AscensionCardBox, EItemType.AscensionCardPack },
            { DisplayStellar, BoosterStellar },
            { DisplayStellarTaux, BoosterStellarTaux },
            { DisplayLegacy, BoosterLegacy },
            { DisplayLegacyTaux, BoosterLegacyTaux }
        };

        public static readonly HashSet<EItemType> OpenableDisplays = new HashSet<EItemType>(DisplayToBooster.Keys);

        public static readonly Dictionary<EItemType, ECollectionPackType> CustomItemToCollectionPackType = new Dictionary<EItemType, ECollectionPackType>
        {
            { BoosterStellar, PackStellar },
            { DisplayStellar, PackStellar },
            { BoosterGoldStellar, PackStellar },
            { BoosterStellarTaux, PackStellarTaux },
            { DisplayStellarTaux, PackStellarTaux },
            { BoosterLegacy, PackLegacy },
            { DisplayLegacy, PackLegacy },
            { BoosterGoldLegacy, PackLegacy },
            { BoosterLegacyTaux, PackLegacyTaux },
            { DisplayLegacyTaux, PackLegacyTaux },
            { BoosterGoldBattle, ECollectionPackType.EpicCardPack }
        };

        public static readonly Dictionary<ECollectionPackType, EItemType> PackTypeToGoldBooster = new Dictionary<ECollectionPackType, EItemType>
        {
            { ECollectionPackType.EpicCardPack, BoosterGoldBattle },
            { ECollectionPackType.DestinyEpicCardPack, BoosterGoldBattle },
            { PackStellar, BoosterGoldStellar },
            { PackStellarTaux, BoosterGoldStellar },
            { PackLegacy, BoosterGoldLegacy },
            { PackLegacyTaux, BoosterGoldLegacy }
        };

        public static bool IsOpenablePack(EItemType itemType) => OpenablePacks.Contains(itemType);
        public static bool IsOpenableDisplay(EItemType itemType) => OpenableDisplays.Contains(itemType);
        public static bool IsGoldBooster(EItemType itemType) => GoldBoosters.Contains(itemType);
        public static bool IsCustomDisplay(EItemType itemType) => CustomDisplays.Contains(itemType);
        public static bool IsCustomBooster(EItemType itemType) => CustomBoosters.Contains(itemType);
        public static bool IsCustomBoosterOrGold(EItemType itemType) => CustomBoosters.Contains(itemType) || GoldBoosters.Contains(itemType);
        public static bool IsCustomPackType(ECollectionPackType packType) => CustomPackTypes.Contains(packType);

        public static EItemType GetBoosterForDisplay(EItemType displayType)
        {
            return DisplayToBooster.TryGetValue(displayType, out EItemType boosterType) ? boosterType : EItemType.None;
        }

        public static bool TryGetCustomCollectionPackType(EItemType itemType, out ECollectionPackType packType)
        {
            return CustomItemToCollectionPackType.TryGetValue(itemType, out packType);
        }

        public static bool TryGetGoldBoosterForPackType(ECollectionPackType packType, out EItemType goldBoosterType)
        {
            return PackTypeToGoldBooster.TryGetValue(packType, out goldBoosterType);
        }
    }
}
