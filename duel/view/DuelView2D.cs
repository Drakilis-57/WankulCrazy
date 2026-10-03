using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WankulCrazy.Duel.Engine;
using WankulCrazyPlugin.importer;

namespace WankulCrazyPlugin.duel
{
    /// <summary>
    /// Vue 2D plein écran pour les duels Wankul TCG, conçue en uGUI avec WankulUiKit.
    /// S'abonne aux événements émis par le DuelEngine et ne mute jamais directement l'état.
    /// </summary>
    public class DuelView2D : MonoBehaviour
    {
        public static DuelView2D Instance { get; private set; }

        private DuelEngine _engine;
        private DuelAI _ai;
        private PlayTableGame _playTable;

        private GameObject _canvasObj;
        private Canvas _canvas;

        // Éléments d'interface
        private Text _topInfoText;
        private Text _statusMessageText;
        private Text[] _slotTerrainTexts = new Text[3];
        private Image[] _slotTerrainImages = new Image[3];
        private Text[] _slotStatusTexts = new Text[3];
        private Text[] _slotP1ForceTexts = new Text[3];
        private Text[] _slotP2ForceTexts = new Text[3];
        private Text[] _slotP1CardsTexts = new Text[3];
        private Text[] _slotP2CardsTexts = new Text[3];

        private Transform _handContainer;
        private Button _endTurnButton;
        private Text _endTurnButtonText;

        // Sélection de slot pour jouer une carte
        private DuelCard _selectedHandCard;
        private Button[] _slotButtons = new Button[3];
        private Outline[] _slotOutlines = new Outline[3];

        public static void Show(PlayTableGame playTable, List<DuelCard> playerDeck, List<DuelCard> opponentDeck, PlayerId? startingPlayer = null)
        {
            if (Instance != null)
            {
                Destroy(Instance.gameObject);
            }

            var go = new GameObject("DuelView2D");
            Instance = go.AddComponent<DuelView2D>();
            Instance.Initialize(playTable, playerDeck, opponentDeck, startingPlayer);
        }

        public void Initialize(PlayTableGame playTable, List<DuelCard> playerDeck, List<DuelCard> opponentDeck, PlayerId? startingPlayer = null)
        {
            _playTable = playTable;

            var rng = new SystemRandomAdapter();
            var rules = new DuelRules(
                startingHandSize: 5,
                cardsDrawnPerTurn: 2,
                terrainsToWin: 5,
                maxCharactersPerTurn: 4,
                maxTerrainsOnBoard: 3
            );

            _engine = new DuelEngine(rng, rules);
            _ai = new DuelAI(PlayerId.Player2, rng);

            // Abonnement aux événements du moteur
            _engine.OnEvent += HandleDuelEvent;

            BuildUI();

            // Démarrage de l'ambiance sonore du duel
            DuelSfx.StartDuelMusic();
            DuelSfx.PlayShuffle();

            // Démarrage du duel avec le premier joueur issu du tirage
            _engine.StartDuel(playerDeck, opponentDeck, fixedStartingPlayer: startingPlayer);
            _engine.StartTurn();

            RefreshView();

            // Si c'est l'adversaire qui commence au Tour 1, lancer son action
            if (_engine.State.ActivePlayer == PlayerId.Player2)
            {
                StartCoroutine(ExecuteInitialAITurn());
            }
        }

        private void OnDestroy()
        {
            DuelSfx.StopDuelMusic();

            if (_engine != null)
            {
                _engine.OnEvent -= HandleDuelEvent;
            }
            if (_canvasObj != null)
            {
                Destroy(_canvasObj);
            }
            Instance = null;
        }

        private void BuildUI()
        {
            _canvasObj = new GameObject("DuelView2DCanvas");
            _canvasObj.transform.SetParent(transform, false);
            _canvas = WankulUiKit.SetupCanvas(_canvasObj, 600);

            var font = WankulUiKit.GetFont();

            // Fond sombre semi-transparent
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(_canvasObj.transform, false);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = WankulUiKit.WhiteSprite;
            bgImg.color = new Color(0.08f, 0.09f, 0.12f, 0.95f);
            WankulUiKit.Stretch(bgObj.GetComponent<RectTransform>());

            // --- 1. BANDEAU SUPÉRIEUR (Score, Tour, Statut) ---
            var topBar = new GameObject("TopBar");
            topBar.transform.SetParent(_canvasObj.transform, false);
            var topBarImg = topBar.AddComponent<Image>();
            topBarImg.sprite = WankulUiKit.WhiteSprite;
            topBarImg.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            var topBarRt = topBar.GetComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0f, 0.88f);
            topBarRt.anchorMax = new Vector2(1f, 1f);
            topBarRt.offsetMin = Vector2.zero;
            topBarRt.offsetMax = Vector2.zero;

            var topInfoObj = new GameObject("TopInfo");
            topInfoObj.transform.SetParent(topBar.transform, false);
            _topInfoText = topInfoObj.AddComponent<Text>();
            _topInfoText.font = font;
            _topInfoText.fontSize = 22;
            _topInfoText.alignment = TextAnchor.MiddleCenter;
            _topInfoText.color = Color.white;
            WankulUiKit.Stretch(topInfoObj.GetComponent<RectTransform>());

