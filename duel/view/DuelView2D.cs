using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WankulCrazy.Duel.Engine;
using WankulCrazyPlugin.cards;
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

        // Bandeau supérieur (TopBar)
        private Text _topTurnBadgeText;
        private Image _topActivePlayerBg;
        private Text _topActivePlayerText;
        private Text _topScoreP1Text;
        private Text _topScoreP1Pips;
        private Text _topScoreP2Text;
        private Text _topScoreP2Pips;

        // Bandeau Message / Guide d'action
        private Text _statusMessageText;

        // Slots Terrains
        private Text[] _slotTerrainTexts = new Text[3];
        private Image[] _slotTerrainImages = new Image[3];
        private Text[] _slotStatusTexts = new Text[3];
        private Text[] _slotP1ForceTexts = new Text[3];
        private Text[] _slotP2ForceTexts = new Text[3];
        private Transform[] _slotP1CardsContainers = new Transform[3];
        private Transform[] _slotP2CardsContainers = new Transform[3];

        // Dock gauche (Quotas et stats pioche/défausse)
        private Text _dockQuotaPipsText;
        private Text _dockQuotaCountText;
        private Text _dockDeckCountText;
        private Text _dockDiscardCountText;

        // Main & Commandes
        private Transform _handContainer;
        private Button _endTurnButton;
        private Image _endTurnButtonBg;
        private Outline _endTurnButtonOutline;
        private Text _endTurnButtonText;

        // Sélection de slot pour jouer une carte
        private DuelCard _selectedHandCard;
        private Button[] _slotButtons = new Button[3];
        private Outline[] _slotOutlines = new Outline[3];

        // Volet d'inspection HD (Clic Droit)
        private GameObject _inspectorPanel;
        private Image _inspectorCardImage;
        private Text _inspectorTitleText;
        private Text _inspectorTypeText;
        private Text _inspectorForceText;
        private Text _inspectorDescText;
        private DuelCard _hoveredCard;
        private HorizontalLayoutGroup _handLayoutGroup;

        // Barre de confrontation / Clash (ScoreClashBar)
        private GameObject _clashBarPanel;
        private Text _clashTitleText;
        private RectTransform _clashP1Fill;
        private RectTransform _clashP2Fill;
        private Text _clashP1ForceText;
        private Text _clashP2ForceText;
        private Text _clashDominanceText;
        private RectTransform _clashCursor;
        private Coroutine _clashAnimationRoutine;

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

            // Fond sombre
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(_canvasObj.transform, false);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = WankulUiKit.WhiteSprite;
            bgImg.color = new Color(0.06f, 0.08f, 0.11f, 0.96f);
            WankulUiKit.Stretch(bgObj.GetComponent<RectTransform>());

            // --- 1. BANDEAU SUPÉRIEUR TACTIQUE (TopBar) ---
            var topBar = new GameObject("TopBar");
            topBar.transform.SetParent(_canvasObj.transform, false);
            var topBarImg = topBar.AddComponent<Image>();
            topBarImg.sprite = WankulUiKit.WhiteSprite;
            topBarImg.color = new Color(0.10f, 0.12f, 0.17f, 1f);
            var topBarRt = topBar.GetComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0f, 0.905f);
            topBarRt.anchorMax = new Vector2(1f, 1f);
            topBarRt.offsetMin = Vector2.zero;
            topBarRt.offsetMax = Vector2.zero;

            var topBarOutline = topBar.AddComponent<Outline>();
            topBarOutline.effectColor = new Color(0.18f, 0.24f, 0.35f, 0.8f);
            topBarOutline.effectDistance = new Vector2(0f, -2f);

            // 1.1 Badge Marque "WANKUL TCG"
            var brandObj = new GameObject("BrandBadge");
            brandObj.transform.SetParent(topBar.transform, false);
            var brandRt = brandObj.AddComponent<RectTransform>();
            brandRt.anchorMin = new Vector2(0.015f, 0.18f);
            brandRt.anchorMax = new Vector2(0.105f, 0.82f);
            brandRt.offsetMin = Vector2.zero;
            brandRt.offsetMax = Vector2.zero;
            var brandBg = brandObj.AddComponent<Image>();
            brandBg.sprite = WankulUiKit.WhiteSprite;
            brandBg.color = new Color(0.14f, 0.18f, 0.26f, 1f);
            var brandOutline = brandObj.AddComponent<Outline>();
            brandOutline.effectColor = new Color(1f, 0.88f, 0.4f, 0.6f);
            brandOutline.effectDistance = new Vector2(1.5f, -1.5f);
            var brandTxt = new GameObject("Text").AddComponent<Text>();
            brandTxt.transform.SetParent(brandObj.transform, false);
            brandTxt.font = font;
            brandTxt.fontSize = 13;
            brandTxt.fontStyle = FontStyle.Bold;
            brandTxt.alignment = TextAnchor.MiddleCenter;
            brandTxt.color = new Color(1f, 0.88f, 0.4f);
            brandTxt.text = "WANKUL TCG";
            WankulUiKit.Stretch(brandTxt.GetComponent<RectTransform>());

            // 1.2 Numéro de tour
            var turnObj = new GameObject("TurnBadge");
            turnObj.transform.SetParent(topBar.transform, false);
            var turnRt = turnObj.AddComponent<RectTransform>();
            turnRt.anchorMin = new Vector2(0.112f, 0.18f);
            turnRt.anchorMax = new Vector2(0.175f, 0.82f);
            turnRt.offsetMin = Vector2.zero;
            turnRt.offsetMax = Vector2.zero;
            var turnBg = turnObj.AddComponent<Image>();
            turnBg.sprite = WankulUiKit.WhiteSprite;
            turnBg.color = new Color(0.13f, 0.16f, 0.22f, 1f);
            var turnOutline = turnObj.AddComponent<Outline>();
            turnOutline.effectColor = new Color(0.25f, 0.32f, 0.45f, 0.6f);
            turnOutline.effectDistance = new Vector2(1f, -1f);
            _topTurnBadgeText = new GameObject("Text").AddComponent<Text>();
            _topTurnBadgeText.transform.SetParent(turnObj.transform, false);
            _topTurnBadgeText.font = font;
            _topTurnBadgeText.fontSize = 12;
            _topTurnBadgeText.fontStyle = FontStyle.Bold;
            _topTurnBadgeText.alignment = TextAnchor.MiddleCenter;
            _topTurnBadgeText.color = new Color(0.85f, 0.90f, 0.98f);
            _topTurnBadgeText.text = "TOUR 1";
            WankulUiKit.Stretch(_topTurnBadgeText.GetComponent<RectTransform>());

            // 1.3 Statut Joueur Actif (Votre tour / Tour adverse)
            var activeObj = new GameObject("ActivePlayerBadge");
            activeObj.transform.SetParent(topBar.transform, false);
            var activeRt = activeObj.AddComponent<RectTransform>();
            activeRt.anchorMin = new Vector2(0.182f, 0.18f);
            activeRt.anchorMax = new Vector2(0.315f, 0.82f);
            activeRt.offsetMin = Vector2.zero;
            activeRt.offsetMax = Vector2.zero;
            _topActivePlayerBg = activeObj.AddComponent<Image>();
            _topActivePlayerBg.sprite = WankulUiKit.WhiteSprite;
            _topActivePlayerBg.color = new Color(0.08f, 0.24f, 0.35f, 0.95f);
            var activeOutline = activeObj.AddComponent<Outline>();
            activeOutline.effectColor = new Color(0.33f, 0.77f, 1f, 0.8f);
            activeOutline.effectDistance = new Vector2(1.5f, -1.5f);
            _topActivePlayerText = new GameObject("Text").AddComponent<Text>();
            _topActivePlayerText.transform.SetParent(activeObj.transform, false);
            _topActivePlayerText.font = font;
            _topActivePlayerText.fontSize = 13;
            _topActivePlayerText.fontStyle = FontStyle.Bold;
            _topActivePlayerText.alignment = TextAnchor.MiddleCenter;
            _topActivePlayerText.color = new Color(0.4f, 0.95f, 1f);
            _topActivePlayerText.text = "● VOTRE TOUR";
            WankulUiKit.Stretch(_topActivePlayerText.GetComponent<RectTransform>());

            // 1.4 Tableau des scores central (P1 Diamonds | VS | P2 Diamonds)
            var scoreBoard = new GameObject("ScoreBoard");
            scoreBoard.transform.SetParent(topBar.transform, false);
            var sbRt = scoreBoard.AddComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0.33f, 0.05f);
            sbRt.anchorMax = new Vector2(0.80f, 0.95f);
            sbRt.offsetMin = Vector2.zero;
            sbRt.offsetMax = Vector2.zero;

            // Score P1 (Gauche)
            var p1ScoreObj = new GameObject("P1Score");
            p1ScoreObj.transform.SetParent(scoreBoard.transform, false);
            var p1SRt = p1ScoreObj.AddComponent<RectTransform>();
            p1SRt.anchorMin = new Vector2(0f, 0.45f);
            p1SRt.anchorMax = new Vector2(0.38f, 1f);
            p1SRt.offsetMin = Vector2.zero;
            p1SRt.offsetMax = Vector2.zero;
            _topScoreP1Text = p1ScoreObj.AddComponent<Text>();
            _topScoreP1Text.font = font;
            _topScoreP1Text.fontSize = 15;
            _topScoreP1Text.alignment = TextAnchor.MiddleRight;
            _topScoreP1Text.color = Color.white;
            _topScoreP1Text.text = "<color=#55C5FF><b>JOUEUR</b></color>  <size=20><b>0</b></size>";

            var p1PipsObj = new GameObject("P1Pips");
            p1PipsObj.transform.SetParent(scoreBoard.transform, false);
            var p1PRt = p1PipsObj.AddComponent<RectTransform>();
            p1PRt.anchorMin = new Vector2(0f, 0f);
            p1PRt.anchorMax = new Vector2(0.38f, 0.45f);
            p1PRt.offsetMin = Vector2.zero;
            p1PRt.offsetMax = Vector2.zero;
            _topScoreP1Pips = p1PipsObj.AddComponent<Text>();
            _topScoreP1Pips.font = font;
            _topScoreP1Pips.fontSize = 12;
            _topScoreP1Pips.alignment = TextAnchor.MiddleRight;
            _topScoreP1Pips.color = Color.white;
            _topScoreP1Pips.text = "<color=#334155>◇ ◇ ◇ ◇ ◇</color>";

            // Séparateur VS (Centre)
            var vsObj = new GameObject("VS");
            vsObj.transform.SetParent(scoreBoard.transform, false);
            var vsRt = vsObj.AddComponent<RectTransform>();
            vsRt.anchorMin = new Vector2(0.38f, 0f);
            vsRt.anchorMax = new Vector2(0.62f, 1f);
            vsRt.offsetMin = Vector2.zero;
            vsRt.offsetMax = Vector2.zero;
            var vsTxt = vsObj.AddComponent<Text>();
            vsTxt.font = font;
            vsTxt.fontSize = 13;
            vsTxt.fontStyle = FontStyle.Bold;
            vsTxt.alignment = TextAnchor.MiddleCenter;
            vsTxt.color = new Color(1f, 0.88f, 0.5f);
            vsTxt.text = "— VS —\n<size=10><color=#7D8FA6>Course à 5 pts</color></size>";

            // Score P2 (Droite)
            var p2ScoreObj = new GameObject("P2Score");
            p2ScoreObj.transform.SetParent(scoreBoard.transform, false);
            var p2SRt = p2ScoreObj.AddComponent<RectTransform>();
            p2SRt.anchorMin = new Vector2(0.62f, 0.45f);
            p2SRt.anchorMax = new Vector2(1f, 1f);
            p2SRt.offsetMin = Vector2.zero;
            p2SRt.offsetMax = Vector2.zero;
            _topScoreP2Text = p2ScoreObj.AddComponent<Text>();
            _topScoreP2Text.font = font;
            _topScoreP2Text.fontSize = 15;
            _topScoreP2Text.alignment = TextAnchor.MiddleLeft;
            _topScoreP2Text.color = Color.white;
            _topScoreP2Text.text = "<size=20><b>0</b></size>  <color=#FF6666><b>ADVERSAIRE</b></color>";

            var p2PipsObj = new GameObject("P2Pips");
            p2PipsObj.transform.SetParent(scoreBoard.transform, false);
            var p2PRt = p2PipsObj.AddComponent<RectTransform>();
            p2PRt.anchorMin = new Vector2(0.62f, 0f);
            p2PRt.anchorMax = new Vector2(1f, 0.45f);
            p2PRt.offsetMin = Vector2.zero;
            p2PRt.offsetMax = Vector2.zero;
            _topScoreP2Pips = p2PipsObj.AddComponent<Text>();
            _topScoreP2Pips.font = font;
            _topScoreP2Pips.fontSize = 12;
            _topScoreP2Pips.alignment = TextAnchor.MiddleLeft;
            _topScoreP2Pips.color = Color.white;
            _topScoreP2Pips.text = "<color=#334155>◇ ◇ ◇ ◇ ◇</color>";

            // 1.5 Bouton Abandonner / Quitter
            var forfeitBtnObj = new GameObject("ForfeitBtn");
            forfeitBtnObj.transform.SetParent(topBar.transform, false);
            var forfeitRt = forfeitBtnObj.AddComponent<RectTransform>();
            forfeitRt.anchorMin = new Vector2(0.915f, 0.18f);
            forfeitRt.anchorMax = new Vector2(0.988f, 0.82f);
            forfeitRt.offsetMin = Vector2.zero;
            forfeitRt.offsetMax = Vector2.zero;

            var forfeitImg = forfeitBtnObj.AddComponent<Image>();
            forfeitImg.sprite = WankulUiKit.WhiteSprite;
            forfeitImg.color = new Color(0.45f, 0.14f, 0.16f, 0.95f);

            var forfeitOutline = forfeitBtnObj.AddComponent<Outline>();
            forfeitOutline.effectColor = new Color(0.9f, 0.35f, 0.38f, 0.8f);
            forfeitOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var forfeitBtn = forfeitBtnObj.AddComponent<Button>();
            forfeitBtn.onClick.AddListener(OnForfeitClicked);

            var forfeitTxtObj = new GameObject("Text");
            forfeitTxtObj.transform.SetParent(forfeitBtnObj.transform, false);
            var forfeitTxt = forfeitTxtObj.AddComponent<Text>();
            forfeitTxt.font = font;
            forfeitTxt.fontSize = 13;
            forfeitTxt.fontStyle = FontStyle.Bold;
            forfeitTxt.alignment = TextAnchor.MiddleCenter;
            forfeitTxt.color = Color.white;
            forfeitTxt.text = "Quitter ✕";
            WankulUiKit.Stretch(forfeitTxtObj.GetComponent<RectTransform>());

            // --- 2. BANNIÈRE D'INSTRUCTION ET GUIDE (MessageBanner) ---
            var msgPanelObj = new GameObject("MessageBanner");
            msgPanelObj.transform.SetParent(_canvasObj.transform, false);
            var msgPRt = msgPanelObj.AddComponent<RectTransform>();
            msgPRt.anchorMin = new Vector2(0.12f, 0.860f);
            msgPRt.anchorMax = new Vector2(0.88f, 0.898f);
            msgPRt.offsetMin = Vector2.zero;
            msgPRt.offsetMax = Vector2.zero;

            var msgPBg = msgPanelObj.AddComponent<Image>();
            msgPBg.sprite = WankulUiKit.WhiteSprite;
            msgPBg.color = new Color(0.09f, 0.12f, 0.18f, 0.92f);

            var msgPOutline = msgPanelObj.AddComponent<Outline>();
            msgPOutline.effectColor = new Color(0.22f, 0.30f, 0.44f, 0.7f);
            msgPOutline.effectDistance = new Vector2(1f, -1f);

            var msgTxtObj = new GameObject("Text");
            msgTxtObj.transform.SetParent(msgPanelObj.transform, false);
            _statusMessageText = msgTxtObj.AddComponent<Text>();
            _statusMessageText.font = font;
            _statusMessageText.fontSize = 14;
            _statusMessageText.fontStyle = FontStyle.Bold;
            _statusMessageText.alignment = TextAnchor.MiddleCenter;
            _statusMessageText.color = new Color(1f, 0.88f, 0.45f);
            WankulUiKit.Stretch(msgTxtObj.GetComponent<RectTransform>());

            // --- 3. ZONE CENTRALE (3 TERRAINS DUEL) ---
            var boardArea = new GameObject("BoardArea");
            boardArea.transform.SetParent(_canvasObj.transform, false);
            var boardRt = boardArea.AddComponent<RectTransform>();
            boardRt.anchorMin = new Vector2(0.025f, 0.315f);
            boardRt.anchorMax = new Vector2(0.975f, 0.850f);
            boardRt.offsetMin = Vector2.zero;
            boardRt.offsetMax = Vector2.zero;

            for (int i = 0; i < 3; i++)
            {
                int slotIndex = i;
                var slotObj = new GameObject($"Slot_{i}");
                slotObj.transform.SetParent(boardArea.transform, false);
                var slotRt = slotObj.AddComponent<RectTransform>();
                float leftAnchor = i * 0.333f + 0.008f;
                float rightAnchor = (i + 1) * 0.333f - 0.008f;
                slotRt.anchorMin = new Vector2(leftAnchor, 0f);
                slotRt.anchorMax = new Vector2(rightAnchor, 1f);
                slotRt.offsetMin = Vector2.zero;
                slotRt.offsetMax = Vector2.zero;

                var slotBg = slotObj.AddComponent<Image>();
                slotBg.sprite = WankulUiKit.WhiteSprite;
                slotBg.color = new Color(0.11f, 0.13f, 0.19f, 0.95f);

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

                // En-tête haut de slot : "TERRAIN X"
                var slotHeaderObj = new GameObject("SlotTopHeader");
                slotHeaderObj.transform.SetParent(slotObj.transform, false);
                var sHRt = slotHeaderObj.AddComponent<RectTransform>();
                sHRt.anchorMin = new Vector2(0.03f, 0.965f);
                sHRt.anchorMax = new Vector2(0.97f, 0.995f);
                sHRt.offsetMin = Vector2.zero;
                sHRt.offsetMax = Vector2.zero;
                var sHText = slotHeaderObj.AddComponent<Text>();
                sHText.font = font;
                sHText.fontSize = 11;
                sHText.fontStyle = FontStyle.Bold;
                sHText.alignment = TextAnchor.MiddleCenter;
                sHText.color = new Color(0.55f, 0.65f, 0.80f);
                sHText.text = $"◆ LIEU {i + 1} ◆";

                // ==================== 1. ZONE HAUT : ADVERSAIRE ====================
                var p2Area = new GameObject("P2Area");
                p2Area.transform.SetParent(slotObj.transform, false);
                var p2Rt = p2Area.AddComponent<RectTransform>();
                p2Rt.anchorMin = new Vector2(0.025f, 0.680f);
                p2Rt.anchorMax = new Vector2(0.975f, 0.960f);
                p2Rt.offsetMin = Vector2.zero;
                p2Rt.offsetMax = Vector2.zero;

                var p2Bg = p2Area.AddComponent<Image>();
                p2Bg.sprite = WankulUiKit.WhiteSprite;
                p2Bg.color = new Color(0.18f, 0.11f, 0.14f, 0.92f);

                var p2AreaOutline = p2Area.AddComponent<Outline>();
                p2AreaOutline.effectColor = new Color(0.40f, 0.18f, 0.22f, 0.7f);
                p2AreaOutline.effectDistance = new Vector2(1f, -1f);

                var p2Header = new GameObject("P2Header");
                p2Header.transform.SetParent(p2Area.transform, false);
                var p2HRt = p2Header.AddComponent<RectTransform>();
                p2HRt.anchorMin = new Vector2(0.03f, 0.78f);
                p2HRt.anchorMax = new Vector2(0.55f, 0.98f);
                p2HRt.offsetMin = Vector2.zero;
                p2HRt.offsetMax = Vector2.zero;
                var p2HText = p2Header.AddComponent<Text>();
                p2HText.font = font;
                p2HText.fontSize = 12;
                p2HText.fontStyle = FontStyle.Bold;
                p2HText.alignment = TextAnchor.MiddleLeft;
                p2HText.color = new Color(1f, 0.55f, 0.55f);
                p2HText.text = "ADVERSAIRE";

                var p2ForceObj = new GameObject("P2Force");
                p2ForceObj.transform.SetParent(p2Area.transform, false);
                var p2FRt = p2ForceObj.AddComponent<RectTransform>();
                p2FRt.anchorMin = new Vector2(0.55f, 0.78f);
                p2FRt.anchorMax = new Vector2(0.97f, 0.98f);
                p2FRt.offsetMin = Vector2.zero;
                p2FRt.offsetMax = Vector2.zero;
                var p2FText = p2ForceObj.AddComponent<Text>();
                p2FText.font = font;
                p2FText.fontSize = 12;
                p2FText.fontStyle = FontStyle.Bold;
                p2FText.alignment = TextAnchor.MiddleRight;
                p2FText.color = new Color(1f, 0.45f, 0.45f);
                p2FText.text = "⚡ Force: 0";
                _slotP2ForceTexts[i] = p2FText;

                // Conteneur dynamique de cartes déployées (Adversaire)
                var p2CardsContainerObj = new GameObject("P2CardsContainer");
                p2CardsContainerObj.transform.SetParent(p2Area.transform, false);
                var p2CCRt = p2CardsContainerObj.AddComponent<RectTransform>();
                p2CCRt.anchorMin = new Vector2(0.02f, 0.04f);
                p2CCRt.anchorMax = new Vector2(0.98f, 0.76f);
                p2CCRt.offsetMin = Vector2.zero;
                p2CCRt.offsetMax = Vector2.zero;
                p2CardsContainerObj.AddComponent<RectMask2D>();
                var p2VLayout = p2CardsContainerObj.AddComponent<VerticalLayoutGroup>();
                p2VLayout.spacing = 3;
                p2VLayout.padding = new RectOffset(2, 2, 2, 2);
                p2VLayout.childAlignment = TextAnchor.UpperCenter;
                p2VLayout.childControlWidth = true;
                p2VLayout.childControlHeight = false;
                _slotP2CardsContainers[i] = p2CardsContainerObj.transform;

                // ==================== 2. ZONE MILIEU : TERRAIN ====================
                var terrainArea = new GameObject("TerrainArea");
                terrainArea.transform.SetParent(slotObj.transform, false);
                var tAreaRt = terrainArea.AddComponent<RectTransform>();
                tAreaRt.anchorMin = new Vector2(0.025f, 0.330f);
                tAreaRt.anchorMax = new Vector2(0.975f, 0.670f);
                tAreaRt.offsetMin = Vector2.zero;
                tAreaRt.offsetMax = Vector2.zero;

                var tBg = terrainArea.AddComponent<Image>();
                tBg.sprite = WankulUiKit.WhiteSprite;
                tBg.color = new Color(0.07f, 0.09f, 0.13f, 0.95f);

                var tOutline = terrainArea.AddComponent<Outline>();
                tOutline.effectColor = new Color(0.20f, 0.28f, 0.40f, 0.6f);
                tOutline.effectDistance = new Vector2(1f, -1f);

                // Image d'illustration du Terrain (pivot central, 90° si inactif)
                var terrainImgObj = new GameObject("TerrainImage");
                terrainImgObj.transform.SetParent(terrainArea.transform, false);
                var tImgRt = terrainImgObj.AddComponent<RectTransform>();
                tImgRt.anchorMin = new Vector2(0.04f, 0.21f);
                tImgRt.anchorMax = new Vector2(0.96f, 0.79f);
                tImgRt.pivot = new Vector2(0.5f, 0.5f);
                tImgRt.offsetMin = Vector2.zero;
                tImgRt.offsetMax = Vector2.zero;
                var tImg = terrainImgObj.AddComponent<Image>();
                tImg.preserveAspect = true;
                tImg.color = new Color(1f, 1f, 1f, 0f);
                _slotTerrainImages[i] = tImg;

                // Hover trigger sur le terrain pour inspection au clic droit
                int curSlot = i;
                var tTrigger = terrainImgObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var tEnter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                tEnter.callback.AddListener((data) => {
                    if (_engine != null && !_engine.State.Slots[curSlot].IsEmpty)
                        _hoveredCard = _engine.State.Slots[curSlot].Card;
                });
                tTrigger.triggers.Add(tEnter);
                var tExit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                tExit.callback.AddListener((data) => {
                    if (_engine != null && !_engine.State.Slots[curSlot].IsEmpty && _hoveredCard == _engine.State.Slots[curSlot].Card)
                        _hoveredCard = null;
                });
                tTrigger.triggers.Add(tExit);

                // Bandeau Titre Terrain
                var titlePanel = new GameObject("TitlePanel");
                titlePanel.transform.SetParent(terrainArea.transform, false);
                var tPRt = titlePanel.AddComponent<RectTransform>();
                tPRt.anchorMin = new Vector2(0.03f, 0.80f);
                tPRt.anchorMax = new Vector2(0.97f, 0.98f);
                tPRt.offsetMin = Vector2.zero;
                tPRt.offsetMax = Vector2.zero;
                var tPBg = titlePanel.AddComponent<Image>();
                tPBg.sprite = WankulUiKit.WhiteSprite;
                tPBg.color = new Color(0.05f, 0.07f, 0.10f, 0.95f);
                var tPOutline = titlePanel.AddComponent<Outline>();
                tPOutline.effectColor = new Color(1f, 0.85f, 0.35f, 0.4f);
                tPOutline.effectDistance = new Vector2(1f, -1f);

                var titleObj = new GameObject("Title");
                titleObj.transform.SetParent(titlePanel.transform, false);
                var tText = titleObj.AddComponent<Text>();
                tText.font = font;
                tText.fontSize = 12;
                tText.fontStyle = FontStyle.Bold;
                tText.alignment = TextAnchor.MiddleCenter;
                tText.color = new Color(1f, 0.88f, 0.35f);
                WankulUiKit.Stretch(titleObj.GetComponent<RectTransform>());
                _slotTerrainTexts[i] = tText;

                // Pastille Statut Terrain
                var statusPanel = new GameObject("StatusPanel");
                statusPanel.transform.SetParent(terrainArea.transform, false);
                var stPRt = statusPanel.AddComponent<RectTransform>();
                stPRt.anchorMin = new Vector2(0.05f, 0.02f);
                stPRt.anchorMax = new Vector2(0.95f, 0.20f);
                stPRt.offsetMin = Vector2.zero;
                stPRt.offsetMax = Vector2.zero;
                var stPBg = statusPanel.AddComponent<Image>();
                stPBg.sprite = WankulUiKit.WhiteSprite;
                stPBg.color = new Color(0.05f, 0.07f, 0.10f, 0.95f);
                var stPOutline = statusPanel.AddComponent<Outline>();
                stPOutline.effectColor = new Color(0.25f, 0.40f, 0.65f, 0.4f);
                stPOutline.effectDistance = new Vector2(1f, -1f);

                var statusObj = new GameObject("Status");
                statusObj.transform.SetParent(statusPanel.transform, false);
                var stText = statusObj.AddComponent<Text>();
                stText.font = font;
                stText.fontSize = 11;
                stText.fontStyle = FontStyle.Bold;
                stText.alignment = TextAnchor.MiddleCenter;
                WankulUiKit.Stretch(statusObj.GetComponent<RectTransform>());
                _slotStatusTexts[i] = stText;

                // ==================== 3. ZONE BAS : VOS PERSONNAGES ====================
                var p1Area = new GameObject("P1Area");
                p1Area.transform.SetParent(slotObj.transform, false);
                var p1Rt = p1Area.AddComponent<RectTransform>();
                p1Rt.anchorMin = new Vector2(0.025f, 0.025f);
                p1Rt.anchorMax = new Vector2(0.975f, 0.315f);
                p1Rt.offsetMin = Vector2.zero;
                p1Rt.offsetMax = Vector2.zero;

                var p1Bg = p1Area.AddComponent<Image>();
                p1Bg.sprite = WankulUiKit.WhiteSprite;
                p1Bg.color = new Color(0.10f, 0.15f, 0.23f, 0.92f);

                var p1AreaOutline = p1Area.AddComponent<Outline>();
                p1AreaOutline.effectColor = new Color(0.18f, 0.32f, 0.48f, 0.7f);
                p1AreaOutline.effectDistance = new Vector2(1f, -1f);

                var p1Header = new GameObject("P1Header");
                p1Header.transform.SetParent(p1Area.transform, false);
                var p1HRt = p1Header.AddComponent<RectTransform>();
                p1HRt.anchorMin = new Vector2(0.03f, 0.78f);
                p1HRt.anchorMax = new Vector2(0.55f, 0.98f);
                p1HRt.offsetMin = Vector2.zero;
                p1HRt.offsetMax = Vector2.zero;
                var p1HText = p1Header.AddComponent<Text>();
                p1HText.font = font;
                p1HText.fontSize = 12;
                p1HText.fontStyle = FontStyle.Bold;
                p1HText.alignment = TextAnchor.MiddleLeft;
                p1HText.color = new Color(0.55f, 0.85f, 1f);
                p1HText.text = "VOS PERSOS";

                var p1ForceObj = new GameObject("P1Force");
                p1ForceObj.transform.SetParent(p1Area.transform, false);
                var p1FRt = p1ForceObj.AddComponent<RectTransform>();
                p1FRt.anchorMin = new Vector2(0.55f, 0.78f);
                p1FRt.anchorMax = new Vector2(0.97f, 0.98f);
                p1FRt.offsetMin = Vector2.zero;
                p1FRt.offsetMax = Vector2.zero;
                var p1FText = p1ForceObj.AddComponent<Text>();
                p1FText.font = font;
                p1FText.fontSize = 12;
                p1FText.fontStyle = FontStyle.Bold;
                p1FText.alignment = TextAnchor.MiddleRight;
                p1FText.color = new Color(0.4f, 0.85f, 1f);
                p1FText.text = "⚡ Force: 0";
                _slotP1ForceTexts[i] = p1FText;

                // Conteneur dynamique de cartes déployées (Joueur)
                var p1CardsContainerObj = new GameObject("P1CardsContainer");
                p1CardsContainerObj.transform.SetParent(p1Area.transform, false);
                var p1CCRt = p1CardsContainerObj.AddComponent<RectTransform>();
                p1CCRt.anchorMin = new Vector2(0.02f, 0.04f);
                p1CCRt.anchorMax = new Vector2(0.98f, 0.76f);
                p1CCRt.offsetMin = Vector2.zero;
                p1CCRt.offsetMax = Vector2.zero;
                p1CardsContainerObj.AddComponent<RectMask2D>();
                var p1VLayout = p1CardsContainerObj.AddComponent<VerticalLayoutGroup>();
                p1VLayout.spacing = 3;
                p1VLayout.padding = new RectOffset(2, 2, 2, 2);
                p1VLayout.childAlignment = TextAnchor.UpperCenter;
                p1VLayout.childControlWidth = true;
                p1VLayout.childControlHeight = false;
                _slotP1CardsContainers[i] = p1CardsContainerObj.transform;
            }

            // --- 4. ZONE INFÉRIEURE (DOCK, MAIN, COMMANDES) ---
            var bottomArea = new GameObject("BottomArea");
            bottomArea.transform.SetParent(_canvasObj.transform, false);
            var bottomRt = bottomArea.AddComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0.015f, 0.015f);
            bottomRt.anchorMax = new Vector2(0.985f, 0.300f);
            bottomRt.offsetMin = Vector2.zero;
            bottomRt.offsetMax = Vector2.zero;

            // 4.1 DOCK GAUCHE (Quotas & Stats Pioche/Poubelle)
            var dockObj = new GameObject("LeftDock");
            dockObj.transform.SetParent(bottomArea.transform, false);
            var dockRt = dockObj.AddComponent<RectTransform>();
            dockRt.anchorMin = new Vector2(0f, 0f);
            dockRt.anchorMax = new Vector2(0.155f, 1f);
            dockRt.offsetMin = Vector2.zero;
            dockRt.offsetMax = Vector2.zero;

            var dockBg = dockObj.AddComponent<Image>();
            dockBg.sprite = WankulUiKit.WhiteSprite;
            dockBg.color = new Color(0.10f, 0.13f, 0.18f, 0.95f);

            var dockOutline = dockObj.AddComponent<Outline>();
            dockOutline.effectColor = new Color(0.22f, 0.30f, 0.44f, 0.7f);
            dockOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Titre Dépôts Perso
            var quotaTitleObj = new GameObject("QuotaTitle");
            quotaTitleObj.transform.SetParent(dockObj.transform, false);
            var qTRt = quotaTitleObj.AddComponent<RectTransform>();
            qTRt.anchorMin = new Vector2(0.05f, 0.82f);
            qTRt.anchorMax = new Vector2(0.95f, 0.96f);
            qTRt.offsetMin = Vector2.zero;
            qTRt.offsetMax = Vector2.zero;
            var qTTxt = quotaTitleObj.AddComponent<Text>();
            qTTxt.font = font;
            qTTxt.fontSize = 11;
            qTTxt.fontStyle = FontStyle.Bold;
            qTTxt.alignment = TextAnchor.MiddleCenter;
            qTTxt.color = new Color(1f, 0.88f, 0.45f);
            qTTxt.text = "DÉPÔTS DE PERSO";

            // Pips visuels (● ● ○ ○)
            var quotaPipsObj = new GameObject("QuotaPips");
            quotaPipsObj.transform.SetParent(dockObj.transform, false);
            var qPRt = quotaPipsObj.AddComponent<RectTransform>();
            qPRt.anchorMin = new Vector2(0.05f, 0.65f);
            qPRt.anchorMax = new Vector2(0.95f, 0.82f);
            qPRt.offsetMin = Vector2.zero;
            qPRt.offsetMax = Vector2.zero;
            _dockQuotaPipsText = quotaPipsObj.AddComponent<Text>();
            _dockQuotaPipsText.font = font;
            _dockQuotaPipsText.fontSize = 18;
            _dockQuotaPipsText.alignment = TextAnchor.MiddleCenter;
            _dockQuotaPipsText.color = Color.white;
            _dockQuotaPipsText.text = "○ ○ ○ ○";

            // Compteur numérique (0 / 4)
            var quotaCountObj = new GameObject("QuotaCount");
            quotaCountObj.transform.SetParent(dockObj.transform, false);
            var qCRt = quotaCountObj.AddComponent<RectTransform>();
            qCRt.anchorMin = new Vector2(0.05f, 0.52f);
            qCRt.anchorMax = new Vector2(0.95f, 0.65f);
            qCRt.offsetMin = Vector2.zero;
            qCRt.offsetMax = Vector2.zero;
            _dockQuotaCountText = quotaCountObj.AddComponent<Text>();
            _dockQuotaCountText.font = font;
            _dockQuotaCountText.fontSize = 12;
            _dockQuotaCountText.fontStyle = FontStyle.Bold;
            _dockQuotaCountText.alignment = TextAnchor.MiddleCenter;
            _dockQuotaCountText.color = new Color(0.4f, 0.85f, 1f);
            _dockQuotaCountText.text = "0 / 4 joués";

            // Séparateur ligne
            var divObj = new GameObject("Divider");
            divObj.transform.SetParent(dockObj.transform, false);
            var divRt = divObj.AddComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0.10f, 0.48f);
            divRt.anchorMax = new Vector2(0.90f, 0.49f);
            divRt.offsetMin = Vector2.zero;
            divRt.offsetMax = Vector2.zero;
            var divImg = divObj.AddComponent<Image>();
            divImg.sprite = WankulUiKit.WhiteSprite;
            divImg.color = new Color(0.20f, 0.28f, 0.40f, 0.6f);

            // Badge Pioche
            var deckObj = new GameObject("DeckBadge");
            deckObj.transform.SetParent(dockObj.transform, false);
            var dRt = deckObj.AddComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.08f, 0.25f);
            dRt.anchorMax = new Vector2(0.92f, 0.45f);
            dRt.offsetMin = Vector2.zero;
            dRt.offsetMax = Vector2.zero;
            _dockDeckCountText = deckObj.AddComponent<Text>();
            _dockDeckCountText.font = font;
            _dockDeckCountText.fontSize = 12;
            _dockDeckCountText.fontStyle = FontStyle.Bold;
            _dockDeckCountText.alignment = TextAnchor.MiddleLeft;
            _dockDeckCountText.color = new Color(0.9f, 0.95f, 1f);
            _dockDeckCountText.text = "🂠 Pioche : <b>24</b>";

            // Badge Défausse
            var discardObj = new GameObject("DiscardBadge");
            discardObj.transform.SetParent(dockObj.transform, false);
            var discRt = discardObj.AddComponent<RectTransform>();
            discRt.anchorMin = new Vector2(0.08f, 0.05f);
            discRt.anchorMax = new Vector2(0.92f, 0.25f);
            discRt.offsetMin = Vector2.zero;
            discRt.offsetMax = Vector2.zero;
            _dockDiscardCountText = discardObj.AddComponent<Text>();
            _dockDiscardCountText.font = font;
            _dockDiscardCountText.fontSize = 12;
            _dockDiscardCountText.fontStyle = FontStyle.Bold;
            _dockDiscardCountText.alignment = TextAnchor.MiddleLeft;
            _dockDiscardCountText.color = new Color(0.85f, 0.70f, 0.75f);
            _dockDiscardCountText.text = "🗑 Poubelle : <b>0</b>";

            // 4.2 MAIN DU JOUEUR (HandContainer)
            var handScroll = new GameObject("HandContainer");
            handScroll.transform.SetParent(bottomArea.transform, false);
            var handRt = handScroll.AddComponent<RectTransform>();
            handRt.anchorMin = new Vector2(0.165f, 0f);
            handRt.anchorMax = new Vector2(0.835f, 1f);
            handRt.offsetMin = Vector2.zero;
            handRt.offsetMax = Vector2.zero;
            handScroll.AddComponent<RectMask2D>();
            var hLayout = handScroll.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 10;
            hLayout.padding = new RectOffset(10, 10, 5, 5);
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandHeight = false;
            hLayout.childForceExpandWidth = false;
            _handContainer = handScroll.transform;
            _handLayoutGroup = hLayout;

            // 4.3 BOUTON FIN DU TOUR
            var btnObj = new GameObject("EndTurnBtn");
            btnObj.transform.SetParent(bottomArea.transform, false);
            var btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.845f, 0.15f);
            btnRt.anchorMax = new Vector2(0.995f, 0.85f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;

            _endTurnButtonBg = btnObj.AddComponent<Image>();
            _endTurnButtonBg.sprite = WankulUiKit.WhiteSprite;
            _endTurnButtonBg.color = new Color(0.70f, 0.18f, 0.22f, 0.95f);

            _endTurnButtonOutline = btnObj.AddComponent<Outline>();
            _endTurnButtonOutline.effectColor = new Color(1f, 0.82f, 0.3f, 1f);
            _endTurnButtonOutline.effectDistance = new Vector2(2.5f, -2.5f);

            _endTurnButton = btnObj.AddComponent<Button>();
            _endTurnButton.onClick.AddListener(OnEndTurnClicked);

            var btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            _endTurnButtonText = btnTxtObj.AddComponent<Text>();
            _endTurnButtonText.font = font;
            _endTurnButtonText.fontSize = 18;
            _endTurnButtonText.fontStyle = FontStyle.Bold;
            _endTurnButtonText.alignment = TextAnchor.MiddleCenter;
            _endTurnButtonText.color = Color.white;
            _endTurnButtonText.text = "<b>FIN DU TOUR</b>\n<size=12><color=#ffe080>► Passer la main</color></size>";
            WankulUiKit.Stretch(btnTxtObj.GetComponent<RectTransform>());

            // --- 5. VOLET D'INSPECTION HD (Clic Droit) ---
            BuildInspectorUI(font);

            // --- 6. BARRE DE CONFRONTATION DYNAMIQUE (Tug-of-War / ScoreClashBar) ---
            BuildClashBarUI(font);
        }

        private void BuildInspectorUI(Font font)
        {
            _inspectorPanel = new GameObject("InspectorPanel");
            _inspectorPanel.transform.SetParent(_canvasObj.transform, false);
            var inspRt = _inspectorPanel.AddComponent<RectTransform>();
            inspRt.anchorMin = new Vector2(0.78f, 0.05f);
            inspRt.anchorMax = new Vector2(0.99f, 0.95f);
            inspRt.offsetMin = Vector2.zero;
            inspRt.offsetMax = Vector2.zero;

            var inspBg = _inspectorPanel.AddComponent<Image>();
            inspBg.sprite = WankulUiKit.WhiteSprite;
            inspBg.color = new Color(0.08f, 0.10f, 0.14f, 0.96f);

            var inspOutline = _inspectorPanel.AddComponent<Outline>();
            inspOutline.effectColor = new Color(1f, 0.85f, 0.35f, 0.85f);
            inspOutline.effectDistance = new Vector2(2f, -2f);

            // Illustration HD de la carte
            var imgObj = new GameObject("CardImage");
            imgObj.transform.SetParent(_inspectorPanel.transform, false);
            var imgRt = imgObj.AddComponent<RectTransform>();
            imgRt.anchorMin = new Vector2(0.08f, 0.45f);
            imgRt.anchorMax = new Vector2(0.92f, 0.96f);
            imgRt.offsetMin = Vector2.zero;
            imgRt.offsetMax = Vector2.zero;
            _inspectorCardImage = imgObj.AddComponent<Image>();
            _inspectorCardImage.preserveAspect = true;

            // Titre
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(_inspectorPanel.transform, false);
            var titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.05f, 0.37f);
            titleRt.anchorMax = new Vector2(0.95f, 0.44f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            _inspectorTitleText = titleObj.AddComponent<Text>();
            _inspectorTitleText.font = font;
            _inspectorTitleText.fontSize = 18;
            _inspectorTitleText.fontStyle = FontStyle.Bold;
            _inspectorTitleText.alignment = TextAnchor.MiddleCenter;
            _inspectorTitleText.color = new Color(1f, 0.9f, 0.4f);

            // Type / Badge
            var typeObj = new GameObject("TypeBadge");
            typeObj.transform.SetParent(_inspectorPanel.transform, false);
            var typeRt = typeObj.AddComponent<RectTransform>();
            typeRt.anchorMin = new Vector2(0.05f, 0.31f);
            typeRt.anchorMax = new Vector2(0.50f, 0.36f);
            typeRt.offsetMin = Vector2.zero;
            typeRt.offsetMax = Vector2.zero;
            _inspectorTypeText = typeObj.AddComponent<Text>();
            _inspectorTypeText.font = font;
            _inspectorTypeText.fontSize = 14;
            _inspectorTypeText.fontStyle = FontStyle.Bold;
            _inspectorTypeText.alignment = TextAnchor.MiddleCenter;
            _inspectorTypeText.color = new Color(0.6f, 0.85f, 1f);

            // Force
            var forceObj = new GameObject("ForceBadge");
            forceObj.transform.SetParent(_inspectorPanel.transform, false);
            var forceRt = forceObj.AddComponent<RectTransform>();
            forceRt.anchorMin = new Vector2(0.50f, 0.31f);
            forceRt.anchorMax = new Vector2(0.95f, 0.36f);
            forceRt.offsetMin = Vector2.zero;
            forceRt.offsetMax = Vector2.zero;
            _inspectorForceText = forceObj.AddComponent<Text>();
            _inspectorForceText.font = font;
            _inspectorForceText.fontSize = 14;
            _inspectorForceText.fontStyle = FontStyle.Bold;
            _inspectorForceText.alignment = TextAnchor.MiddleCenter;
            _inspectorForceText.color = new Color(0.4f, 1f, 0.5f);

            // Description / Effet complet
            var descObj = new GameObject("DescBox");
            descObj.transform.SetParent(_inspectorPanel.transform, false);
            var descRt = descObj.AddComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0.06f, 0.08f);
            descRt.anchorMax = new Vector2(0.94f, 0.30f);
            descRt.offsetMin = Vector2.zero;
            descRt.offsetMax = Vector2.zero;
            _inspectorDescText = descObj.AddComponent<Text>();
            _inspectorDescText.font = font;
            _inspectorDescText.fontSize = 12;
            _inspectorDescText.alignment = TextAnchor.UpperLeft;
            _inspectorDescText.color = new Color(0.9f, 0.9f, 0.95f);

            // Raccourci fermer en bas
            var closeHintObj = new GameObject("CloseHint");
            closeHintObj.transform.SetParent(_inspectorPanel.transform, false);
            var closeHintRt = closeHintObj.AddComponent<RectTransform>();
            closeHintRt.anchorMin = new Vector2(0.05f, 0.01f);
            closeHintRt.anchorMax = new Vector2(0.95f, 0.07f);
            closeHintRt.offsetMin = Vector2.zero;
            closeHintRt.offsetMax = Vector2.zero;
            var closeHintText = closeHintObj.AddComponent<Text>();
            closeHintText.font = font;
            closeHintText.fontSize = 11;
            closeHintText.alignment = TextAnchor.MiddleCenter;
            closeHintText.color = new Color(0.6f, 0.6f, 0.7f);
            closeHintText.text = "[ Clic droit ou Echap pour fermer ]";

            _inspectorPanel.SetActive(false);
        }

        private void BuildClashBarUI(Font font)
        {
            _clashBarPanel = new GameObject("ScoreClashBar");
            _clashBarPanel.transform.SetParent(_canvasObj.transform, false);
            var clashRt = _clashBarPanel.AddComponent<RectTransform>();
            clashRt.anchorMin = new Vector2(0.20f, 0.40f);
            clashRt.anchorMax = new Vector2(0.80f, 0.62f);
            clashRt.offsetMin = Vector2.zero;
            clashRt.offsetMax = Vector2.zero;

            var clashBg = _clashBarPanel.AddComponent<Image>();
            clashBg.sprite = WankulUiKit.WhiteSprite;
            clashBg.color = new Color(0.05f, 0.07f, 0.10f, 0.95f);

            var clashOutline = _clashBarPanel.AddComponent<Outline>();
            clashOutline.effectColor = new Color(1f, 0.85f, 0.3f, 1f);
            clashOutline.effectDistance = new Vector2(3f, -3f);

            // Titre du Clash
            var cTitleObj = new GameObject("Title");
            cTitleObj.transform.SetParent(_clashBarPanel.transform, false);
            var cTitleRt = cTitleObj.AddComponent<RectTransform>();
            cTitleRt.anchorMin = new Vector2(0.05f, 0.72f);
            cTitleRt.anchorMax = new Vector2(0.95f, 0.95f);
            cTitleRt.offsetMin = Vector2.zero;
            cTitleRt.offsetMax = Vector2.zero;
            _clashTitleText = cTitleObj.AddComponent<Text>();
            _clashTitleText.font = font;
            _clashTitleText.fontSize = 20;
            _clashTitleText.fontStyle = FontStyle.Bold;
            _clashTitleText.alignment = TextAnchor.MiddleCenter;
            _clashTitleText.color = new Color(1f, 0.88f, 0.35f);
            _clashTitleText.text = "⚔️ DÉCLENCHEMENT DUEL — SCOREUR ACTIF !";

            // Fond de la jauge (Tug of War)
            var gaugeBgObj = new GameObject("GaugeBg");
            gaugeBgObj.transform.SetParent(_clashBarPanel.transform, false);
            var gBgRt = gaugeBgObj.AddComponent<RectTransform>();
            gBgRt.anchorMin = new Vector2(0.08f, 0.38f);
            gBgRt.anchorMax = new Vector2(0.92f, 0.60f);
            gBgRt.offsetMin = Vector2.zero;
            gBgRt.offsetMax = Vector2.zero;
            var gBgImg = gaugeBgObj.AddComponent<Image>();
            gBgImg.sprite = WankulUiKit.WhiteSprite;
            gBgImg.color = new Color(0.12f, 0.14f, 0.18f, 1f);

            // Jauge P1 (Gauche - Cyan)
            var p1FillObj = new GameObject("P1Fill");
            p1FillObj.transform.SetParent(gaugeBgObj.transform, false);
            _clashP1Fill = p1FillObj.AddComponent<RectTransform>();
            _clashP1Fill.anchorMin = new Vector2(0f, 0f);
            _clashP1Fill.anchorMax = new Vector2(0.5f, 1f);
            _clashP1Fill.offsetMin = Vector2.zero;
            _clashP1Fill.offsetMax = Vector2.zero;
            var p1Img = p1FillObj.AddComponent<Image>();
            p1Img.sprite = WankulUiKit.WhiteSprite;
            p1Img.color = new Color(0.2f, 0.65f, 1f, 0.95f);

            // Jauge P2 (Droite - Rouge)
            var p2FillObj = new GameObject("P2Fill");
            p2FillObj.transform.SetParent(gaugeBgObj.transform, false);
            _clashP2Fill = p2FillObj.AddComponent<RectTransform>();
            _clashP2Fill.anchorMin = new Vector2(0.5f, 0f);
            _clashP2Fill.anchorMax = new Vector2(1f, 1f);
            _clashP2Fill.offsetMin = Vector2.zero;
            _clashP2Fill.offsetMax = Vector2.zero;
            var p2Img = p2FillObj.AddComponent<Image>();
            p2Img.sprite = WankulUiKit.WhiteSprite;
            p2Img.color = new Color(0.95f, 0.25f, 0.3f, 0.95f);

            // Curseur losange central
            var cursorObj = new GameObject("Cursor");
            cursorObj.transform.SetParent(gaugeBgObj.transform, false);
            _clashCursor = cursorObj.AddComponent<RectTransform>();
            _clashCursor.anchorMin = new Vector2(0.5f, 0.5f);
            _clashCursor.anchorMax = new Vector2(0.5f, 0.5f);
            _clashCursor.pivot = new Vector2(0.5f, 0.5f);
            _clashCursor.sizeDelta = new Vector2(24f, 24f);
            var cImg = cursorObj.AddComponent<Image>();
            cImg.sprite = WankulUiKit.WhiteSprite;
            cImg.color = new Color(1f, 0.9f, 0.4f, 1f);
            cursorObj.transform.localEulerAngles = new Vector3(0, 0, 45f);

            // Textes de Force P1 et P2
            var p1TxtObj = new GameObject("P1ForceText");
            p1TxtObj.transform.SetParent(_clashBarPanel.transform, false);
            var p1TxtRt = p1TxtObj.AddComponent<RectTransform>();
            p1TxtRt.anchorMin = new Vector2(0.08f, 0.18f);
            p1TxtRt.anchorMax = new Vector2(0.45f, 0.36f);
            p1TxtRt.offsetMin = Vector2.zero;
            p1TxtRt.offsetMax = Vector2.zero;
            _clashP1ForceText = p1TxtObj.AddComponent<Text>();
            _clashP1ForceText.font = font;
            _clashP1ForceText.fontSize = 15;
            _clashP1ForceText.fontStyle = FontStyle.Bold;
            _clashP1ForceText.alignment = TextAnchor.MiddleLeft;
            _clashP1ForceText.color = new Color(0.4f, 0.8f, 1f);

            var p2TxtObj = new GameObject("P2ForceText");
            p2TxtObj.transform.SetParent(_clashBarPanel.transform, false);
            var p2TxtRt = p2TxtObj.AddComponent<RectTransform>();
            p2TxtRt.anchorMin = new Vector2(0.55f, 0.18f);
            p2TxtRt.anchorMax = new Vector2(0.92f, 0.36f);
            p2TxtRt.offsetMin = Vector2.zero;
            p2TxtRt.offsetMax = Vector2.zero;
            _clashP2ForceText = p2TxtObj.AddComponent<Text>();
            _clashP2ForceText.font = font;
            _clashP2ForceText.fontSize = 15;
            _clashP2ForceText.fontStyle = FontStyle.Bold;
            _clashP2ForceText.alignment = TextAnchor.MiddleRight;
            _clashP2ForceText.color = new Color(1f, 0.45f, 0.45f);

            // Statut de Domination / Gain estimé
            var domObj = new GameObject("DominanceText");
            domObj.transform.SetParent(_clashBarPanel.transform, false);
            var domRt = domObj.AddComponent<RectTransform>();
            domRt.anchorMin = new Vector2(0.05f, 0.04f);
            domRt.anchorMax = new Vector2(0.95f, 0.18f);
            domRt.offsetMin = Vector2.zero;
            domRt.offsetMax = Vector2.zero;
            _clashDominanceText = domObj.AddComponent<Text>();
            _clashDominanceText.font = font;
            _clashDominanceText.fontSize = 13;
            _clashDominanceText.alignment = TextAnchor.MiddleCenter;
            _clashDominanceText.color = new Color(1f, 0.85f, 0.5f);

            _clashBarPanel.SetActive(false);
        }

        private void RefreshView()
        {
            if (_engine == null) return;

            var font = WankulUiKit.GetFont();

            // 1. Bandeau supérieur (TopBar)
            _topTurnBadgeText.text = $"TOUR {_engine.State.TurnNumber}";

            bool isPlayerTurn = _engine.State.ActivePlayer == PlayerId.Player1 && !_engine.State.IsGameOver;
            if (isPlayerTurn)
            {
                _topActivePlayerText.text = "● VOTRE TOUR";
                _topActivePlayerText.color = new Color(0.4f, 0.95f, 1f);
                _topActivePlayerBg.color = new Color(0.08f, 0.24f, 0.35f, 0.95f);
            }
            else
            {
                _topActivePlayerText.text = "● TOUR ADVERSE";
                _topActivePlayerText.color = new Color(1f, 0.45f, 0.45f);
                _topActivePlayerBg.color = new Color(0.32f, 0.10f, 0.14f, 0.95f);
            }

            // Tableau des scores & Diamonds (Course à 5 points)
            int scoreP1 = _engine.State.ScoreP1;
            int scoreP2 = _engine.State.ScoreP2;

            _topScoreP1Text.text = $"<color=#55C5FF><b>JOUEUR</b></color>  <size=20><b>{scoreP1}</b></size>";
            string p1Diamonds = "";
            for (int d = 0; d < 5; d++)
            {
                p1Diamonds += d < scoreP1 ? "<color=#55C5FF>◆</color> " : "<color=#334155>◇</color> ";
            }
            _topScoreP1Pips.text = p1Diamonds.TrimEnd();

            _topScoreP2Text.text = $"<size=20><b>{scoreP2}</b></size>  <color=#FF6666><b>ADVERSAIRE</b></color>";
            string p2Diamonds = "";
            for (int d = 0; d < 5; d++)
            {
                p2Diamonds += d < scoreP2 ? "<color=#FF6666>◆</color> " : "<color=#334155>◇</color> ";
            }
            _topScoreP2Pips.text = p2Diamonds.TrimEnd();

            // 2. Guide d'action & instruction (MessageBanner)
            if (_selectedHandCard != null)
            {
                string actionHint = _selectedHandCard.Kind == CardKind.Terrain
                    ? "👉 Cliquez sur un slot libre en surbrillance verte pour y poser le Terrain (ou recliquez pour annuler)"
                    : $"👉 Cliquez sur un Terrain en surbrillance verte pour y poser [{CardAdapter.FixMojibake(_selectedHandCard.Name)}] (ou recliquez pour annuler)";
                _statusMessageText.text = $"⚡ <b>ACTION :</b> {actionHint}";
            }
            else if (_engine.State.TerrainRequirementPending)
            {
                if (isPlayerTurn)
                    _statusMessageText.text = "⚠ <b>OBLIGATION :</b> Vous devez poser un Terrain sur un slot libre !";
                else
                    _statusMessageText.text = "⏳ L'adversaire pose un Terrain...";
            }
            else if (!isPlayerTurn)
            {
                _statusMessageText.text = "⏳ Tour de l'adversaire en cours...";
            }
            else
            {
                _statusMessageText.text = "💡 Cliquez sur une carte en main pour la sélectionner. Clic droit pour inspecter.";
            }

            // 3. Dock gauche (Quotas et stats pioche/défausse)
            int playedThisTurn = _engine.State.CharactersPlayedThisTurn;
            string quotaPips = "";
            for (int q = 0; q < 4; q++)
            {
                quotaPips += q < playedThisTurn ? "<color=#55C5FF>●</color> " : "<color=#404B5C>○</color> ";
            }
            _dockQuotaPipsText.text = quotaPips.TrimEnd();
            _dockQuotaCountText.text = $"{playedThisTurn} / 4 joués";

            _dockDeckCountText.text = $"🂠 Pioche : <b>{_engine.State.DeckP1.Count}</b>";
            _dockDiscardCountText.text = $"🗑 Poubelle : <b>{_engine.State.DiscardP1.Count}</b>";

            // 4. Terrains & Éligibilité des 3 slots
            for (int i = 0; i < 3; i++)
            {
                var slot = _engine.State.Slots[i];

                // Calcul de l'éligibilité pour un clic direct
                bool isEligible = false;
                if (_selectedHandCard != null && isPlayerTurn)
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
                    string colorTag = forceP1 > forceP2 ? "#55FF88" : (forceP1 < forceP2 ? "#8899AA" : "#DDE4F0");
                    _slotP1ForceTexts[i].text = $"⚡ <color={colorTag}>Force: {forceP1}</color>";
                }
                if (_slotP2ForceTexts[i] != null)
                {
                    string colorTag = forceP2 > forceP1 ? "#FF7777" : (forceP2 < forceP1 ? "#8899AA" : "#DDE4F0");
                    _slotP2ForceTexts[i].text = $"⚡ <color={colorTag}>Force: {forceP2}</color>";
                }

                // Population visuelle des cartes déployées
                PopulateSlotCards(_slotP1CardsContainers[i], slot.CharactersP1, isPlayer: true, font);
                PopulateSlotCards(_slotP2CardsContainers[i], slot.CharactersP2, isPlayer: false, font);

                if (slot.IsEmpty)
                {
                    if (_slotTerrainImages[i] != null)
                    {
                        _slotTerrainImages[i].color = new Color(1f, 1f, 1f, 0f);
                        _slotTerrainImages[i].transform.localEulerAngles = Vector3.zero;
                    }

                    _slotTerrainTexts[i].text = $"[ SLOT {i + 1} LIBRE ]";
                    _slotStatusTexts[i].text = isEligible ? "<color=#55FF55>Cliquez pour poser ici</color>" : "<color=#667788>(Aucun terrain)</color>";
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
                            _slotTerrainImages[i].color = isActive ? new Color(1f, 1f, 1f, 0.95f) : new Color(0.65f, 0.65f, 0.65f, 0.60f);
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
                    _slotStatusTexts[i].text = isActive ? "<color=#55FF88>● ACTIF (Scorable)</color>" : "<color=#FFAA00>◐ INCLINÉ (Inactif)</color>";
                }
            }

            // 5. Main du Joueur (Player 1)
            foreach (Transform child in _handContainer)
            {
                Destroy(child.gameObject);
            }

            var hand = _engine.State.GetHand(PlayerId.Player1);

            // Ajustement dynamique de l'espacement pour empêcher tout débordement sous le bouton Fin du tour
            if (_handLayoutGroup != null)
            {
                if (hand.Count <= 5)
                {
                    _handLayoutGroup.spacing = 8f;
                }
                else
                {
                    // Réduit progressivement l'espacement jusqu'à -70px pour faire un éventail bien tassé sans déborder
                    float dynamicSpacing = Mathf.Clamp(8f - (hand.Count - 5) * 16f, -70f, 8f);
                    _handLayoutGroup.spacing = dynamicSpacing;
                }
            }

            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                var cardBtnObj = new GameObject($"CardBtn_{card.Id}");
                cardBtnObj.transform.SetParent(_handContainer, false);
                var cRt = cardBtnObj.AddComponent<RectTransform>();
                cRt.sizeDelta = new Vector2(140, 195);

                var le = cardBtnObj.AddComponent<LayoutElement>();
                le.minWidth = 140f;
                le.minHeight = 195f;
                le.preferredWidth = 140f;
                le.preferredHeight = 195f;
                le.flexibleWidth = 0f;
                le.flexibleHeight = 0f;

                var img = cardBtnObj.AddComponent<Image>();
                var btn = cardBtnObj.AddComponent<Button>();
                btn.interactable = isPlayerTurn;
                btn.onClick.AddListener(() => OnCardInHandClicked(card));

                // Effet visuel si la carte est sélectionnée (liseré doré + surélévation)
                bool isSelected = _selectedHandCard != null && _selectedHandCard.Id == card.Id;
                if (isSelected)
                {
                    var cardOutline = cardBtnObj.AddComponent<Outline>();
                    cardOutline.effectColor = new Color(1f, 0.85f, 0.2f, 1f);
                    cardOutline.effectDistance = new Vector2(3.5f, -3.5f);
                    cRt.anchoredPosition = new Vector2(0f, 15f);
                    cardBtnObj.transform.SetAsLastSibling();
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

                // Gestion du survol (Hover) pour l'inspection au clic droit et mise au premier plan
                var trigger = cardBtnObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                int cardSiblingIndex = i;
                var capturedObj = cardBtnObj;
                var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                enterEntry.callback.AddListener((data) => {
                    _hoveredCard = card;
                    if (capturedObj != null)
                    {
                        capturedObj.transform.SetAsLastSibling();
                    }
                });
                trigger.triggers.Add(enterEntry);

                var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                exitEntry.callback.AddListener((data) => {
                    if (_hoveredCard == card) _hoveredCard = null;
                    if (capturedObj != null && !isSelected)
                    {
                        capturedObj.transform.SetSiblingIndex(cardSiblingIndex);
                    }
                });
                trigger.triggers.Add(exitEntry);
            }

            // 6. Bouton Fin de tour
            bool pendingTerrain = _engine.State.TerrainRequirementPending;
            bool canEndTurn = isPlayerTurn && !pendingTerrain;
            _endTurnButton.interactable = canEndTurn;

            if (!isPlayerTurn)
            {
                _endTurnButtonText.text = "<color=#8899AA>TOUR ADVERSE\n<size=11>En attente...</size></color>";
                _endTurnButtonBg.color = new Color(0.18f, 0.20f, 0.26f, 0.85f);
                _endTurnButtonOutline.effectColor = new Color(0.3f, 0.35f, 0.45f, 0.5f);
            }
            else if (pendingTerrain)
            {
                _endTurnButtonText.text = "<color=#FF9999>TERRAIN REQUIS\n<size=11>Posez un terrain !</size></color>";
                _endTurnButtonBg.color = new Color(0.40f, 0.18f, 0.18f, 0.9f);
                _endTurnButtonOutline.effectColor = new Color(0.85f, 0.3f, 0.3f, 0.8f);
            }
            else
            {
                _endTurnButtonText.text = "<b>FIN DU TOUR</b>\n<size=12><color=#ffe080>► Passer la main</color></size>";
                _endTurnButtonBg.color = new Color(0.70f, 0.18f, 0.22f, 0.95f);
                _endTurnButtonOutline.effectColor = new Color(1f, 0.82f, 0.3f, 1f);
            }
        }

        private void PopulateSlotCards(Transform container, IReadOnlyList<DuelCard> cards, bool isPlayer, Font font)
        {
            if (container == null) return;

            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }

            if (cards == null || cards.Count == 0)
            {
                var emptyObj = new GameObject("Empty");
                emptyObj.transform.SetParent(container, false);
                var t = emptyObj.AddComponent<Text>();
                t.font = font;
                t.fontSize = 11;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = isPlayer ? new Color(0.45f, 0.55f, 0.70f, 0.75f) : new Color(0.70f, 0.45f, 0.50f, 0.75f);
                t.text = isPlayer ? "(Aucun personnage déployé)" : "(Aucun personnage adverse)";
                var le = emptyObj.AddComponent<LayoutElement>();
                le.minHeight = 22;
                le.preferredHeight = 24;
                le.flexibleWidth = 1f;
                return;
            }

            // Calcul de taille dynamique des plaquettes selon le nombre de cartes déployées
            // cards <= 3 : 24px (font 11) | cards == 4 : 20px (font 10) | cards >= 5 : 17px (font 9)
            float plateHeight = cards.Count <= 3 ? 24f : (cards.Count == 4 ? 20f : 17f);
            int plateFontSize = cards.Count <= 3 ? 11 : (cards.Count == 4 ? 10 : 9);

            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                var itemObj = new GameObject($"CardPlate_{card.Id}");
                itemObj.transform.SetParent(container, false);
                var itemRt = itemObj.AddComponent<RectTransform>();
                itemRt.sizeDelta = new Vector2(0, plateHeight);

                var le = itemObj.AddComponent<LayoutElement>();
                le.minHeight = plateHeight - 2f;
                le.preferredHeight = plateHeight;
                le.flexibleWidth = 1f;

                var bg = itemObj.AddComponent<Image>();
                bg.sprite = WankulUiKit.WhiteSprite;
                bg.color = isPlayer ? new Color(0.12f, 0.18f, 0.28f, 0.95f) : new Color(0.24f, 0.12f, 0.16f, 0.95f);

                var outline = itemObj.AddComponent<Outline>();
                outline.effectColor = isPlayer ? new Color(0.25f, 0.55f, 0.85f, 0.5f) : new Color(0.85f, 0.30f, 0.35f, 0.5f);
                outline.effectDistance = new Vector2(1f, -1f);

                // Miniature sprite
                var sprite = CardAdapter.GetCardSprite(card);
                if (sprite != null)
                {
                    var iconObj = new GameObject("Icon");
                    iconObj.transform.SetParent(itemObj.transform, false);
                    var iconRt = iconObj.AddComponent<RectTransform>();
                    iconRt.anchorMin = new Vector2(0.02f, 0.08f);
                    iconRt.anchorMax = new Vector2(0.12f, 0.92f);
                    iconRt.offsetMin = Vector2.zero;
                    iconRt.offsetMax = Vector2.zero;
                    var iconImg = iconObj.AddComponent<Image>();
                    iconImg.sprite = sprite;
                    iconImg.preserveAspect = true;
                }

                // Nom du personnage
                var nameObj = new GameObject("Name");
                nameObj.transform.SetParent(itemObj.transform, false);
                var nameRt = nameObj.AddComponent<RectTransform>();
                nameRt.anchorMin = new Vector2(sprite != null ? 0.14f : 0.04f, 0f);
                nameRt.anchorMax = new Vector2(0.68f, 1f);
                nameRt.offsetMin = Vector2.zero;
                nameRt.offsetMax = Vector2.zero;
                var nameTxt = nameObj.AddComponent<Text>();
                nameTxt.font = font;
                nameTxt.fontSize = plateFontSize;
                nameTxt.fontStyle = FontStyle.Bold;
                nameTxt.alignment = TextAnchor.MiddleLeft;
                nameTxt.color = Color.white;
                nameTxt.text = CardAdapter.FixMojibake(card.Name);

                // Stats Force & Scoreur
                var statsObj = new GameObject("Stats");
                statsObj.transform.SetParent(itemObj.transform, false);
                var statsRt = statsObj.AddComponent<RectTransform>();
                statsRt.anchorMin = new Vector2(0.68f, 0f);
                statsRt.anchorMax = new Vector2(0.97f, 1f);
                statsRt.offsetMin = Vector2.zero;
                statsRt.offsetMax = Vector2.zero;
                var statsTxt = statsObj.AddComponent<Text>();
                statsTxt.font = font;
                statsTxt.fontSize = plateFontSize;
                statsTxt.fontStyle = FontStyle.Bold;
                statsTxt.alignment = TextAnchor.MiddleRight;
                string star = card.IsScoreur ? " <color=#FFE080>★</color>" : "";
                string forceColor = isPlayer ? "#55C5FF" : "#FF7777";
                statsTxt.text = $"<color={forceColor}>⚡ {card.Force}</color>{star}";

                // Hover pour inspecter au clic droit n'importe quel personnage sur le terrain !
                var trigger = itemObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                enterEntry.callback.AddListener((data) => { _hoveredCard = card; });
                trigger.triggers.Add(enterEntry);

                var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                exitEntry.callback.AddListener((data) => { if (_hoveredCard == card) _hoveredCard = null; });
                trigger.triggers.Add(exitEntry);
            }
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
                if (_inspectorPanel != null && _inspectorPanel.activeSelf)
                {
                    HideInspector();
                }
                else if (_selectedHandCard != null)
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

            // Clic droit : inspecter la carte survolée ou sélectionnée, ou fermer si déjà ouvert
            if (Input.GetMouseButtonDown(1))
            {
                if (_inspectorPanel != null && _inspectorPanel.activeSelf)
                {
                    HideInspector();
                }
                else
                {
                    var cardToInspect = _hoveredCard ?? _selectedHandCard;
                    if (cardToInspect != null)
                    {
                        ShowInspector(cardToInspect);
                    }
                }
            }
        }

        private void ShowInspector(DuelCard card)
        {
            if (_inspectorPanel == null || card == null) return;

            var wankulCard = CardAdapter.GetWankulCard(card);
            var sprite = CardAdapter.GetCardSprite(card);

            if (_inspectorCardImage != null)
            {
                if (sprite != null)
                {
                    _inspectorCardImage.sprite = sprite;
                    _inspectorCardImage.color = Color.white;
                }
                else
                {
                    _inspectorCardImage.sprite = WankulUiKit.WhiteSprite;
                    _inspectorCardImage.color = new Color(0.2f, 0.25f, 0.35f);
                }
            }

            if (_inspectorTitleText != null)
            {
                _inspectorTitleText.text = CardAdapter.FixMojibake(card.Name);
            }

            if (_inspectorTypeText != null)
            {
                string typeLabel = card.Kind == CardKind.Terrain
                    ? "[TERRAIN]"
                    : (card.IsScoreur ? "★ [SCOREUR]" : "[PERSONNAGE]");
                _inspectorTypeText.text = typeLabel;
            }

            if (_inspectorForceText != null)
            {
                _inspectorForceText.text = card.Kind == CardKind.Terrain
                    ? "LIEU DUEL"
                    : $"FORCE : {card.Force}";
            }

            if (_inspectorDescText != null)
            {
                string desc = "";
                if (wankulCard != null)
                {
                    if (wankulCard is EffigyCardData effigy)
                    {
                        if (!string.IsNullOrEmpty(effigy.Rules))
                            desc += $"<b>Règles :</b> {CardAdapter.FixMojibake(effigy.Rules)}\n\n";
                        if (!string.IsNullOrEmpty(effigy.Combo))
                            desc += $"<b>Combo :</b> {CardAdapter.FixMojibake(effigy.Combo)}\n\n";
                        if (!string.IsNullOrEmpty(effigy.Quote))
                            desc += $"<i>« {CardAdapter.FixMojibake(effigy.Quote)} »</i>";
                    }
                    else if (wankulCard is TerrainCardData terrain)
                    {
                        if (!string.IsNullOrEmpty(terrain.SpecialEffect))
                            desc += $"<b>Effet :</b> {CardAdapter.FixMojibake(terrain.SpecialEffect)}\n\n";
                        if (!string.IsNullOrEmpty(terrain.WinningEffect))
                            desc += $"<b>Victoire :</b> {CardAdapter.FixMojibake(terrain.WinningEffect)}\n\n";
                        if (!string.IsNullOrEmpty(terrain.LosingEffect))
                            desc += $"<b>Défaite :</b> {CardAdapter.FixMojibake(terrain.LosingEffect)}";
                    }
                }

                if (string.IsNullOrEmpty(desc))
                {
                    desc = card.IsScoreur
                        ? "<b>SCOREUR :</b> Lorsque cette carte est déployée sur un Terrain actif redressé, résout immédiatement le duel sur ce lieu."
                        : "Aucune règle spéciale inscrite sur cette carte.";
                }

                _inspectorDescText.text = desc;
            }

            _inspectorPanel.SetActive(true);
            DuelSfx.PlaySelectCard();
        }

        private void HideInspector()
        {
            if (_inspectorPanel != null)
            {
                _inspectorPanel.SetActive(false);
                DuelSfx.PlayCancel();
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
                    TriggerScoreClashAnimation(score.SlotIndex, score.ForceP1, score.ForceP2, score.Winner);
                    break;
                case TerrainClearedEvent:
                    DuelSfx.PlayTerrainCleared();
                    break;
                case EffectTriggeredEvent effectEvt:
                    string effPlayer = effectEvt.Player == PlayerId.Player1 ? "Votre" : "Adversaire :";
                    string effCardName = CardAdapter.FixMojibake(effectEvt.SourceCard.Name);
                    _statusMessageText.text = $"⚡ <b>EFFET ACTIVÉ :</b> {effPlayer} [{effCardName}] active son pouvoir !";
                    DuelSfx.PlayPowerup();
                    break;
                case CardsMilledEvent milled:
                    string millPlayer = milled.TargetPlayer == PlayerId.Player1 ? "Le joueur" : "L'adversaire";
                    _statusMessageText.text = $"🂠 <b>MEULE :</b> {millPlayer} défausse {milled.MilledCards.Count} carte(s) du dessus de son deck !";
                    DuelSfx.PlayDiscard();
                    break;
                case CardsDiscardedEvent discarded:
                    string discPlayer = discarded.Player == PlayerId.Player1 ? "Le joueur" : "L'adversaire";
                    _statusMessageText.text = $"🗑 <b>DÉFAUSSE :</b> {discPlayer} a défaussé {discarded.DiscardedCards.Count} carte(s) !";
                    DuelSfx.PlayDiscard();
                    break;
                case DuelEndedEvent ended:
                    DuelSfx.PlayGameOver(ended.Winner == PlayerId.Player1);
                    ShowGameOverModal(ended.Winner, ended.Reason);
                    break;
                case PlayerChoiceRequiredEvent choiceEvt:
                    if (choiceEvt.Player == PlayerId.Player1)
                    {
                        ShowCardChoiceModal(choiceEvt);
                    }
                    else
                    {
                        // Pour l'IA, sélectionne automatiquement le premier candidat valide
                        if (choiceEvt.Candidates != null && choiceEvt.Candidates.Count > 0)
                        {
                            choiceEvt.OnCardSelected?.Invoke(choiceEvt.Candidates[0]);
                        }
                    }
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

        private void TriggerScoreClashAnimation(int slotIndex, int forceP1, int forceP2, PlayerId? winner)
        {
            if (_clashBarPanel == null) return;
            if (_clashAnimationRoutine != null) StopCoroutine(_clashAnimationRoutine);
            _clashAnimationRoutine = StartCoroutine(ScoreClashRoutine(slotIndex, forceP1, forceP2, winner));
        }

        private System.Collections.IEnumerator ScoreClashRoutine(int slotIndex, int forceP1, int forceP2, PlayerId? winner)
        {
            _clashBarPanel.SetActive(true);

            if (_clashTitleText != null)
            {
                string locName = (slotIndex >= 0 && slotIndex < 3 && _engine.State.Slots[slotIndex].Card != null)
                    ? CardAdapter.FixMojibake(_engine.State.Slots[slotIndex].Card!.Name)
                    : $"Slot {slotIndex + 1}";
                _clashTitleText.text = $"⚔️ DÉCLENCHEMENT DUEL — {locName.ToUpper()}";
            }

            if (_clashP1ForceText != null) _clashP1ForceText.text = $"JOUEUR : {forceP1}";
            if (_clashP2ForceText != null) _clashP2ForceText.text = $"ADVERSAIRE : {forceP2}";

            // Calcul du ratio de tir à la corde
            int totalForce = forceP1 + forceP2;
            float targetRatio = 0.5f;
            if (totalForce > 0)
            {
                targetRatio = Mathf.Clamp((float)forceP1 / totalForce, 0.05f, 0.95f);
            }

            if (_clashDominanceText != null)
            {
                if (winner == PlayerId.Player1)
                {
                    int diff = forceP1 - forceP2;
                    _clashDominanceText.text = $"<color=#55FF88>★ VICTOIRE JOUEUR (+{diff} Puissance) • +1 Terrain Conquis !</color>";
                }
                else if (winner == PlayerId.Player2)
                {
                    int diff = forceP2 - forceP1;
                    _clashDominanceText.text = $"<color=#FF6666>▲ VICTOIRE ADVERSAIRE (+{diff} Puissance)</color>";
                }
                else
                {
                    _clashDominanceText.text = "<color=#FFFF88>ÉGALITÉ PARFAITE — Aucun point marqué</color>";
                }
            }

            // Animation fluide de la jauge (0.6s)
            float elapsed = 0f;
            float duration = 0.6f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curRatio = Mathf.Lerp(0.5f, targetRatio, t);

                if (_clashP1Fill != null)
                {
                    _clashP1Fill.anchorMax = new Vector2(curRatio, 1f);
                }
                if (_clashP2Fill != null)
                {
                    _clashP2Fill.anchorMin = new Vector2(curRatio, 0f);
                }
                if (_clashCursor != null)
                {
                    _clashCursor.anchorMin = new Vector2(curRatio, 0.5f);
                    _clashCursor.anchorMax = new Vector2(curRatio, 0.5f);
                }
                yield return null;
            }

            // Pause d'observation dramatique
            yield return new WaitForSeconds(1.0f);

            _clashBarPanel.SetActive(false);
            _clashAnimationRoutine = null;
        }

        private void ShowCardChoiceModal(PlayerChoiceRequiredEvent choiceEvt)
        {
            if (choiceEvt == null || choiceEvt.Candidates == null || choiceEvt.Candidates.Count == 0)
            {
                choiceEvt?.OnCardSelected?.Invoke(null);
                return;
            }

            var font = WankulUiKit.GetFont();

            var modalObj = new GameObject("CardChoiceModal");
            modalObj.transform.SetParent(_canvasObj.transform, false);
            var mRt = modalObj.AddComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.12f, 0.15f);
            mRt.anchorMax = new Vector2(0.88f, 0.85f);
            mRt.offsetMin = Vector2.zero;
            mRt.offsetMax = Vector2.zero;

            var bg = modalObj.AddComponent<Image>();
            bg.sprite = WankulUiKit.WhiteSprite;
            bg.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

            var outline = modalObj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.85f, 0.35f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Titre & Description
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(modalObj.transform, false);
            var tRt = titleObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.04f, 0.86f);
            tRt.anchorMax = new Vector2(0.96f, 0.98f);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            var tText = titleObj.AddComponent<Text>();
            tText.font = font;
            tText.fontSize = 20;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(1f, 0.88f, 0.4f);
            tText.text = CardAdapter.FixMojibake(choiceEvt.Title);

            // Conteneur horizontal de cartes avec défilement/espacement
            var containerObj = new GameObject("CardsList");
            containerObj.transform.SetParent(modalObj.transform, false);
            var cRt = containerObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.04f, 0.08f);
            cRt.anchorMax = new Vector2(0.96f, 0.84f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            containerObj.AddComponent<RectMask2D>();

            var hLayout = containerObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 15;
            hLayout.padding = new RectOffset(10, 10, 10, 10);
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;

            for (int i = 0; i < choiceEvt.Candidates.Count; i++)
            {
                var card = choiceEvt.Candidates[i];
                var cardBtnObj = new GameObject($"ChoiceCard_{card.Id}");
                cardBtnObj.transform.SetParent(containerObj.transform, false);
                var btnRt = cardBtnObj.AddComponent<RectTransform>();
                btnRt.sizeDelta = new Vector2(140, 200);

                var le = cardBtnObj.AddComponent<LayoutElement>();
                le.minWidth = 140f;
                le.minHeight = 200f;
                le.preferredWidth = 140f;
                le.preferredHeight = 200f;

                var img = cardBtnObj.AddComponent<Image>();
                var sprite = CardAdapter.GetCardSprite(card);
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.color = Color.white;
                    img.preserveAspect = true;
                }
                else
                {
                    img.sprite = WankulUiKit.WhiteSprite;
                    img.color = new Color(0.18f, 0.24f, 0.35f);
                }

                var btn = cardBtnObj.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    DuelSfx.PlaySelectCard();
                    Destroy(modalObj);
                    choiceEvt.OnCardSelected?.Invoke(card);
                    RefreshView();
                });

                var cardOutline = cardBtnObj.AddComponent<Outline>();
                cardOutline.effectColor = new Color(0.4f, 0.85f, 1f, 0.8f);
                cardOutline.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }
    }
}
