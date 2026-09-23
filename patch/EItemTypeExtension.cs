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
class Patch_Enum_GetName
{
    static bool Prefix(Type enumType, object value, ref string __result)
    {
        if (EnumExtensions.customEnumValues.ContainsKey(enumType) &&
            EnumExtensions.customEnumValues[enumType].TryGetValue((int)value, out string name))
        {
            __result = name;
            return false; // Skip l'original
        }
        return true;
    }
}

class Patch_Enum_IsDefined
{
    static bool Prefix(Type enumType, object value, ref bool __result)
    {
        if (EnumExtensions.customEnumValues.ContainsKey(enumType))
        {
            __result = EnumExtensions.IsValidEnumValue(enumType, (int)value);
            return false; // Skip l'original
        }
        return true;
    }
}

// 🎯 Patch Enum.Parse() pour supporter CustomItem
class Patch_Enum_Parse
{
    static bool Prefix(Type enumType, string value, bool ignoreCase, ref object __result)
    {
        if (EnumExtensions.customEnumValues.ContainsKey(enumType))
        {
            // Vérifie si la valeur existe dans l'Enum d'origine
            if (Enum.IsDefined(enumType, value))
            {
                int parsedValue = (int)Enum.Parse(enumType, value, ignoreCase);

                // Vérifie si la valeur doit être remappée
                if (EnumExtensions.remappedEnumValues.ContainsKey(enumType) &&
                    EnumExtensions.remappedEnumValues[enumType].ContainsKey(parsedValue))
                {
                    parsedValue = EnumExtensions.remappedEnumValues[enumType][parsedValue];
                }

                __result = (Enum)Enum.ToObject(enumType, parsedValue);
                return false; // Skip l'original
            }

            // Vérifie si c'est une valeur custom
            if (EnumExtensions.customEnumValues[enumType].ContainsValue(value))
            {
                __result = (Enum)Enum.ToObject(enumType, EnumExtensions.customEnumValues[enumType].FirstOrDefault(x => x.Value == value).Key);
                return false; // Skip l'original
            }

            Debug.LogError($"[WankulCrazy] Erreur JSON: '{value}' n'est pas une valeur valide pour {enumType.Name}.");
            __result = Activator.CreateInstance(enumType); // Valeur par défaut
            return false; // Skip l'original
        }

        return true; // Continue normalement pour les autres Enums
    }
}