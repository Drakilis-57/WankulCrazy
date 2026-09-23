using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

public static class EnumExtensions
{
    // 🎯 Dictionnaire des valeurs custom
    public static readonly Dictionary<Type, Dictionary<int, string>> customEnumValues = new Dictionary<Type, Dictionary<int, string>>
    {
        {
            typeof(EItemType), new Dictionary<int, string>
            {
                { 125, "BoosterStellar" },
                { 126, "DisplayStellar" },
                { 127, "BoosterStellarTaux" },
                { 128, "DisplayStellarTaux" },
                { 129, "CaleconStellar" },
                { 130, "StarterApocalypse" },
                { 131, "StarterShowtime" },
                { 132, "TapisS41" },
                { 133, "TapisS42" },
                { 134, "ClasseurS4" },
                { 135, "BoosterGoldBattle" },
                { 136, "BoosterGoldStellar" }
            }
        },
        {
            typeof(ECollectionPackType), new Dictionary<int, string>
            {
                { 15, "Stellar" },
                { 16, "StellarTaux" },
            }
        },
        {
            typeof(EMonsterType), new Dictionary<int, string>
            {
                { 50000, "WankulMonster001" },
                { 50001, "WankulMonster002" },
                // Ajouter une entrée par nouvelle carte custom
            }
        }
    };

    public static readonly Dictionary<Type, Dictionary<int, int>> remappedEnumValues = new Dictionary<Type, Dictionary<int, int>>
    {
        {
            typeof(ECollectionPackType), new Dictionary<int, int>
            {
                { 15, 17 },
            }
        }
    };

    private static readonly Dictionary<string, string> itemTypeAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Booster Stellar", "BoosterStellar" },
        { "Display Stellar", "DisplayStellar" },
        { "Booster Stellar Taux +", "BoosterStellarTaux" },
        { "Booster Stellar Taux+", "BoosterStellarTaux" },
        { "Display Stellar Taux +", "DisplayStellarTaux" },
        { "Display Stellar Taux+", "DisplayStellarTaux" },
        { "Calecon Stellar", "CaleconStellar" },
        { "Caleçon Stellar", "CaleconStellar" },
        { "Boxer Stellar", "CaleconStellar" },
        { "Starter Apocalypse", "StarterApocalypse" },
        { "Starter Showtime", "StarterShowtime" },
        { "Tapi Stellar 1", "TapisS41" },
        { "Tapi Stellar 2", "TapisS42" },
        { "Tapis Stellar 1", "TapisS41" },
        { "Tapis Stellar 2", "TapisS42" },
        { "Classeur Stellar", "ClasseurS4" },
        { "Booster Gold Battle", "BoosterGoldBattle" },
        { "Booster Gold Stellar", "BoosterGoldStellar" }
    };

    public static EItemType SafeParseEItemType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.LogError("[WankulCrazy] Erreur JSON: Valeur itemType vide ou null !");
            return (EItemType)0; // Valeur par défaut
        }

        string trimmed = value.Trim();

        // 1. Alias connus
        if (itemTypeAliases.TryGetValue(trimmed, out string aliasTarget))
        {
            trimmed = aliasTarget;
        }

        // 2. Vérifie si c'est une valeur définie dans l'Enum officiel (exact ou case-insensitive)
        if (Enum.TryParse(typeof(EItemType), trimmed, true, out object result))
        {
            return (EItemType)result;
        }

        // 3. Vérifie si c'est une valeur custom définie
        var customItems = EnumExtensions.customEnumValues[typeof(EItemType)];
        var match = customItems.FirstOrDefault(x => string.Equals(x.Value, trimmed, StringComparison.OrdinalIgnoreCase));
        if (match.Value != null)
        {
            return (EItemType)match.Key;
        }

        // 4. Tentative avec suppression des espaces et tirets
        string normalized = trimmed.Replace(" ", "").Replace("-", "").Replace("_", "");
        if (Enum.TryParse(typeof(EItemType), normalized, true, out object normalizedResult))
        {
            return (EItemType)normalizedResult;
        }

        var normalizedMatch = customItems.FirstOrDefault(x => string.Equals(x.Value, normalized, StringComparison.OrdinalIgnoreCase));
        if (normalizedMatch.Value != null)
        {
            return (EItemType)normalizedMatch.Key;
        }

        Debug.LogError($"[WankulCrazy] Erreur JSON: '{value}' n'est pas une valeur valide pour EItemType.");
        return (EItemType)0; // Valeur par défaut
    }

    public static ECollectionPackType SafeParseECollectionPackType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.LogError("[WankulCrazy] Erreur JSON: Valeur collectionPackType vide ou null !");
            return (ECollectionPackType)0; // Valeur par défaut
        }

        string trimmed = value.Trim();

        // Vérifie si c'est une valeur définie dans l'Enum
        if (Enum.TryParse(typeof(ECollectionPackType), trimmed, true, out object result))
        {
            return (ECollectionPackType)result;
        }

        // Vérifie si c'est une valeur custom
        var customPacks = EnumExtensions.customEnumValues[typeof(ECollectionPackType)];
        var match = customPacks.FirstOrDefault(x => string.Equals(x.Value, trimmed, StringComparison.OrdinalIgnoreCase));
        if (match.Value != null)
        {
            return (ECollectionPackType)match.Key;
        }

        // Normalisation sans espaces
        string normalized = trimmed.Replace(" ", "").Replace("-", "").Replace("_", "");
        if (Enum.TryParse(typeof(ECollectionPackType), normalized, true, out object normalizedResult))
        {
            return (ECollectionPackType)normalizedResult;
        }

        var normalizedMatch = customPacks.FirstOrDefault(x => string.Equals(x.Value, normalized, StringComparison.OrdinalIgnoreCase));
        if (normalizedMatch.Value != null)
        {
            return (ECollectionPackType)normalizedMatch.Key;
        }

        Debug.LogError($"[WankulCrazy] Erreur JSON: '{value}' n'est pas une valeur valide pour ECollectionPackType.");
        return (ECollectionPackType)0; // Valeur par défaut
    }

    public static EMonsterType SafeParseEMonsterType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.LogError("[WankulCrazy] Erreur JSON: Valeur monsterType vide ou null !");
            return (EMonsterType)0;
        }

        string trimmed = value.Trim();

        // 1. Vérifie si c'est une valeur définie dans l'Enum officiel
        if (Enum.TryParse(typeof(EMonsterType), trimmed, true, out object result))
        {
            return (EMonsterType)result;
        }

        // 2. Vérifie si c'est une valeur custom définie
        var customMonsters = EnumExtensions.customEnumValues[typeof(EMonsterType)];
        var match = customMonsters.FirstOrDefault(x => string.Equals(x.Value, trimmed, StringComparison.OrdinalIgnoreCase));
        if (match.Value != null)
        {
            return (EMonsterType)match.Key;
        }

        // 3. Tentative avec suppression des espaces et tirets
        string normalized = trimmed.Replace(" ", "").Replace("-", "").Replace("_", "");
        if (Enum.TryParse(typeof(EMonsterType), normalized, true, out object normalizedResult))
        {
            return (EMonsterType)normalizedResult;
        }

        var normalizedMatch = customMonsters.FirstOrDefault(x => string.Equals(x.Value, normalized, StringComparison.OrdinalIgnoreCase));
        if (normalizedMatch.Value != null)
        {
            return (EMonsterType)normalizedMatch.Key;
        }

        Debug.LogError($"[WankulCrazy] Erreur JSON: '{value}' n'est pas une valeur valide pour EMonsterType.");
        return (EMonsterType)0;
    }

    public static bool TryToInt32(object value, out int result)
    {
        if (value is int i) { result = i; return true; }
        if (value is Enum) { result = Convert.ToInt32(value); return true; }
        if (value != null && !(value is string))
        {
            try { result = Convert.ToInt32(value); return true; } catch { }
        }
        result = 0;
        return false;
    }

    public static string GetEnumName(Type enumType, int value)
    {
        if (Enum.IsDefined(enumType, value))
            return Enum.GetName(enumType, value);
        if (customEnumValues.ContainsKey(enumType) && customEnumValues[enumType].ContainsKey(value))
            return customEnumValues[enumType][value];
        return "Unknown";
    }

    public static bool IsValidEnumValue(Type enumType, int value)
    {
        return Enum.IsDefined(enumType, value) ||
               (customEnumValues.ContainsKey(enumType) && customEnumValues[enumType].ContainsKey(value));
    }
}