            // Bouton Abandonner / Quitter dans le bandeau supérieur
            var forfeitBtnObj = new GameObject("ForfeitBtn");
            forfeitBtnObj.transform.SetParent(topBar.transform, false);
            var forfeitRt = forfeitBtnObj.AddComponent<RectTransform>();
            forfeitRt.anchorMin = new Vector2(0.91f, 0.20f);
            forfeitRt.anchorMax = new Vector2(0.985f, 0.80f);
            forfeitRt.offsetMin = Vector2.zero;
            forfeitRt.offsetMax = Vector2.zero;

            var forfeitImg = forfeitBtnObj.AddComponent<Image>();
            forfeitImg.sprite = WankulUiKit.WhiteSprite;
            forfeitImg.color = new Color(0.45f, 0.14f, 0.16f, 0.92f);

            var forfeitOutline = forfeitBtnObj.AddComponent<Outline>();
            forfeitOutline.effectColor = new Color(0.85f, 0.35f, 0.35f, 0.8f);
            forfeitOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var forfeitBtn = forfeitBtnObj.AddComponent<Button>();
            forfeitBtn.onClick.AddListener(OnForfeitClicked);

            var forfeitTxtObj = new GameObject("Text");
            forfeitTxtObj.transform.SetParent(forfeitBtnObj.transform, false);
            var forfeitTxt = forfeitTxtObj.AddComponent<Text>();
            forfeitTxt.font = font;
            forfeitTxt.fontSize = 14;
            forfeitTxt.fontStyle = FontStyle.Bold;
            forfeitTxt.alignment = TextAnchor.MiddleCenter;
            forfeitTxt.color = Color.white;
            forfeitTxt.text = "Quitter";
            WankulUiKit.Stretch(forfeitTxtObj.GetComponent<RectTransform>());

            // --- 2. ZONE CENTRALE (3 TERRAINS) ---
            var boardArea = new GameObject("BoardArea");
            boardArea.transform.SetParent(_canvasObj.transform, false);
            var boardRt = boardArea.AddComponent<RectTransform>();
            boardRt.anchorMin = new Vector2(0.05f, 0.32f);
            boardRt.anchorMax = new Vector2(0.95f, 0.86f);
            boardRt.offsetMin = Vector2.zero;
            boardRt.offsetMax = Vector2.zero;

