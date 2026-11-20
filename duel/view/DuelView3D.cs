using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WankulCrazy.Duel;
using WankulCrazy.Duel.Engine;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.importer;

namespace WankulCrazyPlugin.duel
{
    /// <summary>
    /// Vue Full 3D officielle du Duel Wankul TCG jouée directement sur <see cref="PlayTableGame"/>.
    /// Orchestre le moteur pur <see cref="DuelEngine"/>, l'IA <see cref="DuelAI"/>,
    /// l'interaction 3D (Clic-Clic + Drag &amp; Drop), la caméra plongeante dynamique et le HUD léger.
    /// </summary>
    public sealed class DuelView3D : MonoBehaviour
    {
        public static DuelView3D ActiveInstance { get; private set; }

        private const float DefaultCameraFov = 44f;
        private const float MinCameraFov = 30f;
        private const float MaxCameraFov = 50f;
        private const float ClashZoomFov = 34f;
        private const float DragThresholdPixels = 10f;

        private PlayTableGame _playTable;
        private DuelEngine _engine;
        private DuelAI _ai;
        private DuelBoard3D _board;
        private DuelCard3dManager _cards;

        // Caméra plongeante dynamique
        private Camera _rayCamera;
        private Transform _camFocusTarget;
        private float _userFov = DefaultCameraFov;
        private float _activeTargetFov = DefaultCameraFov;
        private bool _isClashZoomActive;
        private Vector3 _clashFocusWorldPos;

        // État d'interaction 3D
        private readonly RaycastHit[] _rayHits = new RaycastHit[32];
        private string _selectedCardId;
        private string _hoveredCardId;
        private int _hoveredSlotIndex = -1;
        private int _hoveredInsertIndex = -1;
        private int _hoveredDiscardPile = 0;
        private bool _isPointerDown;
        private bool _isDragging;
        private string _pointerDownCardId;
        private Vector2 _pointerDownScreenPos;

        // État tour IA / choix / clôture
        private bool _isAITurnRunning;
        private bool _isClosing;
        private GameObject _activeChoiceModal;
        private readonly Queue<Action> _pendingModalQueue = new Queue<Action>();
        private PlayerSlotChoiceRequiredEvent _pendingSlotChoice;
        private int _clashHighlightSlot = -1;
        private Coroutine _clashCoroutine;

        // Badges de coût effectif sur la main P1
        private sealed class HandCostBadge
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Bg;
            public Outline Outline;
            public Text Label;
        }
        private RectTransform _hudCanvasRt;
        private Transform _handCostBadgesRoot;
        private readonly List<HandCostBadge> _handCostBadges = new List<HandCostBadge>();

        // HUD périphérique léger
        private GameObject _hudCanvasObj;
        private Text _scoreP1Text;
        private Text _scoreP2Text;
        private Text _turnBannerText;
        private Text _hintText;
        private Text _charsPlayedText;
        private Text _logFeedText;
        private Button _endTurnBtn;
        private Image _endTurnBtnBg;
        private readonly Queue<string> _recentLogs = new Queue<string>();

        // Score Clash Overlay
        private GameObject _clashBarPanel;
        private RectTransform _clashP1Fill;
        private RectTransform _clashP2Fill;
        private RectTransform _clashCursor;
        private Text _clashP1ScoreText;
        private Text _clashP2ScoreText;
        private Text _clashDominanceText;

        // Inspecteur HD (Clic droit) & passifs activables
        private GameObject _inspectorPanel;
        private Image _inspectorCardImg;
        private Text _inspectorTitleText;
        private Text _inspectorStatsText;
        private Text _inspectorEffectText;
        private Button _inspectorActivateBtn;
        private RectTransform _inspectorActivateBtnRt;
        private Text _inspectorCloseHintText;
        private string _inspectorActivatableCardId;
        private int _inspectorActivatableSlotIdx = -1;

        private static readonly HashSet<string> ActivatableBoardPassives = new HashSet<string>(StringComparer.Ordinal)
        {
            "passive_sacrifice_take_top_discard_3",
            "passive_sacrifice_boost_other_3_turn",
            "passive_sacrifice_to_score_1",
            "passive_sacrifice_to_score_up_to_2",
            "passive_banish_self_to_score_if_not_played_this_turn",
            "passive_discard_self_play_free_char",
            "passive_discard_reduce_both_scores_1",
            "passive_own_turn_discard_self_boost_2",
            "passive_opp_turn_discard_self_debuff_2",
            "passive_once_turn_discard_2_double_force",
            "passive_once_turn_if_fewer_won_terrains_score",
            "passive_once_per_turn_hand_to_bottom_deck",
            "passive_once_per_turn_if_no_facedown_discard_to_bottom_deck",
            "passive_once_turn_spend_score_banish_char",
            "passive_swap_with_hand_char_and_discard",
            "passive_end_draw_sacrifice_draw_2",
            "passive_once_turn_if_alone_discard_to_bottom_deck",
            "passive_once_per_turn_add_bottom_deck_to_hand",
            "passive_once_per_turn_move_clown",
            "passive_banish_hand_1_search_copy_on_slot",
            "duo_flantier",
            "duo_aventuriers_manoir"
        };

        private static readonly HashSet<string> ActivatableTerrainPassives = new HashSet<string>(StringComparer.Ordinal)
        {
            "passive_terrain_banish_2_hand_extra_play",
            "passive_once_per_turn_discard_own_play_free",
            "passive_once_per_turn_swap_higher_force_hand"
        };

        /// <summary>
        /// Lance une session Full 3D Wankul TCG directement sur la table de jeu active.
        /// </summary>
        public static DuelView3D Show(
            PlayTableGame game,
            List<DuelCard> playerDeck,
            List<DuelCard> opponentDeck,
            PlayerId startingPlayer)
        {
            if (game == null)
            {
                Plugin.Logger.LogError("[DuelView3D] PlayTableGame est null, impossible de lancer le duel.");
                return null;
            }

            if (ActiveInstance != null)
            {
                Destroy(ActiveInstance.gameObject);
                ActiveInstance = null;
            }

            var rootObj = new GameObject("WankulDuelView3D");
            var view = rootObj.AddComponent<DuelView3D>();
            ActiveInstance = view;
            view.Initialize(game, playerDeck, opponentDeck, startingPlayer);
            return view;
        }

        private void Initialize(
            PlayTableGame game,
            List<DuelCard> playerDeck,
            List<DuelCard> opponentDeck,
            PlayerId startingPlayer)
        {
            _playTable = game;
            _engine = new DuelEngine(new SystemRandomAdapter());
            _engine.RequireInteractiveCostPayment = true;
            _ai = new DuelAI(PlayerId.Player2, new SystemRandomAdapter());

            _board = new DuelBoard3D(_playTable, transform);
            _cards = new DuelCard3dManager(transform);

            var ipc = CSingleton<InteractionPlayerController>.Instance;
            _rayCamera = (ipc != null && ipc.m_Cam != null) ? ipc.m_Cam : Camera.main;

            var focusGo = new GameObject("DuelCamFocusTarget");
            focusGo.transform.SetParent(transform, false);
            focusGo.transform.position = _board.TableCenter;
            _camFocusTarget = focusGo.transform;

            if (ipc != null)
            {
                ipc.StartAimLookAt(_camFocusTarget, 7f);
                if (ipc.m_CameraFOVController != null)
                {
                    _userFov = DefaultCameraFov;
                    _activeTargetFov = DefaultCameraFov;
                    ipc.m_CameraFOVController.StartInterpolateToFOV(DefaultCameraFov);
                }
            }

            BuildHudCanvas();
            BuildInspectorOverlay();
            BuildScoreClashOverlay();

            _engine.OnEvent += HandleDuelEvent;

            DuelSfx.StartDuelMusic();
            PushLog("Début du duel Wankul TCG (Table 3D) !");

            var startResult = _engine.StartDuel(playerDeck, opponentDeck, startingPlayer);
            if (!startResult.Success)
            {
                Plugin.Logger.LogWarning($"[DuelView3D] StartDuel refusé: {startResult.ErrorReason}");
            }

            if (!_engine.State.IsGameOver)
            {
                _engine.StartTurn();
            }

            RefreshAllVisuals();

            if (!_engine.State.IsGameOver && _engine.State.ActivePlayer == PlayerId.Player2)
            {
                StartCoroutine(ExecuteAITurnRoutine());
            }
        }

