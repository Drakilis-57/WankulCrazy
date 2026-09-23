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
                { 136, "BoosterGoldStellar" },
                { 137, "TestCardPack32" },
                { 138, "TestCardPack64" }
            }
        },
        {
            typeof(ECollectionPackType), new Dictionary<int, string>
            {
                { 15, "Stellar" },
                { 16, "StellarTaux" },
                { 17, "SeasonTestPack32" },
                { 18, "SeasonTestPack64" }
            }
        },
        {
            typeof(EMonsterType), new Dictionary<int, string>
            {
                { 50000, "WankulMonster001" },
                { 50001, "WankulMonster002" }
            }
        }
    };

    public static readonly Dictionary<Type, Dictionary<int, int>> remappedEnumValues = new Dictionary<Type, Dictionary<int, int>>
    {
        {
            typeof(ECollectionPackType), new Dictionary<int, int>
            {
                { 15, 17 }
            }
        }
    };

    public static bool TryToInt32(object value, out int intVal)
    {
        intVal = 0;
        if (value == null) return false;
        if (value is int i) { intVal = i; return true; }
        if (value is IConvertible convertible)
        {
            try { intVal = convertible.ToInt32(null); return true; }
            catch { return false; }
        }
        return false;
    }

    public static bool IsCustomEnumValue(Type enumType, object value)
    {
        if (value == null || !customEnumValues.TryGetValue(enumType, out var dict)) return false;

        if (value is string sVal)
        {
            return dict.Values.Any(v => string.Equals(v, sVal, StringComparison.OrdinalIgnoreCase));
        }

        if (TryToInt32(value, out int intVal))
        {
            return dict.ContainsKey(intVal);
        }

        return false;
    }

    public static EItemType SafeParseEItemType(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Debug.LogError("[WankulCrazy] Erreur JSON: Valeur itemType vide ou null !");
            return (EItemType)0;
        }

        try
        {
            if (Enum.TryParse(typeof(EItemType), value, true, out object result))
            {
                return (EItemType)result;
            }
        }
        catch (TypeLoadException) { }

        if (EnumExtensions.customEnumValues.ContainsKey(typeof(EItemType)))
        {
            var kvp = EnumExtensions.customEnumValues[typeof(EItemType)].FirstOrDefault(x => string.Equals(x.Value, value, StringComparison.OrdinalIgnoreCase));
            if (kvp.Value != null)
            {
                return (EItemType)kvp.Key;
            }
        }

        Debug.LogError($"[WankulCrazy] Erreur JSON: '{value}' n'est pas une valeur valide pour EItemType.");
        return (EItemType)0;
    }

    public static ECollectionPackType SafeParseECollectionPackType(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Debug.LogError("[WankulCrazy] Erreur JSON: Valeur itemType vide ou null !");
            return (ECollectionPackType)0;
        }

        try
        {
            if (Enum.TryParse(typeof(ECollectionPackType), value, true, out object result))
            {
                return (ECollectionPackType)result;
            }
        }
        catch (TypeLoadException) { }

        if (EnumExtensions.customEnumValues.ContainsKey(typeof(ECollectionPackType)))
        {
            var kvp = EnumExtensions.customEnumValues[typeof(ECollectionPackType)].FirstOrDefault(x => string.Equals(x.Value, value, StringComparison.OrdinalIgnoreCase));
            if (kvp.Value != null)
            {
                return (ECollectionPackType)kvp.Key;
            }
        }

        Debug.LogError($"[WankulCrazy] Erreur JSON: '{value}' n'est pas une valeur valide pour ECollectionPackType.");
        return (ECollectionPackType)0;
    }

    public static string GetEnumName(Type enumType, int value)
    {
        if (customEnumValues.ContainsKey(enumType) && customEnumValues[enumType].ContainsKey(value))
            return customEnumValues[enumType][value];
        try
        {
            if (Enum.IsDefined(enumType, value))
                return Enum.GetName(enumType, value);
        }
        catch (TypeLoadException) { }

        return "Unknown";
    }

    public static bool IsValidEnumValue(Type enumType, object value)
    {
        if (value == null) return false;
        if (IsCustomEnumValue(enumType, value)) return true;

        try
        {
            if (value is string sVal)
            {
                return Enum.IsDefined(enumType, sVal);
            }
            if (TryToInt32(value, out int intVal))
            {
                return Enum.IsDefined(enumType, intVal);
            }
        }
        catch (TypeLoadException) { }

        return false;
    }
}

