using WankulCrazyPlugin.importer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WankulCrazyPlugin.inventory;
using WankulCrazyPlugin.utils;

namespace WankulCrazyPlugin.cards
{
    public class WankulCardsData : Singleton<WankulCardsData>
    {
        private List<WankulCardData> _cards;
        public List<WankulCardData> cards { get => _cards ??= new List<WankulCardData>(); set { _cards = value; cardsBySeason = null; } }
        public Dictionary<string, WankulCardData> association = [];

        public void EnsureInitialized()
        {
            if (cards == null || cards.Count == 0)
            {
                Plugin.Logger?.LogInfo("[WankulCardsData] Initialisation des cartes déclenchée par EnsureInitialized.");
                JsonImporter.ImportJson();
            }
        }


        // Reverse lookup map (WankulCardData.Index -> CardData) to optimize GetCardDataFromWankulCardData from O(N) to O(1)
        private readonly Dictionary<int, CardData> reverseAssociation = new Dictionary<int, CardData>();

        // Cached enum arrays to avoid allocation on every Enum.GetValues call
        private static readonly ECardExpansionType[] CachedExpansions = (ECardExpansionType[])Enum.GetValues(typeof(ECardExpansionType));
        private static readonly ECardBorderType[] CachedBorders = (ECardBorderType[])Enum.GetValues(typeof(ECardBorderType));

        // Index Saison -> Cartes (base par ID chaîne), construit une seule fois (lazy).
        private Dictionary<string, List<WankulCardData>> cardsBySeason;

        private Dictionary<string, List<WankulCardData>> GetCardsBySeasonIndex()
        {
            if (cardsBySeason == null)
            {
                cardsBySeason = new Dictionary<string, List<WankulCardData>>(StringComparer.OrdinalIgnoreCase);
                foreach (WankulCardData card in cards)
                {
                    string seasonKey = card.SeasonId;
                    if (string.IsNullOrEmpty(seasonKey))
                    {
                        seasonKey = card.Season.ToString();
                    }

                    if (!cardsBySeason.TryGetValue(seasonKey, out List<WankulCardData> list))
                    {
                        list = new List<WankulCardData>();
                        cardsBySeason[seasonKey] = list;
                    }
                    list.Add(card);
                }
            }
            return cardsBySeason;
        }

        public static List<WankulCardData> GetCardsBySeasonFast(string seasonId)
        {
            if (string.IsNullOrEmpty(seasonId)) return new List<WankulCardData>();
            return Instance.GetCardsBySeasonIndex().TryGetValue(seasonId, out List<WankulCardData> list)
                ? list
                : new List<WankulCardData>();
        }

        /// <summary>
        /// Surcharge rétro-compatible utilisant l'enum Season.
        /// </summary>
        public static List<WankulCardData> GetCardsBySeasonFast(Season season)
        {
            return GetCardsBySeasonFast(season.ToString());
        }


        public WankulCardData GetFromMonster(CardData monster, bool allowNull)
        {
            ECardExpansionType expansionType = monster.expansionType;
            MonsterData monsterData = InventoryBase.GetMonsterData(monster.monsterType);
            if (monsterData == null)
            {
                return null;
            }
            ERarity rarity = monsterData.Rarity;

            string key = $"{monster.monsterType}_{monster.borderType}_{expansionType}";

            // Vérification de l'association déjà existante
            if (association.TryGetValue(key, out WankulCardData card))
            {
                return card;
            }
            else if (allowNull)
            {
                // dans les drop on peut avoir des cartes qui ne sont pas dans l'association
                return null;
            }

            // Si pas trouvé dans l'association, déterminer le pack de carte
            ECollectionPackType packType = ECollectionPackType.BasicCardPack;

            switch (expansionType)
            {
                case ECardExpansionType.Tetramon:
                    packType = rarity switch
                    {
                        ERarity.Common => ECollectionPackType.BasicCardPack,
                        ERarity.Rare => ECollectionPackType.RareCardPack,
                        ERarity.Epic => ECollectionPackType.EpicCardPack,
                        ERarity.Legendary => ECollectionPackType.LegendaryCardPack,
                        _ => packType
                    };
                    break;

                case ECardExpansionType.Destiny:
                    packType = rarity switch
                    {
                        ERarity.Common => ECollectionPackType.DestinyBasicCardPack,
                        ERarity.Rare => ECollectionPackType.DestinyRareCardPack,
                        ERarity.Epic => ECollectionPackType.DestinyEpicCardPack,
                        ERarity.Legendary => ECollectionPackType.DestinyLegendaryCardPack,
                        _ => packType
                    };
                    break;
                    // Ajouter d'autres types d'extensions ici si nécessaire
            }

            // Sélection aléatoire d'une carte si elle n'a pas été trouvée dans l'association
            WankulCardData wankulCardData = WankulInventory.randFromPackType(packType);

            // On ne stocke pas dans l'association pour de futures drops
            if (wankulCardData != null)
            {
                //Plugin.Logger.LogInfo($"GetFromMonster Setting association for {key}");
                association[key] = wankulCardData;
                reverseAssociation[wankulCardData.Index] = monster;
            }

            return wankulCardData;
        }

