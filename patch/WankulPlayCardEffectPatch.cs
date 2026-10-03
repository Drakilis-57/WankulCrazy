using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.duel;

namespace WankulCrazyPlugin.patch
{
    public class WankulEffectItem
    {
        public string type { get; set; }
        public string secondaryType { get; set; }
        public int count { get; set; } = 1;
        public int countSecondary { get; set; } = 0;
        public bool isTargetSelf { get; set; } = false;
        public bool isTargetEnemy { get; set; } = false;
        public bool isTargetBoth { get; set; } = false;
        public bool isOptional { get; set; } = false;
        public bool failIfNotEnoughCard { get; set; } = false;
        public float delayStartTime { get; set; } = 0f;
        public string requiredEffigy { get; set; } = null;
    }

    public class WankulCardEffectConfig
    {
        public int cardIndex { get; set; }
        public string cardTitle { get; set; }
        public string comment { get; set; }
        public List<WankulEffectItem> effects { get; set; } = new List<WankulEffectItem>();
    }

    /// <summary>
    /// Gestionnaire et patch d'injection des effets de cartes Wankul pour le duel.
    /// Intercepte PlayCardEffect_ScriptableObject.GetPlayEffectData.
    /// </summary>
    public static class WankulPlayCardEffectPatch
    {
        private static Dictionary<int, WankulCardEffectConfig> effectsByCardIndex = null;
        private static bool isLoaded = false;