        private void Update()
        {
            if (_isClosing || _engine == null) return;

            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Masque le HUD de boutique (argent/heure/niveau) en haut de l'écran pendant le duel
            var gameUi = CSingleton<GameUIScreen>.Instance;
            if (gameUi != null && gameUi.m_CanvasGrp != null && gameUi.m_CanvasGrp.alpha > 0.01f)
            {
                gameUi.m_CanvasGrp.alpha = 0f;
            }

            UpdateDynamicCamera(Time.deltaTime);

            // Raccourcis clavier pratiques
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_inspectorPanel != null && _inspectorPanel.activeSelf)
                {
                    CloseInspector();
                    return;
                }
                if (_selectedCardId != null)
                {
                    _selectedCardId = null;
                    DuelSfx.PlayCancel();
                    RefreshBoardAndCardsOnly();
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.Space) && CanPlayerAct() && _engine != null && !_engine.State.TerrainRequirementPending)
            {
                OnEndTurnClicked();
                return;
            }

            Handle3DTableInteraction();

            if (_cards != null && _board != null)
            {
                _cards.TickAnimations(Time.deltaTime, _board.TableUp);
            }

            UpdateHandCostBadges();
        }

        private void OnDestroy()
        {
            if (ActiveInstance == this)
            {
                ActiveInstance = null;
            }

            if (_engine != null)
            {
                _engine.OnEvent -= HandleDuelEvent;
            }

            RestoreCamera();
            RestoreShopGameUI();

            if (_cards != null) _cards.CleanupAll();
            if (_board != null) _board.Cleanup();
            if (_hudCanvasObj != null) Destroy(_hudCanvasObj);
            DuelSfx.StopDuelMusic();
        }

        private static void RestoreShopGameUI()
        {
            try
            {
                var gameUi = CSingleton<GameUIScreen>.Instance;
                if (gameUi != null && gameUi.m_CanvasGrp != null)
                {
                    gameUi.m_CanvasGrp.alpha = 1f;
                }
            }
            catch { }
        }

        // =====================================================================
        // Caméra plongeante + Zoom molette & Clash
        // =====================================================================

        private void UpdateDynamicCamera(float dt)
        {
            if (_rayCamera == null)
            {
                var ipcFallback = CSingleton<InteractionPlayerController>.Instance;
                _rayCamera = (ipcFallback != null && ipcFallback.m_Cam != null) ? ipcFallback.m_Cam : Camera.main;
            }

            bool blockZoom = (_inspectorPanel != null && _inspectorPanel.activeSelf) || _activeChoiceModal != null;
            if (!blockZoom)
            {
                float scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _userFov = Mathf.Clamp(_userFov - scroll * 2.2f, MinCameraFov, MaxCameraFov);
                }
            }

            float desiredFov = _isClashZoomActive ? Mathf.Min(_userFov, ClashZoomFov) : _userFov;
            if (Mathf.Abs(desiredFov - _activeTargetFov) > 0.05f)
            {
                _activeTargetFov = desiredFov;
                var ipc = CSingleton<InteractionPlayerController>.Instance;
                if (ipc != null && ipc.m_CameraFOVController != null)
                {
                    ipc.m_CameraFOVController.StartInterpolateToFOV(_activeTargetFov);
                }
            }

            if (_camFocusTarget != null && _board != null)
            {
                Vector3 goalFocus = _isClashZoomActive
                    ? Vector3.Lerp(_board.TableCenter, _clashFocusWorldPos, 0.45f)
                    : _board.TableCenter;
                _camFocusTarget.position = Vector3.Lerp(_camFocusTarget.position, goalFocus, 1f - Mathf.Exp(-7f * dt));
            }
        }

        private void RestoreCamera()
        {
            try
            {
                var ipc = CSingleton<InteractionPlayerController>.Instance;
                if (ipc != null)
                {
                    if (_playTable != null && _playTable.m_CamDownTarget != null)
                    {
                        ipc.StartAimLookAt(_playTable.m_CamDownTarget, 8f);
                    }
                    if (ipc.m_CameraFOVController != null)
                    {
                        ipc.m_CameraFOVController.StopInterpolateFOV();
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[DuelView3D] RestoreCamera warning: {ex.Message}");
            }
        }

        // =====================================================================
        // Raycast 3D : Hover, Clic-Clic, Drag & Drop, Clic Droit Inspecteur
        // =====================================================================

        private bool CanPlayerAct()
        {
            return !_isClosing
                && _engine != null
                && !_engine.State.IsGameOver
                && _engine.State.ActivePlayer == PlayerId.Player1
                && !_isAITurnRunning
                && _activeChoiceModal == null
                && _pendingSlotChoice == null
                && (_inspectorPanel == null || !_inspectorPanel.activeSelf);
        }

        private void Handle3DTableInteraction()
        {
            if (_rayCamera == null || _board == null || _cards == null) return;

            bool pointerOverBlockingHud = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            // Mise à jour du raycast sous le curseur
            string newHoveredCard = null;
            int newHoveredSlot = -1;
            int newHoveredInsertIndex = -1;
            int newHoveredDiscardPile = 0;
            Vector3 slotHitPoint = Vector3.zero;

            if (!pointerOverBlockingHud)
            {
                Ray ray = _rayCamera.ScreenPointToRay(Input.mousePosition);
                int hitCount = Physics.RaycastNonAlloc(ray, _rayHits, 25f, ~0, QueryTriggerInteraction.Collide);

                float bestHandDist = float.MaxValue;
                float bestBoardCardDist = float.MaxValue;
                float bestSlotDist = float.MaxValue;
                DuelCard3dManager.CardEntry bestHandCard = null;
                DuelCard3dManager.CardEntry bestBoardCard = null;

                for (int i = 0; i < hitCount; i++)
                {
                    var col = _rayHits[i].collider;
                    if (col == null) continue;
                    float dist = _rayHits[i].distance;

                    int discPile = _board.ResolveDiscardPileCollider(col);
                    if (discPile != 0)
                    {
                        newHoveredDiscardPile = discPile;
                        continue;
                    }

                    var cEntry = _cards.ResolveCollider(col);
                    if (cEntry != null && cEntry.CardId != _pointerDownCardId)
                    {
                        if (cEntry.Zone == DuelCard3dManager.ZoneKind.HandP1)
                        {
                            if (dist < bestHandDist)
                            {
                                bestHandDist = dist;
                                bestHandCard = cEntry;
                            }
                        }
                        else
                        {
                            if (dist < bestBoardCardDist)
                            {
                                bestBoardCardDist = dist;
                                bestBoardCard = cEntry;
                                slotHitPoint = _rayHits[i].point;
                            }
                        }
                        continue;
                    }

                    int sIdx = _board.ResolveSlotCollider(col);
                    if (sIdx >= 0 && dist < bestSlotDist)
                    {
                        bestSlotDist = dist;
                        newHoveredSlot = sIdx;
                        slotHitPoint = _rayHits[i].point;
                    }
                }

                // Priorité aux cartes en main si non en drag, sinon carte plateau puis slot
                if (!_isDragging && bestHandCard != null)
                {
                    newHoveredCard = bestHandCard.CardId;
                    newHoveredDiscardPile = 0; // Une carte en main masque la défausse située en dessous
                }
                else if (!_isDragging && bestBoardCard != null)
                {
                    newHoveredCard = bestBoardCard.CardId;
                    if (newHoveredSlot < 0 && bestBoardCard.SlotIndex >= 0)
                    {
                        newHoveredSlot = bestBoardCard.SlotIndex;
                    }
                }

                if (newHoveredSlot >= 0 && _engine != null && newHoveredSlot < _engine.State.Slots.Count)
                {
                    int alliedCount = _engine.State.Slots[newHoveredSlot].CharactersP1.Count;
                    newHoveredInsertIndex = _board.ComputeInsertIndexForSlot(newHoveredSlot, slotHitPoint, alliedCount);
                }
            }

            bool hoverChanged = newHoveredCard != _hoveredCardId ||
                                newHoveredSlot != _hoveredSlotIndex ||
                                newHoveredInsertIndex != _hoveredInsertIndex ||
                                newHoveredDiscardPile != _hoveredDiscardPile;
            _hoveredCardId = newHoveredCard;
            _hoveredSlotIndex = newHoveredSlot;
            _hoveredInsertIndex = newHoveredInsertIndex;
            _hoveredDiscardPile = newHoveredDiscardPile;
            if (hoverChanged && !_isDragging)
            {
                RefreshBoardAndCardsOnly();
            }

            // Clic droit : fermer ou ouvrir l'Inspecteur HD sur n'importe quelle carte visible
            if (Input.GetMouseButtonDown(1))
            {
                if (_inspectorPanel != null && _inspectorPanel.activeSelf)
                {
                    CloseInspector();
                    return;
                }

                if (!string.IsNullOrEmpty(_hoveredCardId))
                {
                    var hoveredEntry = _cards.GetEntry(_hoveredCardId);
                    if (hoveredEntry != null && hoveredEntry.Zone != DuelCard3dManager.ZoneKind.HandP2 && hoveredEntry.Card != null)
                    {
                        OpenInspector(hoveredEntry.Card);
                        return;
                    }
                }

                if (_selectedCardId != null)
                {
                    _selectedCardId = null;
                    DuelSfx.PlayCancel();
                    RefreshBoardAndCardsOnly();
                }
                return;
            }

            // Si l'inspecteur est ouvert : un clic gauche le ferme (sauf sur le bouton Activer Capacité)
            if (_inspectorPanel != null && _inspectorPanel.activeSelf)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (!IsPointerOverInspectorActivateButton())
                    {
                        CloseInspector();
                    }
                }
                return;
            }

            // Choix interactif d'un slot 3D sur la table (effet nécessitant une cible Terrain)
            if (_pendingSlotChoice != null && !_isAITurnRunning && _activeChoiceModal == null)
            {
                if (!pointerOverBlockingHud && Input.GetMouseButtonDown(0) && _hoveredSlotIndex >= 0)
                {
                    var clickedSlot = _engine.State.Slots[_hoveredSlotIndex];
                    if (clickedSlot.IsEmpty)
                    {
                        DuelSfx.PlayCancel();
                        PushLog("Choisissez un emplacement occupé par un Terrain !");
                        if (_hintText != null) _hintText.text = "Cet emplacement est vide : cliquez sur un Terrain en jeu.";
                        return;
                    }

                    var evt = _pendingSlotChoice;
                    _pendingSlotChoice = null;
                    DuelSfx.PlaySelectCard();
                    evt.OnSlotSelected?.Invoke(_hoveredSlotIndex);
                    RefreshAllVisuals();
                }
                return;
            }

            if (!CanPlayerAct())
            {
                CancelActiveDrag();
                return;
            }

            // Clic gauche : prise de carte en main prioritaire, puis inspection Défausse, placement sur slot ou Score Automatique
            if (Input.GetMouseButtonDown(0) && !pointerOverBlockingHud)
            {
                if (!string.IsNullOrEmpty(_hoveredCardId))
                {
                    var entry = _cards.GetEntry(_hoveredCardId);
                    if (entry != null && entry.Zone == DuelCard3dManager.ZoneKind.HandP1)
                    {
                        _isPointerDown = true;
                        _isDragging = false;
                        _pointerDownCardId = entry.CardId;
                        _pointerDownScreenPos = Input.mousePosition;
                        return;
                    }
                }

                if (_hoveredDiscardPile == 1 && _engine.State.DiscardP1.Count > 0)
                {
                    DuelSfx.PlaySelectCard();
                    ShowCardViewerModal($"Votre Défausse ({_engine.State.DiscardP1.Count} cartes)", _engine.State.DiscardP1);
                    return;
                }
                if (_hoveredDiscardPile == 2 && _engine.State.DiscardP2.Count > 0)
                {
                    DuelSfx.PlaySelectCard();
                    ShowCardViewerModal($"Défausse Adversaire ({_engine.State.DiscardP2.Count} cartes)", _engine.State.DiscardP2);
                    return;
                }

                if (_hoveredSlotIndex >= 0 && !string.IsNullOrEmpty(_selectedCardId))
                {
                    TryPlayCardOnSlot(_selectedCardId, _hoveredSlotIndex, _hoveredInsertIndex);
                    return;
                }

                // Clic sur un Terrain Actif où le joueur atteint le seuil de Score Automatique
                if (_hoveredSlotIndex >= 0 && string.IsNullOrEmpty(_selectedCardId))
                {
                    var slot = _engine.State.Slots[_hoveredSlotIndex];
                    int effectiveThreshold = slot.GetEffectiveAutoScoreThreshold(
                        PlayerId.Player1,
                        _engine.Rules.AutoScoreForceThreshold,
                        _engine.State);
                    if (!slot.IsEmpty &&
                        slot.IsActive(_engine.State.TurnNumber) &&
                        slot.CharactersP1.Count > 0 &&
                        slot.GetForce(PlayerId.Player1, _engine.State) >= effectiveThreshold)
                    {
                        var autoRes = _engine.TriggerAutoScore(PlayerId.Player1, _hoveredSlotIndex);
                        if (autoRes.Success)
                        {
                            PushLog($"⚡ Score automatique déclenché sur Terrain {_hoveredSlotIndex + 1} !");
                            RefreshAllVisuals();
                            return;
                        }
                    }
                }

                // Clic sur un Personnage allié en jeu doté d'une capacité passive activable
                if (!string.IsNullOrEmpty(_hoveredCardId) && string.IsNullOrEmpty(_selectedCardId))
                {
                    var hoveredEntry = _cards.GetEntry(_hoveredCardId);
                    if (hoveredEntry != null &&
                        hoveredEntry.Zone == DuelCard3dManager.ZoneKind.CharacterP1 &&
                        hoveredEntry.Card != null &&
                        TryGetActivatableContext(hoveredEntry.Card, out int actSlotIdx, out string actCardId))
                    {
                        if (TryActivateCardPassiveFromUi(actCardId, actSlotIdx, hoveredEntry.Card.Name))
                        {
                            return;
                        }
                    }
                }

                // Clic dans le vide de la table -> désélectionner
                if (!string.IsNullOrEmpty(_selectedCardId))
                {
                    _selectedCardId = null;
                    DuelSfx.PlayCancel();
                    RefreshBoardAndCardsOnly();
                }
            }

            // Maintien : passage en Drag & Drop 3D si dépassement du seuil pixels
            if (_isPointerDown && Input.GetMouseButton(0) && !string.IsNullOrEmpty(_pointerDownCardId))
            {
                var dragEntry = _cards.GetEntry(_pointerDownCardId);
                if (dragEntry == null || dragEntry.Card3d == null)
                {
                    CancelActiveDrag();
                    return;
                }

                if (!_isDragging && Vector2.Distance(Input.mousePosition, _pointerDownScreenPos) > DragThresholdPixels)
                {
                    _isDragging = true;
                    _selectedCardId = _pointerDownCardId;
                    dragEntry.IsDragging = true;
                    DuelSfx.PlaySelectCard();
                    RefreshBoardAndCardsOnly();
                }

                if (_isDragging)
                {
                    UpdateDraggedCardTransform(dragEntry);
                    _board.UpdateBoardVisuals(_engine.State, _selectedCardId, _hoveredSlotIndex, _clashHighlightSlot, _hoveredInsertIndex);
                }
            }

            // Relâchement : déposer si Drag, sinon sélection Clic-Clic
            if (_isPointerDown && Input.GetMouseButtonUp(0))
            {
                string cardId = _pointerDownCardId;
                bool wasDragging = _isDragging;

                CancelActiveDrag();

                if (wasDragging)
                {
                    if (_hoveredSlotIndex >= 0)
                    {
                        TryPlayCardOnSlot(cardId, _hoveredSlotIndex, _hoveredInsertIndex);
                    }
                    else
                    {
                        _selectedCardId = null;
                        DuelSfx.PlayCancel();
                        RefreshAllVisuals();
                    }
                }
                else
                {
                    // Simple clic sur carte en main : basculer sélection
                    if (_selectedCardId == cardId)
                    {
                        _selectedCardId = null;
                        DuelSfx.PlayCancel();
                    }
                    else
                    {
                        _selectedCardId = cardId;
                        DuelSfx.PlaySelectCard();
                    }
                    RefreshBoardAndCardsOnly();
                }
            }
        }

        private void UpdateDraggedCardTransform(DuelCard3dManager.CardEntry entry)
        {
            Plane dragPlane = new Plane(_board.TableUp, _board.TableCenter + _board.TableUp * 0.095f);
            Ray ray = _rayCamera.ScreenPointToRay(Input.mousePosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Transform tr = entry.Card3d.transform;
                tr.position = Vector3.Lerp(tr.position, hitPoint, 1f - Mathf.Exp(-24f * Time.deltaTime));
                tr.rotation = Quaternion.Slerp(tr.rotation, _board.FaceUpFlatRot, 1f - Mathf.Exp(-16f * Time.deltaTime));
                entry.Card3d.SetTargetLocalScale(Vector3.one * 1.38f);
            }
        }

        private void CancelActiveDrag()
        {
            if (!string.IsNullOrEmpty(_pointerDownCardId))
            {
                var entry = _cards.GetEntry(_pointerDownCardId);
                if (entry != null) entry.IsDragging = false;
            }
            _isPointerDown = false;
            _isDragging = false;
            _pointerDownCardId = null;
        }

        // =====================================================================
        // Actions Joueur & Boucle Tour IA
        // =====================================================================

        private void TryPlayCardOnSlot(string cardId, int slotIndex, int insertIndex = -1)
        {
            if (!CanPlayerAct() || string.IsNullOrEmpty(cardId)) return;

            DuelCard card = null;
            var p1Hand = _engine.State.HandP1;
            for (int i = 0; i < p1Hand.Count; i++)
            {
                if (p1Hand[i].Id == cardId)
                {
                    card = p1Hand[i];
                    break;
                }
            }
            if (card == null)
            {
                _selectedCardId = null;
                RefreshAllVisuals();
                return;
            }

            DuelActionResult result;
            if (card.Kind == CardKind.Terrain)
            {
                result = _engine.PlayTerrain(PlayerId.Player1, card.Id, slotIndex);
            }
            else if (card.Kind == CardKind.Character)
            {
                result = _engine.PlayCharacter(PlayerId.Player1, card.Id, slotIndex, insertIndex);
            }
            else
            {
                result = DuelActionResult.Refused("Type de carte non supporté.");
            }

            if (result.Success)
            {
                _selectedCardId = null;
                RefreshAllVisuals();
            }
            else
            {
                DuelSfx.PlayCancel();
                PushLog($"Action impossible : {result.ErrorReason}");
                if (_hintText != null)
                {
                    _hintText.text = $"Refusé : {result.ErrorReason}";
                }
                RefreshBoardAndCardsOnly();
            }
        }

        private void OnEndTurnClicked()
        {
            if (!CanPlayerAct()) return;

            DuelSfx.PlayButtonClick();
            _selectedCardId = null;
            CancelActiveDrag();

            var res = _engine.EndTurn(PlayerId.Player1);
            if (!res.Success)
            {
                DuelSfx.PlayCancel();
                PushLog($"Fin de tour refusée : {res.ErrorReason}");
                RefreshBoardAndCardsOnly();
                return;
            }

            if (!_engine.State.IsGameOver && _engine.State.ActivePlayer == PlayerId.Player2)
            {
                _engine.StartTurn();
            }

            RefreshAllVisuals();

            if (!_engine.State.IsGameOver && _engine.State.ActivePlayer == PlayerId.Player2)
            {
                StartCoroutine(ExecuteAITurnRoutine());
            }
        }

        private IEnumerator ExecuteAITurnRoutine()
        {
            _isAITurnRunning = true;
            RefreshHudOnly();

            yield return new WaitForSeconds(0.55f);

            if (!_isClosing && _engine != null && !_engine.State.IsGameOver && _engine.State.ActivePlayer == PlayerId.Player2)
            {
                int safetySteps = 0;
                while (!_isClosing &&
                       _engine != null &&
                       !_engine.State.IsGameOver &&
                       _engine.State.ActivePlayer == PlayerId.Player2 &&
                       safetySteps < 10)
                {
                    while (!_isClosing && (_activeChoiceModal != null || _pendingModalQueue.Count > 0 || _pendingSlotChoice != null))
                    {
                        yield return null;
                    }
                    if (_isClosing || _engine == null || _engine.State.IsGameOver || _engine.State.ActivePlayer != PlayerId.Player2)
                    {
                        break;
                    }
                    if (!_ai.PlaySingleStep(_engine))
                    {
                        break;
                    }
                    safetySteps++;
                    RefreshAllVisuals();
                    yield return new WaitForSeconds(0.52f);
                }

                while (!_isClosing && (_activeChoiceModal != null || _pendingModalQueue.Count > 0 || _pendingSlotChoice != null))
                {
                    yield return null;
                }

                if (!_isClosing && _engine != null && !_engine.State.IsGameOver && _engine.State.ActivePlayer == PlayerId.Player2)
                {
                    _engine.EndTurn(PlayerId.Player2);
                }
                RefreshAllVisuals();
                yield return new WaitForSeconds(0.45f);

                while (!_isClosing && (_activeChoiceModal != null || _pendingModalQueue.Count > 0 || _pendingSlotChoice != null))
                {
                    yield return null;
                }

                if (!_isClosing && _engine != null && !_engine.State.IsGameOver && _engine.State.ActivePlayer == PlayerId.Player1)
                {
                    _engine.StartTurn();
                }
            }

            _isAITurnRunning = false;
            RefreshAllVisuals();
        }

        private void OnForfeitClicked()
        {
            if (_isClosing) return;
            DuelSfx.PlayButtonClick();
            ExitDuelAndReportResult(isPlayerWin: false);
        }

        // =====================================================================
        // Synchronisation Etat Moteur -> Objets 3D & HUD
        // =====================================================================

        private void RefreshAllVisuals()
        {
            if (_engine == null || _board == null || _cards == null) return;
            Sync3DCardsToState();
            RefreshBoardAndCardsOnly();
            RefreshHudOnly();
        }

        private void RefreshBoardAndCardsOnly()
        {
            if (_engine == null || _board == null || _cards == null) return;
            _cards.SetHoverState(_hoveredCardId, _selectedCardId);
            _board.UpdateBoardVisuals(_engine.State, _selectedCardId, _hoveredSlotIndex, _clashHighlightSlot, _hoveredInsertIndex);
            RefreshHudHintText();
        }

        private DuelCard FindCardInHandP1(string cardId)
        {
            if (_engine == null || string.IsNullOrEmpty(cardId)) return null;
            var hand = _engine.State.HandP1;
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].Id == cardId) return hand[i];
            }
            return null;
        }

        private void RefreshHudHintText()
        {
            if (_hintText == null || _engine == null) return;
            var s = _engine.State;

            if (_pendingSlotChoice != null)
            {
                _hintText.text = $"★ {_pendingSlotChoice.Title} — Cliquez directement sur un Terrain 3D";
                return;
            }

            if (s.TerrainRequirementPending && s.ActivePlayer == PlayerId.Player1)
            {
                _hintText.text = "Terrains insuffisants : posez un Terrain depuis votre main sur un emplacement libre.";
                return;
            }

            string focusId = !string.IsNullOrEmpty(_selectedCardId) ? _selectedCardId : _hoveredCardId;
            var focusCard = FindCardInHandP1(focusId);
            if (focusCard != null && focusCard.Kind == CardKind.Character)
            {
                int effCost = _engine.GetEffectivePlayCost(PlayerId.Player1, focusCard, _hoveredSlotIndex);
                string cardName = focusCard.Name;
                string costPart = focusCard.IsDuo
                    ? "DUO (Coût 0)"
                    : (effCost == 0 ? "Coût : 0" : $"Coût : {effCost} défausse(s)");
                bool hasHandActivate = HasEffectId(focusCard, "passive_banish_from_hand_free_play");
                string rightClickLabel = hasHandActivate ? "Clic droit : Inspecter / Activer" : "Clic droit : Inspecter";
                if (s.HandP1.Count - 1 < effCost)
                {
                    _hintText.text = $"{cardName} (Force {focusCard.Force}) • {costPart} — Main insuffisante ! • {rightClickLabel}";
                }
                else if (!string.IsNullOrEmpty(_selectedCardId))
                {
                    _hintText.text = $"{cardName} (Force {focusCard.Force}) • {costPart} • Cliquez sur un Terrain vert";
                }
                else
                {
                    _hintText.text = $"{cardName} (Force {focusCard.Force}) • {costPart} • Clic : Sélectionner • {rightClickLabel}";
                }
                return;
            }

            if (string.IsNullOrEmpty(_selectedCardId) && !string.IsNullOrEmpty(_hoveredCardId) && _cards != null)
            {
                var hovEntry = _cards.GetEntry(_hoveredCardId);
                if (hovEntry != null &&
                    (hovEntry.Zone == DuelCard3dManager.ZoneKind.CharacterP1 || hovEntry.Zone == DuelCard3dManager.ZoneKind.Terrain) &&
                    hovEntry.Card != null &&
                    TryGetActivatableContext(hovEntry.Card, out _, out _))
                {
                    string actionHint = hovEntry.Zone == DuelCard3dManager.ZoneKind.CharacterP1
                        ? "Clic ou Clic droit : Activer capacité"
                        : "Clic droit : Inspecter / Activer capacité";
                    _hintText.text = $"★ {hovEntry.Card.Name} • {actionHint}";
                    return;
                }
            }

            if (!string.IsNullOrEmpty(_selectedCardId))
            {
                _hintText.text = "Carte sélectionnée : cliquez sur un Terrain vert • Clic droit pour annuler";
            }
            else if (_hoveredSlotIndex >= 0 && _hoveredSlotIndex < s.Slots.Count)
            {
                var hovSlot = s.Slots[_hoveredSlotIndex];
                int effectiveThresh = hovSlot.GetEffectiveAutoScoreThreshold(PlayerId.Player1, _engine.Rules.AutoScoreForceThreshold, s);
                if (!hovSlot.IsEmpty &&
                    hovSlot.IsActive(s.TurnNumber) &&
                    hovSlot.CharactersP1.Count > 0 &&
                    hovSlot.GetForce(PlayerId.Player1, s) >= effectiveThresh)
                {
                    _hintText.text = $"⚡ Seuil Auto-Score ({effectiveThresh}) atteint sur Terrain {_hoveredSlotIndex + 1} — Cliquez pour scorer !";
                }
                else
                {
                    _hintText.text = "Clic ou Glisser-Déposer vers un Terrain • Molette : Zoom • Clic droit : Inspecter";
                }
            }
            else
            {
                _hintText.text = "Clic ou Glisser-Déposer vers un Terrain • Molette : Zoom • Clic droit : Inspecter";
            }
        }

        private void Sync3DCardsToState()
        {
            var state = _engine.State;
            var activeIds = new HashSet<string>();

            // 1. Collecte de toutes les cartes actuellement présentes sur table ou en main
            for (int i = 0; i < state.HandP1.Count; i++) activeIds.Add(state.HandP1[i].Id);
            for (int i = 0; i < state.HandP2.Count; i++) activeIds.Add(state.HandP2[i].Id);
            for (int s = 0; s < state.Slots.Count; s++)
            {
                var slot = state.Slots[s];
                if (slot.Card != null) activeIds.Add(slot.Card.Id);
                for (int c = 0; c < slot.CharactersP1.Count; c++) activeIds.Add(slot.CharactersP1[c].Id);
                for (int c = 0; c < slot.CharactersP2.Count; c++) activeIds.Add(slot.CharactersP2[c].Id);
            }

            // Suppression des cartes sorties (défaussées / bannies / conquises) avec vol en arc vers la Défausse
            var toRemove = new List<string>();
            foreach (var kv in _cards.EntriesById)
            {
                if (!activeIds.Contains(kv.Key)) toRemove.Add(kv.Key);
            }
            for (int i = 0; i < toRemove.Count; i++)
            {
                string removedId = toRemove[i];
                if (_selectedCardId == removedId) _selectedCardId = null;
                if (_hoveredCardId == removedId) _hoveredCardId = null;
                if (_pointerDownCardId == removedId) CancelActiveDrag();

                var existingEntry = _cards.GetEntry(removedId);
                bool isP2Card = existingEntry != null &&
                                (existingEntry.Zone == DuelCard3dManager.ZoneKind.HandP2 ||
                                 existingEntry.Zone == DuelCard3dManager.ZoneKind.CharacterP2);
                Vector3 discardTarget = isP2Card ? _board.DiscardPosP2 : _board.DiscardPosP1;
                _cards.DestroyCard(removedId, discardTarget, _board.FaceUpFlatRot);
            }

            // 2. Main Joueur 1 (eventail relevé, lisible face caméra + écartement dynamique au survol)
            int h1Count = state.HandP1.Count;
            int hoveredHandIdx = -1;
            if (!string.IsNullOrEmpty(_hoveredCardId))
            {
                for (int i = 0; i < h1Count; i++)
                {
                    if (state.HandP1[i].Id == _hoveredCardId)
                    {
                        hoveredHandIdx = i;
                        break;
                    }
                }
            }
            for (int i = 0; i < h1Count; i++)
            {
                var card = state.HandP1[i];
                var entry = _cards.SpawnOrGetCard(card, _board.DeckPosP1, _board.FaceDownFlatRot, 1.32f);
                if (entry != null)
                {
                    _cards.SetCardTarget(
                        entry,
                        DuelCard3dManager.ZoneKind.HandP1,
                        -1,
                        i,
                        _board.GetHandCardWorldPos(true, i, h1Count, hoveredHandIdx),
                        _board.HandP1Rot,
                        1.32f,
                        true,
                        false);
                }
            }

            // 3. Main Joueur 2 (face cachée côté adverse)
            int h2Count = state.HandP2.Count;
            for (int i = 0; i < h2Count; i++)
            {
                var card = state.HandP2[i];
                var entry = _cards.SpawnOrGetCard(card, _board.DeckPosP2, _board.FaceDownFlatRot, 1.15f);
                if (entry != null)
                {
                    _cards.SetCardTarget(
                        entry,
                        DuelCard3dManager.ZoneKind.HandP2,
                        -1,
                        i,
                        _board.GetHandCardWorldPos(false, i, h2Count),
                        _board.HandP2Rot,
                        1.15f,
                        false,
                        false);
                }
            }

            // 4. Terrains & Personnages dans les 3 Slots 3D
            for (int s = 0; s < state.Slots.Count; s++)
            {
                var slot = state.Slots[s];

                if (slot.Card != null)
                {
                    bool isActive = slot.IsActive(state.TurnNumber);
                    Vector3 spawnOrigin = slot.PlacedBy == PlayerId.Player1 ? _board.DeckPosP1 : _board.DeckPosP2;
                    var tEntry = _cards.SpawnOrGetCard(slot.Card, spawnOrigin, _board.FaceUpFlatRot, 1.24f);
                    if (tEntry != null)
                    {
                        _cards.SetCardTarget(
                            tEntry,
                            DuelCard3dManager.ZoneKind.Terrain,
                            s,
                            0,
                            _board.GetTerrainWorldPos(s),
                            _board.GetTerrainRotation(isActive),
                            1.24f,
                            true,
                            !isActive);
                    }
                }

                int p1Chars = slot.CharactersP1.Count;
                for (int c = 0; c < p1Chars; c++)
                {
                    var ch = slot.CharactersP1[c];
                    var cEntry = _cards.SpawnOrGetCard(ch, _board.DeckPosP1, _board.FaceUpFlatRot, 1.12f);
                    if (cEntry != null)
                    {
                        _cards.SetCardTarget(
                            cEntry,
                            DuelCard3dManager.ZoneKind.CharacterP1,
                            s,
                            c,
                            _board.GetCharacterWorldPos(s, true, c, p1Chars),
                            _board.FaceUpFlatRot,
                            1.12f,
                            true,
                            false);
                    }
                }

                int p2Chars = slot.CharactersP2.Count;
                for (int c = 0; c < p2Chars; c++)
                {
                    var ch = slot.CharactersP2[c];
                    var cEntry = _cards.SpawnOrGetCard(ch, _board.DeckPosP2, _board.FaceUpFlatRot, 1.12f);
                    if (cEntry != null)
                    {
                        _cards.SetCardTarget(
                            cEntry,
                            DuelCard3dManager.ZoneKind.CharacterP2,
                            s,
                            c,
                            _board.GetCharacterWorldPos(s, false, c, p2Chars),
                            _board.FaceUpFlatRot,
                            1.12f,
                            true,
                            false);
                    }
                }
            }

            // 5. Piles visuelles Pioche / Défausse
            DuelCard topDiscardP1 = state.DiscardP1.Count > 0 ? state.DiscardP1[state.DiscardP1.Count - 1] : null;
            DuelCard topDiscardP2 = state.DiscardP2.Count > 0 ? state.DiscardP2[state.DiscardP2.Count - 1] : null;

            _cards.UpdatePileStacks(true, state.DeckP1.Count, topDiscardP1, state.DiscardP1.Count,
                _board.DeckPosP1, _board.FaceDownFlatRot, _board.DiscardPosP1, _board.FaceUpFlatRot, _board.TableUp, 1.25f);
            _cards.UpdatePileStacks(false, state.DeckP2.Count, topDiscardP2, state.DiscardP2.Count,
                _board.DeckPosP2, _board.FaceDownFlatRot, _board.DiscardPosP2, _board.FaceUpFlatRot, _board.TableUp, 1.25f);
        }

        private void RefreshHudOnly()
        {
            if (_engine == null) return;
            var s = _engine.State;

            if (_scoreP1Text != null) _scoreP1Text.text = $"VOUS : ★ {s.ScoreP1} / {s.Rules.TerrainsToWin}";
            if (_scoreP2Text != null) _scoreP2Text.text = $"ADVERSAIRE : ★ {s.ScoreP2} / {s.Rules.TerrainsToWin}";
            if (_charsPlayedText != null) _charsPlayedText.text = $"Persos posés : {s.CharactersPlayedThisTurn} / 4";

            if (_turnBannerText != null)
            {
                if (s.IsGameOver)
                {
                    _turnBannerText.text = "PARTIE TERMINÉE";
                    _turnBannerText.color = new Color(1f, 0.88f, 0.4f);
                }
                else if (s.ActivePlayer == PlayerId.Player1)
                {
                    _turnBannerText.text = $"TOUR {s.TurnNumber} — À VOUS DE JOUER";
                    _turnBannerText.color = new Color(0.45f, 0.88f, 1f);
                }
                else
                {
                    _turnBannerText.text = $"TOUR {s.TurnNumber} — TOUR ADVERSAIRE";
                    _turnBannerText.color = new Color(1f, 0.52f, 0.52f);
                }
            }

            RefreshHudHintText();

            bool canAct = CanPlayerAct() && !s.TerrainRequirementPending;
            if (_endTurnBtn != null) _endTurnBtn.interactable = canAct;
            if (_endTurnBtnBg != null)
            {
                _endTurnBtnBg.color = canAct
                    ? new Color(0.70f, 0.18f, 0.22f, 0.96f)
                    : new Color(0.22f, 0.24f, 0.28f, 0.72f);
            }
        }

        // =====================================================================
        // Gestionnaire d'Événements Moteur
        // =====================================================================

        private void HandleDuelEvent(IDuelEvent evt)
        {
            DuelViewLogger.LogDuelEvent(evt);

            switch (evt)
            {
                case DuelStartedEvent started:
                    PushLog(started.StartingPlayer == PlayerId.Player1
                        ? "Vous commencez la partie."
                        : "L'adversaire commence la partie.");
                    break;

                case CardsDrawnEvent drawn:
                    DuelSfx.PlayDraw();
                    PushLog($"{(drawn.Player == PlayerId.Player1 ? "Vous piochez" : "L'IA pioche")} {drawn.Cards.Count} carte(s).");
                    break;

                case DeckExhaustedEvent exhausted:
                    PushLog(exhausted.Player == PlayerId.Player1
                        ? "Votre pioche est vide !"
                        : "La pioche de l'IA est vide !");
                    break;

                case TerrainPlayedEvent terrain:
                    DuelSfx.PlayPlaceCard();
                    PushLog($"Terrain {terrain.Card.Name} posé (Slot {terrain.SlotIndex + 1}).");
                    break;

                case TerrainRevealedByDigEvent dig:
                    DuelSfx.PlayPowerup();
                    if (dig.Card != null)
                        PushLog($"Terrain révélé (pioche) : {dig.Card.Name}.");
                    break;

                case TerrainDigFailedEvent:
                    PushLog("Dépilage : aucun Terrain restant dans le deck.");
                    break;

                case TerrainsReadiedEvent readied:
                    DuelSfx.PlayPowerup();
                    PushLog($"{readied.ReadiedSlotIndices.Count} terrain(s) redressé(s) et actif(s) !");
                    break;

                case CharacterPlayedEvent charEvt:
                    DuelSfx.PlayPlaceCard();
                    string chName = charEvt.Card.Name;
                    if (charEvt.SlotIndex >= 0 && charEvt.SlotIndex < _engine.State.Slots.Count)
                    {
                        var chars = _engine.State.Slots[charEvt.SlotIndex].GetCharacters(charEvt.Player);
                        for (int i = 1; i < chars.Count; i++)
                        {
                            if (chars[i].Id == charEvt.Card.Id && ComboEvaluator.IsComboActive(chars, i))
                            {
                                DuelSfx.PlayPowerup();
                                PushLog($"★ COMBO activé avec {chName} !");
                                break;
                            }
                        }
                    }
                    PushLog($"{chName} posé sur Terrain {charEvt.SlotIndex + 1}.");
                    break;

                case CharacterMovedSlotEvent moved:
                    DuelSfx.PlaySelectCard();
                    PushLog($"{moved.Card.Name} déplacé vers Terrain {moved.ToSlotIndex + 1}.");
                    break;

                case ComboFormedEvent comboEvt:
                    DuelSfx.PlayPowerup();
                    _board?.TriggerSlotEvoVfx(comboEvt.SlotIndex, comboEvt.Player == PlayerId.Player1);
                    break;

                case ComboBrokenEvent brokenEvt:
                    PushLog($"Combo rompu : {brokenEvt.CardWithBrokenCombo.Name}.");
                    break;

                case ScoreResolvedEvent scoreEvt:
                    bool? p1Won = scoreEvt.Winner == PlayerId.Player1 ? true : (scoreEvt.Winner == PlayerId.Player2 ? (bool?)false : null);
                    DuelSfx.PlayScoreResolved(p1Won);
                    _board?.TriggerClashVfx(scoreEvt.SlotIndex, scoreEvt.Winner == PlayerId.Player1);
                    PushLog($"Score Terrain {scoreEvt.SlotIndex + 1} : {scoreEvt.ForceP1} vs {scoreEvt.ForceP2}");
                    if (_clashCoroutine != null) StopCoroutine(_clashCoroutine);
                    _clashCoroutine = StartCoroutine(ScoreClashRoutine(scoreEvt.SlotIndex, scoreEvt.ForceP1, scoreEvt.ForceP2, scoreEvt.Winner));
                    break;

                case TerrainClearedEvent cleared:
                    DuelSfx.PlayTerrainCleared();
                    PushLog($"Terrain {cleared.SlotIndex + 1} libéré.");
                    break;

                case CardsMilledEvent milled:
                    DuelSfx.PlayDiscard();
                    PushLog($"{(milled.TargetPlayer == PlayerId.Player1 ? "Vous meulez" : "L'IA meule")} {milled.MilledCards.Count} carte(s).");
                    if (_cards != null && _board != null && milled.MilledCards != null && milled.MilledCards.Count > 0)
                    {
                        bool isP1 = milled.TargetPlayer == PlayerId.Player1;
                        Vector3 fromDeck = isP1 ? _board.DeckPosP1 : _board.DeckPosP2;
                        Vector3 toDisc = isP1 ? _board.DiscardPosP1 : _board.DiscardPosP2;
                        var topMilled = milled.MilledCards[milled.MilledCards.Count - 1];
                        _cards.SpawnFlyingDiscardVisual(topMilled, fromDeck, toDisc, _board.FaceUpFlatRot, 1.25f);
                    }
                    break;

                case CardsRevealedEvent revealed:
                    if (revealed.Player == PlayerId.Player1 && revealed.RevealedCards != null && revealed.RevealedCards.Count > 0)
                    {
                        var snapshot = new List<DuelCard>(revealed.RevealedCards);
                        EnqueueOrShowModal(() => ShowCardViewerModal($"Cartes Révélées ({snapshot.Count})", snapshot));
                    }
                    break;

                case CardsDiscardedEvent disc:
                    DuelSfx.PlayDiscard();
                    PushLog($"{disc.DiscardedCards.Count} carte(s) défaussée(s).");
                    break;

                case CardsBanishedEvent ban:
                    DuelSfx.PlayDiscard();
                    PushLog($"{ban.BanishedCards.Count} carte(s) bannie(s).");
                    break;

                case CardsSetAsideEvent setAside:
                    PushLog($"{setAside.SetAsideCards.Count} carte(s) mise(s) de côté.");
                    break;

                case EffectTriggeredEvent eff:
                    DuelSfx.PlayPowerup();
                    PushLog($"Effet : {eff.SourceCard.Name}");
                    break;

                case PlayerChoiceRequiredEvent choiceEvt:
                    if (choiceEvt.Player == PlayerId.Player1)
                    {
                        if (choiceEvt.Candidates != null && choiceEvt.Candidates.Count > 0)
                        {
                            EnqueueOrShowModal(() => ShowCardChoiceModal(choiceEvt));
                        }
                        else if (choiceEvt.OnCardSelected != null)
                        {
                            choiceEvt.OnCardSelected.Invoke(null);
                        }
                        else
                        {
                            PushLog($"Action impossible : {choiceEvt.Title}");
                        }
                    }
                    break;

                case PlayerSlotChoiceRequiredEvent slotChoice:
                    if (slotChoice.Player == PlayerId.Player1)
                    {
                        bool hasOccupiedSlot = false;
                        for (int i = 0; i < _engine.State.Slots.Count; i++)
                        {
                            if (!_engine.State.Slots[i].IsEmpty)
                            {
                                hasOccupiedSlot = true;
                                break;
                            }
                        }
                        if (hasOccupiedSlot)
                        {
                            _pendingSlotChoice = slotChoice;
                            PushLog($"Choix de terrain requis : {slotChoice.Title}");
                            RefreshHudOnly();
                        }
                        else
                        {
                            PushLog("Aucun Terrain disponible sur la table pour ce choix.");
                        }
                    }
                    break;

                case PlayerCostPaymentRequiredEvent costEvt:
                    if (costEvt.Player == PlayerId.Player1)
                    {
                        EnqueueOrShowModal(() => ShowCostPaymentModal(costEvt));
                    }
                    else
                    {
                        costEvt.OnCancelPayment?.Invoke();
                    }
                    break;

                case SubReactionPromptEvent subEvt:
                    if (subEvt.ReactingPlayer == PlayerId.Player1)
                    {
                        EnqueueOrShowModal(() => ShowSubReactionModal(subEvt));
                    }
                    else
                    {
                        subEvt.OnDeclineSub?.Invoke();
                    }
                    break;

                case DuelEndedEvent ended:
                    DuelSfx.PlayGameOver(ended.Winner == PlayerId.Player1);
                    ShowGameOverModal(ended.Winner, ended.Reason);
                    break;
            }
        }

        // =====================================================================
        // Score Clash Animation + Zoom Caméra 3D Temporaire
        // =====================================================================

        private IEnumerator ScoreClashRoutine(int slotIndex, int forceP1, int forceP2, PlayerId? winner)
        {
            _clashHighlightSlot = slotIndex;
            _clashFocusWorldPos = _board.GetSlotCenterWorld(slotIndex);
            _isClashZoomActive = true;

            if (_clashBarPanel != null)
            {
                _clashBarPanel.SetActive(true);
            }
            if (_clashP1ScoreText != null) _clashP1ScoreText.text = $"VOUS : {forceP1}";
            if (_clashP2ScoreText != null) _clashP2ScoreText.text = $"{forceP2} : ADV";

            int total = forceP1 + forceP2;
            float targetRatio = total > 0 ? Mathf.Clamp((float)forceP1 / total, 0.06f, 0.94f) : 0.5f;

            if (_clashDominanceText != null)
            {
                if (winner == PlayerId.Player1)
                    _clashDominanceText.text = $"<color=#55FF88>★ TERRAIN CONQUIS (+{forceP1 - forceP2} Force) !</color>";
                else if (winner == PlayerId.Player2)
                    _clashDominanceText.text = $"<color=#FF6666>▲ TERRAIN PERDU (+{forceP2 - forceP1} Force)</color>";
                else
                    _clashDominanceText.text = "<color=#FFFF88>ÉGALITÉ — Aucun terrain marqué</color>";
            }

            RefreshBoardAndCardsOnly();

            float elapsed = 0f;
            float duration = 0.55f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float ratio = Mathf.Lerp(0.5f, targetRatio, Mathf.Clamp01(elapsed / duration));
                if (_clashP1Fill != null) _clashP1Fill.anchorMax = new Vector2(ratio, 1f);
                if (_clashP2Fill != null) _clashP2Fill.anchorMin = new Vector2(ratio, 0f);
                if (_clashCursor != null)
                {
                    _clashCursor.anchorMin = new Vector2(ratio, 0.5f);
                    _clashCursor.anchorMax = new Vector2(ratio, 0.5f);
                }
                yield return null;
            }

            yield return new WaitForSeconds(0.95f);

            if (_clashBarPanel != null) _clashBarPanel.SetActive(false);
            _clashHighlightSlot = -1;
            _isClashZoomActive = false;
            _clashCoroutine = null;
            RefreshBoardAndCardsOnly();
        }

        // =====================================================================
        // HUD Transparent Périphérique + Modales & Inspecteur HD
        // =====================================================================

        private void BuildHudCanvas()
        {
            _hudCanvasObj = new GameObject("WankulDuelHud3D");
            WankulUiKit.SetupCanvas(_hudCanvasObj, 500);
            _hudCanvasRt = _hudCanvasObj.GetComponent<RectTransform>();
            var font = WankulUiKit.GetFont();

            // 0. Conteneur non-bloquant pour les badges de coût effectif des cartes en main P1
            var badgesGo = new GameObject("HandCostBadges");
            badgesGo.transform.SetParent(_hudCanvasObj.transform, false);
            _handCostBadgesRoot = badgesGo.transform;
            WankulUiKit.Stretch(badgesGo.AddComponent<RectTransform>());

            // 1. Barre Supérieure légère (Scores & Tour)
            var topBar = CreateNonBlockingPanel(_hudCanvasObj.transform, "TopBar",
                new Vector2(0.14f, 0.912f), new Vector2(0.86f, 0.988f), new Color(0.05f, 0.08f, 0.13f, 0.88f),
                new Color(1f, 0.88f, 0.35f, 0.65f));

            _scoreP1Text = CreateHudText(topBar.transform, font, "VOUS : ★ 0 / 5", 19, FontStyle.Bold,
                new Vector2(0.025f, 0.12f), new Vector2(0.27f, 0.88f), TextAnchor.MiddleLeft, new Color(0.35f, 0.85f, 1f));

            _turnBannerText = CreateHudText(topBar.transform, font, "TOUR 1", 18, FontStyle.Bold,
                new Vector2(0.27f, 0.44f), new Vector2(0.73f, 0.95f), TextAnchor.MiddleCenter, new Color(1f, 0.90f, 0.45f));

            _hintText = CreateHudText(topBar.transform, font, "", 12, FontStyle.Normal,
                new Vector2(0.24f, 0.06f), new Vector2(0.76f, 0.44f), TextAnchor.MiddleCenter, new Color(0.85f, 0.89f, 0.96f));

            _scoreP2Text = CreateHudText(topBar.transform, font, "ADVERSAIRE : ★ 0 / 5", 19, FontStyle.Bold,
                new Vector2(0.73f, 0.12f), new Vector2(0.975f, 0.88f), TextAnchor.MiddleRight, new Color(1f, 0.45f, 0.45f));

            // 2. Panneau de contrôle bas-droite (Fin de tour, Persos posés, Abandonner)
            var rightPanel = CreateNonBlockingPanel(_hudCanvasObj.transform, "RightControls",
                new Vector2(0.845f, 0.025f), new Vector2(0.985f, 0.21f), new Color(0.05f, 0.08f, 0.13f, 0.78f),
                new Color(0.85f, 0.75f, 0.35f, 0.45f));

            _charsPlayedText = CreateHudText(rightPanel.transform, font, "Persos posés : 0 / 4", 13, FontStyle.Bold,
                new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.96f), TextAnchor.MiddleCenter, new Color(0.72f, 0.90f, 1f));

            var endTurnGo = new GameObject("EndTurnBtn");
            endTurnGo.transform.SetParent(rightPanel.transform, false);
            var etRt = endTurnGo.AddComponent<RectTransform>();
            etRt.anchorMin = new Vector2(0.08f, 0.35f);
            etRt.anchorMax = new Vector2(0.92f, 0.69f);
            etRt.offsetMin = Vector2.zero;
            etRt.offsetMax = Vector2.zero;
            _endTurnBtnBg = endTurnGo.AddComponent<Image>();
            _endTurnBtnBg.sprite = WankulUiKit.WhiteSprite;
            _endTurnBtnBg.color = new Color(0.70f, 0.18f, 0.22f, 0.95f);
            _endTurnBtn = endTurnGo.AddComponent<Button>();
            _endTurnBtn.onClick.AddListener(OnEndTurnClicked);
            CreateHudText(endTurnGo.transform, font, "FIN DE TOUR", 15, FontStyle.Bold,
                Vector2.zero, Vector2.one, TextAnchor.MiddleCenter, Color.white);

            var forfeitBtn = WankulUiKit.CreateButton(rightPanel.transform, "Abandonner",
                new Color(0.24f, 0.26f, 0.32f, 0.92f), OnForfeitClicked);
            var forfeitTxt = forfeitBtn.GetComponentInChildren<Text>();
            if (forfeitTxt != null)
            {
                forfeitTxt.fontSize = 12;
                forfeitTxt.color = new Color(0.88f, 0.88f, 0.92f);
            }
            WankulUiKit.Place(forfeitBtn.GetComponent<RectTransform>(), new Vector2(0.16f, 0.07f), new Vector2(0.84f, 0.29f), Vector2.zero, Vector2.zero);

            // 3. Journal d'actions bas-gauche (remplit tout le rectangle)
            var logPanel = CreateNonBlockingPanel(_hudCanvasObj.transform, "ActionLogPanel",
                new Vector2(0.015f, 0.025f), new Vector2(0.265f, 0.205f), new Color(0.04f, 0.06f, 0.10f, 0.80f),
                new Color(0.45f, 0.65f, 0.95f, 0.45f));
            logPanel.AddComponent<RectMask2D>();

            CreateHudText(logPanel.transform, font, "JOURNAL DU DUEL", 12, FontStyle.Bold,
                new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.97f), TextAnchor.MiddleLeft, new Color(1f, 0.88f, 0.42f));

            _logFeedText = CreateHudText(logPanel.transform, font, "", 13, FontStyle.Normal,
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.80f), TextAnchor.UpperLeft, new Color(0.90f, 0.94f, 0.98f));
            _logFeedText.lineSpacing = 1.08f;
            _logFeedText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _logFeedText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void BuildScoreClashOverlay()
        {
            var font = WankulUiKit.GetFont();
            _clashBarPanel = CreateNonBlockingPanel(_hudCanvasObj.transform, "ScoreClashBar",
                new Vector2(0.28f, 0.78f), new Vector2(0.72f, 0.895f), new Color(0.05f, 0.07f, 0.12f, 0.90f),
                new Color(1f, 0.85f, 0.30f, 0.9f));

            _clashP1ScoreText = CreateHudText(_clashBarPanel.transform, font, "VOUS : 0", 16, FontStyle.Bold,
                new Vector2(0.03f, 0.56f), new Vector2(0.42f, 0.96f), TextAnchor.MiddleLeft, new Color(0.35f, 0.85f, 1f));

            _clashP2ScoreText = CreateHudText(_clashBarPanel.transform, font, "0 : ADV", 16, FontStyle.Bold,
                new Vector2(0.58f, 0.56f), new Vector2(0.97f, 0.96f), TextAnchor.MiddleRight, new Color(1f, 0.42f, 0.42f));

            _clashDominanceText = CreateHudText(_clashBarPanel.transform, font, "", 13, FontStyle.Bold,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.32f), TextAnchor.MiddleCenter, Color.white);
            _clashDominanceText.supportRichText = true;

            var barBg = CreateNonBlockingPanel(_clashBarPanel.transform, "ClashBarTrack",
                new Vector2(0.04f, 0.34f), new Vector2(0.96f, 0.54f), new Color(0.15f, 0.18f, 0.24f),
                new Color(1f, 1f, 1f, 0.2f));

            var p1FillObj = new GameObject("P1Fill");
            p1FillObj.transform.SetParent(barBg.transform, false);
            _clashP1Fill = p1FillObj.AddComponent<RectTransform>();
            _clashP1Fill.anchorMin = Vector2.zero;
            _clashP1Fill.anchorMax = new Vector2(0.5f, 1f);
            _clashP1Fill.offsetMin = Vector2.zero;
            _clashP1Fill.offsetMax = Vector2.zero;
            var p1Img = p1FillObj.AddComponent<Image>();
            p1Img.sprite = WankulUiKit.WhiteSprite;
            p1Img.color = new Color(0.25f, 0.65f, 0.95f, 0.92f);
            p1Img.raycastTarget = false;

            var p2FillObj = new GameObject("P2Fill");
            p2FillObj.transform.SetParent(barBg.transform, false);
            _clashP2Fill = p2FillObj.AddComponent<RectTransform>();
            _clashP2Fill.anchorMin = new Vector2(0.5f, 0f);
            _clashP2Fill.anchorMax = Vector2.one;
            _clashP2Fill.offsetMin = Vector2.zero;
            _clashP2Fill.offsetMax = Vector2.zero;
            var p2Img = p2FillObj.AddComponent<Image>();
            p2Img.sprite = WankulUiKit.WhiteSprite;
            p2Img.color = new Color(0.92f, 0.28f, 0.32f, 0.92f);
            p2Img.raycastTarget = false;

            var cursorObj = new GameObject("ClashCursor");
            cursorObj.transform.SetParent(barBg.transform, false);
            _clashCursor = cursorObj.AddComponent<RectTransform>();
            _clashCursor.anchorMin = new Vector2(0.5f, 0.5f);
            _clashCursor.anchorMax = new Vector2(0.5f, 0.5f);
            _clashCursor.sizeDelta = new Vector2(6f, 24f);
            var curImg = cursorObj.AddComponent<Image>();
            curImg.sprite = WankulUiKit.WhiteSprite;
            curImg.color = Color.white;
            curImg.raycastTarget = false;

            _clashBarPanel.SetActive(false);
        }

        private void UpdateHandCostBadges()
        {
            if (_handCostBadgesRoot == null || _hudCanvasRt == null || _engine == null || _cards == null || _rayCamera == null)
            {
                return;
            }

            bool shouldShow = !_isClosing &&
                              !_engine.State.IsGameOver &&
                              _activeChoiceModal == null &&
                              (_inspectorPanel == null || !_inspectorPanel.activeSelf);

            if (_handCostBadgesRoot.gameObject.activeSelf != shouldShow)
            {
                _handCostBadgesRoot.gameObject.SetActive(shouldShow);
            }

            if (!shouldShow) return;

            var hand = _engine.State.HandP1;
            int badgeIdx = 0;

            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (card.Kind != CardKind.Character) continue;
                if (!_cards.EntriesById.TryGetValue(card.Id, out var entry) || entry?.Card3d == null) continue;
                if (!entry.Card3d.gameObject.activeInHierarchy) continue;

                Vector3 screenPos = _rayCamera.WorldToScreenPoint(entry.Card3d.transform.position);
                if (screenPos.z <= 0f) continue;

                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _hudCanvasRt,
                        new Vector2(screenPos.x, screenPos.y - 36f),
                        null,
                        out Vector2 localPos))
                {
                    continue;
                }

                HandCostBadge badge = GetOrCreateHandCostBadge(badgeIdx++);
                badge.Root.SetActive(true);
                badge.Rect.anchoredPosition = localPos;

                int effCost = _engine.GetEffectivePlayCost(PlayerId.Player1, card, _hoveredSlotIndex);
                int baseCost = card.Cost;
                bool affordable = hand.Count - 1 >= effCost;

                if (card.IsDuo)
                {
                    badge.Label.text = "DUO 0";
                    badge.Bg.color = new Color(0.08f, 0.34f, 0.44f, 0.92f);
                    badge.Outline.effectColor = new Color(0.45f, 0.95f, 1f, 0.95f);
                    badge.Label.color = new Color(0.85f, 0.98f, 1f);
                }
                else if (!affordable)
                {
                    badge.Label.text = effCost != baseCost ? $"Coût {effCost} ({baseCost})" : $"Coût {effCost}";
                    badge.Bg.color = new Color(0.50f, 0.12f, 0.16f, 0.92f);
                    badge.Outline.effectColor = new Color(1f, 0.40f, 0.45f, 0.95f);
                    badge.Label.color = new Color(1f, 0.85f, 0.85f);
                }
                else if (effCost < baseCost)
                {
                    badge.Label.text = $"Coût {effCost} (-{baseCost - effCost})";
                    badge.Bg.color = new Color(0.08f, 0.38f, 0.20f, 0.92f);
                    badge.Outline.effectColor = new Color(0.42f, 1f, 0.58f, 0.95f);
                    badge.Label.color = new Color(0.86f, 1f, 0.90f);
                }
                else if (effCost > baseCost)
                {
                    badge.Label.text = $"Coût {effCost} (+{effCost - baseCost})";
                    badge.Bg.color = new Color(0.46f, 0.22f, 0.06f, 0.92f);
                    badge.Outline.effectColor = new Color(1f, 0.72f, 0.28f, 0.95f);
                    badge.Label.color = new Color(1f, 0.92f, 0.78f);
                }
                else
                {
                    badge.Label.text = $"Coût {effCost}";
                    badge.Bg.color = new Color(0.06f, 0.09f, 0.15f, 0.90f);
                    badge.Outline.effectColor = new Color(1f, 0.85f, 0.35f, 0.85f);
                    badge.Label.color = Color.white;
                }
            }

            for (int i = badgeIdx; i < _handCostBadges.Count; i++)
            {
                if (_handCostBadges[i].Root.activeSelf)
                {
                    _handCostBadges[i].Root.SetActive(false);
                }
            }
        }

        private HandCostBadge GetOrCreateHandCostBadge(int index)
        {
            while (_handCostBadges.Count <= index)
            {
                var go = new GameObject($"CostBadge_{_handCostBadges.Count}");
                go.transform.SetParent(_handCostBadgesRoot, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(74f, 20f);

                var bg = go.AddComponent<Image>();
                bg.sprite = WankulUiKit.WhiteSprite;
                bg.raycastTarget = false;

                var outline = go.AddComponent<Outline>();
                outline.effectDistance = new Vector2(1f, -1f);

                var txtGo = new GameObject("Label");
                txtGo.transform.SetParent(go.transform, false);
                WankulUiKit.Stretch(txtGo.AddComponent<RectTransform>());
                var label = txtGo.AddComponent<Text>();
                label.font = WankulUiKit.GetFont();
                label.fontSize = 11;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.raycastTarget = false;

                go.SetActive(false);
                _handCostBadges.Add(new HandCostBadge
                {
                    Root = go,
                    Rect = rt,
                    Bg = bg,
                    Outline = outline,
                    Label = label
                });
            }

            return _handCostBadges[index];
        }

        private void BuildInspectorOverlay()
        {
            var font = WankulUiKit.GetFont();
            _inspectorPanel = new GameObject("HDInspectorOverlay");
            _inspectorPanel.transform.SetParent(_hudCanvasObj.transform, false);
            WankulUiKit.Stretch(_inspectorPanel.GetComponent<RectTransform>() ?? _inspectorPanel.AddComponent<RectTransform>());
            var dimBg = _inspectorPanel.AddComponent<Image>();
            dimBg.sprite = WankulUiKit.WhiteSprite;
            dimBg.color = new Color(0.02f, 0.04f, 0.08f, 0.80f);

            var box = CreateNonBlockingPanel(_inspectorPanel.transform, "InspectorBox",
                new Vector2(0.24f, 0.16f), new Vector2(0.76f, 0.84f), new Color(0.07f, 0.10f, 0.16f, 0.97f),
                new Color(1f, 0.85f, 0.35f, 0.85f));

            var cardObj = new GameObject("CardImg");
            cardObj.transform.SetParent(box.transform, false);
            var cRt = cardObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.04f, 0.08f);
            cRt.anchorMax = new Vector2(0.46f, 0.92f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            _inspectorCardImg = cardObj.AddComponent<Image>();
            _inspectorCardImg.preserveAspect = true;
            _inspectorCardImg.raycastTarget = false;

            _inspectorTitleText = CreateHudText(box.transform, font, "", 22, FontStyle.Bold,
                new Vector2(0.50f, 0.78f), new Vector2(0.95f, 0.93f), TextAnchor.UpperLeft, new Color(1f, 0.88f, 0.42f));

            _inspectorStatsText = CreateHudText(box.transform, font, "", 15, FontStyle.Bold,
                new Vector2(0.50f, 0.56f), new Vector2(0.95f, 0.77f), TextAnchor.UpperLeft, new Color(0.48f, 0.86f, 1f));

            _inspectorEffectText = CreateHudText(box.transform, font, "", 14, FontStyle.Normal,
                new Vector2(0.50f, 0.16f), new Vector2(0.95f, 0.55f), TextAnchor.UpperLeft, Color.white);

            _inspectorActivateBtn = WankulUiKit.CreateButton(box.transform, "ACTIVER CAPACITÉ",
                new Color(0.14f, 0.56f, 0.28f, 0.96f),
                OnInspectorActivateClicked);
            _inspectorActivateBtnRt = _inspectorActivateBtn.GetComponent<RectTransform>();
            WankulUiKit.Place(_inspectorActivateBtnRt,
                new Vector2(0.50f, 0.04f), new Vector2(0.76f, 0.13f), Vector2.zero, Vector2.zero);
            _inspectorActivateBtn.gameObject.SetActive(false);

            _inspectorCloseHintText = CreateHudText(box.transform, font, "Clic ou Échap pour fermer", 12, FontStyle.Italic,
                new Vector2(0.50f, 0.04f), new Vector2(0.95f, 0.14f), TextAnchor.MiddleRight, new Color(0.70f, 0.75f, 0.85f));

            _inspectorPanel.SetActive(false);
        }

        private bool IsPointerOverInspectorActivateButton()
        {
            if (_inspectorActivateBtn == null ||
                !_inspectorActivateBtn.gameObject.activeInHierarchy ||
                _inspectorActivateBtnRt == null)
            {
                return false;
            }

            var canvas = _hudCanvasObj != null ? _hudCanvasObj.GetComponent<Canvas>() : null;
            Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
            return RectTransformUtility.RectangleContainsScreenPoint(_inspectorActivateBtnRt, Input.mousePosition, uiCam);
        }

        private static bool HasAnyEffectInSet(DuelCard card, HashSet<string> set)
        {
            if (card == null) return false;
            for (int i = 0; i < card.PassiveEffectIds.Count; i++)
            {
                if (set.Contains(card.PassiveEffectIds[i])) return true;
            }
            for (int i = 0; i < card.EffectIds.Count; i++)
            {
                if (set.Contains(card.EffectIds[i])) return true;
            }
            return false;
        }

        private static bool HasEffectId(DuelCard card, string targetId)
        {
            if (card == null || string.IsNullOrEmpty(targetId)) return false;
            for (int i = 0; i < card.PassiveEffectIds.Count; i++)
            {
                if (string.Equals(card.PassiveEffectIds[i], targetId, StringComparison.Ordinal)) return true;
            }
            for (int i = 0; i < card.EffectIds.Count; i++)
            {
                if (string.Equals(card.EffectIds[i], targetId, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private bool TryGetActivatableContext(DuelCard card, out int slotIndex, out string targetCardId)
        {
            slotIndex = -1;
            targetCardId = null;
            if (_engine == null || card == null) return false;

            var state = _engine.State;
            for (int i = 0; i < state.HandP1.Count; i++)
            {
                if (string.Equals(state.HandP1[i].Id, card.Id, StringComparison.Ordinal))
                {
                    if (HasEffectId(state.HandP1[i], "passive_banish_from_hand_free_play"))
                    {
                        slotIndex = -1;
                        targetCardId = card.Id;
                        return true;
                    }
                    break;
                }
            }

            for (int s = 0; s < state.Slots.Count; s++)
            {
                var slot = state.Slots[s];
                for (int i = 0; i < slot.CharactersP1.Count; i++)
                {
                    var ch = slot.CharactersP1[i];
                    if (string.Equals(ch.Id, card.Id, StringComparison.Ordinal))
                    {
                        if (HasAnyEffectInSet(ch, ActivatableBoardPassives) ||
                            (slot.Card != null && HasAnyEffectInSet(slot.Card, ActivatableTerrainPassives)))
                        {
                            slotIndex = s;
                            targetCardId = ch.Id;
                            return true;
                        }
                        return false;
                    }
                }

                if (slot.Card != null &&
                    string.Equals(slot.Card.Id, card.Id, StringComparison.Ordinal) &&
                    slot.CharactersP1.Count > 0 &&
                    HasAnyEffectInSet(slot.Card, ActivatableTerrainPassives))
                {
                    slotIndex = s;
                    targetCardId = slot.CharactersP1[0].Id;
                    return true;
                }
            }

            return false;
        }

        private bool TryActivateCardPassiveFromUi(string cardId, int slotIndex, string cardDisplayName)
        {
            if (_engine == null || string.IsNullOrEmpty(cardId)) return false;

            var res = _engine.ActivateCardPassive(PlayerId.Player1, cardId, slotIndex);
            if (res.Success)
            {
                DuelSfx.PlayPowerup();
                PushLog($"⚡ Capacité activée : {cardDisplayName}");
                RefreshAllVisuals();
                return true;
            }

            DuelSfx.PlayCancel();
            if (_hintText != null)
            {
                _hintText.text = $"Capacité indisponible : {res.ErrorReason}";
            }
            return false;
        }

        private void OnInspectorActivateClicked()
        {
            string cardId = _inspectorActivatableCardId;
            int slotIdx = _inspectorActivatableSlotIdx;
            string cardName = _inspectorTitleText != null ? _inspectorTitleText.text : "Carte";
            CloseInspector();
            TryActivateCardPassiveFromUi(cardId, slotIdx, cardName);
        }

        private void OpenInspector(DuelCard card)
        {
            if (card == null || _inspectorPanel == null) return;
            DuelSfx.PlaySelectCard();

            Sprite spr = CardAdapter.GetCardSprite(card);
            if (_inspectorCardImg != null)
            {
                _inspectorCardImg.sprite = spr != null ? spr : WankulUiKit.WhiteSprite;
                _inspectorCardImg.color = spr != null ? Color.white : new Color(0.18f, 0.24f, 0.34f);
            }

            _inspectorTitleText.text = card.Name;

            WankulCardData wCard = CardAdapter.GetWankulCard(card);
            int terrainSlotIdx = _hoveredSlotIndex;
            if (_engine != null)
            {
                for (int s = 0; s < _engine.State.Slots.Count; s++)
                {
                    if (_engine.State.Slots[s].Card != null &&
                        string.Equals(_engine.State.Slots[s].Card.Id, card.Id, StringComparison.Ordinal))
                    {
                        terrainSlotIdx = s;
                        break;
                    }
                }
            }

            int scoreThresh = _engine != null ? _engine.State.Rules.AutoScoreForceThreshold : 11;
            if (_engine != null && terrainSlotIdx >= 0 && terrainSlotIdx < _engine.State.Slots.Count)
            {
                scoreThresh = _engine.State.Slots[terrainSlotIdx].GetEffectiveAutoScoreThreshold(
                    PlayerId.Player1,
                    _engine.State.Rules.AutoScoreForceThreshold,
                    _engine.State);
            }

            if (card.Kind == CardKind.Terrain)
            {
                _inspectorStatsText.text = $"TYPE : TERRAIN\nSeuil auto : {scoreThresh} Force";
            }
            else
            {
                var gemParts = new List<string>();
                if (card.HasOpeningOrangeGem) gemParts.Add("◆ Ouv. Orange");
                if (card.HasOpeningVioletGem) gemParts.Add("◆ Ouv. Violet");
                if (card.HasClosingOrangeGem) gemParts.Add("◆ Ferm. Orange");
                if (card.HasClosingVioletGem) gemParts.Add("◆ Ferm. Violet");
                string gems = gemParts.Count > 0 ? string.Join("  ", gemParts) : "Aucune";
                int baseCost = card.Cost;
                int effCost = _engine != null ? _engine.GetEffectivePlayCost(PlayerId.Player1, card, _hoveredSlotIndex) : baseCost;
                string costStr = card.IsDuo
                    ? "0 (DUO)"
                    : (effCost != baseCost ? $"{effCost} (base {baseCost})" : $"{effCost}");
                string effigy = !string.IsNullOrEmpty(card.Effigy) ? $"  |  Effigie : {card.Effigy}" : "";
                _inspectorStatsText.text =
                    $"TYPE : PERSONNAGE\nForce : {card.Force}  |  Coût : {costStr}{effigy}\nGemmes Combo : {gems}";
            }

            string desc = "";
            if (wCard is EffigyCardData eff)
            {
                desc = !string.IsNullOrWhiteSpace(eff.Combo)
                    ? $"{eff.Rules}\n\nCombo : {eff.Combo}"
                    : eff.Rules;
            }
            else if (wCard is TerrainCardData ter)
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(ter.SpecialEffect)) parts.Add($"Spécial : {ter.SpecialEffect}");
                if (!string.IsNullOrWhiteSpace(ter.WinningEffect)) parts.Add($"Gagnant : {ter.WinningEffect}");
                if (!string.IsNullOrWhiteSpace(ter.LosingEffect)) parts.Add($"Perdant : {ter.LosingEffect}");
                desc = string.Join("\n", parts);
            }

            _inspectorEffectText.text = string.IsNullOrWhiteSpace(desc) ? "Aucun effet spécial." : desc;

            int actSlotIdx = -1;
            string actCardId = null;
            bool canActivate = CanPlayerAct() && TryGetActivatableContext(card, out actSlotIdx, out actCardId);
            _inspectorActivatableCardId = canActivate ? actCardId : null;
            _inspectorActivatableSlotIdx = canActivate ? actSlotIdx : -1;
            if (_inspectorActivateBtn != null)
            {
                _inspectorActivateBtn.gameObject.SetActive(canActivate);
            }
            if (_inspectorCloseHintText != null)
            {
                var hintRt = _inspectorCloseHintText.rectTransform;
                hintRt.anchorMin = new Vector2(canActivate ? 0.77f : 0.50f, 0.04f);
                hintRt.anchorMax = new Vector2(0.95f, 0.14f);
            }

            _inspectorPanel.SetActive(true);
        }

        private void CloseInspector()
        {
            _inspectorActivatableCardId = null;
            _inspectorActivatableSlotIdx = -1;
            if (_inspectorActivateBtn != null) _inspectorActivateBtn.gameObject.SetActive(false);
            if (_inspectorPanel != null) _inspectorPanel.SetActive(false);
            DuelSfx.PlayCancel();
        }

        private void EnqueueOrShowModal(Action showAction)
        {
            if (showAction == null || _isClosing) return;
            if (_engine != null && _engine.State.IsGameOver) return;

            if (_activeChoiceModal == null)
            {
                showAction();
            }
            else
            {
                _pendingModalQueue.Enqueue(showAction);
            }
        }

        private void CloseActiveModalAndAdvance(GameObject modalObj, Action onAfterClose = null)
        {
            if (_activeChoiceModal == modalObj)
            {
                _activeChoiceModal = null;
            }
            if (modalObj != null)
            {
                Destroy(modalObj);
            }

            onAfterClose?.Invoke();

            if (_activeChoiceModal == null &&
                _pendingModalQueue.Count > 0 &&
                !_isClosing &&
                (_engine == null || !_engine.State.IsGameOver))
            {
                var next = _pendingModalQueue.Dequeue();
                next?.Invoke();
            }

            RefreshAllVisuals();
        }

        private void ShowCardViewerModal(string title, IReadOnlyList<DuelCard> cardsToView)
        {
            if (cardsToView == null || cardsToView.Count == 0) return;
            var evt = new PlayerChoiceRequiredEvent(
                PlayerId.Player1,
                title,
                "Cliquez sur une carte pour l'inspecter ou sur Fermer",
                cardsToView,
                chosen =>
                {
                    if (chosen != null)
                    {
                        OpenInspector(chosen);
                    }
                });
            EnqueueOrShowModal(() => ShowCardChoiceModal(evt, allowCloseButton: true));
        }

        private void ShowCardChoiceModal(PlayerChoiceRequiredEvent choiceEvt, bool allowCloseButton = false)
        {
            if (choiceEvt == null || choiceEvt.Candidates == null || choiceEvt.Candidates.Count == 0)
            {
                choiceEvt?.OnCardSelected?.Invoke(null);
                return;
            }

            if (_activeChoiceModal != null) Destroy(_activeChoiceModal);

            var font = WankulUiKit.GetFont();
            var modalObj = new GameObject("CardChoiceModal");
            modalObj.transform.SetParent(_hudCanvasObj.transform, false);
            var mRt = modalObj.AddComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.14f, 0.18f);
            mRt.anchorMax = new Vector2(0.86f, 0.82f);
            mRt.offsetMin = Vector2.zero;
            mRt.offsetMax = Vector2.zero;

            var bg = modalObj.AddComponent<Image>();
            bg.sprite = WankulUiKit.WhiteSprite;
            bg.color = new Color(0.07f, 0.10f, 0.16f, 0.97f);
            var outline = modalObj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.85f, 0.35f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            _activeChoiceModal = modalObj;

            CreateHudText(modalObj.transform, font, choiceEvt.Title, 20, FontStyle.Bold,
                new Vector2(0.04f, 0.88f), new Vector2(allowCloseButton ? 0.80f : 0.96f, 0.97f), TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.42f));

            if (!string.IsNullOrWhiteSpace(choiceEvt.Description))
            {
                CreateHudText(modalObj.transform, font, choiceEvt.Description, 13, FontStyle.Italic,
                    new Vector2(0.04f, 0.82f), new Vector2(allowCloseButton ? 0.80f : 0.96f, 0.88f), TextAnchor.MiddleCenter, new Color(0.78f, 0.86f, 0.96f));
            }

            if (allowCloseButton)
            {
                var closeBtn = WankulUiKit.CreateButton(modalObj.transform, "FERMER ✕",
                    new Color(0.55f, 0.16f, 0.20f, 0.95f),
                    () =>
                    {
                        DuelSfx.PlayCancel();
                        CloseActiveModalAndAdvance(modalObj);
                    });
                WankulUiKit.Place(closeBtn.GetComponent<RectTransform>(), new Vector2(0.82f, 0.86f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);
            }

            // Zone de défilement horizontale (ScrollRect + Viewport masqué + Content extensif + Slider)
            var scrollObj = new GameObject("CandidatesScrollView");
            scrollObj.transform.SetParent(modalObj.transform, false);
            var scrollRt = scrollObj.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.04f, 0.14f);
            scrollRt.anchorMax = new Vector2(0.96f, 0.83f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;

            var scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.sprite = WankulUiKit.WhiteSprite;
            scrollBg.color = new Color(0.04f, 0.06f, 0.10f, 0.45f);

            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 36f;

            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var vpRt = viewportObj.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            var vpImg = viewportObj.AddComponent<Image>();
            vpImg.sprite = WankulUiKit.WhiteSprite;
            vpImg.color = new Color(1f, 1f, 1f, 0.01f);
            viewportObj.AddComponent<RectMask2D>();
            scrollRect.viewport = vpRt;

            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRt = contentObj.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0f);
            contentRt.anchorMax = new Vector2(0f, 1f);
            contentRt.pivot = new Vector2(0f, 0.5f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var layout = contentObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.padding = new RectOffset(16, 16, 10, 10);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.content = contentRt;

            // Scrollbar / Slider horizontal sous les cartes
            var scrollbarObj = new GameObject("HorizontalScrollbar");
            scrollbarObj.transform.SetParent(modalObj.transform, false);
            var sbRt = scrollbarObj.AddComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0.08f, 0.055f);
            sbRt.anchorMax = new Vector2(0.92f, 0.105f);
            sbRt.offsetMin = Vector2.zero;
            sbRt.offsetMax = Vector2.zero;

            var sbBg = scrollbarObj.AddComponent<Image>();
            sbBg.sprite = WankulUiKit.WhiteSprite;
            sbBg.color = new Color(0.14f, 0.18f, 0.26f, 0.95f);
            var sbOutline = scrollbarObj.AddComponent<Outline>();
            sbOutline.effectColor = new Color(1f, 0.85f, 0.35f, 0.5f);
            sbOutline.effectDistance = new Vector2(1f, -1f);

            var slidingAreaObj = new GameObject("SlidingArea");
            slidingAreaObj.transform.SetParent(scrollbarObj.transform, false);
            var saRt = slidingAreaObj.AddComponent<RectTransform>();
            saRt.anchorMin = new Vector2(0.01f, 0.12f);
            saRt.anchorMax = new Vector2(0.99f, 0.88f);
            saRt.offsetMin = Vector2.zero;
            saRt.offsetMax = Vector2.zero;

            var handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(slidingAreaObj.transform, false);
            var handleRt = handleObj.AddComponent<RectTransform>();
            handleRt.anchorMin = Vector2.zero;
            handleRt.anchorMax = Vector2.one;
            handleRt.offsetMin = Vector2.zero;
            handleRt.offsetMax = Vector2.zero;
            var handleImg = handleObj.AddComponent<Image>();
            handleImg.sprite = WankulUiKit.WhiteSprite;
            handleImg.color = new Color(1f, 0.82f, 0.28f, 0.95f);

            var scrollbar = scrollbarObj.AddComponent<Scrollbar>();
            scrollbar.targetGraphic = handleImg;
            scrollbar.handleRect = handleRt;
            scrollbar.direction = Scrollbar.Direction.LeftToRight;

            scrollRect.horizontalScrollbar = scrollbar;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            for (int i = 0; i < choiceEvt.Candidates.Count; i++)
            {
                var cand = choiceEvt.Candidates[i];
                var btnObj = new GameObject($"Cand_{cand.Id}");
                btnObj.transform.SetParent(contentObj.transform, false);
                btnObj.AddComponent<RectTransform>().sizeDelta = new Vector2(140f, 200f);
                var le = btnObj.AddComponent<LayoutElement>();
                le.preferredWidth = 140f;
                le.preferredHeight = 200f;

                var img = btnObj.AddComponent<Image>();
                var spr = CardAdapter.GetCardSprite(cand);
                img.sprite = spr != null ? spr : WankulUiKit.WhiteSprite;
                img.color = spr != null ? Color.white : new Color(0.18f, 0.24f, 0.34f);
                img.preserveAspect = true;

                var cardOutline = btnObj.AddComponent<Outline>();
                cardOutline.effectColor = new Color(0.4f, 0.85f, 1f, 0.85f);
                cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

                var btn = btnObj.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    DuelSfx.PlaySelectCard();
                    CloseActiveModalAndAdvance(modalObj, () => choiceEvt.OnCardSelected?.Invoke(cand));
                });

                string shortName = cand.Name;
                string capText = cand.Kind == CardKind.Character
                    ? $"{shortName}\nF:{cand.Force} C:{cand.Cost}"
                    : shortName;
                var capBg = CreateNonBlockingPanel(btnObj.transform, "Cap",
                    new Vector2(0f, 0f), new Vector2(1f, 0.24f), new Color(0.04f, 0.06f, 0.10f, 0.84f),
                    new Color(0.4f, 0.85f, 1f, 0.5f));
                var capTxt = CreateHudText(capBg.transform, font, capText, 10, FontStyle.Bold,
                    new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), TextAnchor.MiddleCenter, Color.white);
                capTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
                capTxt.verticalOverflow = VerticalWrapMode.Truncate;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
            Canvas.ForceUpdateCanvases();
            scrollRect.horizontalNormalizedPosition = 0f;
        }

        private void ShowCostPaymentModal(PlayerCostPaymentRequiredEvent payEvt)
        {
            if (payEvt == null) return;
            if (payEvt.RequiredCount <= 0)
            {
                payEvt.OnConfirmPayment?.Invoke(Array.Empty<string>());
                return;
            }
            if (payEvt.PayableHandCards == null || payEvt.PayableHandCards.Count < payEvt.RequiredCount)
            {
                payEvt.OnCancelPayment?.Invoke();
                return;
            }

            if (_activeChoiceModal != null) Destroy(_activeChoiceModal);

            var font = WankulUiKit.GetFont();
            var modalObj = new GameObject("CostPaymentModal");
            modalObj.transform.SetParent(_hudCanvasObj.transform, false);
            var mRt = modalObj.AddComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.12f, 0.15f);
            mRt.anchorMax = new Vector2(0.88f, 0.85f);
            mRt.offsetMin = Vector2.zero;
            mRt.offsetMax = Vector2.zero;

            var bg = modalObj.AddComponent<Image>();
            bg.sprite = WankulUiKit.WhiteSprite;
            bg.color = new Color(0.07f, 0.10f, 0.16f, 0.97f);
            var outline = modalObj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.78f, 0.25f, 0.92f);
            outline.effectDistance = new Vector2(2f, -2f);

            _activeChoiceModal = modalObj;

            string cardTitle = payEvt.CardToPlay.Name;
            CreateHudText(modalObj.transform, font,
                $"PAIEMENT DU COÛT : {cardTitle}", 20, FontStyle.Bold,
                new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.97f), TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.42f));

            var counterText = CreateHudText(modalObj.transform, font,
                $"Défaussez exactement {payEvt.RequiredCount} carte(s) de votre main  (0 / {payEvt.RequiredCount} sélectionnée(s))",
                14, FontStyle.Bold,
                new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.88f), TextAnchor.MiddleCenter, new Color(0.76f, 0.88f, 1f));

            // ScrollRect horizontal
            var scrollObj = new GameObject("PayableCardsScroll");
            scrollObj.transform.SetParent(modalObj.transform, false);
            var scrollRt = scrollObj.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.04f, 0.19f);
            scrollRt.anchorMax = new Vector2(0.96f, 0.79f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;

            var scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.sprite = WankulUiKit.WhiteSprite;
            scrollBg.color = new Color(0.04f, 0.06f, 0.10f, 0.50f);

            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 36f;

            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var vpRt = viewportObj.AddComponent<RectTransform>();
            WankulUiKit.Stretch(vpRt);
            var vpImg = viewportObj.AddComponent<Image>();
            vpImg.sprite = WankulUiKit.WhiteSprite;
            vpImg.color = new Color(1f, 1f, 1f, 0.01f);
            viewportObj.AddComponent<RectMask2D>();
            scrollRect.viewport = vpRt;

            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRt = contentObj.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0f);
            contentRt.anchorMax = new Vector2(0f, 1f);
            contentRt.pivot = new Vector2(0f, 0.5f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var layout = contentObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.padding = new RectOffset(16, 16, 10, 10);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            scrollRect.content = contentRt;

            var selectedIds = new List<string>();
            var outlinesById = new Dictionary<string, Outline>(StringComparer.Ordinal);
            var badgesById = new Dictionary<string, GameObject>(StringComparer.Ordinal);

            Button confirmBtn = null;
            Image confirmBtnImg = null;

            void RefreshSelectionState()
            {
                bool ready = selectedIds.Count == payEvt.RequiredCount;
                counterText.text = ready
                    ? $"Prêt : {selectedIds.Count} / {payEvt.RequiredCount} carte(s) sélectionnée(s)"
                    : $"Défaussez exactement {payEvt.RequiredCount} carte(s) de votre main  ({selectedIds.Count} / {payEvt.RequiredCount} sélectionnée(s))";
                counterText.color = ready ? new Color(0.45f, 1f, 0.60f) : new Color(0.76f, 0.88f, 1f);

                if (confirmBtn != null) confirmBtn.interactable = ready;
                if (confirmBtnImg != null)
                {
                    confirmBtnImg.color = ready
                        ? new Color(0.14f, 0.56f, 0.28f, 0.96f)
                        : new Color(0.22f, 0.25f, 0.30f, 0.85f);
                }

                for (int i = 0; i < payEvt.PayableHandCards.Count; i++)
                {
                    string id = payEvt.PayableHandCards[i].Id;
                    bool isSelected = selectedIds.Contains(id);
                    if (outlinesById.TryGetValue(id, out var cardOutline) && cardOutline != null)
                    {
                        cardOutline.effectColor = isSelected
                            ? new Color(0.32f, 1f, 0.48f, 1f)
                            : new Color(0.40f, 0.78f, 1f, 0.65f);
                        cardOutline.effectDistance = isSelected ? new Vector2(3f, -3f) : new Vector2(1.5f, -1.5f);
                    }
                    if (badgesById.TryGetValue(id, out var badgeGo) && badgeGo != null)
                    {
                        badgeGo.SetActive(isSelected);
                    }
                }
            }

            for (int i = 0; i < payEvt.PayableHandCards.Count; i++)
            {
                var cand = payEvt.PayableHandCards[i];
                var btnObj = new GameObject($"PayCand_{cand.Id}");
                btnObj.transform.SetParent(contentObj.transform, false);
                btnObj.AddComponent<RectTransform>().sizeDelta = new Vector2(140f, 200f);
                var le = btnObj.AddComponent<LayoutElement>();
                le.preferredWidth = 140f;
                le.preferredHeight = 200f;

                var img = btnObj.AddComponent<Image>();
                var spr = CardAdapter.GetCardSprite(cand);
                img.sprite = spr != null ? spr : WankulUiKit.WhiteSprite;
                img.color = spr != null ? Color.white : new Color(0.18f, 0.24f, 0.34f);
                img.preserveAspect = true;

                var cardOutline = btnObj.AddComponent<Outline>();
                cardOutline.effectColor = new Color(0.40f, 0.78f, 1f, 0.65f);
                cardOutline.effectDistance = new Vector2(1.5f, -1.5f);
                outlinesById[cand.Id] = cardOutline;

                string shortName = cand.Name;
                string capText = cand.Kind == CardKind.Character
                    ? $"{shortName}\nF:{cand.Force} C:{cand.Cost}"
                    : $"{shortName}\nTERRAIN";
                var capBg = CreateNonBlockingPanel(btnObj.transform, "Cap",
                    new Vector2(0f, 0f), new Vector2(1f, 0.24f), new Color(0.04f, 0.06f, 0.10f, 0.86f),
                    new Color(0.4f, 0.85f, 1f, 0.5f));
                var capTxt = CreateHudText(capBg.transform, font, capText, 10, FontStyle.Bold,
                    new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), TextAnchor.MiddleCenter, Color.white);
                capTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
                capTxt.verticalOverflow = VerticalWrapMode.Truncate;

                var selBadge = CreateNonBlockingPanel(btnObj.transform, "SelectedTag",
                    new Vector2(0.10f, 0.82f), new Vector2(0.90f, 0.96f), new Color(0.12f, 0.58f, 0.26f, 0.95f),
                    new Color(0.65f, 1f, 0.75f, 1f));
                CreateHudText(selBadge.transform, font, "✓ DÉFAUSSER", 10, FontStyle.Bold,
                    Vector2.zero, Vector2.one, TextAnchor.MiddleCenter, Color.white);
                selBadge.SetActive(false);
                badgesById[cand.Id] = selBadge;

                var btn = btnObj.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    if (selectedIds.Contains(cand.Id))
                    {
                        selectedIds.Remove(cand.Id);
                        DuelSfx.PlayCancel();
                    }
                    else if (payEvt.RequiredCount == 1)
                    {
                        selectedIds.Clear();
                        selectedIds.Add(cand.Id);
                        DuelSfx.PlaySelectCard();
                    }
                    else if (selectedIds.Count < payEvt.RequiredCount)
                    {
                        selectedIds.Add(cand.Id);
                        DuelSfx.PlaySelectCard();
                    }
                    RefreshSelectionState();
                });
            }

            confirmBtn = WankulUiKit.CreateButton(modalObj.transform, "CONFIRMER PAIEMENT",
                new Color(0.22f, 0.25f, 0.30f, 0.85f),
                () =>
                {
                    if (selectedIds.Count != payEvt.RequiredCount) return;
                    var chosenCopy = new List<string>(selectedIds);
                    DuelSfx.PlayPlaceCard();
                    CloseActiveModalAndAdvance(modalObj, () => payEvt.OnConfirmPayment?.Invoke(chosenCopy));
                });
            confirmBtnImg = confirmBtn.GetComponent<Image>();
            WankulUiKit.Place(confirmBtn.GetComponent<RectTransform>(),
                new Vector2(0.24f, 0.05f), new Vector2(0.49f, 0.15f), Vector2.zero, Vector2.zero);

            var cancelBtn = WankulUiKit.CreateButton(modalObj.transform, "ANNULER",
                new Color(0.55f, 0.16f, 0.20f, 0.95f),
                () =>
                {
                    DuelSfx.PlayCancel();
                    CloseActiveModalAndAdvance(modalObj, () => payEvt.OnCancelPayment?.Invoke());
                });
            WankulUiKit.Place(cancelBtn.GetComponent<RectTransform>(),
                new Vector2(0.53f, 0.05f), new Vector2(0.76f, 0.15f), Vector2.zero, Vector2.zero);

            RefreshSelectionState();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
            Canvas.ForceUpdateCanvases();
            scrollRect.horizontalNormalizedPosition = 0f;
        }

        private void ShowSubReactionModal(SubReactionPromptEvent subEvt)
        {
            if (subEvt == null) return;
            if (subEvt.SubCard == null)
            {
                subEvt.OnDeclineSub?.Invoke();
                return;
            }

            if (_activeChoiceModal != null) Destroy(_activeChoiceModal);

            var modalObj = new GameObject("SubReactionModal");
            modalObj.transform.SetParent(_hudCanvasObj.transform, false);
            var mRt = modalObj.AddComponent<RectTransform>();
            WankulUiKit.Place(mRt, new Vector2(0.22f, 0.20f), new Vector2(0.78f, 0.80f), Vector2.zero, Vector2.zero);

            var bg = modalObj.AddComponent<Image>();
            bg.sprite = WankulUiKit.WhiteSprite;
            bg.color = new Color(0.07f, 0.10f, 0.16f, 0.97f);
            var outline = modalObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.45f, 0.86f, 1f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            _activeChoiceModal = modalObj;

            var title = WankulUiKit.CreateText(modalObj.transform, "SubTitle", "RÉACTION (SUB) DISPONIBLE !", 22, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Color(0.48f, 0.88f, 1f));
            title.raycastTarget = false;
            WankulUiKit.Place(title.rectTransform, new Vector2(0.04f, 0.85f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);

            Sprite spr = CardAdapter.GetCardSprite(subEvt.SubCard);
            var cardImg = WankulUiKit.CreateImage(modalObj.transform, "SubCardImg",
                spr != null ? Color.white : new Color(0.18f, 0.24f, 0.34f));
            cardImg.sprite = spr != null ? spr : WankulUiKit.WhiteSprite;
            cardImg.preserveAspect = true;
            cardImg.raycastTarget = false;
            WankulUiKit.Place(cardImg.rectTransform, new Vector2(0.05f, 0.20f), new Vector2(0.37f, 0.82f), Vector2.zero, Vector2.zero);

            string cardName = subEvt.SubCard.Name;
            string triggerDesc = string.IsNullOrWhiteSpace(subEvt.TriggerDescription)
                ? "Condition de réaction remplie."
                : subEvt.TriggerDescription;

            WankulCardData wCard = CardAdapter.GetWankulCard(subEvt.SubCard);
            string rulesText = wCard is EffigyCardData eff && !string.IsNullOrWhiteSpace(eff.Rules)
                ? eff.Rules
                : (wCard is TerrainCardData ter && !string.IsNullOrWhiteSpace(ter.SpecialEffect)
                    ? ter.SpecialEffect
                    : string.Join(", ", subEvt.SubCard.SubEffectIds));

            var infoHeader = WankulUiKit.CreateText(modalObj.transform, "SubCardHeader",
                $"{cardName}\nForce : {subEvt.SubCard.Force}  |  Coût (Sub) : {subEvt.SubCard.Cost}",
                17, FontStyle.Bold, TextAnchor.UpperLeft, new Color(1f, 0.88f, 0.42f));
            infoHeader.raycastTarget = false;
            WankulUiKit.Place(infoHeader.rectTransform, new Vector2(0.40f, 0.64f), new Vector2(0.95f, 0.82f), Vector2.zero, Vector2.zero);

            var triggerText = WankulUiKit.CreateText(modalObj.transform, "SubTriggerDesc",
                $"Déclencheur : {triggerDesc}\n\nEffet : {rulesText}",
                14, FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            triggerText.raycastTarget = false;
            WankulUiKit.Place(triggerText.rectTransform, new Vector2(0.40f, 0.22f), new Vector2(0.95f, 0.62f), Vector2.zero, Vector2.zero);

            var acceptBtn = WankulUiKit.CreateButton(modalObj.transform, "ACTIVER (SUB)",
                new Color(0.14f, 0.56f, 0.28f, 0.96f),
                () =>
                {
                    DuelSfx.PlayPowerup();
                    CloseActiveModalAndAdvance(modalObj, () => subEvt.OnAcceptSub?.Invoke());
                });
            WankulUiKit.Place(acceptBtn.GetComponent<RectTransform>(),
                new Vector2(0.22f, 0.05f), new Vector2(0.48f, 0.16f), Vector2.zero, Vector2.zero);

            var declineBtn = WankulUiKit.CreateButton(modalObj.transform, "PASSER",
                new Color(0.45f, 0.18f, 0.22f, 0.95f),
                () =>
                {
                    DuelSfx.PlayCancel();
                    CloseActiveModalAndAdvance(modalObj, () => subEvt.OnDeclineSub?.Invoke());
                });
            WankulUiKit.Place(declineBtn.GetComponent<RectTransform>(),
                new Vector2(0.52f, 0.05f), new Vector2(0.78f, 0.16f), Vector2.zero, Vector2.zero);
        }

        private void ShowGameOverModal(PlayerId? winner, GameOverReason reason)
        {
            _pendingModalQueue.Clear();
            if (_activeChoiceModal != null) Destroy(_activeChoiceModal);

            var font = WankulUiKit.GetFont();
            bool isWin = winner == PlayerId.Player1;

            var modalObj = new GameObject("GameOverModal");
            modalObj.transform.SetParent(_hudCanvasObj.transform, false);
            var mRt = modalObj.AddComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.28f, 0.28f);
            mRt.anchorMax = new Vector2(0.72f, 0.72f);
            mRt.offsetMin = Vector2.zero;
            mRt.offsetMax = Vector2.zero;

            var bg = modalObj.AddComponent<Image>();
            bg.sprite = WankulUiKit.WhiteSprite;
            bg.color = new Color(0.06f, 0.08f, 0.14f, 0.97f);
            var outline = modalObj.AddComponent<Outline>();
            outline.effectColor = isWin ? new Color(1f, 0.85f, 0.3f, 0.95f) : new Color(0.95f, 0.3f, 0.3f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);

            _activeChoiceModal = modalObj;

            string title = isWin ? "★ VICTOIRE ! ★" : "DÉFAITE...";
            Color titleColor = isWin ? new Color(1f, 0.88f, 0.35f) : new Color(1f, 0.42f, 0.42f);

            CreateHudText(modalObj.transform, font, title, 34, FontStyle.Bold,
                new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.90f), TextAnchor.MiddleCenter, titleColor);

            string reasonLabel = reason == GameOverReason.DeckOut
                ? (isWin ? "Victoire par meule (pioche et main adverses vides) !" : "Défaite par meule (pioche et main vides)...")
                : $"Score final : {_engine.State.ScoreP1} - {_engine.State.ScoreP2}";

            CreateHudText(modalObj.transform, font, reasonLabel, 18, FontStyle.Normal,
                new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.58f), TextAnchor.MiddleCenter, Color.white);

            var exitBtn = WankulUiKit.CreateButton(modalObj.transform, "RETOURNER EN BOUTIQUE",
                isWin ? new Color(0.20f, 0.62f, 0.35f) : new Color(0.55f, 0.22f, 0.25f),
                () => ExitDuelAndReportResult(isWin));
            WankulUiKit.Place(exitBtn.GetComponent<RectTransform>(), new Vector2(0.18f, 0.10f), new Vector2(0.82f, 0.28f), Vector2.zero, Vector2.zero);
        }

        private void ExitDuelAndReportResult(bool isPlayerWin)
        {
            if (_isClosing) return;
            _isClosing = true;

            var table = _playTable;
            RestoreCamera();
            Destroy(gameObject);

            if (table != null)
            {
                try
                {
                    table.ReportWinner(isPlayerWin, isDraw: false);
                    if (table.m_PlayCardSetUIScreen_WinLoseScreen != null)
                    {
                        table.m_PlayCardSetUIScreen_WinLoseScreen.OnPressLeaveButton();
                    }
                    else
                    {
                        table.FinishLeaveGame(isPlayerWin);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogWarning($"[DuelView3D] Exception FinishLeaveGame (gérée): {ex.Message}");
                }
            }
        }

        private void PushLog(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            _recentLogs.Enqueue($"• {message}");
            while (_recentLogs.Count > 7) _recentLogs.Dequeue();

            if (_logFeedText != null)
            {
                _logFeedText.text = string.Join("\n", _recentLogs);
            }
        }

        private static GameObject CreateNonBlockingPanel(
            Transform parent,
            string name,
            Vector2 aMin,
            Vector2 aMax,
            Color bgColor,
            Color outlineColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.sprite = WankulUiKit.WhiteSprite;
            img.color = bgColor;
            img.raycastTarget = false;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return go;
        }

        private static Text CreateHudText(
            Transform parent,
            Font font,
            string text,
            int size,
            FontStyle style,
            Vector2 aMin,
            Vector2 aMax,
            TextAnchor align,
            Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var txt = go.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.alignment = align;
            txt.color = color;
            txt.text = text;
            txt.raycastTarget = false;
            return txt;
        }
    }
}
