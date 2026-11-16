using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WankulCrazy.Duel.Engine;
using WankulCrazyPlugin.importer;

namespace WankulCrazyPlugin.duel
{
    /// <summary>
    /// Gère la géométrie 3D du plateau sur <see cref="PlayTableGame"/> :
    /// ancrages des 3 slots Terrains, rangées Personnages J1/J2, mains J1/J2, piles Pioche/Défausse,
    /// zones de raycast 3D des slots et badges World-Space intégrés sur le tapis.
    /// </summary>
    public sealed class DuelBoard3D
    {
        private const int IgnoreRaycastLayer = 2;
        private const float SlotSpacingX = 0.23f;
        private const float SlotHalfWidth = 0.105f;
        private const float SlotHalfDepth = 0.20f;

        private readonly PlayTableGame _game;
        private readonly Transform _root;
        private readonly List<Animation> _disabledVanillaAnims = new List<Animation>();
        private readonly List<GameObject> _disabledVanillaVfx = new List<GameObject>();

        private sealed class SlotVisual
        {
            public int Index;
            public GameObject Root;
            public Collider HitCollider;
            public Image FrameBg;
            public Outline FrameOutline;
            public Text TerrainBadge;
            public Text ForceP1Text;
            public Text ForceP2Text;
            public GameObject ComboBadgeP1;
            public GameObject ComboBadgeP2;
            public Image CardTargetZone;
            public Outline CardTargetOutline;
            public Color BaseFrameColor;
        }

        private readonly SlotVisual[] _slots = new SlotVisual[3];
        private readonly Dictionary<Collider, int> _slotByCollider = new Dictionary<Collider, int>();
        private Collider _p1DiscardCollider;
        private Collider _p2DiscardCollider;

        private Text _p1DeckCountText;
        private Text _p2DeckCountText;
        private Text _p1DiscardCountText;
        private Text _p2DiscardCountText;

        public Vector3 TableCenter { get; private set; }
        public Vector3 TableUp { get; private set; }
        public Vector3 TableForward { get; private set; }
        public Vector3 TableRight { get; private set; }

        public Quaternion FaceUpFlatRot { get; private set; }
        public Quaternion FaceDownFlatRot { get; private set; }
        public Quaternion HandP1Rot { get; private set; }
        public Quaternion HandP2Rot { get; private set; }

        public Vector3 DeckPosP1 { get; private set; }
        public Vector3 DeckPosP2 { get; private set; }
        public Vector3 DiscardPosP1 { get; private set; }
        public Vector3 DiscardPosP2 { get; private set; }
        public Vector3 CenterShowPosP1 { get; private set; }

        public DuelBoard3D(PlayTableGame game, Transform root)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _root = root ?? throw new ArgumentNullException(nameof(root));

            ComputeBoardFrame();
            HideVanillaTableDecorations();
            BuildSlotZonesAndBadges();
            BuildPileBadges();
        }