            for (int i = 0; i < 3; i++)
            {
                int slotIndex = i;
                var slotObj = new GameObject($"Slot_{i}");
                slotObj.transform.SetParent(boardArea.transform, false);
                var slotRt = slotObj.AddComponent<RectTransform>();
                float leftAnchor = i * 0.333f + 0.01f;
                float rightAnchor = (i + 1) * 0.333f - 0.01f;
                slotRt.anchorMin = new Vector2(leftAnchor, 0f);
                slotRt.anchorMax = new Vector2(rightAnchor, 1f);
                slotRt.offsetMin = Vector2.zero;
                slotRt.offsetMax = Vector2.zero;

                var slotBg = slotObj.AddComponent<Image>();
                slotBg.sprite = WankulUiKit.WhiteSprite;
                slotBg.color = new Color(0.16f, 0.19f, 0.25f, 0.9f);

                // Liseré vert / halo de surbrillance pour slot cliquable
                var slotOutline = slotObj.AddComponent<Outline>();
                slotOutline.effectColor = new Color(0.2f, 0.95f, 0.4f, 0.95f);
                slotOutline.effectDistance = new Vector2(4f, -4f);
                slotOutline.enabled = false;
                _slotOutlines[i] = slotOutline;

                var slotBtn = slotObj.AddComponent<Button>();
                slotBtn.onClick.AddListener(() => OnSlotClicked(slotIndex));
                slotBtn.interactable = false;
                _slotButtons[i] = slotBtn;

                // ==================== 1. ZONE HAUT : ADVERSAIRE ====================
                var p2Area = new GameObject("P2Area");
                p2Area.transform.SetParent(slotObj.transform, false);
                var p2Rt = p2Area.AddComponent<RectTransform>();
                p2Rt.anchorMin = new Vector2(0.02f, 0.69f);
                p2Rt.anchorMax = new Vector2(0.98f, 0.98f);
                p2Rt.offsetMin = Vector2.zero;
                p2Rt.offsetMax = Vector2.zero;

                var p2Bg = p2Area.AddComponent<Image>();
                p2Bg.sprite = WankulUiKit.WhiteSprite;
                p2Bg.color = new Color(0.25f, 0.12f, 0.15f, 0.75f);

                var p2Header = new GameObject("P2Header");
                p2Header.transform.SetParent(p2Area.transform, false);
                var p2HText = p2Header.AddComponent<Text>();
                p2HText.font = font;
                p2HText.fontSize = 13;
                p2HText.fontStyle = FontStyle.Bold;
                p2HText.alignment = TextAnchor.MiddleLeft;
                p2HText.color = new Color(1f, 0.55f, 0.55f);
                p2HText.text = "  ADVERSAIRE";
                var p2HRt = p2Header.GetComponent<RectTransform>();
                p2HRt.anchorMin = new Vector2(0f, 0.65f);
                p2HRt.anchorMax = new Vector2(0.6f, 1f);
                p2HRt.offsetMin = Vector2.zero;
                p2HRt.offsetMax = Vector2.zero;

                var p2ForceObj = new GameObject("P2Force");
                p2ForceObj.transform.SetParent(p2Area.transform, false);
                var p2FText = p2ForceObj.AddComponent<Text>();
                p2FText.font = font;
                p2FText.fontSize = 13;
                p2FText.fontStyle = FontStyle.Bold;
                p2FText.alignment = TextAnchor.MiddleRight;
                p2FText.color = new Color(1f, 0.4f, 0.4f);
                p2FText.text = "Force: 0  ";
                var p2FRt = p2ForceObj.GetComponent<RectTransform>();
                p2FRt.anchorMin = new Vector2(0.6f, 0.65f);
                p2FRt.anchorMax = new Vector2(1f, 1f);
                p2FRt.offsetMin = Vector2.zero;
                p2FRt.offsetMax = Vector2.zero;
                _slotP2ForceTexts[i] = p2FText;

                var p2CardsObj = new GameObject("P2Cards");
                p2CardsObj.transform.SetParent(p2Area.transform, false);
                var p2CRt = p2CardsObj.AddComponent<RectTransform>();
                p2CRt.anchorMin = new Vector2(0.04f, 0.05f);
                p2CRt.anchorMax = new Vector2(0.96f, 0.65f);
                p2CRt.offsetMin = Vector2.zero;
                p2CRt.offsetMax = Vector2.zero;
                var p2CText = p2CardsObj.AddComponent<Text>();
                p2CText.font = font;
                p2CText.fontSize = 12;
                p2CText.alignment = TextAnchor.UpperCenter;
                p2CText.color = new Color(0.9f, 0.85f, 0.85f);
                _slotP2CardsTexts[i] = p2CText;

                // ==================== 2. ZONE MILIEU : TERRAIN ====================
                var terrainArea = new GameObject("TerrainArea");
                terrainArea.transform.SetParent(slotObj.transform, false);
                var tAreaRt = terrainArea.AddComponent<RectTransform>();
                tAreaRt.anchorMin = new Vector2(0.02f, 0.33f);
                tAreaRt.anchorMax = new Vector2(0.98f, 0.67f);
                tAreaRt.offsetMin = Vector2.zero;
                tAreaRt.offsetMax = Vector2.zero;

                var tBg = terrainArea.AddComponent<Image>();
                tBg.sprite = WankulUiKit.WhiteSprite;
                tBg.color = new Color(0.08f, 0.10f, 0.14f, 0.65f);

                // Image d'illustration du Terrain posé (centrée, peut pivoter à 90° si inactif)
                var terrainImgObj = new GameObject("TerrainImage");
                terrainImgObj.transform.SetParent(terrainArea.transform, false);
                var tImgRt = terrainImgObj.AddComponent<RectTransform>();
                tImgRt.anchorMin = new Vector2(0.05f, 0.22f);
                tImgRt.anchorMax = new Vector2(0.95f, 0.78f);
                tImgRt.pivot = new Vector2(0.5f, 0.5f);
                tImgRt.offsetMin = Vector2.zero;
                tImgRt.offsetMax = Vector2.zero;
                var tImg = terrainImgObj.AddComponent<Image>();
                tImg.preserveAspect = true;
                tImg.color = new Color(1f, 1f, 1f, 0f);
                _slotTerrainImages[i] = tImg;

                // Bandeau Titre Terrain en haut de la zone (fond sombre semi-transparent pour lisibilité parfaite)
                var titlePanel = new GameObject("TitlePanel");
                titlePanel.transform.SetParent(terrainArea.transform, false);
                var tPRt = titlePanel.AddComponent<RectTransform>();
                tPRt.anchorMin = new Vector2(0.04f, 0.79f);
                tPRt.anchorMax = new Vector2(0.96f, 0.98f);
                tPRt.offsetMin = Vector2.zero;
                tPRt.offsetMax = Vector2.zero;
                var tPBg = titlePanel.AddComponent<Image>();
                tPBg.sprite = WankulUiKit.WhiteSprite;
                tPBg.color = new Color(0.04f, 0.06f, 0.09f, 0.88f);
                var tPOutline = titlePanel.AddComponent<Outline>();
                tPOutline.effectColor = new Color(1f, 0.85f, 0.3f, 0.35f);
                tPOutline.effectDistance = new Vector2(1f, -1f);

                var titleObj = new GameObject("Title");
                titleObj.transform.SetParent(titlePanel.transform, false);
                var tText = titleObj.AddComponent<Text>();
                tText.font = font;
                tText.fontSize = 13;
                tText.fontStyle = FontStyle.Bold;
                tText.alignment = TextAnchor.MiddleCenter;
                tText.color = new Color(1f, 0.88f, 0.35f);
                WankulUiKit.Stretch(titleObj.GetComponent<RectTransform>());
                _slotTerrainTexts[i] = tText;

                // Pastille Statut Terrain en bas de la zone (fond sombre semi-transparent)
                var statusPanel = new GameObject("StatusPanel");
                statusPanel.transform.SetParent(terrainArea.transform, false);
                var stPRt = statusPanel.AddComponent<RectTransform>();
                stPRt.anchorMin = new Vector2(0.08f, 0.02f);
                stPRt.anchorMax = new Vector2(0.92f, 0.20f);
                stPRt.offsetMin = Vector2.zero;
                stPRt.offsetMax = Vector2.zero;
                var stPBg = statusPanel.AddComponent<Image>();
                stPBg.sprite = WankulUiKit.WhiteSprite;
                stPBg.color = new Color(0.04f, 0.06f, 0.09f, 0.88f);
                var stPOutline = statusPanel.AddComponent<Outline>();
                stPOutline.effectColor = new Color(0.3f, 0.5f, 0.8f, 0.35f);
                stPOutline.effectDistance = new Vector2(1f, -1f);

                var statusObj = new GameObject("Status");
                statusObj.transform.SetParent(statusPanel.transform, false);
                var stText = statusObj.AddComponent<Text>();
                stText.font = font;
                stText.fontSize = 12;
                stText.alignment = TextAnchor.MiddleCenter;
                WankulUiKit.Stretch(statusObj.GetComponent<RectTransform>());
                _slotStatusTexts[i] = stText;

                // ==================== 3. ZONE BAS : VOS PERSONNAGES ====================
                var p1Area = new GameObject("P1Area");
                p1Area.transform.SetParent(slotObj.transform, false);
                var p1Rt = p1Area.AddComponent<RectTransform>();
                p1Rt.anchorMin = new Vector2(0.02f, 0.02f);
                p1Rt.anchorMax = new Vector2(0.98f, 0.31f);
                p1Rt.offsetMin = Vector2.zero;
                p1Rt.offsetMax = Vector2.zero;

                var p1Bg = p1Area.AddComponent<Image>();
                p1Bg.sprite = WankulUiKit.WhiteSprite;
                p1Bg.color = new Color(0.12f, 0.18f, 0.28f, 0.75f);

                var p1Header = new GameObject("P1Header");
                p1Header.transform.SetParent(p1Area.transform, false);
                var p1HText = p1Header.AddComponent<Text>();
                p1HText.font = font;
                p1HText.fontSize = 13;
                p1HText.fontStyle = FontStyle.Bold;
                p1HText.alignment = TextAnchor.MiddleLeft;
                p1HText.color = new Color(0.55f, 0.85f, 1f);
                p1HText.text = "  VOS PERSOS";
                var p1HRt = p1Header.GetComponent<RectTransform>();
                p1HRt.anchorMin = new Vector2(0f, 0.65f);
                p1HRt.anchorMax = new Vector2(0.6f, 1f);
                p1HRt.offsetMin = Vector2.zero;
                p1HRt.offsetMax = Vector2.zero;

                var p1ForceObj = new GameObject("P1Force");
                p1ForceObj.transform.SetParent(p1Area.transform, false);
                var p1FText = p1ForceObj.AddComponent<Text>();
                p1FText.font = font;
                p1FText.fontSize = 13;
                p1FText.fontStyle = FontStyle.Bold;
                p1FText.alignment = TextAnchor.MiddleRight;
                p1FText.color = new Color(0.4f, 0.85f, 1f);
                p1FText.text = "Force: 0  ";
                var p1FRt = p1ForceObj.GetComponent<RectTransform>();
                p1FRt.anchorMin = new Vector2(0.6f, 0.65f);
                p1FRt.anchorMax = new Vector2(1f, 1f);
                p1FRt.offsetMin = Vector2.zero;
                p1FRt.offsetMax = Vector2.zero;
                _slotP1ForceTexts[i] = p1FText;

                var p1CardsObj = new GameObject("P1Cards");
                p1CardsObj.transform.SetParent(p1Area.transform, false);
                var p1CRt = p1CardsObj.AddComponent<RectTransform>();
                p1CRt.anchorMin = new Vector2(0.04f, 0.05f);
                p1CRt.anchorMax = new Vector2(0.96f, 0.65f);
                p1CRt.offsetMin = Vector2.zero;
                p1CRt.offsetMax = Vector2.zero;
                var p1CText = p1CardsObj.AddComponent<Text>();
                p1CText.font = font;
                p1CText.fontSize = 12;
                p1CText.alignment = TextAnchor.UpperCenter;
                p1CText.color = new Color(0.85f, 0.9f, 0.95f);
                _slotP1CardsTexts[i] = p1CText;
            }

