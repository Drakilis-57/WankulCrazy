using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

public static class EnumExtensions
{
    public static readonly Dictionary<Type, Dictionary<int, string>> customEnumValues = new Dictionary<Type, Dictionary<int, string>>
    {
        { typeof(EItemType), new Dictionary<int, string>
            {
                { 125, "BoosterStellar" }, { 126, "DisplayStellar" }, { 127, "BoosterStellarTaux" },
                { 128, "DisplayStellarTaux" }, { 129, "CaleconStellar" }, { 130, "StarterApocalypse" },
                { 131, "StarterShowtime" }, { 132, "TapisS41" }, { 133, "TapisS42" }, { 134, "ClasseurS4" },
                { 135, "BoosterGoldBattle" }, { 136, "BoosterGoldStellar" },
                { 137, "TestCardPack32" }, { 138, "TestCardPack64" }
            }
        },
        { typeof(ECollectionPackType), new Dictionary<int, string>
            {
                { 15, "Stellar" }, { 16, "StellarTaux" },
                { 17, "SeasonTestPack32" }, { 18, "SeasonTestPack64" }
            }
        },
        { typeof(EMonsterType), new Dictionary<int, string>
            {
                { 50000, "WankulMonster001" }, { 50001, "WankulMonster002" }
            }
        }
    };

    public static readonly Dictionary<Type, Dictionary<int, int>> remappedEnumValues = new Dictionary<Type, Dictionary<int, int>>
    {
        { typeof(ECollectionPackType), new Dictionary<int, int> { { 15, 17 } } }
    };

