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