        private void ComputeBoardFrame()
        {
            var pSet = _game.m_PlayCardSetPlayer;
            var eSet = _game.m_PlayCardSetEnemy;

            TableUp = _game.transform.up.sqrMagnitude > 0.001f ? _game.transform.up.normalized : Vector3.up;

            Vector3 pToE = Vector3.zero;
            if (pSet != null && eSet != null)
            {
                pToE = Vector3.ProjectOnPlane(eSet.transform.position - pSet.transform.position, TableUp);
            }
            if (pToE.sqrMagnitude < 0.0001f)
            {
                pToE = Vector3.ProjectOnPlane(_game.transform.forward, TableUp);
            }
            TableForward = pToE.sqrMagnitude > 0.0001f ? pToE.normalized : Vector3.forward;
            TableRight = Vector3.Cross(TableUp, TableForward).normalized;

            float surfaceY = _game.transform.position.y + 0.78f;
            if (pSet != null && pSet.m_ElementAreaPosList != null && pSet.m_ElementAreaPosList.Count > 0 &&
                pSet.m_ElementAreaPosList[0] != null && pSet.m_ElementAreaPosList[0].transformList != null &&
                pSet.m_ElementAreaPosList[0].transformList.Count > 0 &&
                pSet.m_ElementAreaPosList[0].transformList[0] != null)
            {
                surfaceY = pSet.m_ElementAreaPosList[0].transformList[0].position.y;
            }
            else if (pSet != null && pSet.m_DiscardStartPos != null)
            {
                surfaceY = pSet.m_DiscardStartPos.position.y;
            }
            else if (pSet != null && pSet.m_CenterAreaShowPos != null)
            {
                surfaceY = pSet.m_CenterAreaShowPos.position.y;
            }

            Vector3 rawCenter = _game.m_CamDownTarget != null
                ? _game.m_CamDownTarget.position
                : ((pSet != null && eSet != null) ? (pSet.transform.position + eSet.transform.position) * 0.5f : _game.transform.position);

            // Ajuste Y sur le plan réel du tapis
            float deltaY = surfaceY - Vector3.Dot(rawCenter, TableUp);
            TableCenter = rawCenter + TableUp * (deltaY + 0.003f);

            // Rotation face visible sur table récupérée depuis le prefab du joueur
            if (pSet != null && pSet.m_ElementAreaPosList != null && pSet.m_ElementAreaPosList.Count > 0 &&
                pSet.m_ElementAreaPosList[0] != null && pSet.m_ElementAreaPosList[0].transformList != null &&
                pSet.m_ElementAreaPosList[0].transformList.Count > 0 &&
                pSet.m_ElementAreaPosList[0].transformList[0] != null)
            {
                FaceUpFlatRot = pSet.m_ElementAreaPosList[0].transformList[0].rotation;
            }
            else if (pSet != null && pSet.m_DiscardStartPos != null)
            {
                FaceUpFlatRot = pSet.m_DiscardStartPos.rotation;
            }
            else
            {
                FaceUpFlatRot = Quaternion.LookRotation(TableUp, TableForward);
            }

            // Face cachée (dos vers le haut) : rotation 180° autour de l'axe droit/avant
            if (pSet != null && pSet.m_DeckStartPos != null)
            {
                FaceDownFlatRot = pSet.m_DeckStartPos.rotation;
            }
            else
            {
                FaceDownFlatRot = Quaternion.AngleAxis(180f, TableForward) * FaceUpFlatRot;
            }

            // Main J1 relevée vers la caméra plongeante (~36°) pour lecture facile
            HandP1Rot = Quaternion.AngleAxis(-36f, TableRight) * FaceUpFlatRot;
            // Main J2 côté opposé, face cachée légèrement relevée vers le joueur adverse
            HandP2Rot = Quaternion.AngleAxis(24f, TableRight) * (Quaternion.AngleAxis(180f, TableUp) * FaceDownFlatRot);

            DeckPosP1 = ProjectToTableSurface(pSet != null && pSet.m_DeckStartPos != null
                ? pSet.m_DeckStartPos.position
                : TableCenter + TableRight * 0.43f - TableForward * 0.14f);
            DiscardPosP1 = ProjectToTableSurface(pSet != null && pSet.m_DiscardStartPos != null
                ? pSet.m_DiscardStartPos.position
                : TableCenter - TableRight * 0.43f - TableForward * 0.14f);

            DeckPosP2 = ProjectToTableSurface(eSet != null && eSet.m_DeckStartPos != null
                ? eSet.m_DeckStartPos.position
                : TableCenter - TableRight * 0.43f + TableForward * 0.16f);
            DiscardPosP2 = ProjectToTableSurface(eSet != null && eSet.m_DiscardStartPos != null
                ? eSet.m_DiscardStartPos.position
                : TableCenter + TableRight * 0.43f + TableForward * 0.16f);

            CenterShowPosP1 = pSet != null && pSet.m_CenterAreaShowPos != null
                ? pSet.m_CenterAreaShowPos.position
                : TableCenter - TableForward * 0.10f + TableUp * 0.16f;
        }