        public CardData GetCardDataFromWankulCardData(WankulCardData card)
        {
            if (card == null) return null;

            if (reverseAssociation.TryGetValue(card.Index, out CardData cardData))
            {
                return cardData;
            }

            // Rien a reconstruire : le reverse lookup couvre deja toutes les associations
            if (reverseAssociation.Count >= association.Count) return null;

            // Fallback & population of reverse lookup if reverseAssociation doesn't have it yet
            foreach (var entry in association)
            {
                if (entry.Value != null)
                {
                    CardData cd = GetCardDataFromKey(entry.Key);
                    if (cd != null)
                    {
                        reverseAssociation[entry.Value.Index] = cd;
                    }
                }
            }

            if (reverseAssociation.TryGetValue(card.Index, out cardData))
            {
                return cardData;
            }

            return null;
        }

        // Cache du parsing des cles "monster_border_expansion" : Enum.Parse (patche par Harmony)
        // etait appele 3 fois par carte a chaque chargement de sauvegarde.
        private static readonly Dictionary<string, (EMonsterType monster, ECardBorderType border, ECardExpansionType expansion)> parsedKeyCache
            = new Dictionary<string, (EMonsterType, ECardBorderType, ECardExpansionType)>();

        public CardData GetCardDataFromKey(string key)
        {
            if (!parsedKeyCache.TryGetValue(key, out var parsed))
            {
                // Découper la clé en utilisant l'underscore comme séparateur
                string[] parts = key.Split('_');

                if (parts.Length != 3)
                {
                    Debug.LogError("La clé ne contient pas le bon nombre de parties.");
                    return null;
                }

                parsed = (
                    (EMonsterType)Enum.Parse(typeof(EMonsterType), parts[0]),
                    (ECardBorderType)Enum.Parse(typeof(ECardBorderType), parts[1]),
                    (ECardExpansionType)Enum.Parse(typeof(ECardExpansionType), parts[2])
                );
                parsedKeyCache[key] = parsed;
            }

            // Nouvelle instance a chaque appel : CardData est mutable (isFoil, isNew...)
            CardData cardData = new CardData();
            cardData.monsterType = parsed.monster;
            cardData.borderType = parsed.border;
            cardData.expansionType = parsed.expansion;

            return cardData;
        }

        public void SetFromMonster(CardData monster, WankulCardData card)
        {
            ECardExpansionType expansionType = monster.expansionType;
            MonsterData monsterData = InventoryBase.GetMonsterData(monster.monsterType);

            string key = monster.monsterType.ToString() + "_" + monster.borderType.ToString() + "_" + expansionType.ToString();
            // Vérifiez si la clé existe déjà
            if (!association.ContainsKey(key))
            {
                //Plugin.Logger.LogInfo($"SetFromMonster Setting association for {key}");
                association[key] = card;  // Créez une nouvelle association
                if (card != null)
                {
                    reverseAssociation[card.Index] = monster;
                }
            }
            else
            {
                Debug.LogError("La carte existe déjà dans l'association.");
            }
        }

        private struct Slot
        {
            public ECardExpansionType Expansion;
            public ECardBorderType Border;
            public EMonsterType Monster;
            public string Key;
        }

        // Liste ordonnee (meme ordre de parcours qu'avant) de tous les slots utilisables,
        // construite une seule fois, + curseur sur le premier slot potentiellement libre.
        private List<Slot> slotOrder;
        private int slotCursor;
        private Dictionary<string, WankulCardData> slotCursorAssociation;
        private int slotCursorAssociationCount;

