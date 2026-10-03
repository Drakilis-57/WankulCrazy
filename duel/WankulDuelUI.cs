using UnityEngine;
using UnityEngine.UI;
using WankulCrazyPlugin.duel;

namespace WankulCrazyPlugin.duel
{
    /// <summary>
    /// Interface Utilisateur Dédiée aux Duels Wankul TCG.
    /// Remplace visuellement les compteurs Tetramon (500 PV, symboles élémentaires)
    /// par un affichage propre des 3 Slots de Terrain, des Forces et du Score.
    /// </summary>
    public class WankulDuelUI : MonoBehaviour
    {
        public static WankulDuelUI Instance { get; private set; }

        private GameObject canvasObj;
        private Canvas canvas;
        private Text[] slotTexts = new Text[WankulBoardState.MAX_TERRAIN_SLOTS];
        private Text scoreText;
        private Text statusText;

        public static void Create(WankulDuelController controller)
        {
            if (Instance != null) return;
            var go = new GameObject("WankulDuelUI");
            go.transform.SetParent(controller.transform, false);
            Instance = go.AddComponent<WankulDuelUI>();
            Instance.BuildUI(controller);
        }

        public void DestroyUI()
        {
            if (canvasObj != null)
            {
                Destroy(canvasObj);
            }
            Instance = null;
        }

        private void BuildUI(WankulDuelController controller)
        {
            // Canvas en ScreenSpace-Overlay
            canvasObj = new GameObject("WankulDuelCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Panneau supérieur : Score Wankul
            GameObject scorePanel = new GameObject("ScorePanel");
            scorePanel.transform.SetParent(canvasObj.transform, false);
            var scoreRect = scorePanel.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.5f, 1f);
            scoreRect.anchorMax = new Vector2(0.5f, 1f);
            scoreRect.pivot = new Vector2(0.5f, 1f);
            scoreRect.anchoredPosition = new Vector2(0, -20);
            scoreRect.sizeDelta = new Vector2(650, 70);

            var scoreBg = scorePanel.AddComponent<Image>();
            scoreBg.color = new Color(0.08f, 0.12f, 0.18f, 0.88f);

            var scoreOutline = scorePanel.AddComponent<Outline>();
            scoreOutline.effectColor = new Color(0.95f, 0.75f, 0.15f, 1f);
            scoreOutline.effectDistance = new Vector2(2, -2);

            GameObject scoreTextObj = new GameObject("ScoreText");
            scoreTextObj.transform.SetParent(scorePanel.transform, false);
            var scoreTextRect = scoreTextObj.AddComponent<RectTransform>();
            scoreTextRect.anchorMin = Vector2.zero;
            scoreTextRect.anchorMax = Vector2.one;
            scoreTextRect.sizeDelta = Vector2.zero;

            scoreText = scoreTextObj.AddComponent<Text>();
            scoreText.font = defaultFont;
            scoreText.fontSize = 28;
            scoreText.alignment = TextAnchor.MiddleCenter;
            scoreText.color = Color.white;
            scoreText.text = "WANKUL TCG  |  JOUEUR : 0 / 5  —  ADVERSAIRE : 0 / 5";

            // Panneau des 3 Terrains (aligné sous le bandeau de score en haut)
            GameObject terrainsContainer = new GameObject("TerrainsContainer");
            terrainsContainer.transform.SetParent(canvasObj.transform, false);
            var containerRect = terrainsContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 1f);
            containerRect.anchorMax = new Vector2(0.5f, 1f);
            containerRect.pivot = new Vector2(0.5f, 1f);
            containerRect.anchoredPosition = new Vector2(0, -95);
            containerRect.sizeDelta = new Vector2(920, 105);

            float[] xOffsets = new float[] { -305f, 0f, 305f };

            for (int i = 0; i < WankulBoardState.MAX_TERRAIN_SLOTS; i++)
            {
                int slotIdx = i;
                GameObject slotPanel = new GameObject($"SlotPanel_{i}");
                slotPanel.transform.SetParent(terrainsContainer.transform, false);
                var slotRect = slotPanel.AddComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.anchoredPosition = new Vector2(xOffsets[i], 0);
                slotRect.sizeDelta = new Vector2(290, 95);

                var slotBg = slotPanel.AddComponent<Image>();
                slotBg.color = new Color(0.08f, 0.12f, 0.18f, 0.75f);

                var slotOutline = slotPanel.AddComponent<Outline>();
                slotOutline.effectColor = new Color(0.3f, 0.5f, 0.8f, 0.6f);
                slotOutline.effectDistance = new Vector2(1.5f, -1.5f);

                GameObject textObj = new GameObject("SlotText");
                textObj.transform.SetParent(slotPanel.transform, false);
                var textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(8, 6);
                textRect.offsetMax = new Vector2(-8, -6);

                var txt = textObj.AddComponent<Text>();
                txt.font = defaultFont;
                txt.fontSize = 15;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                txt.text = $"[ TERRAIN {i + 1} ]\n(Vide)";
                slotTexts[i] = txt;
            }