        private Vector3 ProjectToTableSurface(Vector3 pos)
        {
            Vector3 offset = pos - TableCenter;
            Vector3 planar = Vector3.ProjectOnPlane(offset, TableUp);
            return TableCenter + planar;
        }

        private void HideVanillaTableDecorations()
        {
            DisableSetDecorations(_game.m_PlayCardSetPlayer);
            DisableSetDecorations(_game.m_PlayCardSetEnemy);
        }

        private void DisableSetDecorations(PlayCardSet set)
        {
            if (set == null) return;

            if (set.m_PlayCardElemAtkAnimList != null)
            {
                for (int i = 0; i < set.m_PlayCardElemAtkAnimList.Count; i++)
                {
                    var anim = set.m_PlayCardElemAtkAnimList[i];
                    if (anim != null && anim.gameObject.activeSelf)
                    {
                        anim.gameObject.SetActive(false);
                        _disabledVanillaAnims.Add(anim);
                    }
                }
            }

            if (set.m_EvoVFXList != null)
            {
                for (int i = 0; i < set.m_EvoVFXList.Count; i++)
                {
                    var vfx = set.m_EvoVFXList[i];
                    if (vfx != null && vfx.activeSelf)
                    {
                        vfx.SetActive(false);
                        _disabledVanillaVfx.Add(vfx);
                    }
                }
            }
        }

        private void BuildSlotZonesAndBadges()
        {
            var font = WankulUiKit.GetFont();

            for (int i = 0; i < 3; i++)
            {
                Vector3 slotPos = GetSlotCenterWorld(i);

                var slotGo = new GameObject($"WankulSlotZone_{i}");
                slotGo.layer = IgnoreRaycastLayer;
                slotGo.transform.SetParent(_root, false);
                slotGo.transform.position = slotPos;
                slotGo.transform.rotation = Quaternion.LookRotation(-TableUp, TableForward);

                var col = slotGo.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(SlotHalfWidth * 2f, SlotHalfDepth * 2f, 0.03f);
                _slotByCollider[col] = i;

                // Canvas World-Space plaqué à +2mm du tapis (1 px UI = 0.001 m -> 200x360 px = 0.20m x 0.36m)
                var canvasGo = new GameObject($"SlotCanvas_{i}");
                canvasGo.transform.SetParent(slotGo.transform, false);
                canvasGo.transform.localPosition = new Vector3(0f, 0f, -0.002f);
                canvasGo.transform.localRotation = Quaternion.identity;
                canvasGo.transform.localScale = Vector3.one * 0.001f;

                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 5;

                var rt = canvasGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(210f, 380f);

                var frameImg = canvasGo.AddComponent<Image>();
                frameImg.sprite = WankulUiKit.WhiteSprite;
                frameImg.color = new Color(0.06f, 0.09f, 0.15f, 0.32f);

                var outline = canvasGo.AddComponent<Outline>();
                outline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.45f);
                outline.effectDistance = new Vector2(2f, -2f);

                // Zone centrale réservée au Terrain
                var terrainBoxGo = new GameObject("TerrainCenterGuide");
                terrainBoxGo.transform.SetParent(canvasGo.transform, false);
                var tRt = terrainBoxGo.AddComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0.14f, 0.37f);
                tRt.anchorMax = new Vector2(0.86f, 0.63f);
                tRt.offsetMin = Vector2.zero;
                tRt.offsetMax = Vector2.zero;
                var tBg = terrainBoxGo.AddComponent<Image>();
                tBg.sprite = WankulUiKit.WhiteSprite;
                tBg.color = new Color(0.10f, 0.14f, 0.22f, 0.36f);
                var tOutline = terrainBoxGo.AddComponent<Outline>();
                tOutline.effectColor = new Color(1f, 0.85f, 0.35f, 0.4f);
                tOutline.effectDistance = new Vector2(1.5f, -1.5f);