// 🎯 Patch (int)myitem.type → Supporte 999
// 🎯 Patch Enum.GetName() et Enum.IsDefined()
public class Patch_Enum_GetName
{
    public static bool Prefix(Type enumType, object value, ref string __result)
    {
        if (enumType != null && value != null && EnumExtensions.customEnumValues.TryGetValue(enumType, out var customDict))
        {
            if (EnumExtensions.TryToInt32(value, out int intVal))
            {
                if (customDict.TryGetValue(intVal, out string name))
                {
                    __result = name;
                    return false; // Skip l'original
                }
            }
        }
        return true;
    }
}

public class Patch_Enum_IsDefined
{
    public static bool Prefix(Type enumType, object value, ref bool __result)
    {
        if (enumType != null && value != null && EnumExtensions.customEnumValues.TryGetValue(enumType, out var customDict))
        {
            if (value is string strVal)
            {
                if (customDict.ContainsValue(strVal))
                {
                    __result = true;
                    return false; // Skip l'original
                }
                return true; // Laisse faire Enum.IsDefined d'origine pour les chaînes natifs
            }

            if (EnumExtensions.TryToInt32(value, out int intVal))
            {
                if (customDict.ContainsKey(intVal))
                {
                    __result = true;
                    return false; // Skip l'original
                }
            }
        }
        return true;
    }
}

// 🎯 Patch Enum.Parse() pour supporter CustomItem
public class Patch_Enum_Parse
{
    public static bool Prefix(Type enumType, string value, bool ignoreCase, ref object __result)
    {
        if (enumType != null && !string.IsNullOrEmpty(value) && EnumExtensions.customEnumValues.TryGetValue(enumType, out var customDict))
        {
            StringComparison comp = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            // 1. Vérifie si c'est une valeur custom
            var customMatch = customDict.FirstOrDefault(x => string.Equals(x.Value, value, comp));
            if (customMatch.Value != null)
            {
                __result = Enum.ToObject(enumType, customMatch.Key);
                return false; // Skip l'original
            }

            // 2. Vérifie si la valeur doit être remappée
            try
            {
                if (EnumExtensions.remappedEnumValues.TryGetValue(enumType, out var remappedDict))
                {
                    if (Enum.TryParse(enumType, value, ignoreCase, out object parsedObj))
                    {
                        int parsedValue = Convert.ToInt32(parsedObj);
                        if (remappedDict.TryGetValue(parsedValue, out int remappedValue))
                        {
                            __result = Enum.ToObject(enumType, remappedValue);
                            return false; // Skip l'original
                        }
                    }
                }
            }
            catch { }
        }

        return true; // Continue normalement pour les autres Enums
    }
}