class Patch_Enum_Transpiler
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .SelectMany(t => AccessTools.GetDeclaredMethods(t))
            .Where(m => m.GetParameters().Any(p => EnumExtensions.customEnumValues.ContainsKey(p.ParameterType)) ||
                        EnumExtensions.customEnumValues.ContainsKey(m.ReturnType));
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Conv_I4)
            {
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call,
                    typeof(Patch_Enum_Transpiler).GetMethod(nameof(HandleCustomEnumValue))));
            }
        }

        return codes;
    }

    public static int HandleCustomEnumValue(int originalValue)
    {
        foreach (var customEnum in EnumExtensions.customEnumValues)
        {
            if (customEnum.Value.ContainsKey(originalValue))
            {
                UnityEngine.Debug.Log($"Custom Enum détecté : {originalValue}");
                return originalValue;
            }
        }

        foreach (var remappedEnum in EnumExtensions.remappedEnumValues)
        {
            if (remappedEnum.Value.ContainsKey(originalValue))
            {
                int newValue = remappedEnum.Value[originalValue];
                UnityEngine.Debug.Log($"Valeur Enum remappée : {originalValue} → {newValue}");
                return newValue;
            }
        }

        return originalValue;
    }
}

class Patch_Enum_Comparison
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .SelectMany(t => AccessTools.GetDeclaredMethods(t))
            .Where(m => m.GetMethodBody() != null);
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        for (int i = 0; i < codes.Count - 1; i++)
        {
            if (codes[i].opcode == OpCodes.Beq || codes[i].opcode == OpCodes.Bne_Un)
            {
                codes.Insert(i, new CodeInstruction(OpCodes.Call,
                    typeof(Patch_Enum_Comparison).GetMethod(nameof(HandleEnumComparison))));
            }
        }

        return codes;
    }

    public static bool HandleEnumComparison(int a, int b)
    {
        foreach (var customEnum in EnumExtensions.customEnumValues)
        {
            if (customEnum.Value.ContainsKey(a) || customEnum.Value.ContainsKey(b))
                return a == b;
        }
        return a == b;
    }
}

public class Patch_Enum_GetName
{
    public static bool Prefix(Type enumType, object value, ref string __result)
    {
        if (value != null && EnumExtensions.TryToInt32(value, out int intVal) &&
            EnumExtensions.customEnumValues.ContainsKey(enumType) &&
            EnumExtensions.customEnumValues[enumType].TryGetValue(intVal, out string name))
        {
            __result = name;
            return false;
        }
        return true;
    }
}

public class Patch_Enum_IsDefined
{
    public static bool Prefix(Type enumType, object value, ref bool __result)
    {
        if (value != null && EnumExtensions.customEnumValues.ContainsKey(enumType))
        {
            if (EnumExtensions.IsCustomEnumValue(enumType, value))
            {
                __result = true;
                return false;
            }
            return true;
        }
        return true;
    }
}

public class Patch_Enum_Parse
{
    public static bool Prefix(Type enumType, string value, bool ignoreCase, ref object __result)
    {
        if (EnumExtensions.customEnumValues.ContainsKey(enumType))
        {
            try
            {
                if (Enum.IsDefined(enumType, value))
                {
                    int parsedValue = (int)Enum.Parse(enumType, value, ignoreCase);

                    if (EnumExtensions.remappedEnumValues.ContainsKey(enumType) &&
                        EnumExtensions.remappedEnumValues[enumType].ContainsKey(parsedValue))
                    {
                        parsedValue = EnumExtensions.remappedEnumValues[enumType][parsedValue];
                        __result = Enum.ToObject(enumType, parsedValue);
                        return false;
                    }

                    return true;
                }
            }
            catch (TypeLoadException)
            {
            }

            var customKvp = EnumExtensions.customEnumValues[enumType].FirstOrDefault(x => string.Equals(x.Value, value, StringComparison.OrdinalIgnoreCase));
            if (customKvp.Value != null)
            {
                __result = Enum.ToObject(enumType, customKvp.Key);
                return false;
            }
        }

        return true;
    }
}