                // Canvas surélevé (+38mm au-dessus du tapis) pour que les badges de Force & Statut
                // restent toujours au-dessus des piles de cartes 3D
                var overlayGo = new GameObject($"SlotOverlayCanvas_{i}");
                overlayGo.transform.SetParent(slotGo.transform, false);
                overlayGo.transform.localPosition = new Vector3(0f, 0f, -0.038f);
                overlayGo.transform.localRotation = Quaternion.identity;
                overlayGo.transform.localScale = Vector3.one * 0.001f;

                var overlayCanvas = overlayGo.AddComponent<Canvas>();
                overlayCanvas.renderMode = RenderMode.WorldSpace;
                overlayCanvas.sortingOrder = 60;
                overlayGo.GetComponent<RectTransform>().sizeDelta = new Vector2(210f, 380f);

                // Badge Terrain / Statut avec cartouche sombre opaque
                var badgePill = CreatePillBox(overlayGo.transform, "TerrainStatusPill",
                    new Vector2(0.06f, 0.305f), new Vector2(0.94f, 0.375f),
                    new Color(0.05f, 0.08f, 0.14f, 0.92f), new Color(1f, 0.85f, 0.35f, 0.75f));
                var badgeText = CreateWorldText(badgePill.transform, font, $"TERRAIN {i + 1}", 13,
                    Vector2.zero, Vector2.one, new Color(1f, 0.88f, 0.45f));

                // Capsule Force J2 (haut du slot, bien détachée et opaque)
                var forceP2Pill = CreatePillBox(overlayGo.transform, "ForceP2Pill",
                    new Vector2(0.08f, 0.92f), new Vector2(0.92f, 1.03f),
                    new Color(0.16f, 0.05f, 0.08f, 0.95f), new Color(1f, 0.42f, 0.42f, 0.90f));
                var forceP2Text = CreateWorldText(forceP2Pill.transform, font, "ADV : 0", 19,
                    Vector2.zero, Vector2.one, new Color(1f, 0.58f, 0.58f));

                // Capsule Force J1 (bas du slot, bien détachée et opaque)
                var forceP1Pill = CreatePillBox(overlayGo.transform, "ForceP1Pill",
                    new Vector2(0.08f, -0.03f), new Vector2(0.92f, 0.08f),
                    new Color(0.04f, 0.11f, 0.20f, 0.95f), new Color(0.35f, 0.82f, 1f, 0.90f));
                var forceP1Text = CreateWorldText(forceP1Pill.transform, font, "VOUS : 0", 19,
                    Vector2.zero, Vector2.one, new Color(0.50f, 0.90f, 1f));

                // Indicateurs Combo J1 / J2 flottants au-dessus des cartes
                var comboP1 = CreateComboPill(overlayGo.transform, font, new Vector2(0.22f, 0.09f), new Vector2(0.78f, 0.15f));
                var comboP2 = CreateComboPill(overlayGo.transform, font, new Vector2(0.22f, 0.85f), new Vector2(0.78f, 0.91f));

