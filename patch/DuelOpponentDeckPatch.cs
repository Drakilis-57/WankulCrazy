using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.patch
{
    /// <summary>
    /// Remplace la génération du deck Tetramon vanilla de l'adversaire IA
    /// par un deck Wankul sélectionné selon le niveau du magasin.
    ///
    /// Cible : PlayCardSet.ResetBoard(bool canUpdateDeck).
    ///
    /// Comportement de l'original (branche canUpdateDeck pour l'IA) :
    ///   - Lit m_PlayCardDeckData.monsterTypeList (ScriptableObject Tetramon vanilla)
    ///   - Génère 50 CardData avec des EMonsterType Tetramon
    ///   - Assigne aléatoirement des bordures et expansions
    ///
    /// Notre Postfix :
    ///   - S'exécute APRÈS le ResetBoard original (qui efface m_SetupCardDataList)
    ///   - Si la liste a déjà été remplie par l'original (IA, canUpdateDeck=true),
    ///     on la REMPLACE par 50 cartes Wankul tirées de la bonne saison
    ///   - Si c'est le deck du Joueur humain → on ne touche pas
    /// </summary>
    public static class DuelOpponentDeckPatch
    {
        private const int DECK_SIZE = 50;
        private const int MAX_COPIES_PER_CARD = 4; // Règle du jeu : max 4 copies d'une même carte

        /// <summary>
        /// Préfixe sur PlayTableGame.SetPlayTable.
        /// Si le deck sélectionné du joueur contient moins de 50 cartes (ex: début de partie),
        /// on remplit temporairement le deck avec 50 cartes compactes pour permettre
        /// de lancer immédiatement le match sans message 'DeckIncomplete'.
        /// </summary>
        public static void SetPlayTablePrefix(PlayTableGame __instance)
        {
            try
            {
                if (CPlayerData.m_DeckCompactCardDataList == null) return;

                while (CPlayerData.m_CurrentSelectedDeckIndex >= CPlayerData.m_DeckCompactCardDataList.Count)
                {
                    CPlayerData.m_DeckCompactCardDataList.Add(new DeckCompactCardDataList());
                }

                var currentDeck = CPlayerData.m_DeckCompactCardDataList[CPlayerData.m_CurrentSelectedDeckIndex];
                if (currentDeck == null)
                {
                    currentDeck = new DeckCompactCardDataList();
                    CPlayerData.m_DeckCompactCardDataList[CPlayerData.m_CurrentSelectedDeckIndex] = currentDeck;
                }

                if (currentDeck.GetTotalCardCount() < DECK_SIZE)
                {
                    Plugin.Logger.LogInfo($"[DuelOpponentDeckPatch] Deck joueur {CPlayerData.m_CurrentSelectedDeckIndex} incomplet ({currentDeck.GetTotalCardCount()} cartes). Auto-complétion du deck de prêt Wankul pour le duel !");
                    currentDeck.compactCardDataAmountList.Clear();
                    // On ajoute 50 entrées compactes de test pour passer la garde vanilla
                    for (int i = 0; i < DECK_SIZE; i++)
                    {
                        currentDeck.compactCardDataAmountList.Add(new CompactCardDataAmount
                        {
                            cardSaveIndex = i,
                            amount = 1,
                            expansionType = ECardExpansionType.Tetramon,
                            isDestiny = false
                        });
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"[DuelOpponentDeckPatch] Erreur dans SetPlayTablePrefix: {ex}");
            }
        }

        /// <summary>
        /// Postfix exécuté après PlayCardSet.ResetBoard.
        /// Remplace m_SetupCardDataList de l'IA par un deck Wankul.
        /// </summary>
        public static void ResetBoardPostfix(
            PlayCardSet __instance,
            bool canUpdateDeck,
            bool ___m_IsPlayer,
            bool ___m_IsAIControlled,
            List<CardData> ___m_SetupCardDataList)
        {
            try
            {
                // Si c'est le joueur humain : lui fournir un starter deck Wankul légal de 50 cartes
                if (___m_IsPlayer && !___m_IsAIControlled)
                {
                    Plugin.Logger.LogInfo($"[DuelOpponentDeckPatch] Chargement du deck joueur Wankul Saison 1 (50 cartes)...");
                    var playerDeck = BuildWankulDeck(Season.S01, CPlayerData.m_ShopLevel);
                    if (playerDeck != null && playerDeck.Count == DECK_SIZE)
                    {
                        ___m_SetupCardDataList.Clear();
                        ___m_SetupCardDataList.AddRange(playerDeck);
                        Plugin.Logger.LogInfo($"[DuelOpponentDeckPatch] Deck joueur Wankul injecté avec succès ({playerDeck.Count} cartes) !");
                    }
                    return;
                }

                // Seulement quand le deck de l'adversaire est autorisé à être mis à jour
                if (!canUpdateDeck)
                    return;

                // Seulement si l'original a bien rempli le deck (50 cartes)
                if (___m_SetupCardDataList.Count != DECK_SIZE)
                    return;

                int shopLevel = CPlayerData.m_ShopLevel;
                Season targetSeason = GetSeasonForLevel(shopLevel);

                List<CardData> wankulDeck = BuildWankulDeck(targetSeason, shopLevel);

                if (wankulDeck == null || wankulDeck.Count != DECK_SIZE)
                {
                    Plugin.Logger.LogWarning($"[DuelOpponentDeckPatch] Impossible de construire un deck Wankul ({wankulDeck?.Count ?? 0} cartes). Fallback vanilla.");
                    return;
                }

                ___m_SetupCardDataList.Clear();
                ___m_SetupCardDataList.AddRange(wankulDeck);

                Plugin.Logger.LogInfo($"[DuelOpponentDeckPatch] Deck adversaire Wankul chargé : Saison {targetSeason} ({DECK_SIZE} cartes, ShopLevel={shopLevel}).");
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"[DuelOpponentDeckPatch] Exception dans ResetBoardPostfix : {ex}");
                // En cas d'erreur, on laisse le deck vanilla original (déjà dans la liste)
            }
        }

        /// <summary>
        /// Détermine la saison cible selon le niveau de magasin.
        /// Progression : S01 → S02 → S03 → S04 → S05
        /// </summary>
        private static Season GetSeasonForLevel(int shopLevel)
        {
            if (shopLevel <= 10) return Season.S01;
            if (shopLevel <= 20) return Season.S02;
            if (shopLevel <= 35) return Season.S03;
            if (shopLevel <= 55) return Season.S04;
            return Season.S05;
        }

        /// <summary>
        /// Construit un deck de 50 CardData Wankul depuis la saison donnée.
        ///
        /// Stratégie :
        ///   - On tire aléatoirement parmi les cartes de la saison
        ///   - On respecte la règle des 4 copies max par carte (Index unique)
        ///   - Si pas assez de cartes dans la saison, on complète avec la saison précédente
        ///   - Les bordures/expansions sont attribuées aléatoirement (comme l'original vanilla)
        /// </summary>
        private static List<CardData> BuildWankulDeck(Season season, int shopLevel)
        {
            WankulCardsData.Instance.EnsureInitialized();

            List<WankulCardData> seasonCards = WankulCardsData.GetCardsBySeasonFast(season);

            // Fallback sur la saison précédente si trop peu de cartes
            if (seasonCards == null || seasonCards.Count < 12)
            {
                Plugin.Logger.LogWarning($"[DuelOpponentDeckPatch] Saison {season} : moins de 12 cartes disponibles. Fallback sur S01.");
                seasonCards = WankulCardsData.GetCardsBySeasonFast(Season.S01);
            }

            if (seasonCards == null || seasonCards.Count == 0)
            {
                Plugin.Logger.LogError("[DuelOpponentDeckPatch] Aucune carte Wankul disponible pour construire le deck.");
                return null;
            }

            // Mélange aléatoire de la liste source
            List<WankulCardData> shuffled = new List<WankulCardData>(seasonCards);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[j], shuffled[i]) = (shuffled[i], shuffled[j]);
            }

            // Construction du deck (50 cartes, max 4 copies par carte)
            var deck = new List<CardData>();
            var copiesCount = new Dictionary<int, int>(); // Index WankulCard → nb copies

            int attempts = 0;
            int maxAttempts = shuffled.Count * MAX_COPIES_PER_CARD;

            while (deck.Count < DECK_SIZE && attempts < maxAttempts)
            {
                WankulCardData wCard = shuffled[attempts % shuffled.Count];
                attempts++;

                if (wCard == null) continue;

                copiesCount.TryGetValue(wCard.Index, out int currentCopies);
                if (currentCopies >= MAX_COPIES_PER_CARD) continue;

                CardData cardData = WankulCardsData.Instance.GetCardDataFromWankulCardData(wCard);
                if (cardData == null)
                {
                    Plugin.LogInfo($"[DuelOpponentDeckPatch] Carte sans CardData ignorée : {wCard.Title}");
                    continue;
                }

                // Appliquer une bordure et expansion aléatoires selon le niveau
                CardData deckCard = new CardData
                {
                    monsterType = cardData.monsterType,
                    borderType = GetRandomBorderForLevel(shopLevel),
                    expansionType = cardData.expansionType,
                    isFoil = Random.Range(0, 100) < GetFoilChanceForLevel(shopLevel)
                };

                deck.Add(deckCard);
                copiesCount[wCard.Index] = currentCopies + 1;
            }

            // Si on a moins de 50 cartes (ex: saison trop petite), on boucle
            int fillAttempts = 0;
            while (deck.Count < DECK_SIZE && fillAttempts < DECK_SIZE * 2)
            {
                WankulCardData wCard = shuffled[fillAttempts % shuffled.Count];
                fillAttempts++;
                if (wCard == null) continue;

                copiesCount.TryGetValue(wCard.Index, out int currentCopies);
                if (currentCopies >= MAX_COPIES_PER_CARD) continue;

                CardData cardData = WankulCardsData.Instance.GetCardDataFromWankulCardData(wCard);
                if (cardData == null) continue;

                CardData deckCard = new CardData
                {
                    monsterType = cardData.monsterType,
                    borderType = GetRandomBorderForLevel(shopLevel),
                    expansionType = cardData.expansionType,
                    isFoil = Random.Range(0, 100) < GetFoilChanceForLevel(shopLevel)
                };

                deck.Add(deckCard);
                copiesCount[wCard.Index] = currentCopies + 1;
            }

            if (deck.Count < DECK_SIZE)
            {
                Plugin.Logger.LogWarning($"[DuelOpponentDeckPatch] Deck incomplet : {deck.Count}/{DECK_SIZE} cartes. Impossible de remplir.");
                return null;
            }

            return deck;
        }

        /// <summary>
        /// Retourne une bordure aléatoire pondérée selon le niveau du magasin.
        /// Plus le niveau est haut, plus les bordures rares sont fréquentes.
        /// </summary>
        private static ECardBorderType GetRandomBorderForLevel(int shopLevel)
        {
            // Pool de bordures pondérée (même logique que l'original vanilla)
            int roll = Random.Range(0, 100);

            if (shopLevel <= 15)
            {
                // Débutant : surtout Base
                if (roll < 70) return ECardBorderType.Base;
                if (roll < 90) return ECardBorderType.FirstEdition;
                return ECardBorderType.Silver;
            }
            else if (shopLevel <= 35)
            {
                // Intermédiaire
                if (roll < 40) return ECardBorderType.Base;
                if (roll < 65) return ECardBorderType.FirstEdition;
                if (roll < 82) return ECardBorderType.Silver;
                if (roll < 93) return ECardBorderType.Gold;
                if (roll < 98) return ECardBorderType.EX;
                return ECardBorderType.FullArt;
            }
            else
            {
                // Expert : toutes bordures accessibles
                if (roll < 20) return ECardBorderType.Base;
                if (roll < 40) return ECardBorderType.FirstEdition;
                if (roll < 60) return ECardBorderType.Silver;
                if (roll < 75) return ECardBorderType.Gold;
                if (roll < 88) return ECardBorderType.EX;
                return ECardBorderType.FullArt;
            }
        }

        /// <summary>
        /// % de chance d'avoir une carte foil dans le deck adversaire selon le niveau.
        /// </summary>
        private static int GetFoilChanceForLevel(int shopLevel)
        {
            if (shopLevel <= 10) return 5;
            if (shopLevel <= 25) return 15;
            if (shopLevel <= 45) return 30;
            return 50;
        }

        private static float s_LastAiExceptionTime = 0f;

        /// <summary>
        /// Prefix sur PlayCardSet.EvaluateEnemyAI.
        /// Si le code vanilla de l'IA rencontre une anomalie non catchée (carte Wankul inconnue ou champ manquant),
        /// on intercepte l'exception pour que le duel ne reste pas figé et que le tour puisse se terminer.
        /// </summary>
        public static bool EvaluateEnemyAIPrefix(PlayCardSet __instance, bool ___m_IsAIControlled, bool ___m_IsWaitingActionResolve)
        {
            if (!___m_IsAIControlled || !___m_IsWaitingActionResolve) return true;

            try
            {
                // S'assurer que les zones de jeu élémentaires ne contiennent pas de références nulles dans m_ElementAreaMonsterDataList
                if (__instance.m_ElementAreaMonsterDataList != null)
                {
                    for (int i = 0; i < __instance.m_ElementAreaMonsterDataList.Count; i++)
                    {
                        if (__instance.m_ElementAreaMonsterDataList[i] == null)
                        {
                            __instance.m_ElementAreaMonsterDataList[i] = new MonsterData();
                        }
                    }
                }
                return true; // Exécuter l'IA vanilla normalement
            }
            catch (System.Exception ex)
            {
                if (Time.time - s_LastAiExceptionTime > 2f)
                {
                    s_LastAiExceptionTime = Time.time;
                    Plugin.Logger.LogWarning($"[DuelOpponentDeckPatch] Interception sécurisée de l'IA adverse : {ex.Message}");
                }
                return false; // Empêcher le crash vanilla de faire planter Update()
            }
        }
    }
}