            // Message de statut / actions
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(canvasObj.transform, false);
            var statusRect = statusObj.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.5f, 0f);
            statusRect.anchorMax = new Vector2(0.5f, 0f);
            statusRect.pivot = new Vector2(0.5f, 0f);
            statusRect.anchoredPosition = new Vector2(0, 115);
            statusRect.sizeDelta = new Vector2(800, 35);

            statusText = statusObj.AddComponent<Text>();
            statusText.font = defaultFont;
            statusText.fontSize = 20;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = new Color(1f, 0.9f, 0.4f, 1f);
            statusText.text = "Cliquez sur une carte en main, puis sur un slot pour la poser.";
        }

        private void Update()
        {
            var ctrl = WankulDuelController.Instance;
            if (ctrl == null || !ctrl.IsWankulDuelActive) return;

            // Masquer le rendu du Canvas tant que le setup et le Mulligan ne sont pas terminés
            if (canvas != null)
            {
                bool shouldShow = ctrl.IsGameSetupCompleted;
                if (canvas.enabled != shouldShow)
                {
                    canvas.enabled = shouldShow;
                }
            }

            // Assurer que le bouton Fin du tour est visible quand c'est le tour du joueur
            if (ctrl.IsGameSetupCompleted && ctrl.GameTable != null && ctrl.BoardState.IsPlayerTurn)
            {
                ctrl.GameTable.m_PlayCardSetPlayer?.m_PlayCardSetUI?.SetEndTurnBtnVisibility(true);
            }

            var board = ctrl.BoardState;
            if (board == null) return;

            // Mise à jour du score
            if (scoreText != null)
            {
                scoreText.text = $"WANKUL TCG  |  JOUEUR : {board.PlayerScore} / {WankulBoardState.WINNING_TERRAIN_COUNT}  —  ADVERSAIRE : {board.EnemyScore} / {WankulBoardState.WINNING_TERRAIN_COUNT}";
            }

            // Mise à jour de chaque slot
            for (int i = 0; i < WankulBoardState.MAX_TERRAIN_SLOTS; i++)
            {
                if (slotTexts[i] == null) continue;
                var slot = board.Terrains[i];

                if (!slot.HasTerrain)
                {
                    slotTexts[i].text = $"<b>[ TERRAIN {i + 1} ]</b>\n<color=#aaaaaa>(Vide)</color>\nPosez un Terrain ici";
                }
                else
                {
                    string status = slot.IsActive ? "<color=#55ff55>Actif</color>" : "<color=#ffaa33>Incliné (ce tour)</color>";
                    int playerForce = slot.GetPlayerTotalForce();
                    int enemyForce = slot.GetEnemyTotalForce();

                    slotTexts[i].text = $"<b>[ {slot.TerrainData.Title.ToUpper()} ]</b> ({status})\n" +
                                        $"Joueur: <b><color=#44bbff>{playerForce} Force</color></b> ({slot.PlayerCharacters.Count} persos)\n" +
                                        $"Adversaire: <b><color=#ff5555>{enemyForce} Force</color></b> ({slot.EnemyCharacters.Count} persos)";
                }
            }

            // Masquer les éléments Tetramon parasites (HP, bouclier, éléments 0-3) pour Joueur et IA
            if (ctrl.GameTable != null)
            {
                if (ctrl.GameTable.m_PlayCardElementDamageDisplay != null &&
                    ctrl.GameTable.m_PlayCardElementDamageDisplay.gameObject.activeSelf)
                {
                    ctrl.GameTable.m_PlayCardElementDamageDisplay.HideUI();
                }

                HideTetramonUIElements(ctrl.GameTable.m_PlayCardSetPlayer?.m_PlayCardSetUI);
                HideTetramonUIElements(ctrl.GameTable.m_PlayCardSetEnemy?.m_PlayCardSetUI);
            }
        }

        private void HideTetramonUIElements(PlayCardSetUI setUI)
        {
            if (setUI == null) return;
            try
            {
                // Hexagone des 500 PV
                if (setUI.m_HPText != null && setUI.m_HPText.transform.parent != null)
                {
                    var hpParent = setUI.m_HPText.transform.parent.gameObject;
                    if (hpParent.activeSelf) hpParent.SetActive(false);
                }

                // Bouclier
                if (setUI.m_ShieldGrp != null && setUI.m_ShieldGrp.activeSelf)
                {
                    setUI.m_ShieldGrp.SetActive(false);
                }

                // Compteurs d'attaque élémentaires (Feu, Eau, Plante, Foudre)
                if (setUI.m_ElementAtkPowerTextList != null)
                {
                    foreach (var atkText in setUI.m_ElementAtkPowerTextList)
                    {
                        if (atkText != null && atkText.transform.parent != null && atkText.transform.parent.gameObject.activeSelf)
                        {
                            atkText.transform.parent.gameObject.SetActive(false);
                        }
                    }
                }
            }
            catch { }
        }
    }
}
