using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.duel;

namespace WankulCrazyPlugin.patch
{
    /// <summary>
    /// Point d'entrée pour lancer le duel Wankul TCG (moteur indépendant et DuelView2D)
    /// au lieu de la logique Tetramon vanilla quand le joueur s'assoit à une table de duel.
    /// </summary>
    public static class WankulDuelEntryPatch
    {
        private static readonly System.Reflection.FieldInfo _startGameCoroutineField = AccessTools.Field(typeof(PlayTableGame), "m_StartGameCoroutine");
        private static readonly System.Reflection.FieldInfo _hasSelectedTurnStartCardField = AccessTools.Field(typeof(PlayTableGame), "m_HasSelectedTurnStartCard");
        private static readonly System.Reflection.FieldInfo _isPlayerTurnField = AccessTools.Field(typeof(PlayTableGame), "m_IsPlayerTurn");
        private static readonly System.Reflection.FieldInfo _canExitField = AccessTools.Field(typeof(PlayTableGame), "m_CanExit");
        private static readonly System.Reflection.FieldInfo _isPlayerWinField = AccessTools.Field(typeof(PlayTableGame), "m_IsPlayerWin");

        public static void SetPlayTablePostfix(PlayTableGame __instance)
        {
            try
            {
                if (__instance == null) return;

                Plugin.Logger.LogInfo("[WankulDuelEntryPatch] SetPlayTablePostfix : Interception et préparation du duel Wankul TCG.");

                // 1. Préparation du deck du joueur et de l'adversaire (decks légaux 50 cartes : 10 terrains, 35 persos, 5 scoreurs)
                var wankulPool = WankulCardsData.Instance?.cards;
                var playerDeck = CardAdapter.BuildLegalDeck(wankulPool, "Joueur");
                var opponentDeck = CardAdapter.BuildLegalDeck(wankulPool, "Adversaire");

                int pTerrains = 0, pPersos = 0, pScoreurs = 0;
                foreach (var c in playerDeck)
                {
                    if (c.Kind == WankulCrazy.Duel.Engine.CardKind.Terrain) pTerrains++;
                    else if (c.IsScoreur) pScoreurs++;
                    else pPersos++;
                }

                Plugin.Logger.LogInfo($"[WankulDuelEntryPatch] Deck Joueur prêt : {playerDeck.Count} cartes (Terrains: {pTerrains}, Persos: {pPersos}, Scoreurs: {pScoreurs})");
                Plugin.Logger.LogInfo($"[WankulDuelEntryPatch] Deck Adversaire prêt : {opponentDeck.Count} cartes.");

                // 2. Stopper la coroutine vanilla DelayStart (qui lancerait Tetramon) et démarrer la nôtre
                if (_startGameCoroutineField != null)
                {
                    var coroutine = _startGameCoroutineField.GetValue(__instance) as Coroutine;
                    if (coroutine != null)
                    {
                        __instance.StopCoroutine(coroutine);
                    }
                }

                var routine = __instance.StartCoroutine(WankulStartGameRoutine(__instance, playerDeck, opponentDeck));
                _startGameCoroutineField?.SetValue(__instance, routine);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[WankulDuelEntryPatch] Exception dans SetPlayTablePostfix: {ex}");
            }
        }