            // --- 3. ZONE INFÉRIEURE (Main du Joueur & Commandes) ---
            var bottomArea = new GameObject("BottomArea");
            bottomArea.transform.SetParent(_canvasObj.transform, false);
            var bottomRt = bottomArea.AddComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0.02f, 0.02f);
            bottomRt.anchorMax = new Vector2(0.98f, 0.30f);
            bottomRt.offsetMin = Vector2.zero;
            bottomRt.offsetMax = Vector2.zero;

            var handScroll = new GameObject("HandContainer");
            handScroll.transform.SetParent(bottomArea.transform, false);
            var handRt = handScroll.AddComponent<RectTransform>();
            handRt.anchorMin = new Vector2(0f, 0f);
            handRt.anchorMax = new Vector2(0.82f, 1f);
            handRt.offsetMin = Vector2.zero;
            handRt.offsetMax = Vector2.zero;
            var hLayout = handScroll.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 10;
            hLayout.padding = new RectOffset(10, 10, 10, 10);
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandHeight = false;
            hLayout.childForceExpandWidth = false;
            _handContainer = handScroll.transform;

            // Bouton Fin de tour stylisé
            var btnObj = new GameObject("EndTurnBtn");
            btnObj.transform.SetParent(bottomArea.transform, false);
            var btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.84f, 0.20f);
            btnRt.anchorMax = new Vector2(0.985f, 0.80f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;

            var btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = WankulUiKit.WhiteSprite;
            btnImg.color = new Color(0.70f, 0.18f, 0.22f, 0.95f);

            var btnOutline = btnObj.AddComponent<Outline>();
            btnOutline.effectColor = new Color(1f, 0.82f, 0.3f, 1f);
            btnOutline.effectDistance = new Vector2(2.5f, -2.5f);

            _endTurnButton = btnObj.AddComponent<Button>();
            _endTurnButton.onClick.AddListener(OnEndTurnClicked);

            var btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            _endTurnButtonText = btnTxtObj.AddComponent<Text>();
            _endTurnButtonText.font = font;
            _endTurnButtonText.fontSize = 20;
            _endTurnButtonText.fontStyle = FontStyle.Bold;
            _endTurnButtonText.alignment = TextAnchor.MiddleCenter;
            _endTurnButtonText.color = Color.white;
            _endTurnButtonText.text = "FIN DU TOUR\n<size=13><color=#ffe080>► Passer</color></size>";
            WankulUiKit.Stretch(btnTxtObj.GetComponent<RectTransform>());

            // Message de statut
            var msgObj = new GameObject("StatusMessage");
            msgObj.transform.SetParent(_canvasObj.transform, false);
            var msgRt = msgObj.AddComponent<RectTransform>();
            msgRt.anchorMin = new Vector2(0.2f, 0.84f);
            msgRt.anchorMax = new Vector2(0.8f, 0.88f);
            msgRt.offsetMin = Vector2.zero;
            msgRt.offsetMax = Vector2.zero;
            _statusMessageText = msgObj.AddComponent<Text>();
            _statusMessageText.font = font;
            _statusMessageText.fontSize = 18;
            _statusMessageText.alignment = TextAnchor.MiddleCenter;
            _statusMessageText.color = new Color(1f, 0.7f, 0.2f);
        }

        private void RefreshView()
        {
            if (_engine == null) return;

            // 1. Bandeau supérieur
            string activePlayerName = _engine.State.ActivePlayer == PlayerId.Player1 ? "Votre Tour" : "Tour de l'Adversaire";
            _topInfoText.text = $"Tour {_engine.State.TurnNumber}  |  {activePlayerName}  |  Score : {_engine.State.ScoreP1} - {_engine.State.ScoreP2} (Victoire à 5)  |  Persos joués : {_engine.State.CharactersPlayedThisTurn}/4";

            // Statut alerte ou invite d'action
            if (_selectedHandCard != null)
            {
                string actionHint = _selectedHandCard.Kind == CardKind.Terrain
                    ? "👉 Cliquez sur un slot libre en surbrillance verte pour y poser le Terrain (ou recliquez pour annuler)"
                    : $"👉 Cliquez sur un Terrain en surbrillance verte pour y poser [{_selectedHandCard.Name}] (ou recliquez pour annuler)";
                _statusMessageText.text = actionHint;
            }
            else if (_engine.State.TerrainRequirementPending)
            {
                if (_engine.State.ActivePlayer == PlayerId.Player1)
                    _statusMessageText.text = "⚠ OBLIGATION : Vous devez poser un Terrain sur un slot libre !";
                else
                    _statusMessageText.text = "L'adversaire pose un Terrain...";
            }
            else if (_engine.State.ActivePlayer == PlayerId.Player2)
            {
                _statusMessageText.text = "Tour de l'adversaire en cours...";
            }
            else
            {
                _statusMessageText.text = "Cliquez sur une carte en main pour la sélectionner.";
            }

            // 2. Terrains & Éligibilité des slots
            for (int i = 0; i < 3; i++)
            {
                var slot = _engine.State.Slots[i];

                // Calcul de l'éligibilité pour un clic direct
                bool isEligible = false;
                if (_selectedHandCard != null && _engine.State.ActivePlayer == PlayerId.Player1 && !_engine.State.IsGameOver)
                {
                    if (_selectedHandCard.Kind == CardKind.Terrain)
                    {
                        isEligible = slot.IsEmpty;
                    }
                    else
                    {
                        isEligible = !slot.IsEmpty && _engine.State.CharactersPlayedThisTurn < 4;
                    }
                }

                if (_slotOutlines[i] != null)
                {
                    _slotOutlines[i].enabled = isEligible;
                }
                if (_slotButtons[i] != null)
                {
                    _slotButtons[i].interactable = isEligible;
                }

                int forceP1 = slot.IsEmpty ? 0 : slot.GetForce(PlayerId.Player1);
                int forceP2 = slot.IsEmpty ? 0 : slot.GetForce(PlayerId.Player2);

                if (_slotP1ForceTexts[i] != null)
                {
                    string colorTag = forceP1 > forceP2 ? "#55FF88" : (forceP1 < forceP2 ? "#AAAAAA" : "#FFFFFF");
                    _slotP1ForceTexts[i].text = $"<color={colorTag}>Force: {forceP1}</color>  ";
                }
                if (_slotP2ForceTexts[i] != null)
                {
                    string colorTag = forceP2 > forceP1 ? "#FF6666" : (forceP2 < forceP1 ? "#AAAAAA" : "#FFFFFF");
                    _slotP2ForceTexts[i].text = $"<color={colorTag}>Force: {forceP2}</color>  ";
                }

                if (_slotP1CardsTexts[i] != null)
                {
                    _slotP1CardsTexts[i].text = FormatCharactersList(slot.CharactersP1);
                }
                if (_slotP2CardsTexts[i] != null)
                {
                    _slotP2CardsTexts[i].text = FormatCharactersList(slot.CharactersP2);
                }

                if (slot.IsEmpty)
                {
                    if (_slotTerrainImages[i] != null)
                    {
                        _slotTerrainImages[i].color = new Color(1f, 1f, 1f, 0f);
                        _slotTerrainImages[i].transform.localEulerAngles = Vector3.zero;
                    }

                    _slotTerrainTexts[i].text = $"[ SLOT {i + 1} LIBRE ]";
                    _slotStatusTexts[i].text = isEligible ? "<color=#55FF55>Cliquez pour poser ici</color>" : "<color=#777777>(Aucun terrain)</color>";
                }
                else
                {
                    bool isActive = slot.IsActive(_engine.State.TurnNumber);

                    if (_slotTerrainImages[i] != null)
                    {
                        var tSprite = CardAdapter.GetCardSprite(slot.Card);
                        if (tSprite != null)
                        {
                            _slotTerrainImages[i].sprite = tSprite;
                            _slotTerrainImages[i].color = isActive ? new Color(1f, 1f, 1f, 0.75f) : new Color(0.65f, 0.65f, 0.65f, 0.5f);
                            // Inclinaison à 90° si inactif (incliné ce tour) / redressé à 0° si actif !
                            _slotTerrainImages[i].transform.localEulerAngles = isActive ? Vector3.zero : new Vector3(0f, 0f, 90f);
                        }
                        else
                        {
                            _slotTerrainImages[i].color = new Color(1f, 1f, 1f, 0f);
                            _slotTerrainImages[i].transform.localEulerAngles = Vector3.zero;
                        }
                    }

                    _slotTerrainTexts[i].text = CardAdapter.FixMojibake(slot.Card!.Name);
                    _slotStatusTexts[i].text = isActive ? "<color=#55FF55>● ACTIF (Scorable)</color>" : "<color=#FFAA00>◐ INCLINÉ (Inactif)</color>";
                }
            }

            // 3. Main du Joueur (Player 1)
            foreach (Transform child in _handContainer)
            {
                Destroy(child.gameObject);
            }

            var hand = _engine.State.GetHand(PlayerId.Player1);
            var font = WankulUiKit.GetFont();

            Plugin.Logger.LogInfo($"[DuelView2D] RefreshView - Main Joueur (P1) : {hand.Count} cartes en main. ActivePlayer={_engine.State.ActivePlayer}, Tour={_engine.State.TurnNumber}");

            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                var cardBtnObj = new GameObject($"CardBtn_{card.Id}");
                cardBtnObj.transform.SetParent(_handContainer, false);
                var cRt = cardBtnObj.AddComponent<RectTransform>();
                cRt.sizeDelta = new Vector2(150, 205);

                var le = cardBtnObj.AddComponent<LayoutElement>();
                le.minWidth = 150f;
                le.minHeight = 205f;
                le.preferredWidth = 150f;
                le.preferredHeight = 205f;
                le.flexibleWidth = 0f;
                le.flexibleHeight = 0f;

                var img = cardBtnObj.AddComponent<Image>();
                var btn = cardBtnObj.AddComponent<Button>();
                btn.interactable = _engine.State.ActivePlayer == PlayerId.Player1 && !_engine.State.IsGameOver;
                btn.onClick.AddListener(() => OnCardInHandClicked(card));

                // Effet visuel si la carte est sélectionnée (liseré doré + surélévation)
                bool isSelected = _selectedHandCard != null && _selectedHandCard.Id == card.Id;
                if (isSelected)
                {
                    var cardOutline = cardBtnObj.AddComponent<Outline>();
                    cardOutline.effectColor = new Color(1f, 0.85f, 0.2f, 1f);
                    cardOutline.effectDistance = new Vector2(3.5f, -3.5f);
                    cRt.anchoredPosition = new Vector2(0f, 15f);
                }

                Sprite cardSprite = CardAdapter.GetCardSprite(card);
                if (cardSprite != null)
                {
                    img.sprite = cardSprite;
                    img.color = Color.white;
                    img.preserveAspect = true;

                    // Badge de type/force discret au bas de la carte
                    var badgeObj = new GameObject("CardBadge");
                    badgeObj.transform.SetParent(cardBtnObj.transform, false);
                    var bRt = badgeObj.AddComponent<RectTransform>();
                    bRt.anchorMin = new Vector2(0.04f, 0.04f);
                    bRt.anchorMax = new Vector2(0.96f, 0.20f);
                    bRt.offsetMin = Vector2.zero;
                    bRt.offsetMax = Vector2.zero;

                    var bImg = badgeObj.AddComponent<Image>();
                    bImg.sprite = WankulUiKit.WhiteSprite;
                    bImg.color = card.Kind == CardKind.Terrain
                        ? new Color(0.1f, 0.4f, 0.15f, 0.85f)
                        : (card.IsScoreur ? new Color(0.6f, 0.15f, 0.35f, 0.85f) : new Color(0.12f, 0.18f, 0.3f, 0.85f));

                    var bTxtObj = new GameObject("Text");
                    bTxtObj.transform.SetParent(badgeObj.transform, false);
                    var bTxt = bTxtObj.AddComponent<Text>();
                    bTxt.font = font;
                    bTxt.fontSize = 11;
                    bTxt.fontStyle = FontStyle.Bold;
                    bTxt.alignment = TextAnchor.MiddleCenter;
                    bTxt.color = Color.white;

                    string label = card.Kind == CardKind.Terrain
                        ? "TERRAIN"
                        : (card.IsScoreur ? "SCOREUR" : $"FORCE : {card.Force}");
                    bTxt.text = label;
                    WankulUiKit.Stretch(bTxtObj.GetComponent<RectTransform>());
                }
                else
                {
                    img.sprite = WankulUiKit.WhiteSprite;
                    img.color = card.Kind == CardKind.Terrain
                        ? new Color(0.2f, 0.45f, 0.25f)
                        : (card.IsScoreur ? new Color(0.6f, 0.2f, 0.4f) : new Color(0.25f, 0.35f, 0.55f));

                    var txtObj = new GameObject("Text");
                    txtObj.transform.SetParent(cardBtnObj.transform, false);
                    var t = txtObj.AddComponent<Text>();
                    t.font = font;
                    t.fontSize = 14;
                    t.alignment = TextAnchor.MiddleCenter;
                    t.color = Color.white;

                    string typeTag = card.Kind == CardKind.Terrain ? "[TERRAIN]" : (card.IsScoreur ? "[SCOREUR]" : "[PERSO]");
                    string forceTag = card.Kind == CardKind.Character ? $"\nForce: {card.Force}" : "";
                    t.text = $"{typeTag}\n\n{card.Name}{forceTag}";
                    WankulUiKit.Stretch(txtObj.GetComponent<RectTransform>());
                }

                Plugin.Logger.LogInfo($"[DuelView2D] Affichage carte [{i}] : {card.Name} (Sprite: {(cardSprite != null ? "OUI" : "NON")})");
            }

            // Bouton Fin de tour activable seulement pendant notre tour et sans obligation en suspens
            bool canEndTurn = _engine.State.ActivePlayer == PlayerId.Player1 && !_engine.State.TerrainRequirementPending;
            _endTurnButton.interactable = canEndTurn;
        }

        private static string FormatCharactersList(IReadOnlyList<DuelCard> cards)
        {
            if (cards == null || cards.Count == 0) return "<color=#777777>(Aucun personnage)</color>";
            var list = new List<string>();
            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                string cleanName = CardAdapter.FixMojibake(c.Name);
                string star = c.IsScoreur ? " ★" : "";
                list.Add($"• <b>{cleanName}</b> ({c.Force}{star})");
            }
            return string.Join("\n", list);
        }

        private void OnCardInHandClicked(DuelCard card)
        {
            if (_engine.State.ActivePlayer != PlayerId.Player1 || _engine.State.IsGameOver)
                return;

            if (_selectedHandCard != null && _selectedHandCard.Id == card.Id)
            {
                // Reclic sur la même carte : désélection
                _selectedHandCard = null;
                DuelSfx.PlayCancel();
            }
            else
            {
                _selectedHandCard = card;
                DuelSfx.PlaySelectCard();
            }
            RefreshView();
        }

        private void OnSlotClicked(int slotIndex)
        {
            if (_engine.State.ActivePlayer != PlayerId.Player1 || _engine.State.IsGameOver)
                return;

            if (_selectedHandCard == null)
                return;

            var cardToPlay = _selectedHandCard;
            _selectedHandCard = null;
            ExecutePlayCard(cardToPlay, slotIndex);
        }

        private void ExecutePlayCard(DuelCard card, int slotIndex)
        {
            DuelActionResult result;
            if (card.Kind == CardKind.Terrain)
            {
                result = _engine.PlayTerrain(PlayerId.Player1, card.Id, slotIndex);
            }
            else
            {
                result = _engine.PlayCharacter(PlayerId.Player1, card.Id, slotIndex);
            }

            if (!result.Success)
            {
                _statusMessageText.text = $"Action refusée : {result.ErrorReason}";
            }
            RefreshView();
        }

        private void OnEndTurnClicked()
        {
            if (_engine.State.ActivePlayer != PlayerId.Player1 || _engine.State.IsGameOver)
                return;

            DuelSfx.PlayButtonClick();

            var result = _engine.EndTurn(PlayerId.Player1);
            if (!result.Success)
            {
                _statusMessageText.text = result.ErrorReason ?? "Action refusée";
                return;
            }

            RefreshView();

            // Lancer le tour de l'IA après un petit délai
            StartCoroutine(ExecuteAITurnWithDelay());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_selectedHandCard != null)
                {
                    _selectedHandCard = null;
                    DuelSfx.PlayCancel();
                    RefreshView();
                }
                else
                {
                    OnForfeitClicked();
                }
            }
        }

        private void OnForfeitClicked()
        {
            DuelSfx.PlayCancel();
            Destroy(gameObject);
            if (_playTable != null)
            {
                _playTable.ReportWinner(isPlayerWin: false, isDraw: false);
                _playTable.FinishLeaveGame(isPlayerWin: false);
            }
        }

        private System.Collections.IEnumerator ExecuteInitialAITurn()
        {
            yield return new WaitForSeconds(1.0f);

            if (_engine.State.IsGameOver) yield break;

            // L'IA joue son Tour 1 (qui appelle _engine.EndTurn(Player2))
            _ai.PlayTurn(_engine);
            RefreshView();

            if (!_engine.State.IsGameOver)
            {
                yield return new WaitForSeconds(0.8f);
                // Début du Tour 2 pour le joueur
                _engine.StartTurn();
                RefreshView();
            }
        }

        private System.Collections.IEnumerator ExecuteAITurnWithDelay()
        {
            yield return new WaitForSeconds(0.6f);

            if (_engine.State.IsGameOver) yield break;

            // Début du tour de l'adversaire (P2)
            _engine.StartTurn();
            RefreshView();

            yield return new WaitForSeconds(0.8f);

            if (_engine.State.IsGameOver) yield break;

            // L'IA joue son tour
            _ai.PlayTurn(_engine);
            RefreshView();

            if (!_engine.State.IsGameOver)
            {
                yield return new WaitForSeconds(0.8f);
                // Début du prochain tour du joueur
                _engine.StartTurn();
                RefreshView();
            }
        }

        private void HandleDuelEvent(IDuelEvent evt)
        {
            switch (evt)
            {
                case CardsDrawnEvent:
                    DuelSfx.PlayDraw();
                    break;
                case TerrainPlayedEvent:
                case CharacterPlayedEvent:
                    DuelSfx.PlayPlaceCard();
                    break;
                case TurnStartedEvent turn:
                    DuelSfx.PlayTurnStart(turn.ActivePlayer == PlayerId.Player1);
                    break;
                case ScoreResolvedEvent score:
                    bool? isPlayerWinner = score.Winner == null ? (bool?)null : (score.Winner == PlayerId.Player1);
                    DuelSfx.PlayScoreResolved(isPlayerWinner);
                    break;
                case TerrainClearedEvent:
                    DuelSfx.PlayTerrainCleared();
                    break;
                case DuelEndedEvent ended:
                    DuelSfx.PlayGameOver(ended.Winner == PlayerId.Player1);
                    ShowGameOverModal(ended.Winner, ended.Reason);
                    break;
            }
        }

        private void ShowGameOverModal(PlayerId winner, GameOverReason reason)
        {
            bool isPlayerWin = winner == PlayerId.Player1;
            string reasonStr = reason == GameOverReason.FiveTerrains ? "5 Terrains remportés !" : "Deck adverse épuisé (Meule) !";

            var modal = new GameObject("GameOverModal");
            modal.transform.SetParent(_canvasObj.transform, false);
            var mRt = modal.AddComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.25f, 0.3f);
            mRt.anchorMax = new Vector2(0.75f, 0.7f);
            mRt.offsetMin = Vector2.zero;
            mRt.offsetMax = Vector2.zero;

            var bg = modal.AddComponent<Image>();
            bg.sprite = WankulUiKit.WhiteSprite;
            bg.color = isPlayerWin ? new Color(0.1f, 0.35f, 0.15f, 0.98f) : new Color(0.4f, 0.1f, 0.1f, 0.98f);

            var font = WankulUiKit.GetFont();

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(modal.transform, false);
            var t = textObj.AddComponent<Text>();
            t.font = font;
            t.fontSize = 28;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = $"{(isPlayerWin ? "VICTOIRE !" : "DÉFAITE...")}\n\n{reasonStr}";
            var tRt = textObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.05f, 0.35f);
            tRt.anchorMax = new Vector2(0.95f, 0.95f);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;

            var quitBtnObj = new GameObject("QuitBtn");
            quitBtnObj.transform.SetParent(modal.transform, false);
            var qRt = quitBtnObj.AddComponent<RectTransform>();
            qRt.anchorMin = new Vector2(0.35f, 0.1f);
            qRt.anchorMax = new Vector2(0.65f, 0.3f);
            qRt.offsetMin = Vector2.zero;
            qRt.offsetMax = Vector2.zero;

            var qImg = quitBtnObj.AddComponent<Image>();
            qImg.sprite = WankulUiKit.WhiteSprite;
            qImg.color = Color.white;

            var qBtn = quitBtnObj.AddComponent<Button>();
            qBtn.onClick.AddListener(() =>
            {
                Destroy(gameObject);
                if (_playTable != null)
                {
                    _playTable.ReportWinner(isPlayerWin, isDraw: false);
                    _playTable.FinishLeaveGame(isPlayerWin);
                }
            });

            var qTxtObj = new GameObject("Text");
            qTxtObj.transform.SetParent(quitBtnObj.transform, false);
            var qTxt = qTxtObj.AddComponent<Text>();
            qTxt.font = font;
            qTxt.fontSize = 20;
            qTxt.fontStyle = FontStyle.Bold;
            qTxt.alignment = TextAnchor.MiddleCenter;
            qTxt.color = Color.black;
            qTxt.text = "Quitter";
            WankulUiKit.Stretch(qTxtObj.GetComponent<RectTransform>());
        }
    }
}
