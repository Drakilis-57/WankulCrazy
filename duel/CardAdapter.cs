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
        private static Dictionary<string, WankulCardData> _cardsByIdCache;

        private static void EnsureIndexBuilt()
        {
            if (_cardsByIdCache != null) return;
            _cardsByIdCache = new Dictionary<string, WankulCardData>(StringComparer.OrdinalIgnoreCase);
            var allCards = WankulCardsData.Instance.cards;
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

            string title = data.Title != null ? data.Title.ToUpperInvariant() : "";

            if (data is TerrainCardData terrain)
            {
                if (title.Contains("MORIA")) list.Add("boost_laink_20");
                else if (title.Contains("PORTAL")) list.Add("boost_terracid_20");
                else if (title.Contains("RUST") || title.Contains("GOLF")) list.Add("terrain_draw_1_turn_start");
                else if (title.Contains("NAVIRE PIRATE")) list.Add("discard_both_hand_1");
                else if (title.Contains("F.A.Q") || title.Contains("FAQ")) list.Add("draw_both_1");

                if (!string.IsNullOrEmpty(terrain.WinningEffect))
                {
                    var winEffect = terrain.WinningEffect.ToLowerInvariant();
                    if (winEffect.Contains("pioche jusqu'à avoir 7 cartes")) list.Add("win_draw_until_7");
                    else if (winEffect.Contains("pioche 3 cartes") || winEffect.Contains("piochez 3 cartes")) list.Add("win_draw_3");
                    else if (winEffect.Contains("pioche 2 cartes") || winEffect.Contains("piochez 2 cartes")) list.Add("win_draw_2");
                    else if (winEffect.Contains("pioche") || winEffect.Contains("piochez une carte") || winEffect.Contains("pioche 1 carte")) list.Add("win_draw_1");

                    if (winEffect.Contains("meule 2 cartes") || winEffect.Contains("défausse 2 cartes")) list.Add("win_mill_2");
                    else if (winEffect.Contains("meule 3 cartes") || winEffect.Contains("défausse 3 cartes")) list.Add("win_mill_3");
                }
                if (!string.IsNullOrEmpty(terrain.LosingEffect))
                {
                    var loseEffect = terrain.LosingEffect.ToLowerInvariant();
                    if (loseEffect.Contains("défausse 2 cartes") || loseEffect.Contains("défausse 2 de sa main")) list.Add("lose_discard_2");
                    else if (loseEffect.Contains("défausse 3 cartes")) list.Add("lose_discard_3");
                    else if (loseEffect.Contains("défausse") && !loseEffect.Contains("défausse 3") && !loseEffect.Contains("défausse 2")) list.Add("lose_discard_1");

                    if (loseEffect.Contains("meule 1 carte")) list.Add("lose_mill_1");
                    else if (loseEffect.Contains("meule 2 cartes")) list.Add("lose_mill_2");
                    else if (loseEffect.Contains("meule 3 cartes")) list.Add("lose_mill_3");
                    else if (loseEffect.Contains("meule 4 cartes")) list.Add("lose_mill_4");
                    else if (loseEffect.Contains("meule 5 cartes")) list.Add("lose_mill_5");
                    else if (loseEffect.Contains("meule 6 cartes")) list.Add("lose_mill_6");
                }
            }
            else if (data is EffigyCardData effigy)
            {
                string text = ((effigy.Rules ?? "") + " " + (effigy.Combo ?? "")).ToLowerInvariant();
                if (text.Contains("piochez 2 cartes") || text.Contains("pioche 2 cartes") || title.Contains("ASTRONAUTE"))
                    list.Add("draw_2");
                else if (text.Contains("piochez 1 carte") || text.Contains("piochez une carte") || text.Contains("pioche 1 carte"))
                    list.Add("draw_1");


                if (text.Contains("défausse les 3") || text.Contains("défausser 3") || title.Contains("GRUDGE"))
                    list.Add("mill_3");
                else if (text.Contains("défausse les 2") || text.Contains("défausser 2"))
                    list.Add("mill_2");

                if (text.Contains("défausse une carte de sa main") || text.Contains("défausse 1 carte de sa main") || title.Contains("VENDEUR") || title.Contains("ANNABELLE"))
                    list.Add("discard_opp_hand_1");

                // --- Stellar specific mapping ---
                if (text.Contains("déplacez un personnage") || text.Contains("déplace un personnage") || text.Contains("déplacez 3 personnages"))
                    list.Add("move_character");

                if (text.Contains("carte du dessous de votre deck à votre main") || text.Contains("les 2 cartes du dessous de son deck à sa main"))
                    list.Add("add_bottom_deck_to_hand");

                if (text.Contains("regardez les 5 cartes du dessus de vos decks"))
                    list.Add("look_top_deck_5");
                // ---------------------------------


                if (title.Contains("CAMIONNEUR"))
                    list.Add("discard_opp_char_force3");
                else if (title.Contains("GRUDGE"))
                    list.Add("discard_opp_char_any");
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

                return new DuelCard(
                    id: id,
                    name: name,
                    kind: CardKind.Character,
                    force: force,
                    isScoreur: effigy.IsScoreur,
                    effectIds: effectIds,
                    effigy: effigy.Effigy
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