        private static System.Collections.IEnumerator WankulStartGameRoutine(
            PlayTableGame game,
            List<WankulCrazy.Duel.Engine.DuelCard> playerDeck,
            List<WankulCrazy.Duel.Engine.DuelCard> opponentDeck)
        {
            // Musique d'ouverture
            CSingleton<LightManager>.Instance.SetCanChangeBGM(false);
            SoundManager.BlendToMusic("BGM_FightOpening", 0.5f, isLinearBlend: true);
            SoundManager.QueueMusic("BGM_FightOpening", "BGM_FightLoop", 1f);

            _canExitField?.SetValue(game, false);
            _isPlayerWinField?.SetValue(game, false);

            // Initialiser les UI vanilla pour l'animation 3D
            ControllerScreenUIExtManager.OnOpenScreen(game.m_ControllerScreenUIExtension_PlayCardSet);
            game.m_PlayCardSetPlayer.m_PlayCardSetUI.SetActive(true);
            game.m_PlayCardSetEnemy.m_PlayCardSetUI.SetActive(true);
            if (game.m_PhysicsBlocker != null) game.m_PhysicsBlocker.gameObject.SetActive(false);
            if (game.m_ItemSpawnPhysicsBlocker != null) game.m_ItemSpawnPhysicsBlocker.SetActive(false);
            if (game.m_ItemSpawnParentGrp != null) game.m_ItemSpawnParentGrp.gameObject.SetActive(false);

            game.m_PlayCardSetPlayer.m_PlayCardSetUI.m_TurnSelectUIGrp.SetActive(false);
            game.m_PlayCardSetPlayer.ResetBoard(canUpdateDeck: true);
            game.m_PlayCardSetEnemy.ResetBoard(canUpdateDeck: false);
            game.m_PlayCardSetPlayer.SetIsTurnActive(false);
            game.m_PlayCardSetEnemy.SetIsTurnActive(false);
            if (game.m_PlayCardElementDamageDisplay != null) game.m_PlayCardElementDamageDisplay.HideUI();

            yield return new WaitForSeconds(0.5f);

            game.m_Grp.SetActive(true);
            game.m_PlayCardSetPlayer.m_PlayCardSetUI.m_TurnSelectUIGrp.SetActive(false);
            game.m_PlayCardSetPlayer.SetAllCardVisibility(true);
            game.m_PlayCardSetEnemy.SetAllCardVisibility(true);

            game.m_PlayCardSetPlayer.EvaluatePlaymatDeckboxMesh();
            game.m_PlayCardSetEnemy.EvaluatePlaymatDeckboxMesh();

            if (game.m_PhysicsBlocker != null)
            {
                game.m_PhysicsBlocker.gameObject.SetActive(true);
                game.m_PhysicsBlocker.isTrigger = true;
                yield return new WaitForSeconds(0.01f);
                game.m_PhysicsBlocker.isTrigger = false;
            }

            _hasSelectedTurnStartCardField?.SetValue(game, false);

            // Caméra de dessus
            CSingleton<InteractionPlayerController>.Instance.SetCameraWorldPositionTarget(game.m_CamLoc);
            CSingleton<InteractionPlayerController>.Instance.StartAimLookAt(game.m_CamDownTarget, 8f);
            CSingleton<InteractionPlayerController>.Instance.m_IsPlayingTopDownGameMode = true;
            CSingleton<InteractionPlayerController>.Instance.m_CameraFOVController.StartLerpToFOV(40f);

            // Activer le groupe TurnSelectUIGrp AVANT de déclencher les animations de mélange
            game.m_PlayCardSetPlayer.m_PlayCardSetUI.m_TurnSelectUIGrp.SetActive(true);

            // Animation de mélange des deux cartes
            if (game.m_PlayCardSetPlayer.m_PlayCardSetUI.m_TurnSelectAreaAnimList != null &&
                game.m_PlayCardSetPlayer.m_PlayCardSetUI.m_TurnSelectAreaAnimList.Count >= 2)
            {
                game.m_PlayCardSetPlayer.m_PlayCardSetUI.m_TurnSelectAreaAnimList[0].gameObject.SetActive(true);
                game.m_PlayCardSetPlayer.m_PlayCardSetUI.m_TurnSelectAreaAnimList[1].gameObject.SetActive(true);
                game.m_PlayCardSetPlayer.m_PlayCardSetUI.m_TurnSelectAreaAnimList[0].Play("PlayCardTurnSelectShuffleA");
                game.m_PlayCardSetPlayer.m_PlayCardSetUI.m_TurnSelectAreaAnimList[1].Play("PlayCardTurnSelectShuffleB");
            }
            SoundManager.PlayAudio("PlayCard_Shuffle2", 0.3f);

            yield return new WaitForSeconds(1.75f);

            // Invitation à choisir qui commence
            ControllerScreenUIExtManager.OnOpenScreen(game.m_ControllerScreenUIExtension_WhoGoesFirst);
            game.m_IsWaitingResponse = true;

            if (game.m_PlayCardSetPlayer.m_IsAIControlled)
            {
                game.OnPressTurnSelectCard(UnityEngine.Random.Range(0, 2));
            }

            // Attente du choix de la carte par le joueur
            yield return new WaitUntil(() => _hasSelectedTurnStartCardField != null && (bool)_hasSelectedTurnStartCardField.GetValue(game));

            game.m_IsWaitingResponse = false;
            ControllerScreenUIExtManager.OnCloseScreen(game.m_ControllerScreenUIExtension_WhoGoesFirst);

            // Temps d'observation de la carte retournée (1er ou 2ème joueur)
            yield return new WaitForSeconds(1.5f);

            ControllerScreenUIExtManager.OnOpenScreen(game.m_ControllerScreenUIExtension_PlayCardSet);
            game.m_PlayCardSetPlayer.m_PlayCardSetUI.m_TurnSelectUIGrp.SetActive(false);

            // Masquer les éléments vanilla
            game.m_PlayCardSetPlayer.m_PlayCardSetUI.SetActive(false);
            game.m_PlayCardSetEnemy.m_PlayCardSetUI.SetActive(false);
            ControllerScreenUIExtManager.OnCloseScreen(game.m_ControllerScreenUIExtension_PlayCardSet);

            bool isPlayerTurn = _isPlayerTurnField != null && (bool)_isPlayerTurnField.GetValue(game);
            var startingPlayer = isPlayerTurn ? WankulCrazy.Duel.Engine.PlayerId.Player1 : WankulCrazy.Duel.Engine.PlayerId.Player2;

            Plugin.Logger.LogInfo($"[WankulDuelEntryPatch] Tirage terminé : {(isPlayerTurn ? "Joueur (P1)" : "Adversaire (P2)")} commence !");

            // Lancement de la vue 2D Wankul TCG
            DuelView2D.Show(game, playerDeck, opponentDeck, startingPlayer);
        }
    }
}