        private static bool IsExcludedMonster(EMonsterType monster)
        {
            return monster == EMonsterType.EarlyPlayer ||
                   monster == EMonsterType.START_CATJOB ||
                   monster == EMonsterType.START_FANTASYRPG ||
                   monster == EMonsterType.START_MEGABOT ||
                   monster == EMonsterType.None ||
                   monster == EMonsterType.MAX ||
                   monster == EMonsterType.MAX_CATJOB ||
                   monster == EMonsterType.MAX_FANTASYRPG ||
                   monster == EMonsterType.MAX_MEGABOT;
        }

        private static List<Slot> BuildSlotOrder()
        {
            var slots = new List<Slot>();
            foreach (ECardExpansionType expansion in CachedExpansions)
            {
                if (
                    expansion == ECardExpansionType.None ||
                    expansion == ECardExpansionType.Ghost ||
                    expansion == ECardExpansionType.FoodieGO ||
                    expansion == ECardExpansionType.MAX
                )
                {
                    continue;
                }
                foreach (ECardBorderType border in CachedBorders)
                {
                    foreach (var (startMonsterList, endMonsterList) in GetMonsterRanges(expansion))
                    {
                        for (int i = startMonsterList; i <= endMonsterList; i++)
                        {
                            EMonsterType monster = (EMonsterType)i;
                            if (IsExcludedMonster(monster)) continue;

                            slots.Add(new Slot
                            {
                                Expansion = expansion,
                                Border = border,
                                Monster = monster,
                                Key = $"{monster}_{border}_{expansion}"
                            });
                        }
                    }
                }
            }
            return slots;
        }

        public CardData GetUnassciatedCardData()
        {
            slotOrder ??= BuildSlotOrder();

            // Si le dictionnaire a ete remplace ou vide (nouvelle sauvegarde), on repart du debut.
            if (!ReferenceEquals(slotCursorAssociation, association) || association.Count < slotCursorAssociationCount)
            {
                slotCursor = 0;
                slotCursorAssociation = association;
            }
            slotCursorAssociationCount = association.Count;

            // Un slot n'est jamais libere : le curseur ne recule pas, on ne reparcourt plus tout a chaque appel.
            while (slotCursor < slotOrder.Count)
            {
                Slot slot = slotOrder[slotCursor];
                if (!association.ContainsKey(slot.Key))
                {
                    CardData cardData = new CardData();
                    cardData.borderType = slot.Border;
                    cardData.expansionType = slot.Expansion;
                    cardData.monsterType = slot.Monster;
                    return cardData; // Premier CardData manquant
                }
                slotCursor++;
            }
            return null; // Aucune CardData manquante
        }

        // Plages de EMonsterType valides par expansion. Chaque expansion peut avoir plusieurs
        // segments disjoints (ex: plage native du jeu + plage custom Wankul), évitant d'itérer
        // sur des dizaines de milliers de valeurs vides entre deux segments éloignés.
        private static readonly Dictionary<ECardExpansionType, List<(int start, int end)>> MonsterRanges =
            new Dictionary<ECardExpansionType, List<(int start, int end)>>
        {
            { ECardExpansionType.Tetramon, new List<(int, int)> { (0, 500) } },
            { ECardExpansionType.Destiny,  new List<(int, int)> { (0, 500) } },
            { ECardExpansionType.Megabot,  new List<(int, int)> { (1000, 1500) } },
            { ECardExpansionType.FantasyRPG, new List<(int, int)> { (2000, 2500) } },
            { ECardExpansionType.CatJob,   new List<(int, int)> { (3000, 3500) } },
        };

        private static List<(int start, int end)> GetMonsterRanges(ECardExpansionType expansion)
        {
            return MonsterRanges.TryGetValue(expansion, out var ranges) ? ranges : new List<(int, int)>();
        }

        private static readonly Dictionary<string, bool> keyValidCache = new Dictionary<string, bool>();

        public static bool IsKeyValid(string keyToCheck)
        {
            if (keyToCheck == null) return false;
            if (!keyValidCache.TryGetValue(keyToCheck, out bool valid))
            {
                valid = IsKeyValidUncached(keyToCheck);
                keyValidCache[keyToCheck] = valid;
            }
            return valid;
        }

