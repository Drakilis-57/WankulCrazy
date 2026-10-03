using System;
using UnityEngine;

namespace WankulCrazyPlugin.duel
{
    /// <summary>
    /// Gestionnaire des effets sonores (SFX) et musiques vanilla pour le Duel Wankul.
    /// Réutilise les ressources audio natives de Card Shop Simulator.
    /// </summary>
    public static class DuelSfx
    {
        private static readonly System.Random _random = new System.Random();

        /// <summary>
        /// Joue le son de pioche de carte (variante aléatoire Draw1..3).
        /// </summary>
        public static void PlayDraw()
        {
            try
            {
                int variant = _random.Next(1, 4);
                SoundManager.PlayAudio($"PlayCard_Draw{variant}", 0.5f);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[DuelSfx] Erreur PlayDraw : {ex.Message}");
            }
        }

        /// <summary>
        /// Joue le son de pose de carte sur le plateau (variante aléatoire PlaceCard1..10).
        /// </summary>
        public static void PlayPlaceCard()
        {
            try
            {
                int variant = _random.Next(1, 11);
                SoundManager.PlayAudio($"PlayCard_PlaceCard{variant}", 0.55f);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[DuelSfx] Erreur PlayPlaceCard : {ex.Message}");
            }
        }

        /// <summary>
        /// Joue le son de sélection d'une carte dans la main.
        /// </summary>
        public static void PlaySelectCard()
        {
            try
            {
                SoundManager.PlayAudio("PlayCard_SelectCard", 0.35f);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[DuelSfx] Erreur PlaySelectCard : {ex.Message}");
            }
        }

        /// <summary>
        /// Joue le son de mélange / préparation de paquet.
        /// </summary>
        public static void PlayShuffle()
        {
            try
            {
                SoundManager.PlayAudio("PlayCard_Shuffle2", 0.4f);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[DuelSfx] Erreur PlayShuffle : {ex.Message}");
            }
        }

        /// <summary>
        /// Son émis lors de la résolution d'un score de Terrain.
        /// </summary>
        public static void PlayScoreResolved(bool? isPlayerWinner)
        {
            try
            {
                if (isPlayerWinner == true)
                {
                    // Le joueur remporte le terrain
                    SoundManager.PlayAudio("SFX_PercStarJingle3", 0.65f);
                    SoundManager.PlayAudio("PlayCard_Powerup", 0.45f);
                }
                else if (isPlayerWinner == false)
                {
                    // L'adversaire remporte le terrain
                    SoundManager.PlayAudio("PlayCard_MonsterKO", 0.35f);
                }
                else
                {
                    // Égalité (aucun point marqué)
                    SoundManager.PlayAudio("PlayCard_Miss", 0.3f);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[DuelSfx] Erreur PlayScoreResolved : {ex.Message}");
            }
        }

        /// <summary>
        /// Son émis lors du nettoyage d'un terrain (envoi vers la défausse).
        /// </summary>
        public static void PlayTerrainCleared()
        {
            try
            {
                SoundManager.PlayAudio("PlayCard_Heal", 0.25f);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[DuelSfx] Erreur PlayTerrainCleared : {ex.Message}");
            }
        }

        /// <summary>
        /// Son émis lors du début d'un tour.
        /// </summary>
        public static void PlayTurnStart(bool isPlayerTurn)
        {
            try
            {
                if (isPlayerTurn)
                {
                    SoundManager.PlayAudio("PlayCard_FlipCard", 0.45f);
                }
                else
                {
                    SoundManager.PlayAudio("PlayCard_Riffle", 0.35f);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[DuelSfx] Erreur PlayTurnStart : {ex.Message}");
            }
        }

        /// <summary>
        /// Son de fin de partie (victoire / défaite).
        /// </summary>
        public static void PlayGameOver(bool isPlayerWin)
        {
            try
            {
                if (isPlayerWin)
                {
                    SoundManager.PlayAudio("SFX_Gift", 0.75f);
                }
                else
                {
                    SoundManager.PlayAudio("PlayCard_MonsterKO", 0.5f);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[DuelSfx] Erreur PlayGameOver : {ex.Message}");
            }
        }

        /// <summary>
        /// Son générique de clic de bouton UI.
        /// </summary>
        public static void PlayButtonClick()
        {
            try
            {
                SoundManager.GenericConfirm();
            }
            catch { }
        }

        /// <summary>
        /// Son d'annulation / fermeture de pop-up.
        /// </summary>
        public static void PlayCancel()
        {
            try
            {
                SoundManager.GenericCancel();
            }
            catch { }
        }

        /// <summary>
        /// Démarre la musique de combat vanilla en boucle.
        /// </summary>
        public static void StartDuelMusic()
        {
            try
            {
                if (CSingleton<LightManager>.Instance != null)
                {
                    CSingleton<LightManager>.Instance.SetCanChangeBGM(false);
                }
                SoundManager.BlendToMusic("BGM_FightOpening", 0.5f, isLinearBlend: true);
                SoundManager.QueueMusic("BGM_FightOpening", "BGM_FightLoop", 1f);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[DuelSfx] Erreur StartDuelMusic : {ex.Message}");
            }
        }

        /// <summary>
        /// Restaure la musique normale du magasin.
        /// </summary>
        public static void StopDuelMusic()
        {
            try
            {
                if (CSingleton<LightManager>.Instance != null)
                {
                    CSingleton<LightManager>.Instance.SetCanChangeBGM(true);
                    CSingleton<LightManager>.Instance.RefreshBGM();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[DuelSfx] Erreur StopDuelMusic : {ex.Message}");
            }
        }
    }
}