    private static readonly Dictionary<string, string> itemTypeAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Booster Stellar", "BoosterStellar" }, { "Display Stellar", "DisplayStellar" },
        { "Booster Stellar Taux +", "BoosterStellarTaux" }, { "Booster Stellar Taux+", "BoosterStellarTaux" },
        { "Display Stellar Taux +", "DisplayStellarTaux" }, { "Display Stellar Taux+", "DisplayStellarTaux" },
        { "Calecon Stellar", "CaleconStellar" }, { "Caleçon Stellar", "CaleconStellar" },
        { "Boxer Stellar", "CaleconStellar" }, { "Starter Apocalypse", "StarterApocalypse" },
        { "Starter Showtime", "StarterShowtime" }, { "Tapi Stellar 1", "TapisS41" },
        { "Tapi Stellar 2", "TapisS42" }, { "Tapis Stellar 1", "TapisS41" },
        { "Tapis Stellar 2", "TapisS42" }, { "Classeur Stellar", "ClasseurS4" },
        { "Booster Gold Battle", "BoosterGoldBattle" }, { "Booster Gold Stellar", "BoosterGoldStellar" },
        { "Test Card Pack 32", "TestCardPack32" }, { "Test Card Pack 64", "TestCardPack64" }
    };

    public static EItemType SafeParseEItemType(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) { Debug.LogError("[WankulCrazy] Erreur JSON: Valeur itemType vide ou null !"); return (EItemType)0; }
        string trimmed = value.Trim();
        if (itemTypeAliases.TryGetValue(trimmed, out string aliasTarget)) trimmed = aliasTarget;
        if (Enum.TryParse(typeof(EItemType), trimmed, true, out object result)) return (EItemType)result;
        var customItems = customEnumValues[typeof(EItemType)];
        var match = customItems.FirstOrDefault(x => string.Equals(x.Value, trimmed, StringComparison.OrdinalIgnoreCase));
        if (match.Value != null) return (EItemType)match.Key;
        string normalized = trimmed.Replace(" ", "").Replace("-", "").Replace("_", "");
        if (Enum.TryParse(typeof(EItemType), normalized, true, out object normalizedResult)) return (EItemType)normalizedResult;
        var normalizedMatch = customItems.FirstOrDefault(x => string.Equals(x.Value, normalized, StringComparison.OrdinalIgnoreCase));
        if (normalizedMatch.Value != null) return (EItemType)normalizedMatch.Key;
        Debug.LogError($"[WankulCrazy] Erreur JSON: '{value}' n'est pas une valeur valide pour EItemType.");
        return (EItemType)0;
    }

    public static ECollectionPackType SafeParseECollectionPackType(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return (ECollectionPackType)0;
        string trimmed = value.Trim();
        if (Enum.TryParse(typeof(ECollectionPackType), trimmed, true, out object result)) return (ECollectionPackType)result;
        var customPacks = customEnumValues[typeof(ECollectionPackType)];
        var match = customPacks.FirstOrDefault(x => string.Equals(x.Value, trimmed, StringComparison.OrdinalIgnoreCase));
        if (match.Value != null) return (ECollectionPackType)match.Key;
        string normalized = trimmed.Replace(" ", "").Replace("-", "").Replace("_", "");
        if (Enum.TryParse(typeof(ECollectionPackType), normalized, true, out object normalizedResult)) return (ECollectionPackType)normalizedResult;
        var normalizedMatch = customPacks.FirstOrDefault(x => string.Equals(x.Value, normalized, StringComparison.OrdinalIgnoreCase));
        if (normalizedMatch.Value != null) return (ECollectionPackType)normalizedMatch.Key;
        Debug.LogError($"[WankulCrazy] Erreur JSON: '{value}' n'est pas une valeur valide pour ECollectionPackType.");
        return (ECollectionPackType)0;
    }

    public static EMonsterType SafeParseEMonsterType(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return (EMonsterType)0;
        string trimmed = value.Trim();
        if (Enum.TryParse(typeof(EMonsterType), trimmed, true, out object result)) return (EMonsterType)result;
        var customMonsters = customEnumValues[typeof(EMonsterType)];
        var match = customMonsters.FirstOrDefault(x => string.Equals(x.Value, trimmed, StringComparison.OrdinalIgnoreCase));
        if (match.Value != null) return (EMonsterType)match.Key;
        string normalized = trimmed.Replace(" ", "").Replace("-", "").Replace("_", "");
        if (Enum.TryParse(typeof(EMonsterType), normalized, true, out object normalizedResult)) return (EMonsterType)normalizedResult;
        var normalizedMatch = customMonsters.FirstOrDefault(x => string.Equals(x.Value, normalized, StringComparison.OrdinalIgnoreCase));
        return normalizedMatch.Value != null ? (EMonsterType)normalizedMatch.Key : (EMonsterType)0;
    }

    public static bool TryToInt32(object value, out int result)
    {
        if (value is int i) { result = i; return true; }
        if (value is Enum) { result = Convert.ToInt32(value); return true; }
        try { if (value != null && !(value is string)) { result = Convert.ToInt32(value); return true; } } catch { }
        result = 0; return false;
    }

    public static string GetEnumName(Type enumType, int value)
    {
        if (Enum.IsDefined(enumType, value)) return Enum.GetName(enumType, value);
        return customEnumValues.ContainsKey(enumType) && customEnumValues[enumType].TryGetValue(value, out string name) ? name : "Unknown";
    }

    public static bool IsValidEnumValue(Type enumType, int value) => Enum.IsDefined(enumType, value) || (customEnumValues.ContainsKey(enumType) && customEnumValues[enumType].ContainsKey(value));
}

public class Patch_Enum_GetName
{
    public static bool Prefix(Type enumType, object value, ref string __result)
    {
        if (enumType != null && value != null && EnumExtensions.customEnumValues.TryGetValue(enumType, out var values) && EnumExtensions.TryToInt32(value, out int intVal) && values.TryGetValue(intVal, out string name)) { __result = name; return false; }
        return true;
    }
}

public class Patch_Enum_IsDefined
{
    public static bool Prefix(Type enumType, object value, ref bool __result)
    {
        if (enumType != null && value != null && EnumExtensions.customEnumValues.TryGetValue(enumType, out var values))
        {
            if (value is string strVal && values.ContainsValue(strVal)) { __result = true; return false; }
            if (EnumExtensions.TryToInt32(value, out int intVal) && values.ContainsKey(intVal)) { __result = true; return false; }
        }
        return true;
    }
}

public class Patch_Enum_Parse
{
    public static bool Prefix(Type enumType, string value, bool ignoreCase, ref object __result)
    {
        if (enumType != null && !string.IsNullOrEmpty(value) && EnumExtensions.customEnumValues.TryGetValue(enumType, out var values))
        {
            var match = values.FirstOrDefault(x => string.Equals(x.Value, value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
            if (match.Value != null) { __result = Enum.ToObject(enumType, match.Key); return false; }
        }
        return true;
    }
}
