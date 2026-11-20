using System;
using System.Collections.Generic;
using UnityEngine;
using WankulCrazy.Duel.Engine;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.duel
{
    /// <summary>
    /// Adaptateur convertissant les données du jeu de base WankulCardData
    /// en DTO DuelCard manipulable par le moteur pur DuelEngine.
    /// </summary>
    public static class CardAdapter
    {
        public class EffectMapping
        {
            public int cardIndex { get; set; }
            public List<int> cardIndexes { get; set; }
            public int? costOverride { get; set; }
            public bool isDuo { get; set; }
            public List<string> onPlay { get; set; }
            public List<string> combo { get; set; }
            public List<string> passive { get; set; }
            public List<string> sub { get; set; }
            public List<string> win { get; set; }
            public List<string> lose { get; set; }
            public List<string> effects { get; set; }

            public bool HasEnrichedFormat =>
                effects == null || effects.Count == 0 ||
                onPlay != null || combo != null || passive != null ||
                sub != null || win != null || lose != null;
        }

        private static readonly object _cacheLock = new object();
        private static Dictionary<string, WankulCardData> _cardsByIdCache;
        private static volatile Dictionary<int, EffectMapping> _mappingsByIndexCache;
        private static volatile Dictionary<int, GemMask> _gemsByIndexCache;

        /// <summary>
        /// Initialise ou enrichit manuellement le cache de cartes (utile pour injecter directement le pool en jeu ou dans les tests).
        /// </summary>
        public static void SetCardsPool(IEnumerable<WankulCardData> pool)
        {
            if (pool == null) return;
            if (_cardsByIdCache == null)
            {
                _cardsByIdCache = new Dictionary<string, WankulCardData>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (var c in pool)
            {
                if (c == null) continue;

                if (!string.IsNullOrEmpty(c.Number) && !_cardsByIdCache.ContainsKey(c.Number))
                {
                    _cardsByIdCache[c.Number] = c;
                }

                string idxStr = c.Index.ToString();
                if (!_cardsByIdCache.ContainsKey(idxStr))
                {
                    _cardsByIdCache[idxStr] = c;
                }

                if (!string.IsNullOrEmpty(c.Title) && !_cardsByIdCache.ContainsKey(c.Title))
                {
                    _cardsByIdCache[c.Title] = c;
                }
            }
        }

        private static string ResolveCustomDeckDataPath(string fileName)
        {
            try
            {
                var type = System.Type.GetType("BepInEx.Paths, BepInEx");
                if (type != null)
                {
                    var prop = type.GetProperty("PluginPath", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (prop != null)
                    {
                        string pluginPath = (string)prop.GetValue(null);
                        string p = System.IO.Path.Combine(pluginPath, "WankulCrazy", "data", "customdecks", fileName);
                        if (System.IO.File.Exists(p)) return p;
                    }
                }
            }
            catch { }

            string baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            for (int depth = 0; depth < 6 && !string.IsNullOrEmpty(baseDir); depth++)
            {
                string candidate = System.IO.Path.Combine(baseDir, "data", "customdecks", fileName);
                if (System.IO.File.Exists(candidate)) return candidate;
                var parent = System.IO.Directory.GetParent(baseDir);
                baseDir = parent != null ? parent.FullName : null;
            }

            return System.IO.Path.Combine("data", "customdecks", fileName);
        }

        private static void LoadCardEffectsCache()
        {
            if (_mappingsByIndexCache != null) return;
            lock (_cacheLock)
            {
                if (_mappingsByIndexCache != null) return;
                var cache = new Dictionary<int, EffectMapping>();
                string effectsPath = ResolveCustomDeckDataPath("duel_card_effects.json");

                if (System.IO.File.Exists(effectsPath))
                {
                    try
                    {
                        string json = System.IO.File.ReadAllText(effectsPath, System.Text.Encoding.UTF8);
                        var list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<EffectMapping>>(json);
                        if (list != null)
                        {
                            foreach (var m in list)
                            {
                                if (m == null) continue;

                                if (m.cardIndex > 0 || m.cardIndexes == null || m.cardIndexes.Count == 0)
                                {
                                    cache[m.cardIndex] = m;
                                }
                                if (m.cardIndexes != null)
                                {
                                    foreach (int idx in m.cardIndexes)
                                    {
                                        if (idx > 0)
                                        {
                                            cache[idx] = m;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        System.Console.WriteLine("Error loading duel_card_effects.json: " + ex);
                    }
                }

                _mappingsByIndexCache = cache;
            }
        }

        private static GemMask ParseGemMask(string tokens)
        {
            if (string.IsNullOrWhiteSpace(tokens)) return GemMask.None;
            GemMask mask = GemMask.None;
            if (tokens.Contains("CO", StringComparison.OrdinalIgnoreCase)) mask |= GemMask.ClosingOrange;
            if (tokens.Contains("CV", StringComparison.OrdinalIgnoreCase)) mask |= GemMask.ClosingViolet;
            if (tokens.Contains("OO", StringComparison.OrdinalIgnoreCase)) mask |= GemMask.OpeningOrange;
            if (tokens.Contains("OV", StringComparison.OrdinalIgnoreCase)) mask |= GemMask.OpeningViolet;
            return mask;
        }

        private static void LoadCardGemsCache()
        {
            if (_gemsByIndexCache != null) return;
            lock (_cacheLock)
            {
                if (_gemsByIndexCache != null) return;
                var cache = new Dictionary<int, GemMask>();
                string gemsPath = ResolveCustomDeckDataPath("duel_card_gems.json");

                if (System.IO.File.Exists(gemsPath))
                {
                    try
                    {
                        string json = System.IO.File.ReadAllText(gemsPath, System.Text.Encoding.UTF8);
                        var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                        if (dict != null)
                        {
                            foreach (var kv in dict)
                            {
                                if (int.TryParse(kv.Key, out int cardIndex))
                                {
                                    cache[cardIndex] = ParseGemMask(kv.Value);
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        System.Console.WriteLine("Error loading duel_card_gems.json: " + ex);
                    }
                }

                _gemsByIndexCache = cache;
            }
        }

        private static void EnsureIndexBuilt()
        {
            LoadCardEffectsCache();

            if (_cardsByIdCache != null && _cardsByIdCache.Count > 0) return;

            try
            {
                TryInitializeFromWankulCardsData();
            }
            catch { }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void TryInitializeFromWankulCardsData()
        {
            // Résolution via l'assembly exécutant pour éviter le chargement prématuré de MonoBehaviour en mode test headless
            var type = typeof(CardAdapter).Assembly.GetType("WankulCrazyPlugin.cards.WankulCardsData");
            if (type != null)
            {
                var ensureMethod = type.GetMethod("EnsureInitialized", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var propInst = type.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var inst = propInst?.GetValue(null);
                if (inst != null)
                {
                    ensureMethod?.Invoke(inst, null);
                    var propCards = type.GetProperty("cards", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    var cards = propCards?.GetValue(inst) as IEnumerable<WankulCardData>;
                    if (cards != null)
                    {
                        SetCardsPool(cards);
                    }
                }
            }
        }

        public static WankulCardData GetWankulCard(DuelCard card)
        {
            if (card == null) return null;
            EnsureIndexBuilt();
            if (_cardsByIdCache != null)
            {
                if (!string.IsNullOrEmpty(card.Id))
                {
                    if (_cardsByIdCache.TryGetValue(card.Id, out var data))
                    {
                        return data;
                    }

                    // Support des IDs d'instance uniques générés par BuildLegalDeck (ex: "Joueur#12#S1-005")
                    int sepIdx = card.Id.LastIndexOf('#');
                    if (sepIdx >= 0 && sepIdx + 1 < card.Id.Length)
                    {
                        string baseId = card.Id.Substring(sepIdx + 1);
                        if (_cardsByIdCache.TryGetValue(baseId, out var dataByBaseId))
                        {
                            return dataByBaseId;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(card.Name) && _cardsByIdCache.TryGetValue(card.Name, out var dataByName))
                {
                    return dataByName;
                }
            }
            return null;
        }

        public static Sprite GetCardSprite(DuelCard card)
        {
            var data = GetWankulCard(card);
            if (data == null) return null;

            if (data.Sprite is Sprite s)
            {
                return s;
            }

            if (data.Texture is Texture2D tex)
            {
                try
                {
                    var sprite = CreateSpriteFromTexture(tex);
                    data.Sprite = sprite;
                    return sprite;
                }
                catch { }
            }

            return null;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Sprite CreateSpriteFromTexture(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static EffectMapping ResolveEffectMapping(WankulCardData data)
        {
            if (data == null) return null;
            EnsureIndexBuilt();
            if (_mappingsByIndexCache != null && _mappingsByIndexCache.TryGetValue(data.Index, out var mapping))
            {
                return mapping;
            }
            return null;
        }

        private static bool ShouldSkipCharacterEffect(string effId, int force)
        {
            if (string.IsNullOrEmpty(effId)) return true;

            // Ignorer les effets de victoire/défaite de Terrain ("win_play_free_score", etc.) placés par erreur sur des Personnages
            if (effId.StartsWith("win_", StringComparison.OrdinalIgnoreCase) ||
                effId.StartsWith("lose_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Sur l'échelle réelle Wankul (Force 0..10), ignorer les anciens multiplicateurs ATK Tetramon (boost_self_15..90)
            if (force <= 15 &&
                (effId == "boost_self_15" || effId == "boost_self_20" || effId == "boost_self_30" ||
                 effId == "boost_self_45" || effId == "boost_self_60" || effId == "boost_self_75" ||
                 effId == "boost_self_90"))
            {
                return true;
            }

            return false;
        }

        private static void AddFilteredEffects(List<string> target, IEnumerable<string> source, int force)
        {
            if (source == null) return;
            foreach (var effId in source)
            {
                if (!ShouldSkipCharacterEffect(effId, force))
                {
                    target.Add(effId);
                }
            }
        }

        private static string NormalizeTerrainEffectId(string effId, int cardIndex)
        {
            if (cardIndex >= 100000)
            {
                if (effId == "boost_laink_20") return "boost_laink_1";
                if (effId == "boost_terracid_20") return "boost_terracid_1";
            }
            return effId;
        }

        private static void AddNormalizedTerrainEffects(List<string> target, IEnumerable<string> source, int cardIndex)
        {
            if (source == null) return;
            foreach (var effId in source)
            {
                if (!string.IsNullOrEmpty(effId))
                {
                    target.Add(NormalizeTerrainEffectId(effId, cardIndex));
                }
            }
        }

        private static readonly System.Text.RegularExpressions.Regex s_ComboBoostRegex =
            new System.Text.RegularExpressions.Regex(@"\+?\s?(\d+)\s?(?:points? )?de force", System.Text.RegularExpressions.RegexOptions.Compiled);

        public static DuelCard ToDuelCard(WankulCardData cardData)
        {
            if (cardData == null)
            {
                return new DuelCard("unknown", "Inconnue", CardKind.Character, 0);
            }

            string id = !string.IsNullOrEmpty(cardData.Number) ? cardData.Number : cardData.Index.ToString();
            string name = !string.IsNullOrEmpty(cardData.Title) ? cardData.Title : "Carte Wankul";
            var mapping = ResolveEffectMapping(cardData);

            if (cardData is EffigyCardData effigy)
            {
                bool isDuo = (mapping != null && mapping.isDuo) ||
                             string.Equals(effigy.RarityId, "DUO", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(effigy.Effigy, "Duo", StringComparison.OrdinalIgnoreCase);

                // Dans les données Wankul, Force = -1 désigne les cartes utilitaires sans force imprimée (tiret '-').
                // Les cartes DUO S5 n'ont ni Force (0) ni Coût de pose (0).
                int force = isDuo ? 0 : (effigy.Force == -1 ? 0 : effigy.Force);
                int cost = isDuo ? 0 : Math.Max(0, mapping?.costOverride ?? effigy.Cost);

                var onPlayEffectIds = new List<string>();
                var comboEffectIds = new List<string>();
                var passiveEffectIds = new List<string>();
                var subEffectIds = new List<string>();
                bool hasRulesText = !string.IsNullOrWhiteSpace(effigy.Rules);
                bool hasComboText = !string.IsNullOrWhiteSpace(effigy.Combo);
                bool hasClosingGem = false;

                if (mapping != null && mapping.HasEnrichedFormat)
                {
                    AddFilteredEffects(onPlayEffectIds, mapping.onPlay, force);
                    AddFilteredEffects(comboEffectIds, mapping.combo, force);
                    AddFilteredEffects(passiveEffectIds, mapping.passive, force);
                    AddFilteredEffects(subEffectIds, mapping.sub, force);
                    hasClosingGem = comboEffectIds.Count > 0;
                }
                else
                {
                    var legacyEffects = mapping?.effects;
                    if (legacyEffects != null)
                    {
                        for (int i = 0; i < legacyEffects.Count; i++)
                        {
                            string effId = legacyEffects[i];
                            if (ShouldSkipCharacterEffect(effId, force)) continue;

                            if (effId.StartsWith("sub_", StringComparison.OrdinalIgnoreCase))
                            {
                                subEffectIds.Add(effId);
                            }
                            else if (effId.StartsWith("passive_", StringComparison.OrdinalIgnoreCase))
                            {
                                passiveEffectIds.Add(effId);
                            }
                            else if (!hasRulesText && hasComboText)
                            {
                                comboEffectIds.Add(effId);
                            }
                            else
                            {
                                onPlayEffectIds.Add(effId);
                            }
                        }
                    }

                    if (hasComboText)
                    {
                        string comboText = effigy.Combo.ToLowerInvariant();
                        hasClosingGem = true;

                        var matchBoost = s_ComboBoostRegex.Match(comboText);
                        if (matchBoost.Success && int.TryParse(matchBoost.Groups[1].Value, out int bonus))
                        {
                            comboEffectIds.Add($"boost_self_{bonus}");
                        }
                        else if (comboText.Contains("+ 20 de force") || comboText.Contains("+20 de force"))
                        {
                            comboEffectIds.Add("boost_self_20");
                        }
                        else if (comboText.Contains("+ 15") || comboText.Contains("+15"))
                        {
                            comboEffectIds.Add("boost_self_15");
                        }
                        else if (comboText.Contains("+ 30") || comboText.Contains("+30"))
                        {
                            comboEffectIds.Add("boost_self_30");
                        }
                    }
                }

                bool isDirectScoreur = effigy.isScoreur ||
                    (effigy.Rules != null && (effigy.Rules.Contains("Scoreur") || effigy.Rules.Contains("Scorez un terrain") || effigy.Rules.Contains("Scorez un Terrain")));
                bool isComboScoreur = !isDirectScoreur &&
                    effigy.Combo != null && (effigy.Combo.Contains("Scorez un Terrain") || effigy.Combo.Contains("Scorez un terrain"));

                LoadCardGemsCache();
                GemMask? mappedGems = null;
                if (_gemsByIndexCache != null && _gemsByIndexCache.TryGetValue(cardData.Index, out var foundGems))
                {
                    mappedGems = foundGems;
                }

                return new DuelCard(
                    id: id,
                    name: name,
                    kind: CardKind.Character,
                    force: force,
                    isScoreur: isDirectScoreur,
                    effectIds: onPlayEffectIds,
                    effigy: effigy.Effigy,
                    comboEffectIds: comboEffectIds,
                    hasOpeningGem: false,
                    hasClosingGem: hasClosingGem,
                    isComboScoreur: isComboScoreur,
                    gemMask: mappedGems,
                    cost: cost,
                    passiveEffectIds: passiveEffectIds,
                    subEffectIds: subEffectIds,
                    isDuo: isDuo
                );
            }

            if (cardData is TerrainCardData || cardData.CardType == CardType.Terrain)
            {
                var terrainEffectIds = new List<string>();
                var terrainPassiveIds = new List<string>();
                var terrainComboIds = new List<string>();
                var terrainSubIds = new List<string>();

                if (mapping != null && mapping.HasEnrichedFormat)
                {
                    AddNormalizedTerrainEffects(terrainEffectIds, mapping.onPlay, cardData.Index);
                    AddNormalizedTerrainEffects(terrainEffectIds, mapping.win, cardData.Index);
                    AddNormalizedTerrainEffects(terrainEffectIds, mapping.lose, cardData.Index);
                    AddNormalizedTerrainEffects(terrainPassiveIds, mapping.passive, cardData.Index);
                    AddNormalizedTerrainEffects(terrainComboIds, mapping.combo, cardData.Index);
                    AddNormalizedTerrainEffects(terrainSubIds, mapping.sub, cardData.Index);
                }
                else if (mapping?.effects != null)
                {
                    for (int i = 0; i < mapping.effects.Count; i++)
                    {
                        string effId = mapping.effects[i];
                        if (string.IsNullOrEmpty(effId)) continue;
                        string normalized = NormalizeTerrainEffectId(effId, cardData.Index);
                        if (normalized.StartsWith("passive_", StringComparison.OrdinalIgnoreCase))
                        {
                            terrainPassiveIds.Add(normalized);
                        }
                        else if (normalized.StartsWith("sub_", StringComparison.OrdinalIgnoreCase))
                        {
                            terrainSubIds.Add(normalized);
                        }
                        else
                        {
                            terrainEffectIds.Add(normalized);
                        }
                    }
                }

                return new DuelCard(
                    id: id,
                    name: name,
                    kind: CardKind.Terrain,
                    force: 0,
                    isScoreur: false,
                    effectIds: terrainEffectIds,
                    comboEffectIds: terrainComboIds,
                    hasOpeningGem: false,
                    hasClosingGem: false,
                    cost: 0,
                    passiveEffectIds: terrainPassiveIds,
                    subEffectIds: terrainSubIds,
                    isDuo: false
                );
            }

            // Fallback pour SpecialCardData ou autres
            var fallbackEffects = new List<string>();
            if (mapping != null)
            {
                if (mapping.onPlay != null) fallbackEffects.AddRange(mapping.onPlay);
                if (mapping.effects != null && fallbackEffects.Count == 0) fallbackEffects.AddRange(mapping.effects);
            }
            return new DuelCard(
                id: id,
                name: name,
                kind: CardKind.Character,
                force: 0,
                isScoreur: false,
                effectIds: fallbackEffects,
                cost: Math.Max(0, mapping?.costOverride ?? 0),
                passiveEffectIds: mapping?.passive,
                subEffectIds: mapping?.sub,
                isDuo: mapping?.isDuo ?? false
            );
        }

        public static List<DuelCard> ConvertDeck(IEnumerable<WankulCardData> deckData)
        {
            var result = new List<DuelCard>();
            if (deckData == null) return result;

            foreach (var card in deckData)
            {
                result.Add(ToDuelCard(card));
            }
            return result;
        }

        /// <summary>
        /// Construit un deck légal de 50 cartes (10 terrains, 35 persos, 5 scoreurs)
        /// en puisant dans la collection Wankul disponible et en complétant si nécessaire.
        /// </summary>
        public static List<DuelCard> BuildLegalDeck(IEnumerable<WankulCardData> pool, string prefix = "Player")
        {
            var deck = new List<DuelCard>();
            var terrains = new List<DuelCard>();
            var characters = new List<DuelCard>();
            var scoreurs = new List<DuelCard>();

            if (pool != null)
            {
                foreach (var cardData in pool)
                {
                    if (cardData == null) continue;
                    var card = ToDuelCard(cardData);
                    if (cardData.Index > 0)
                    {
                        card = CloneWithUniqueId(card, cardData.Index.ToString());
                    }
                    if (card.Kind == CardKind.Terrain)
                    {
                        terrains.Add(card);
                    }
                    else if (card.IsScoreur)
                    {
                        scoreurs.Add(card);
                    }
                    else
                    {
                        characters.Add(card);
                    }
                }
            }

            // 1. Terrains (exactement 10)
            for (int i = 0; i < 10; i++)
            {
                if (i < terrains.Count)
                {
                    deck.Add(CloneWithUniqueId(terrains[i], $"{prefix}#{deck.Count + 1}#{terrains[i].Id}"));
                }
                else
                {
                    deck.Add(new DuelCard($"{prefix}_T_{i + 1}", $"Terrain {i + 1}", CardKind.Terrain));
                }
            }

            // 2. Scoreurs (exactement 5)
            for (int i = 0; i < 5; i++)
            {
                if (i < scoreurs.Count)
                {
                    deck.Add(CloneWithUniqueId(scoreurs[i], $"{prefix}#{deck.Count + 1}#{scoreurs[i].Id}"));
                }
                else
                {
                    deck.Add(new DuelCard($"{prefix}_S_{i + 1}", $"Scoreur {i + 1}", CardKind.Character, force: 100, isScoreur: true));
                }
            }

            // 3. Personnages normaux (exactement 35)
            for (int i = 0; i < 35; i++)
            {
                if (i < characters.Count)
                {
                    deck.Add(CloneWithUniqueId(characters[i], $"{prefix}#{deck.Count + 1}#{characters[i].Id}"));
                }
                else
                {
                    int force = 50 + (i % 7) * 50;
                    deck.Add(new DuelCard($"{prefix}_C_{i + 1}", $"Perso {i + 1}", CardKind.Character, force: force));
                }
            }

            return deck;
        }

        private static DuelCard CloneWithUniqueId(DuelCard source, string instanceId)
        {
            return new DuelCard(
                id: instanceId,
                name: source.Name,
                kind: source.Kind,
                force: source.Force,
                isScoreur: source.IsScoreur,
                effectIds: source.EffectIds,
                effigy: source.Effigy,
                comboEffectIds: source.ComboEffectIds,
                hasOpeningGem: source.HasOpeningGem,
                hasClosingGem: source.HasClosingGem,
                isComboScoreur: source.IsComboScoreur,
                gemMask: source.Gems,
                cost: source.Cost,
                passiveEffectIds: source.PassiveEffectIds,
                subEffectIds: source.SubEffectIds,
                isDuo: source.IsDuo
            );
        }

        /// <summary>
        /// Génère un deck Wankul équilibré (10 terrains, 35 persos normaux, 5 scoreurs)
        /// à partir des cartes enregistrées dans WankulCardsData.
        /// </summary>
        public static List<DuelCard> GenerateFallbackDeck(string prefix = "Opponent")
        {
            return BuildLegalDeck(null, prefix);
        }
    }
}