        public static void EnsureLoaded()
        {
            if (isLoaded) return;
            isLoaded = true;
            effectsByCardIndex = new Dictionary<int, WankulCardEffectConfig>();

            try
            {
                string pluginPath = Plugin.GetPluginPath();
                string filePath = Path.Combine(pluginPath, "data", "customdecks", "card_effects.json");

                if (!File.Exists(filePath))
                {
                    Plugin.LogInfo($"[WankulPlayCardEffectPatch] Fichier non trouvé : {filePath}. Aucun effet Wankul configuré.");
                    return;
                }

                string json = File.ReadAllText(filePath);
                var list = JsonConvert.DeserializeObject<List<WankulCardEffectConfig>>(json);

                if (list != null)
                {
                    foreach (var item in list)
                    {
                        if (item != null)
                        {
                            effectsByCardIndex[item.cardIndex] = item;
                        }
                    }
                    Plugin.Logger.LogInfo($"[WankulPlayCardEffectPatch] {effectsByCardIndex.Count} configurations d'effets Wankul chargées depuis card_effects.json.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[WankulPlayCardEffectPatch] Erreur chargement card_effects.json: {ex.Message}");
            }
        }

        private static readonly Dictionary<EMonsterType, PlayEffectData> effectDataCache = new Dictionary<EMonsterType, PlayEffectData>();

        /// <summary>
        /// Postfix sur PlayCardEffect_ScriptableObject.GetPlayEffectData(EMonsterType monsterType).
        /// Si la carte est une carte Wankul enregistrée dans card_effects.json,
        /// on remplace l'effet vanilla par son effet Wankul officiel !
        /// IMPORTANT : Les PlayEffectData sont mis en cache pour garantir la stabilité des références
        /// d'instances et de playEffectQueueDataList lors des coroutines du jeu (TriggerPlayEffect).
        /// </summary>
        public static void GetPlayEffectDataPostfix(EMonsterType monsterType, ref PlayEffectData __result)
        {
            try
            {
                if (effectDataCache.TryGetValue(monsterType, out var cachedData))
                {
                    __result = cachedData;
                    return;
                }

                // Si un duel Wankul est actif, nous désactivons le moteur d'effets Tetramon
                // pour éviter que des effets comme DiscardHandCard ouvrent l'UI vanilla et bloquent le tour.
                if (WankulDuelController.Instance != null && WankulDuelController.Instance.IsWankulDuelActive)
                {
                    __result = new PlayEffectData
                    {
                        name = "WankulEmptyEffect",
                        monsterType = monsterType,
                        playEffectQueueDataList = new List<PlayEffectQueueData>(),
                        effectMonsterTypeList = new List<EMonsterType>()
                    };
                    return;
                }

                EnsureLoaded();
                if (effectsByCardIndex == null || effectsByCardIndex.Count == 0) return;

                // Résoudre WankulCardData depuis monsterType
                WankulCardData wCard = ResolveWankulCard(monsterType);
                if (wCard == null) return;

                PlayEffectData createdData;
                if (!effectsByCardIndex.TryGetValue(wCard.Index, out var config) || config.effects == null || config.effects.Count == 0)
                {
                    // Carte sans effet Wankul configuré : renvoyer un PlayEffectData vide valide
                    // (L'IA vanilla crashe si playEffectData == null en tentant de lire playEffectQueueDataList.Count)
                    createdData = new PlayEffectData
                    {
                        name = wCard.Title ?? "VanillaEmpty",
                        monsterType = monsterType,
                        playEffectQueueDataList = new List<PlayEffectQueueData>(),
                        effectMonsterTypeList = new List<EMonsterType>()
                    };
                }
                else
                {
                    var customEffect = BuildPlayEffectData(monsterType, config, wCard);
                    if (customEffect != null)
                    {
                        createdData = customEffect;
                        Plugin.Logger.LogInfo($"[WankulPlayCardEffectPatch] Effet Wankul appliqué pour '{wCard.Title}' (Index={wCard.Index}) -> {customEffect.playEffectQueueDataList[0].playEffectType}");
                    }
                    else
                    {
                        createdData = new PlayEffectData
                        {
                            name = wCard.Title ?? "VanillaEmpty",
                            monsterType = monsterType,
                            playEffectQueueDataList = new List<PlayEffectQueueData>(),
                            effectMonsterTypeList = new List<EMonsterType>()
                        };
                    }
                }

                effectDataCache[monsterType] = createdData;
                __result = createdData;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[WankulPlayCardEffectPatch] Erreur dans GetPlayEffectDataPostfix: {ex}");
            }
        }

        private static WankulCardData ResolveWankulCard(EMonsterType monsterType)
        {
            var data = WankulCardsData.Instance;
            if (data == null || data.association == null) return null;

            // Parcourir l'association WankulCardsData pour trouver la carte liée à ce monsterType
            foreach (var kvp in data.association)
            {
                if (kvp.Value == null) continue;
                string[] parts = kvp.Key.Split('_');
                if (parts.Length > 0 && Enum.TryParse<EMonsterType>(parts[0], out var mType) && mType == monsterType)
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        private static PlayEffectData BuildPlayEffectData(EMonsterType monsterType, WankulCardEffectConfig config, WankulCardData wCard)
        {
            var effectData = new PlayEffectData
            {
                name = config.cardTitle ?? "WankulEffect",
                monsterType = monsterType,
                playEffectQueueDataList = new List<PlayEffectQueueData>(),
                effectMonsterTypeList = new List<EMonsterType>()
            };

            foreach (var e in config.effects)
            {
                if (string.IsNullOrEmpty(e.type)) continue;

                // Si l'effet exige une effigie particulière (ex: "Terracid"), vérifier la carte
                if (!string.IsNullOrEmpty(e.requiredEffigy))
                {
                    bool matches = false;
                    if (wCard is EffigyCardData effigyCard && !string.IsNullOrEmpty(effigyCard.Effigy))
                    {
                        matches = string.Equals(effigyCard.Effigy, e.requiredEffigy, StringComparison.OrdinalIgnoreCase);
                    }
                    else if (!string.IsNullOrEmpty(wCard.Title))
                    {
                        matches = wCard.Title.IndexOf(e.requiredEffigy, StringComparison.OrdinalIgnoreCase) >= 0;
                    }

                    if (!matches)
                    {
                        // Effet ignoré car la carte n'a pas l'effigie requise
                        continue;
                    }
                }

                if (!Enum.TryParse<PlayEffectType>(e.type, out var effectType))
                {
                    Plugin.Logger.LogWarning($"[WankulPlayCardEffectPatch] Type d'effet inconnu : {e.type}");
                    continue;
                }

                PlayEffectTypeSecondary secType = PlayEffectTypeSecondary.None;
                if (!string.IsNullOrEmpty(e.secondaryType))
                {
                    Enum.TryParse(e.secondaryType, out secType);
                }

                var queue = new PlayEffectQueueData
                {
                    playEffectType = effectType,
                    playEffectTypeSecondary = secType,
                    countList = new List<int> { e.count },
                    isTargetBoth = e.isTargetBoth,
                    isTargetEnemy = e.isTargetEnemy,
                    isOptional = e.isOptional,
                    failIfNotEnoughCard = e.failIfNotEnoughCard,
                    delayStartTime = e.delayStartTime
                };

                if (e.countSecondary > 0)
                {
                    queue.countList.Add(e.countSecondary);
                }

                // Pour les effets comme DrawCard, DiscardHandCard, etc.
                // Par défaut, si ni enemy ni both n'est spécifié, l'effet vise le joueur actif (self)
                effectData.playEffectQueueDataList.Add(queue);
            }

            return effectData.playEffectQueueDataList.Count > 0 ? effectData : null;
        }
    }
}
