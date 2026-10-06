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
            switch (data.Index)
            {
                case 100001: list.AddRange(new[] { "mill_3" }); break;
                case 100002: list.AddRange(new[] { "boost_terracid_20", "mill_3" }); break;
                case 100003: list.AddRange(new[] { "mill_3" }); break;
                case 100004: list.AddRange(new[] { "draw_both_1", "mill_3" }); break;
                case 100005: list.AddRange(new[] { "draw_both_1", "mill_3" }); break;
                case 100006: list.AddRange(new[] { "mill_3" }); break;
                case 100007: list.AddRange(new[] { "draw_both_1", "mill_3" }); break;
                case 100008: list.AddRange(new[] { "mill_3" }); break;
                case 100009: list.AddRange(new[] { "draw_both_1", "mill_3" }); break;
                case 100010: list.AddRange(new[] { "draw_both_1", "mill_3" }); break;
                case 100011: list.AddRange(new[] { "draw_both_1", "mill_3" }); break;
                case 100012: list.AddRange(new[] { "mill_3" }); break;
                case 100013: list.AddRange(new[] { "mill_3" }); break;
                case 100014: list.AddRange(new[] { "mill_3" }); break;
                case 100015: list.AddRange(new[] { "mill_3" }); break;
                case 100016: list.AddRange(new[] { "mill_3" }); break;
                case 100017: list.AddRange(new[] { "boost_laink_20", "mill_3" }); break;
                case 100018: list.AddRange(new[] { "mill_3" }); break;
                case 100019: list.AddRange(new[] { "mill_3" }); break;
                case 100020: list.AddRange(new[] { "mill_3" }); break;
                case 100021: list.AddRange(new[] { "mill_3" }); break;
                case 100022: list.AddRange(new[] { "mill_3" }); break;
                case 100023: list.AddRange(new[] { "mill_3" }); break;
                case 100024: list.AddRange(new[] { "mill_3" }); break;
                case 100025: list.AddRange(new[] { "mill_3" }); break;
                case 100026: list.AddRange(new[] { "discard_both_hand_1", "mill_3" }); break;
                case 100027: list.AddRange(new[] { "mill_3" }); break;
                case 100028: list.AddRange(new[] { "mill_3" }); break;
                case 100029: list.AddRange(new[] { "mill_3" }); break;
                case 100030: list.AddRange(new[] { "mill_3" }); break;
                case 100031: list.AddRange(new[] { "draw_1", "boost_self_20" }); break;
                case 100032: list.AddRange(new[] { "draw_1" }); break;
                case 100033: list.AddRange(new[] { "draw_1", "boost_self_20" }); break;
                case 100034: list.AddRange(new[] { "boost_self_20" }); break;
                case 100035: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100036: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100037: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100038: list.AddRange(new[] { "draw_1", "discard_opp_hand_1", "boost_self_20" }); break;
                case 100039: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100040: list.AddRange(new[] { "draw_2", "discard_opp_hand_1" }); break;
                case 100041: list.AddRange(new[] { "draw_2", "discard_opp_hand_1" }); break;
                case 100042: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100043: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100044: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100045: list.AddRange(new[] { "boost_self_20" }); break;
                case 100046: list.AddRange(new[] { "boost_self_20" }); break;
                case 100047: list.AddRange(new[] { "boost_self_45" }); break;
                case 100048: list.AddRange(new[] { "boost_self_30" }); break;
                case 100049: list.AddRange(new[] { "discard_opp_hand_1", "boost_self_30" }); break;
                case 100050: list.AddRange(new[] { "discard_opp_hand_1", "boost_self_20" }); break;
                case 100051: list.AddRange(new[] { "boost_self_30" }); break;
                case 100052: list.AddRange(new[] { "boost_self_30" }); break;
                case 100053: list.AddRange(new[] { "boost_self_30" }); break;
                case 100054: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100055: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100056: list.AddRange(new[] { "boost_self_15" }); break;
                case 100057: list.AddRange(new[] { "draw_1" }); break;
                case 100058: list.AddRange(new[] { "boost_self_15" }); break;
                case 100059: list.AddRange(new[] { "boost_self_15" }); break;
                case 100060: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100061: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100062: list.AddRange(new[] { "boost_self_20" }); break;
                case 100063: list.AddRange(new[] { "draw_1", "boost_self_20" }); break;
                case 100064: list.AddRange(new[] { "boost_self_30" }); break;
                case 100065: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100066: list.AddRange(new[] { "boost_self_20" }); break;
                case 100067: list.AddRange(new[] { "boost_self_20" }); break;
                case 100068: list.AddRange(new[] { "boost_self_75" }); break;
                case 100069: list.AddRange(new[] { "boost_self_90" }); break;
                case 100070: list.AddRange(new[] { "boost_self_20" }); break;
                case 100071: list.AddRange(new[] { "boost_self_20" }); break;
                case 100072: list.AddRange(new[] { "discard_opp_hand_1", "boost_self_20" }); break;
                case 100073: list.AddRange(new[] { "boost_self_20" }); break;
                case 100074: list.AddRange(new[] { "boost_self_60" }); break;
                case 100075: list.AddRange(new[] { "boost_self_45" }); break;
                case 100076: list.AddRange(new[] { "discard_opp_hand_1", "boost_self_20" }); break;
                case 100077: list.AddRange(new[] { "boost_self_30" }); break;
                case 100078: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100079: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100080: list.AddRange(new[] { "boost_self_30" }); break;
                case 100081: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100082: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100083: list.AddRange(new[] { "draw_1" }); break;
                case 100084: list.AddRange(new[] { "draw_1" }); break;
                case 100085: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100086: list.AddRange(new[] { "discard_opp_hand_1", "boost_self_30" }); break;
                case 100087: list.AddRange(new[] { "draw_1", "boost_self_30" }); break;
                case 100088: list.AddRange(new[] { "draw_1", "boost_self_30" }); break;
                case 100089: list.AddRange(new[] { "boost_self_75" }); break;
                case 100090: list.AddRange(new[] { "boost_self_60" }); break;
                case 100091: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100092: list.AddRange(new[] { "draw_1", "discard_opp_hand_1" }); break;
                case 100093: list.AddRange(new[] { "draw_1" }); break;
                case 100094: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100095: list.AddRange(new[] { "draw_1", "boost_self_30" }); break;
                case 100096: list.AddRange(new[] { "boost_self_30" }); break;
                case 100097: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100098: list.AddRange(new[] { "draw_1", "discard_opp_hand_1" }); break;
                case 100099: list.AddRange(new[] { "discard_opp_hand_1", "boost_self_30" }); break;
                case 100100: list.AddRange(new[] { "draw_1", "discard_opp_hand_1" }); break;
                case 100101: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100102: list.AddRange(new[] { "boost_self_20" }); break;
                case 100103: list.AddRange(new[] { "boost_self_30" }); break;
                case 100104: list.AddRange(new[] { "draw_1" }); break;
                case 100105: list.AddRange(new[] { "draw_1" }); break;
                case 100106: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100107: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100108: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100109: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100110: list.AddRange(new[] { "discard_opp_hand_1", "boost_self_30" }); break;
                case 100111: list.AddRange(new[] { "discard_opp_hand_1", "boost_self_30" }); break;
                case 100112: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100113: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100114: list.AddRange(new[] { "boost_self_30" }); break;
                case 100115: list.AddRange(new[] { "boost_self_30" }); break;
                case 100116: list.AddRange(new[] { "draw_1" }); break;
                case 100117: list.AddRange(new[] { "boost_self_20" }); break;
                case 100118: list.AddRange(new[] { "boost_self_75" }); break;
                case 100119: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100120: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100121: list.AddRange(new[] { "boost_self_60" }); break;
                case 100122: list.AddRange(new[] { "boost_self_20" }); break;
                case 100123: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100124: list.AddRange(new[] { "boost_self_15" }); break;
                case 100125: list.AddRange(new[] { "boost_self_15" }); break;
                case 100126: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100127: list.AddRange(new[] { "draw_1", "boost_self_30" }); break;
                case 100128: list.AddRange(new[] { "draw_2" }); break;
                case 100129: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100130: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100131: list.AddRange(new[] { "boost_self_30" }); break;
                case 100132: list.AddRange(new[] { "discard_opp_hand_1", "boost_self_30" }); break;
                case 100133: list.AddRange(new[] { "boost_self_20" }); break;
                case 100134: list.AddRange(new[] { "boost_self_30" }); break;
                case 100135: list.AddRange(new[] { "draw_1", "discard_opp_hand_1" }); break;
                case 100136: list.AddRange(new[] { "draw_1", "discard_opp_hand_1" }); break;
                case 100137: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100138: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100139: list.AddRange(new[] { "draw_1", "boost_self_20" }); break;
                case 100140: list.AddRange(new[] { "draw_1" }); break;
                case 100141: list.AddRange(new[] { "boost_self_20" }); break;
                case 100142: list.AddRange(new[] { "boost_self_20" }); break;
                case 100143: list.AddRange(new[] { "boost_self_20" }); break;
                case 100144: list.AddRange(new[] { "boost_self_20" }); break;
                case 100145: list.AddRange(new[] { "boost_self_30" }); break;
                case 100146: list.AddRange(new[] { "boost_self_20" }); break;
                case 100147: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100148: list.AddRange(new[] { "boost_self_15" }); break;
                case 100149: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100150: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100151: list.AddRange(new[] { "discard_opp_hand_1", "boost_self_30" }); break;
                case 100152: list.AddRange(new[] { "discard_opp_hand_1", "boost_self_30" }); break;
                case 100153: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100154: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100155: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100156: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100157: list.AddRange(new[] { "discard_opp_hand_1", "boost_self_30" }); break;
                case 100158: list.AddRange(new[] { "discard_opp_hand_1", "boost_self_20" }); break;
                case 100159: list.AddRange(new[] { "discard_opp_hand_1", "boost_self_30" }); break;
                case 100160: list.AddRange(new[] { "draw_1", "boost_self_30" }); break;
                case 100161: list.AddRange(new[] { "boost_self_75" }); break;
                case 100162: list.AddRange(new[] { "boost_self_60" }); break;
                case 100163: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100164: list.AddRange(new[] { "draw_2" }); break;
                case 100165: list.AddRange(new[] { "draw_1" }); break;
                case 100166: list.AddRange(new[] { "draw_1" }); break;
                case 100167: list.AddRange(new[] { "discard_opp_hand_1", "boost_self_30" }); break;
                case 100168: list.AddRange(new[] { "draw_1", "boost_self_30" }); break;
                case 100169: list.AddRange(new[] { "draw_1", "boost_self_20" }); break;
                case 100170: list.AddRange(new[] { "draw_1" }); break;
                case 100171: list.AddRange(new[] { "boost_self_20" }); break;
                case 100172: list.AddRange(new[] { "boost_self_20" }); break;
                case 100173: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100174: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100175: list.AddRange(new[] { "boost_self_20" }); break;
                case 100176: list.AddRange(new[] { "boost_self_30" }); break;
                case 100177: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100178: list.AddRange(new[] { "boost_self_15" }); break;
                case 100179: list.AddRange(new[] { "discard_opp_hand_1" }); break;
                case 100180: list.AddRange(new[] { "discard_opp_hand_1" }); break;
            }

            // Fallback & S2 Campus / S3 Battle rules
            if (list.Count == 0)
            {
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
                        if (terrain.WinningEffect.Contains("Le gagnant ajoute 2 terrains")) list.Add("win_shuffle_terrains");
                        else if (terrain.WinningEffect.Contains("Le gagnant bannit les 2 cartes")) list.Add("win_banish_deck_2");
                        else if (terrain.WinningEffect.Contains("cherche 1 carte dans son deck")) list.Add("win_search_card");
                        else if (terrain.WinningEffect.Contains("copie l'effet gagnant d'un autre terrain")) list.Add("win_copy_effect");
                        else if (terrain.WinningEffect.Contains("joue un personnage gratuitement, puis score un autre terrain")) list.Add("win_play_free_score");
                        else if (terrain.WinningEffect.Contains("joue un personnage sans payer son coût") || terrain.WinningEffect.Contains("joue un personnage sans payer son coÃ»t")) list.Add("win_play_free");
                        else if (terrain.WinningEffect.Contains("met dans sa main ses 3 cartes mises de côtés") || terrain.WinningEffect.Contains("met dans sa main ses 3 cartes mises de cÃ´tÃ©s")) list.Add("win_take_setaside_3");
                        else if (terrain.WinningEffect.Contains("mélange sa main à son deck, puis pioche 5 cartes") || terrain.WinningEffect.Contains("mÃ©lange sa main Ã  son deck, puis pioche 5 cartes")) list.Add("win_shuffle_hand_draw_5");
                        else if (terrain.WinningEffect.Contains("pioche 3 cartes ou met les 3 cartes")) list.Add("win_draw_or_put_under_deck");
                        else if (terrain.WinningEffect.Contains("pioche et révèle jusqu'à obtenir une carte de coût impair") || terrain.WinningEffect.Contains("pioche et rÃ©vÃ¨le jusqu'Ã  obtenir une carte de coÃ»t impair")) list.Add("win_draw_until_odd");
                        else if (terrain.WinningEffect.Contains("prend de 0 à 5 cartes du dessus de sa défausse") || terrain.WinningEffect.Contains("prend de 0 Ã  5 cartes du dessus de sa dÃ©fausse")) list.Add("win_put_under_deck");
                        else if (terrain.WinningEffect.Contains("repend en main les 2 cartes du dessus de sa défausse") || terrain.WinningEffect.Contains("repend en main les 2 cartes du dessus de sa dÃ©fausse")) list.Add("win_take_discard_2");
                        else if (terrain.WinningEffect.Contains("reprend en main autant de cartes de sa défausse que la Force de base la plus petite") || terrain.WinningEffect.Contains("reprend en main autant de cartes de sa dÃ©fausse que la Force de base la plus petite")) list.Add("win_take_discard_min_force");
                        else if (terrain.WinningEffect.Contains("reprend en main la carte du dessus de sa défausse") || terrain.WinningEffect.Contains("reprend en main la carte du dessus de sa dÃ©fausse")) list.Add("win_take_discard_1");
                        else if (terrain.WinningEffect.IndexOf("pioche", StringComparison.OrdinalIgnoreCase) >= 0) list.Add("win_draw_1");
                        else if (terrain.WinningEffect.IndexOf("défausse", StringComparison.OrdinalIgnoreCase) >= 0 || terrain.WinningEffect.IndexOf("meule", StringComparison.OrdinalIgnoreCase) >= 0) list.Add("win_mill_2");
                    }
                    if (!string.IsNullOrEmpty(terrain.LosingEffect))
                    {
                        // Parse S3 Losing Self Mill (ex: défausse les X cartes du dessus de son deck)
                        var matchLoseMill = System.Text.RegularExpressions.Regex.Match(terrain.LosingEffect, @"défausse (?:les )?(\d+) cartes du dessus de son deck");
                        if (matchLoseMill.Success)
                        {
                            list.Add($"lose_self_mill_{matchLoseMill.Groups[1].Value}");
                        }

                        if (terrain.LosingEffect.Contains("bannit les 3 cartes du dessus de son deck")) list.Add("lose_banish_deck_3");
                        else if (terrain.LosingEffect.Contains("bannit les 4 cartes du dessus de son deck")) list.Add("lose_banish_deck_4");
                        else if (terrain.LosingEffect.Contains("bannit les 5 cartes du dessus de sa défausse") || terrain.LosingEffect.Contains("bannit les 5 cartes du dessus de sa dÃ©fausse")) list.Add("lose_banish_discard_5");
                        else if (terrain.LosingEffect.Contains("bannit ses 3 cartes mises de côtés") || terrain.LosingEffect.Contains("bannit ses 3 cartes mises de cÃ´tÃ©s")) list.Add("lose_banish_setaside_3");
                        else if (terrain.LosingEffect.Contains("bannit toutes ses cartes en main et en pioche le même nombre") || terrain.LosingEffect.Contains("bannit toutes ses cartes en main et en pioche le mÃªme nombre")) list.Add("lose_banish_hand_draw");
                        else if (terrain.LosingEffect.Contains("bannit un total de 4 cartes depuis le dessus de son deck et/ou sa main")) list.Add("lose_banish_deck_hand_4");
                        else if (terrain.LosingEffect.Contains("défausse 3 cartes de sa main") || terrain.LosingEffect.Contains("dÃ©fausse 3 cartes de sa main")) list.Add("lose_discard_3");
                        else if (terrain.LosingEffect.Contains("défausse 3 cartes du dessus de son deck") || terrain.LosingEffect.Contains("dÃ©fausse 3 cartes du dessus de son deck")) list.Add("lose_mill_3");
                        else if (terrain.LosingEffect.Contains("défausse 5 cartes du dessus de son deck, moins le nombre") || terrain.LosingEffect.Contains("dÃ©fausse 5 cartes du dessus de son deck, moins le nombre")) list.Add("lose_mill_5_minus_drawn");
                        else if (terrain.LosingEffect.Contains("défausse autant de cartes de son deck que la Force de base la plus grande") || terrain.LosingEffect.Contains("dÃ©fausse autant de cartes de son deck que la Force de base la plus grande")) list.Add("lose_mill_max_force");
                        else if (terrain.LosingEffect.Contains("défausse autant de cartes du dessus de son deck que la valeur de Force de base la plus élevée") || terrain.LosingEffect.Contains("dÃ©fausse autant de cartes du dessus de son deck que la valeur de Force de base la plus Ã©levÃ©e")) list.Add("lose_mill_max_force");
                        else if (terrain.LosingEffect.Contains("défausse les 3 cartes du dessus de son deck") || terrain.LosingEffect.Contains("dÃ©fausse les 3 cartes du dessus de son deck")) list.Add("lose_mill_3");
                        else if (terrain.LosingEffect.Contains("défausse les 4 cartes du dessus de son deck") || terrain.LosingEffect.Contains("dÃ©fausse les 4 cartes du dessus de son deck")) list.Add("lose_mill_4");
                        else if (terrain.LosingEffect.Contains("défausse les 5 cartes du dessus de son deck") || terrain.LosingEffect.Contains("dÃ©fausse les 5 cartes du dessus de son deck")) list.Add("lose_mill_5");
                        else if (terrain.LosingEffect.Contains("défausse un de ses personnages en jeu") || terrain.LosingEffect.Contains("dÃ©fausse un de ses personnages en jeu")) list.Add("lose_discard_char_in_play");
                        else if (terrain.LosingEffect.IndexOf("défausse", StringComparison.OrdinalIgnoreCase) >= 0) list.Add("lose_discard_1");
                    }
                }
                else if (data is EffigyCardData effigy)
                {
                    string text = ((effigy.Rules ?? "") + " " + (effigy.Combo ?? "")).ToLowerInvariant();

                    // S3 Battle Self Mill for characters
                    var matchSelfMill = System.Text.RegularExpressions.Regex.Match(text, @"défaussez les (\d+) cartes du dessus de votre deck");
                    if (matchSelfMill.Success)
                    {
                        list.Add($"self_mill_{matchSelfMill.Groups[1].Value}");
                    }

                    if (text.Contains("défaussez un personnage adverse") || text.Contains("défausse un personnage adverse"))
                    {
                        if (!list.Contains("discard_opp_char_any"))
                            list.Add("discard_opp_char_any");
                    }

                    // Campus specific rules
                    if (text.Contains("bannissez jusqu'à 2 personnages adverses") || text.Contains("bannissez jusqu'Ã  2 personnages adverses")) list.Add("lose_discard_char_in_play");
                    if (text.Contains("bannissez un personnage de force inférieure") || text.Contains("bannissez un personnage de force infÃ©rieure")) list.Add("lose_discard_char_in_play");
                    if (text.Contains("chaque joueur met de côtés les 3 cartes") || text.Contains("chaque joueur met de cÃ´tÃ©s les 3 cartes")) list.Add("win_take_setaside_3");
                    if (text.Contains("piochez autant de cartes que la valeur")) list.Add("win_draw_until_odd");
                    if (text.Contains("scorez un terrain")) list.Add("win_play_free_score");
                    if (text.Contains("défaussez jusqu'à 2 personnages adverses") || text.Contains("dÃ©faussez jusqu'Ã  2 personnages adverses")) list.Add("lose_discard_char_in_play");
                    if (text.Contains("votre adversaire pioche 1 carte")) list.Add("draw_both_1");
                    if (text.Contains("votre adversaire défausse les 2 cartes") || text.Contains("votre adversaire dÃ©fausse les 2 cartes")) list.Add("mill_2");
                    if (text.Contains("votre adversaire défausse les 3 cartes") || text.Contains("votre adversaire dÃ©fausse les 3 cartes")) list.Add("mill_3");

                    // Standard / S1 rules
                    if (text.Contains("piochez 2 cartes") || text.Contains("pioche 2 cartes") || title.Contains("ASTRONAUTE")) list.Add("draw_2");
                    else if (text.Contains("piochez 1 carte") || text.Contains("piochez une carte") || text.Contains("pioche 1 carte")) list.Add("draw_1");

                    if (text.Contains("défausse les 3") || text.Contains("défausser 3") || title.Contains("GRUDGE")) list.Add("mill_3");
                    else if (text.Contains("défausse les 2") || text.Contains("défausser 2")) list.Add("mill_2");

                    if (text.Contains("défausse une carte de sa main") || text.Contains("défausse 1 carte de sa main") || title.Contains("VENDEUR") || title.Contains("ANNABELLE")) list.Add("discard_opp_hand_1");

                    if (title.Contains("CAMIONNEUR")) list.Add("discard_opp_char_force3");
                    else if (title.Contains("GRUDGE")) list.Add("discard_opp_char_any");
                }
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