        private static bool IsKeyValidUncached(string keyToCheck)
        {
            // Découper la clé pour récupérer les valeurs
            string[] parts = keyToCheck.Split('_');
            if (parts.Length != 3)
            {
                //Console.WriteLine($"❌ Format incorrect pour la clé : {keyToCheck}");
                return false;
            }

            // Parser les valeurs
            if (!Enum.TryParse(parts[0], out EMonsterType monster))
            {
                //Console.WriteLine($"❌ Type de monstre invalide : {parts[0]}");
                return false;
            }

            if (!Enum.TryParse(parts[1], out ECardBorderType border))
            {
                //Console.WriteLine($"❌ Type de bordure invalide : {parts[1]}");
                return false;
            }

            if (!Enum.TryParse(parts[2], out ECardExpansionType expansion))
            {
                //Console.WriteLine($"❌ Type d'expansion invalide : {parts[2]}");
                return false;
            }

            // Vérifier que l'expansion est valide
            if (expansion == ECardExpansionType.None ||
                expansion == ECardExpansionType.FantasyRPG ||
                expansion == ECardExpansionType.Megabot ||
                expansion == ECardExpansionType.CatJob ||
                expansion == ECardExpansionType.Ghost ||
                expansion == ECardExpansionType.FoodieGO ||
                expansion == ECardExpansionType.MAX)
            {
                //Console.WriteLine($"❌ Expansion interdite : {expansion}");
                return false;
            }

            // Vérifier que le monstre est dans une des plages valides pour l'expansion donnée
            bool inRange = GetMonsterRanges(expansion).Any(r => (int)monster >= r.start && (int)monster <= r.end);
            if (!inRange)
            {
                //Console.WriteLine($"❌ Monstre {monster} hors des plages valides pour l'expansion {expansion}");
                return false;
            }

            // Vérifier que le monstre ne fait pas partie des valeurs interdites
            if (monster == EMonsterType.EarlyPlayer ||
                monster == EMonsterType.START_CATJOB ||
                monster == EMonsterType.START_FANTASYRPG ||
                monster == EMonsterType.START_MEGABOT ||
                monster == EMonsterType.None ||
                monster == EMonsterType.MAX ||
                monster == EMonsterType.MAX_CATJOB ||
                monster == EMonsterType.MAX_FANTASYRPG ||
                monster == EMonsterType.MAX_MEGABOT)
            {
                //Console.WriteLine($"❌ Monstre interdit : {monster}");
                return false;
            }

            // Si tout est bon, la clé est valide
            //Console.WriteLine($"✅ Clé valide : {keyToCheck}");
            return true;
        }


        public static int GetExperienceFromWankulCard(WankulCardData wankulCardData)
        {
            int shopLevel = 1;
            try
            {
                var field = typeof(CPlayerData).GetField("m_ShopLevel", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (field != null)
                {
                    shopLevel = Convert.ToInt32(field.GetValue(null));
                }
            }
            catch
            {
                shopLevel = 1;
            }

            return RaritiesManager.CalculateExperience(wankulCardData, shopLevel);
        }


        public static int GetTotalCardsCount()
        {
            return Instance.cards.Count;
        }

        public static List<WankulCardData> GetCardsFromSeason(string seasonId)
        {
            if (string.IsNullOrEmpty(seasonId)) return new List<WankulCardData>();
            return Instance.cards.FindAll(wankulCard => string.Equals(wankulCard.SeasonId, seasonId, StringComparison.OrdinalIgnoreCase));
        }

        public static List<WankulCardData> GetCardsFromSeason(Season season)
        {
            return GetCardsFromSeason(season.ToString());
        }

        public static WankulCardData GetAJETER()
        {
            return WankulCardsData.Instance.cards.Find(wankulCard => wankulCard is SpecialCardData special && special.Special == Specials.AJETER);
        }

        public void DebugDisplayAllCardsAssociations()
        {
            Dictionary<string, WankulCardData> associations = WankulCardsData.Instance.association;

            Dictionary<string, string> knewAssociations = new Dictionary<string, string>();
            foreach (var association in associations)
            {
                knewAssociations.Add(association.Key, association.Value.Title);
            }
            string json = JsonConvert.SerializeObject(knewAssociations);
        }
    }
}
