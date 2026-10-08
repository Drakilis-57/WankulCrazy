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
                public class EffectMapping {
            public int cardIndex { get; set; }
            public List<string> effects { get; set; }
        }

        private static Dictionary<string, WankulCardData> _cardsByIdCache;
        private static Dictionary<int, List<string>> _effectsByIndexCache;

        private static void EnsureIndexBuilt()
        {
            if (_cardsByIdCache != null) return;
            _cardsByIdCache = new Dictionary<string, WankulCardData>(StringComparer.OrdinalIgnoreCase);

            _effectsByIndexCache = new Dictionary<int, List<string>>();
            string effectsPath = "data/customdecks/duel_card_effects.json";

            try {
                var type = System.Type.GetType("BepInEx.Paths, BepInEx");
                if (type != null) {
                    var prop = type.GetProperty("PluginPath", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (prop != null) {
                        string pluginPath = (string)prop.GetValue(null);
                        string p = System.IO.Path.Combine(pluginPath, "WankulCrazy", "data", "customdecks", "duel_card_effects.json");
                        if (System.IO.File.Exists(p)) {
                            effectsPath = p;
                        }
                    }
                }
            } catch { }

            if (!System.IO.File.Exists(effectsPath)) {
                effectsPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "data", "customdecks", "duel_card_effects.json");
                if (!System.IO.File.Exists(effectsPath)) {
                    effectsPath = System.IO.Path.Combine("data", "customdecks", "duel_card_effects.json");
                }
            }

            if (System.IO.File.Exists(effectsPath)) {
                try {
                    string json = System.IO.File.ReadAllText(effectsPath, System.Text.Encoding.UTF8);
                    var list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<EffectMapping>>(json);
                    if (list != null) {
                        foreach (var m in list) {
                            _effectsByIndexCache[m.cardIndex] = m.effects;
                        }
                    }
                } catch (System.Exception ex) {
                    System.Console.WriteLine("Error loading duel_card_effects.json: " + ex);
                }
            }
            System.Collections.Generic.List<WankulCardData> allCards = null;
            try {
                var type = System.Type.GetType("WankulCrazyPlugin.cards.WankulCardsData, WankulCrazyPlugin");
                if (type != null) {
                    var prop = type.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (prop != null) {
                        var inst = prop.GetValue(null);
                        if (inst != null) {
                            var f = type.GetField("cards", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            if (f != null) {
                                allCards = f.GetValue(inst) as System.Collections.Generic.List<WankulCardData>;
                            }
                        }
                    }
                }
            } catch { }
            if (allCards == null) return;

            for (int i = 0; i < allCards.Count; i++)
            {
                var c = allCards[i];
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

        public static WankulCardData GetWankulCard(DuelCard card)
        {
            if (card == null) return null;
            EnsureIndexBuilt();
            if (_cardsByIdCache != null)
            {
                if (!string.IsNullOrEmpty(card.Id) && _cardsByIdCache.TryGetValue(card.Id, out var data))
                {
                    return data;
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
            return null;
        }
        /// <summary>
        /// Répare le mojibake (caractères UTF-8 français mal décodés comme 'FRANÃƒâ€¡AIS' ou 'FRANÃ‡AIS' -> 'FRANÇAIS').
        /// Gère le simple et le double encodage UTF-8/Windows-1252 ainsi qu'un fallback direct sans dépendance.
        /// </summary>
        public static string FixMojibake(string text)
        {
            if (string.IsNullOrEmpty(text) || !text.Contains("Ã")) return text;

            string current = text;
            try
            {
                var enc = System.Text.Encoding.GetEncoding("windows-1252");
                for (int i = 0; i < 2 && current.Contains("Ã"); i++)
                {
                    byte[] bytes = enc.GetBytes(current);
                    string decoded = System.Text.Encoding.UTF8.GetString(bytes);
                    if (decoded.Contains("\ufffd") || decoded == current)
                        break;
                    current = decoded;
                }
            }
            catch
            {
                // Unity Mono sans encodage 1252 disponible : le fallback direct ci-dessous prend le relais
            }

            // Si des résidus d'encodage double ou simple subsistent (ou si le catch a été déclenché) :
            if (current.Contains("Ã"))
            {
                // 1. Remplacements double mojibake d'abord (séquences avec préfixe Ãƒ)
                current = current
                    .Replace("Ãƒâ€¡", "Ç")
                    .Replace("ÃƒÂ§", "ç")
                    .Replace("ÃƒÂ©", "é")
                    .Replace("ÃƒÂ¨", "è")
                    .Replace("ÃƒÂª", "ê")
                    .Replace("ÃƒÂ«", "ë")
                    .Replace("Ãƒ ", "à")
                    .Replace("ÃƒÂ ", "à")
                    .Replace("ÃƒÂ¢", "â")
                    .Replace("ÃƒÂ¤", "ä")
                    .Replace("ÃƒÂ®", "î")
                    .Replace("ÃƒÂ¯", "ï")
                    .Replace("ÃƒÂ´", "ô")
                    .Replace("ÃƒÂ¶", "ö")
                    .Replace("ÃƒÂ¹", "ù")
                    .Replace("ÃƒÂ»", "û")
                    .Replace("ÃƒÂ¼", "ü")
                    .Replace("Ãƒâ‚¬", "À")
                    .Replace("Ãƒâ€°", "É")
                    .Replace("ÃƒË†", "È");

                // 2. Remplacements simple mojibake ensuite
                current = current
                    .Replace("Ã‡", "Ç")
                    .Replace("Ã§", "ç")
                    .Replace("Ã©", "é")
                    .Replace("Ã¨", "è")
                    .Replace("Ãª", "ê")
                    .Replace("Ã«", "ë")
                    .Replace("Ã ", "à")
                    .Replace("Ã ", "à")
                    .Replace("Ã¢", "â")
                    .Replace("Ã¤", "ä")
                    .Replace("Ã®", "î")
                    .Replace("Ã¯", "ï")
                    .Replace("Ã´", "ô")
                    .Replace("Ã¶", "ö")
                    .Replace("Ã¹", "ù")
                    .Replace("Ã»", "û")
                    .Replace("Ã¼", "ü")
                    .Replace("Ã€", "À")
                    .Replace("Ã‰", "É")
                    .Replace("Ãˆ", "È");
            }

            return current;
        }

        private static List<string> ResolveEffectIds(WankulCardData data)
        {
            var list = new List<string>();
            if (data == null) return list;

            EnsureIndexBuilt();
            if (_effectsByIndexCache != null && _effectsByIndexCache.TryGetValue(data.Index, out var mappedEffects))
            {
                list.AddRange(mappedEffects);
            }

            return list;
        }

        public static DuelCard ToDuelCard(WankulCardData cardData)
        {
            if (cardData == null)
            {
                return new DuelCard("unknown", "Inconnue", CardKind.Character, 0);
            }

            string id = !string.IsNullOrEmpty(cardData.Number) ? cardData.Number : cardData.Index.ToString();
            string name = !string.IsNullOrEmpty(cardData.Title) ? FixMojibake(cardData.Title) : "Carte Wankul";
            var effectIds = ResolveEffectIds(cardData);

            if (cardData is EffigyCardData effigy)
            {
                // Dans les données Wankul, Force = -1 désigne les cartes utilitaires sans force imprimée (tiret '-')
                int force = effigy.Force == -1 ? 0 : effigy.Force;

                var comboEffectIds = new List<string>();
                bool hasClosingGem = false;

                if (!string.IsNullOrEmpty(effigy.Combo))
                {
                    string comboText = effigy.Combo.ToLowerInvariant();
                    hasClosingGem = true;

                    var matchBoost = System.Text.RegularExpressions.Regex.Match(comboText, @"\+?\s?(\d+)\s?(?:points? )?de force");
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

                return new DuelCard(
                    id: id,
                    name: name,
                    kind: CardKind.Character,
                    force: force,
                    isScoreur: effigy.IsScoreur,
                    effectIds: effectIds,
                    effigy: effigy.Effigy,
                    comboEffectIds: comboEffectIds,
                    hasOpeningGem: true,
                    hasClosingGem: hasClosingGem
                );
            }

            if (cardData is TerrainCardData || cardData.CardType == CardType.Terrain)
            {
                return new DuelCard(id, name, CardKind.Terrain, 0, isScoreur: false, effectIds: effectIds);
            }

            // Fallback pour SpecialCardData ou autres
            return new DuelCard(id, name, CardKind.Character, 0, isScoreur: false, effectIds: effectIds);
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
                    var card = ToDuelCard(cardData);
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
                    deck.Add(terrains[i]);
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
                    deck.Add(scoreurs[i]);
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
                    deck.Add(characters[i]);
                }
                else
                {
                    int force = 50 + (i % 7) * 50;
                    deck.Add(new DuelCard($"{prefix}_C_{i + 1}", $"Perso {i + 1}", CardKind.Character, force: force));
                }
            }

            return deck;
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