                _slots[i] = new SlotVisual
                {
                    Index = i,
                    Root = slotGo,
                    HitCollider = col,
                    FrameBg = frameImg,
                    FrameOutline = outline,
                    TerrainBadge = badgeText,
                    ForceP1Text = forceP1Text,
                    ForceP2Text = forceP2Text,
                    ComboBadgeP1 = comboP1,
                    ComboBadgeP2 = comboP2,
                    CardTargetZone = tBg,
                    CardTargetOutline = tOutline,
                    BaseFrameColor = new Color(0.06f, 0.09f, 0.15f, 0.32f)
                };
            }
        }

        private void BuildPileBadges()
        {
            var font = WankulUiKit.GetFont();
            _p1DeckCountText = CreatePileCounter(DeckPosP1 - TableForward * 0.075f, font, "Pioche", new Color(0.55f, 0.88f, 1f));
            _p1DiscardCountText = CreatePileCounter(DiscardPosP1 - TableForward * 0.075f, font, "Défausse", new Color(0.85f, 0.85f, 0.9f));
            _p2DeckCountText = CreatePileCounter(DeckPosP2 + TableForward * 0.075f, font, "Pioche IA", new Color(1f, 0.55f, 0.55f));
            _p2DiscardCountText = CreatePileCounter(DiscardPosP2 + TableForward * 0.075f, font, "Défausse IA", new Color(0.85f, 0.72f, 0.72f));

            _p1DiscardCollider = CreateDiscardHitZone("DiscardHitZone_P1", DiscardPosP1);
            _p2DiscardCollider = CreateDiscardHitZone("DiscardHitZone_P2", DiscardPosP2);
        }

        private Collider CreateDiscardHitZone(string name, Vector3 worldPos)
        {
            var go = new GameObject(name);
            go.layer = IgnoreRaycastLayer;
            go.transform.SetParent(_root, false);
            go.transform.position = worldPos + TableUp * 0.02f;
            go.transform.rotation = Quaternion.LookRotation(-TableUp, TableForward);
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(0.14f, 0.18f, 0.06f);
            return col;
        }

        public int ResolveDiscardPileCollider(Collider col)
        {
            if (col == null) return 0;
            if (col == _p1DiscardCollider) return 1;
            if (col == _p2DiscardCollider) return 2;
            return 0;
        }

        private Text CreatePileCounter(Vector3 worldPos, Font font, string label, Color color)
        {
            var go = new GameObject($"PileLabel_{label}");
            go.transform.SetParent(_root, false);
            go.transform.position = worldPos + TableUp * 0.004f;
            go.transform.rotation = Quaternion.LookRotation(-TableUp, TableForward);
            go.transform.localScale = Vector3.one * 0.001f;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 6;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(130f, 32f);

            var bg = go.AddComponent<Image>();
            bg.sprite = WankulUiKit.WhiteSprite;
            bg.color = new Color(0.05f, 0.07f, 0.12f, 0.78f);

            return CreateWorldText(go.transform, font, $"{label}: 0", 13, Vector2.zero, Vector2.one, color);
        }

        private static Text CreateWorldText(Transform parent, Font font, string initialText, int fontSize, Vector2 aMin, Vector2 aMax, Color color)
        {
            var go = new GameObject("Txt");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var txt = go.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.text = initialText;
            return txt;
        }

        private static GameObject CreatePillBox(Transform parent, string name, Vector2 aMin, Vector2 aMax, Color bgColor, Color outlineColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.sprite = WankulUiKit.WhiteSprite;
            bg.color = bgColor;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1.8f, -1.8f);
            return go;
        }

        private static GameObject CreateComboPill(Transform parent, Font font, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject("ComboPill");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.sprite = WankulUiKit.WhiteSprite;
            bg.color = new Color(1f, 0.72f, 0.12f, 0.85f);

            CreateWorldText(go.transform, font, "★ COMBO ★", 11, Vector2.zero, Vector2.one, new Color(0.08f, 0.06f, 0.02f));
            go.SetActive(false);
            return go;
        }

        public int ResolveSlotCollider(Collider col)
        {
            if (col == null) return -1;
            return _slotByCollider.TryGetValue(col, out int idx) ? idx : -1;
        }

        public Vector3 GetSlotCenterWorld(int slotIndex)
        {
            float x = (slotIndex - 1) * SlotSpacingX;
            return TableCenter + TableRight * x + TableForward * 0.01f;
        }

        public Vector3 GetTerrainWorldPos(int slotIndex)
        {
            return GetSlotCenterWorld(slotIndex) + TableUp * 0.008f;
        }

        public Quaternion GetTerrainRotation(bool isActive)
        {
            if (isActive) return FaceUpFlatRot;
            return Quaternion.AngleAxis(90f, TableUp) * FaceUpFlatRot;
        }

        public Vector3 GetCharacterWorldPos(int slotIndex, bool isPlayer1, int indexInSlot, int totalInSlot)
        {
            Vector3 basePos = GetSlotCenterWorld(slotIndex);
            float forwardOffset = isPlayer1 ? -0.115f : 0.135f;

            float spread = totalInSlot <= 1 ? 0f : Mathf.Min(0.048f, 0.135f / (totalInSlot - 1));
            float xOffset = (indexInSlot - (totalInSlot - 1) * 0.5f) * spread;
            float stackLift = indexInSlot * 0.0025f;

            return basePos
                + TableForward * forwardOffset
                + TableRight * xOffset
                + TableUp * (0.012f + stackLift);
        }

        public Vector3 GetHandCardWorldPos(bool isPlayer1, int cardIndex, int totalCards, int hoveredCardIndex = -1)
        {
            if (totalCards <= 0) totalCards = 1;
            float maxArcSpan = isPlayer1 ? 0.62f : 0.42f;
            float spacing = totalCards <= 1 ? 0f : Mathf.Min(0.095f, maxArcSpan / (totalCards - 1));
            float centerIdx = (totalCards - 1) * 0.5f;
            float rel = cardIndex - centerIdx;

            float xPos = rel * spacing;
            // Écarter les cartes voisines de la carte survolée pour une lisibilité parfaite même avec 8+ cartes en main
            if (isPlayer1 && hoveredCardIndex >= 0 && totalCards >= 5 && cardIndex != hoveredCardIndex)
            {
                float dir = cardIndex < hoveredCardIndex ? -1f : 1f;
                float dist = Mathf.Abs(cardIndex - hoveredCardIndex);
                float push = dist == 1 ? 0.026f : 0.014f;
                xPos += dir * push;
            }

            float forwardDist = isPlayer1 ? -0.255f : 0.275f;
            float height = isPlayer1 ? 0.068f : 0.032f;
            // Léger éventail vertical pour éviter le Z-fighting entre cartes adjacentes
            float overlapLift = cardIndex * 0.0018f;

            return TableCenter
                + TableRight * xPos
                + TableForward * forwardDist
                + TableUp * (height + overlapLift);
        }

        /// <summary>
        /// Détermine si le joueur vise le côté Gauche (insertIndex = 0) ou Droit (insertIndex = -1)
        /// de sa ligne de personnages sur un terrain pour former un Combo.
        /// </summary>
        public int ComputeInsertIndexForSlot(int slotIndex, Vector3 worldHitPoint, int existingAlliedChars)
        {
            if (existingAlliedChars <= 0) return -1;
            float localX = Vector3.Dot(worldHitPoint - GetSlotCenterWorld(slotIndex), TableRight);
            return localX < -0.015f ? 0 : -1;
        }

        public void TriggerSlotEvoVfx(int slotIndex, bool isPlayer1)
        {
            try
            {
                var set = isPlayer1 ? _game.m_PlayCardSetPlayer : _game.m_PlayCardSetEnemy;
                if (set?.m_EvoVFXList != null && slotIndex >= 0 && slotIndex < set.m_EvoVFXList.Count)
                {
                    var vfx = set.m_EvoVFXList[slotIndex];
                    if (vfx != null)
                    {
                        vfx.transform.position = GetSlotCenterWorld(slotIndex) + TableUp * 0.02f;
                        vfx.SetActive(false);
                        vfx.SetActive(true);
                    }
                }
            }
            catch { }
        }

        public void TriggerClashVfx(int slotIndex, bool isPlayer1)
        {
            try
            {
                var set = isPlayer1 ? _game.m_PlayCardSetPlayer : _game.m_PlayCardSetEnemy;
                if (set?.m_DamagedVFX != null)
                {
                    set.m_DamagedVFX.transform.position = GetSlotCenterWorld(slotIndex) + TableUp * 0.03f;
                    set.m_DamagedVFX.SetActive(false);
                    set.m_DamagedVFX.SetActive(true);
                }
            }
            catch { }
        }

        /// <summary>
        /// Met à jour les badges de Force, l'état des terrains, les indicateurs Combo et les compteurs de piles.
        /// </summary>
        public void UpdateBoardVisuals(DuelState state, string selectedHandCardId, int hoveredSlotIndex, int forceHighlightSlot = -1, int previewInsertIndex = -1)
        {
            if (state == null) return;

            DuelCard selectedCard = null;
            if (!string.IsNullOrEmpty(selectedHandCardId))
            {
                var p1Hand = state.HandP1;
                for (int i = 0; i < p1Hand.Count; i++)
                {
                    if (p1Hand[i].Id == selectedHandCardId)
                    {
                        selectedCard = p1Hand[i];
                        break;
                    }
                }
            }

            for (int i = 0; i < 3 && i < state.Slots.Count; i++)
            {
                var slot = state.Slots[i];
                var vis = _slots[i];
                if (vis == null) continue;

                int f1 = slot.GetForce(PlayerId.Player1);
                int f2 = slot.GetForce(PlayerId.Player2);
                vis.ForceP1Text.text = $"VOUS : {f1}";
                vis.ForceP2Text.text = $"ADV : {f2}";

                bool canAutoScoreP1 = !slot.IsEmpty &&
                                      slot.IsActive(state.TurnNumber) &&
                                      state.ActivePlayer == PlayerId.Player1 &&
                                      !state.TerrainRequirementPending &&
                                      slot.CharactersP1.Count > 0 &&
                                      f1 >= state.Rules.AutoScoreForceThreshold;

                if (slot.IsEmpty)
                {
                    vis.TerrainBadge.text = $"LIBRE ({i + 1})";
                    vis.TerrainBadge.color = new Color(0.65f, 0.72f, 0.85f);
                }
                else if (hoveredSlotIndex == i && selectedCard != null && selectedCard.Kind == CardKind.Character && slot.CharactersP1.Count > 0)
                {
                    bool formsCombo = previewInsertIndex == 0
                        ? ComboEvaluator.CanConnect(selectedCard, slot.CharactersP1[0])
                        : ComboEvaluator.CanConnect(slot.CharactersP1[slot.CharactersP1.Count - 1], selectedCard);
                    string sideLabel = previewInsertIndex == 0 ? "◀ GAUCHE" : "DROITE ▶";
                    vis.TerrainBadge.text = formsCombo ? $"{sideLabel} ★ COMBO !" : $"POSER {sideLabel}";
                    vis.TerrainBadge.color = formsCombo ? new Color(1f, 0.88f, 0.22f) : new Color(0.55f, 0.95f, 1f);
                }
                else if (canAutoScoreP1)
                {
                    vis.TerrainBadge.text = $"⚡ SCORER ({f1}≥{state.Rules.AutoScoreForceThreshold})";
                    vis.TerrainBadge.color = new Color(1f, 0.92f, 0.22f);
                }
                else if (slot.IsActive(state.TurnNumber))
                {
                    vis.TerrainBadge.text = $"ACTIF ({state.Rules.AutoScoreForceThreshold}+)";
                    vis.TerrainBadge.color = new Color(0.45f, 1f, 0.55f);
                }
                else
                {
                    vis.TerrainBadge.text = "EN ATTENTE (90°)";
                    vis.TerrainBadge.color = new Color(1f, 0.78f, 0.35f);
                }

                bool hasComboP1 = false;
                for (int c = 1; c < slot.CharactersP1.Count; c++)
                {
                    if (ComboEvaluator.IsComboActive(slot.CharactersP1, c))
                    {
                        hasComboP1 = true;
                        break;
                    }
                }
                vis.ComboBadgeP1.SetActive(hasComboP1);

                bool hasComboP2 = false;
                for (int c = 1; c < slot.CharactersP2.Count; c++)
                {
                    if (ComboEvaluator.IsComboActive(slot.CharactersP2, c))
                    {
                        hasComboP2 = true;
                        break;
                    }
                }
                vis.ComboBadgeP2.SetActive(hasComboP2);

                // Surbrillance interactive du slot selon carte sélectionnée / draggée / clash / autoscore prêt
                bool isValidTarget = IsSlotValidForCard(state, i, selectedCard);
                if (forceHighlightSlot == i || canAutoScoreP1)
                {
                    vis.FrameBg.color = new Color(0.95f, 0.72f, 0.18f, 0.42f);
                    vis.FrameOutline.effectColor = new Color(1f, 0.88f, 0.35f, 0.95f);
                }
                else if (selectedCard != null && isValidTarget)
                {
                    bool hovered = hoveredSlotIndex == i;
                    vis.FrameBg.color = hovered
                        ? new Color(0.22f, 0.85f, 0.48f, 0.45f)
                        : new Color(0.15f, 0.65f, 0.35f, 0.30f);
                    vis.FrameOutline.effectColor = hovered
                        ? new Color(0.35f, 1f, 0.55f, 0.95f)
                        : new Color(0.25f, 0.92f, 0.48f, 0.75f);
                }
                else if (hoveredSlotIndex == i)
                {
                    vis.FrameBg.color = new Color(0.25f, 0.60f, 0.95f, 0.28f);
                    vis.FrameOutline.effectColor = new Color(0.40f, 0.82f, 1f, 0.75f);
                }
                else
                {
                    vis.FrameBg.color = vis.BaseFrameColor;
                    vis.FrameOutline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.42f);
                }
            }

            if (_p1DeckCountText != null) _p1DeckCountText.text = $"Pioche: {state.DeckP1.Count}";
            if (_p1DiscardCountText != null) _p1DiscardCountText.text = state.DiscardP1.Count > 0 ? $"Défausse: {state.DiscardP1.Count} 🔍" : "Défausse: 0";
            if (_p2DeckCountText != null) _p2DeckCountText.text = $"Pioche IA: {state.DeckP2.Count}";
            if (_p2DiscardCountText != null) _p2DiscardCountText.text = state.DiscardP2.Count > 0 ? $"Défausse IA: {state.DiscardP2.Count} 🔍" : "Défausse IA: 0";
        }

        public static bool IsSlotValidForCard(DuelState state, int slotIndex, DuelCard card)
        {
            if (state == null || card == null || slotIndex < 0 || slotIndex >= state.Slots.Count)
                return false;

            var slot = state.Slots[slotIndex];
            if (card.Kind == CardKind.Terrain)
            {
                return slot.IsEmpty;
            }
            if (card.Kind == CardKind.Character)
            {
                return !slot.IsEmpty && !state.TerrainRequirementPending && state.CharactersPlayedThisTurn < 4;
            }
            return false;
        }

        public void Cleanup()
        {
            for (int i = 0; i < _disabledVanillaAnims.Count; i++)
            {
                if (_disabledVanillaAnims[i] != null)
                {
                    _disabledVanillaAnims[i].gameObject.SetActive(true);
                }
            }
            _disabledVanillaAnims.Clear();

            for (int i = 0; i < _disabledVanillaVfx.Count; i++)
            {
                if (_disabledVanillaVfx[i] != null)
                {
                    _disabledVanillaVfx[i].SetActive(true);
                }
            }
            _disabledVanillaVfx.Clear();

            _slotByCollider.Clear();
        }
    }
}
