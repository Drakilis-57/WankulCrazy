using System;
using System.Collections.Generic;
using UnityEngine;
using WankulCrazy.Duel.Engine;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.patch;

namespace WankulCrazyPlugin.duel
{
    /// <summary>
    /// Fabrique et gestionnaire des cartes 3D (<see cref="InteractableCard3d"/> + <see cref="Card3dUIGroup"/>)
    /// posées sur la table PlayTableGame pendant un duel Wankul TCG.
    /// </summary>
    public sealed class DuelCard3dManager
    {
        public enum ZoneKind
        {
            None,
            DeckStack,
            DiscardStack,
            HandP1,
            HandP2,
            Terrain,
            CharacterP1,
            CharacterP2,
            Transient
        }

        public sealed class CardEntry
        {
            public string CardId;
            public DuelCard Card;
            public InteractableCard3d Card3d;
            public Card3dUIGroup CardUI;
            public Collider HitCollider;
            public ZoneKind Zone;
            public int SlotIndex = -1;
            public int OrderInZone;
            public bool IsFaceUp;
            public bool IsTapped90;
            public bool IsSelected;
            public bool IsHovered;
            public bool IsDragging;
            public Vector3 TargetWorldPos;
            public Quaternion TargetWorldRot;
            public float TargetScale = 1.6f;
            public float ArcProgress = 1f;
            public float ArcHeight = 0f;
        }

        private sealed class DyingCardAnim
        {
            public InteractableCard3d Card3d;
            public Vector3 StartPos;
            public Vector3 EndPos;
            public Quaternion StartRot;
            public Quaternion EndRot;
            public float Progress;
        }

        private const int IgnoreRaycastLayer = 2;
        private const float DefaultCardScale = 1.55f;

        private readonly Transform _root;
        private readonly Dictionary<string, CardEntry> _entriesById = new Dictionary<string, CardEntry>();
        private readonly Dictionary<Collider, CardEntry> _entriesByCollider = new Dictionary<Collider, CardEntry>();
        private readonly List<InteractableCard3d> _deckVisualP1 = new List<InteractableCard3d>();
        private readonly List<InteractableCard3d> _deckVisualP2 = new List<InteractableCard3d>();
        private readonly List<InteractableCard3d> _discardVisualP1 = new List<InteractableCard3d>();
        private readonly List<InteractableCard3d> _discardVisualP2 = new List<InteractableCard3d>();
        private readonly List<InteractableCard3d> _transientCards = new List<InteractableCard3d>();
        private readonly List<DyingCardAnim> _dyingCards = new List<DyingCardAnim>();

        private CardData _fallbackCardData;

        public DuelCard3dManager(Transform root)
        {
            _root = root;
        }

        public IReadOnlyDictionary<string, CardEntry> EntriesById => _entriesById;

        public CardEntry GetEntry(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;
            return _entriesById.TryGetValue(cardId, out var entry) ? entry : null;
        }

        public CardEntry ResolveCollider(Collider col)
        {
            if (col == null) return null;
            return _entriesByCollider.TryGetValue(col, out var entry) ? entry : null;
        }

        /// <summary>
        /// Récupère la carte 3D existante pour cet ID de duel, ou la crée via les spawners natifs.
        /// </summary>
        public CardEntry SpawnOrGetCard(DuelCard card, Vector3 spawnPos, Quaternion spawnRot, float scale = DefaultCardScale)
        {
            if (card == null) return null;

            if (_entriesById.TryGetValue(card.Id, out var existing) && existing.Card3d != null)
            {
                existing.Card = card;
                return existing;
            }

            if (!SpawnNativeCardVisual(card, spawnPos, spawnRot, scale, out var card3d, out var cardUI))
            {
                return null;
            }

            card3d.SetEnableCollision(false);
            if (_root != null)
            {
                card3d.transform.SetParent(_root, true);
            }

            Collider hitCol = AttachHitCollider(card3d);

            var entry = new CardEntry
            {
                CardId = card.Id,
                Card = card,
                Card3d = card3d,
                CardUI = cardUI,
                HitCollider = hitCol,
                Zone = ZoneKind.Transient,
                SlotIndex = -1,
                TargetWorldPos = spawnPos,
                TargetWorldRot = spawnRot,
                TargetScale = scale,
                IsFaceUp = true
            };

            _entriesById[card.Id] = entry;
            if (hitCol != null)
            {
                _entriesByCollider[hitCol] = entry;
            }

            return entry;
        }

        /// <summary>
        /// Déplace en douceur une carte 3D vers une cible monde (position, rotation, échelle),
        /// en appliquant un léger soulèvement en cas de survol ou de sélection.
        /// </summary>
        public void SetCardTarget(
            CardEntry entry,
            ZoneKind zone,
            int slotIndex,
            int orderInZone,
            Vector3 worldPos,
            Quaternion worldRot,
            float scale,
            bool isFaceUp,
            bool isTapped90,
            bool immediate = false)
        {
            if (entry == null || entry.Card3d == null) return;

            bool zoneChanged = entry.Zone != zone;
            entry.Zone = zone;
            entry.SlotIndex = slotIndex;
            entry.OrderInZone = orderInZone;
            entry.IsFaceUp = isFaceUp;
            entry.IsTapped90 = isTapped90;
            entry.TargetWorldPos = worldPos;
            entry.TargetWorldRot = worldRot;
            entry.TargetScale = scale;

            if (immediate)
            {
                entry.ArcProgress = 1f;
                entry.ArcHeight = 0f;
                entry.Card3d.StopLerpToTransform();
                entry.Card3d.transform.position = worldPos;
                entry.Card3d.transform.rotation = worldRot;
                entry.Card3d.SetTargetLocalScale(Vector3.one * scale);
            }
            else if (zoneChanged)
            {
                entry.ArcProgress = 0f;
                bool isBoardPlay = zone == ZoneKind.Terrain || zone == ZoneKind.CharacterP1 || zone == ZoneKind.CharacterP2;
                entry.ArcHeight = isBoardPlay ? 0.085f : 0.042f;
                try
                {
                    var anim = entry.CardUI?.m_CardUIAnimGrp?.GetComponent<Animation>();
                    if (anim != null)
                    {
                        anim.Play(isBoardPlay ? "CardSettleBounce" : "CardSettleGeneric");
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Met à jour chaque frame l'interpolation fluide des cartes 3D non-dragguées ainsi que les vols vers la Défausse.
        /// </summary>
        public void TickAnimations(float deltaTime, Vector3 tableUp)
        {
            float posSpeed = 14f;
            float rotSpeed = 14f;
            float tPos = 1f - Mathf.Exp(-posSpeed * deltaTime);
            float tRot = 1f - Mathf.Exp(-rotSpeed * deltaTime);

            foreach (var kv in _entriesById)
            {
                var entry = kv.Value;
                if (entry == null || entry.Card3d == null || entry.IsDragging)
                    continue;

                Vector3 goalPos = entry.TargetWorldPos;
                float goalScale = entry.TargetScale;

                if (entry.ArcProgress < 1f)
                {
                    entry.ArcProgress = Mathf.Min(1f, entry.ArcProgress + deltaTime * 3.8f);
                    goalPos += tableUp * (Mathf.Sin(entry.ArcProgress * Mathf.PI) * entry.ArcHeight);
                }

                if (entry.IsSelected)
                {
                    goalPos += tableUp * 0.045f;
                    goalScale *= 1.14f;
                }
                else if (entry.IsHovered)
                {
                    goalPos += tableUp * 0.022f;
                    goalScale *= 1.07f;
                }

                Transform tr = entry.Card3d.transform;
                tr.position = Vector3.Lerp(tr.position, goalPos, tPos);
                tr.rotation = Quaternion.Slerp(tr.rotation, entry.TargetWorldRot, tRot);
                entry.Card3d.SetTargetLocalScale(Vector3.one * goalScale);
            }

            // Animation des cartes en vol vers la pile de Défausse
            for (int i = _dyingCards.Count - 1; i >= 0; i--)
            {
                var anim = _dyingCards[i];
                if (anim == null || anim.Card3d == null)
                {
                    _dyingCards.RemoveAt(i);
                    continue;
                }

                anim.Progress += deltaTime * 3.6f;
                if (anim.Progress >= 1f)
                {
                    try { anim.Card3d.OnDestroyed(); } catch { }
                    _dyingCards.RemoveAt(i);
                }
                else
                {
                    float u = anim.Progress;
                    float smooth = u * u * (3f - 2f * u);
                    Vector3 pos = Vector3.Lerp(anim.StartPos, anim.EndPos, smooth)
                                  + tableUp * (Mathf.Sin(u * Mathf.PI) * 0.075f);
                    anim.Card3d.transform.position = pos;
                    anim.Card3d.transform.rotation = Quaternion.Slerp(anim.StartRot, anim.EndRot, smooth);
                }
            }
        }

        /// <summary>
        /// Met en surbrillance visuelle une carte sélectionnée ou cible de combo.
        /// </summary>
        public void SetHoverState(string hoveredCardId, string selectedCardId)
        {
            foreach (var kv in _entriesById)
            {
                var entry = kv.Value;
                if (entry == null) continue;
                entry.IsHovered = !string.IsNullOrEmpty(hoveredCardId) && entry.CardId == hoveredCardId;
                entry.IsSelected = !string.IsNullOrEmpty(selectedCardId) && entry.CardId == selectedCardId;
            }
        }

        /// <summary>
        /// Supprime proprement une carte 3D (ou la fait voler vers la Défausse si une cible est fournie).
        /// </summary>
        public void DestroyCard(string cardId, Vector3? flyToDiscardPos = null, Quaternion? flyToDiscardRot = null)
        {
            if (string.IsNullOrEmpty(cardId)) return;
            if (!_entriesById.TryGetValue(cardId, out var entry)) return;

            _entriesById.Remove(cardId);
            if (entry.HitCollider != null)
            {
                _entriesByCollider.Remove(entry.HitCollider);
            }

            if (entry.Card3d != null)
            {
                if (flyToDiscardPos.HasValue)
                {
                    _dyingCards.Add(new DyingCardAnim
                    {
                        Card3d = entry.Card3d,
                        StartPos = entry.Card3d.transform.position,
                        EndPos = flyToDiscardPos.Value,
                        StartRot = entry.Card3d.transform.rotation,
                        EndRot = flyToDiscardRot ?? entry.Card3d.transform.rotation,
                        Progress = 0f
                    });
                    return;
                }

                try
                {
                    entry.Card3d.OnDestroyed();
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogWarning($"[DuelCard3dManager] Erreur destruction Card3d {cardId}: {ex.Message}");
                }
            }
        }

        public void SpawnFlyingDiscardVisual(DuelCard card, Vector3 startPos, Vector3 endPos, Quaternion rot, float scale = DefaultCardScale)
        {
            if (card == null) return;
            if (!SpawnNativeCardVisual(card, startPos, rot, scale, out var card3d, out _))
                return;

            card3d.SetEnableCollision(false);
            if (_root != null)
            {
                card3d.transform.SetParent(_root, true);
            }

            _dyingCards.Add(new DyingCardAnim
            {
                Card3d = card3d,
                StartPos = startPos,
                EndPos = endPos,
                StartRot = rot,
                EndRot = rot,
                Progress = 0f
            });
        }

        /// <summary>
        /// Synchronise le nombre visuel de cartes empilées sur les zones Pioche et Défausse.
        /// </summary>
        public void UpdatePileStacks(
            bool isPlayer1,
            int deckCount,
            DuelCard topDiscardCard,
            int discardCount,
            Vector3 deckBasePos,
            Quaternion deckRot,
            Vector3 discardBasePos,
            Quaternion discardRot,
            Vector3 tableUp,
            float scale)
        {
            var deckList = isPlayer1 ? _deckVisualP1 : _deckVisualP2;
            int desiredDeckLayers = deckCount <= 0 ? 0 : Mathf.Clamp((deckCount + 7) / 8, 1, 6);
            SyncDummyStack(deckList, desiredDeckLayers, null, deckBasePos, deckRot, tableUp, scale * 0.92f);

            var discardList = isPlayer1 ? _discardVisualP1 : _discardVisualP2;
            int desiredDiscardLayers = discardCount <= 0 ? 0 : Mathf.Clamp((discardCount + 7) / 8, 1, 4);
            SyncDummyStack(discardList, desiredDiscardLayers, topDiscardCard, discardBasePos, discardRot, tableUp, scale * 0.92f);
        }

        private void SyncDummyStack(
            List<InteractableCard3d> list,
            int desiredCount,
            DuelCard topCard,
            Vector3 basePos,
            Quaternion rot,
            Vector3 tableUp,
            float scale)
        {
            while (list.Count > desiredCount)
            {
                int last = list.Count - 1;
                if (list[last] != null)
                {
                    try { list[last].OnDestroyed(); } catch { }
                }
                list.RemoveAt(last);
            }

            while (list.Count < desiredCount)
            {
                if (!SpawnNativeCardVisual(null, basePos, rot, scale, out var c3d, out _))
                    break;
                c3d.SetEnableCollision(false);
                if (_root != null) c3d.transform.SetParent(_root, true);
                list.Add(c3d);
            }

            for (int i = 0; i < list.Count; i++)
            {
                var c3d = list[i];
                if (c3d == null) continue;
                c3d.transform.position = basePos + tableUp * (i * 0.0045f);
                c3d.transform.rotation = rot;
                c3d.SetTargetLocalScale(Vector3.one * scale);

                // La carte tout en haut de la défausse affiche son illustration Wankul
                if (topCard != null && i == list.Count - 1 && c3d.m_Card3dUI != null && c3d.m_Card3dUI.m_CardUI != null)
                {
                    ApplyDuelCardVisuals(c3d.m_Card3dUI.m_CardUI, topCard);
                }
            }
        }

        /// <summary>
        /// Crée une carte 3D éphémère (ex: révélation de carte piochée/creusée) et la détruit après utilisation.
        /// </summary>
        public InteractableCard3d SpawnTransientCard(DuelCard card, Vector3 pos, Quaternion rot, float scale)
        {
            if (!SpawnNativeCardVisual(card, pos, rot, scale, out var c3d, out _))
                return null;
            c3d.SetEnableCollision(false);
            if (_root != null) c3d.transform.SetParent(_root, true);
            _transientCards.Add(c3d);
            return c3d;
        }

        public void DestroyTransientCard(InteractableCard3d c3d)
        {
            if (c3d == null) return;
            _transientCards.Remove(c3d);
            try { c3d.OnDestroyed(); } catch { }
        }

        /// <summary>
        /// Nettoie toutes les cartes 3D spawnées à la fin du duel.
        /// </summary>
        public void CleanupAll()
        {
            var ids = new List<string>(_entriesById.Keys);
            for (int i = 0; i < ids.Count; i++)
            {
                DestroyCard(ids[i]);
            }
            _entriesById.Clear();
            _entriesByCollider.Clear();

            CleanupDummyList(_deckVisualP1);
            CleanupDummyList(_deckVisualP2);
            CleanupDummyList(_discardVisualP1);
            CleanupDummyList(_discardVisualP2);
            CleanupDummyList(_transientCards);

            for (int i = 0; i < _dyingCards.Count; i++)
            {
                if (_dyingCards[i]?.Card3d != null)
                {
                    try { _dyingCards[i].Card3d.OnDestroyed(); } catch { }
                }
            }
            _dyingCards.Clear();
        }

        private static void CleanupDummyList(List<InteractableCard3d> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                {
                    try { list[i].OnDestroyed(); } catch { }
                }
            }
            list.Clear();
        }

        private bool SpawnNativeCardVisual(
            DuelCard duelCard,
            Vector3 spawnPos,
            Quaternion spawnRot,
            float scale,
            out InteractableCard3d card3d,
            out Card3dUIGroup cardUI)
        {
            card3d = null;
            cardUI = null;

            try
            {
                var spawner = CSingleton<Card3dUISpawner>.Instance;
                if (spawner == null)
                {
                    Plugin.Logger.LogError("[DuelCard3dManager] Card3dUISpawner.Instance introuvable.");
                    return false;
                }

                var spawnedObj = ShelfManager.SpawnInteractableObject(EObjectType.Card3d);
                if (spawnedObj == null)
                {
                    Plugin.Logger.LogError("[DuelCard3dManager] ShelfManager.SpawnInteractableObject(Card3d) a renvoyé null.");
                    return false;
                }

                card3d = spawnedObj.GetComponent<InteractableCard3d>();
                if (card3d == null)
                {
                    Plugin.Logger.LogError("[DuelCard3dManager] Composant InteractableCard3d manquant.");
                    return false;
                }

                cardUI = spawner.GetCardUI();
                if (cardUI == null || cardUI.m_CardUI == null)
                {
                    Plugin.Logger.LogError("[DuelCard3dManager] Card3dUIGroup ou m_CardUI null.");
                    card3d.OnDestroyed();
                    return false;
                }

                cardUI.m_IgnoreCulling = true;
                cardUI.m_CardUI.SetFoilCullListVisibility(true);
                cardUI.m_CardUI.ResetFarDistanceCull();
                if (cardUI.m_CardUIAnimGrp != null)
                {
                    cardUI.m_CardUIAnimGrp.gameObject.SetActive(true);
                }

                ApplyDuelCardVisuals(cardUI.m_CardUI, duelCard);

                cardUI.m_CardUI.SetFoilMaterialListFromSettingData(isWorldView: false);
                cardUI.m_CardUI.SetFoilBlendedMaterialListFromSettingData(isWorldView: false);

                card3d.transform.position = spawnPos;
                card3d.transform.rotation = spawnRot;
                cardUI.transform.position = spawnPos;
                cardUI.transform.rotation = spawnRot;

                card3d.SetCardUIFollow(cardUI);
                card3d.SetTargetLocalScale(Vector3.one * scale);
                card3d.SetTargetLocalPos(Vector3.zero);
                card3d.SetTargetRotation(Quaternion.identity);

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[DuelCard3dManager] Échec SpawnNativeCardVisual pour '{duelCard?.Name ?? "Dos"}': {ex}");
                return false;
            }
        }

        private void ApplyDuelCardVisuals(CardUI cardUI, DuelCard duelCard)
        {
            if (cardUI == null) return;

            var fallback = GetOrCreateFallbackCardData();

            if (duelCard == null)
            {
                cardUI.SetCardUI(fallback);
                return;
            }

            WankulCardData wCard = CardAdapter.GetWankulCard(duelCard);
            if (wCard != null)
            {
                CardData mapped = WankulCardsData.Instance.GetCardDataFromWankulCardData(wCard);
                if (mapped != null)
                {
                    cardUI.SetCardUI(mapped);
                    return;
                }

                // Carte Wankul valide mais non associée à un slot CardData : on applique directement le visuel Wankul
                bool isFoil = false;
                if (wCard is EffigyCardData eff)
                {
                    var rarityData = RaritiesManager.GetRarity(eff.RarityId);
                    isFoil = (rarityData != null && rarityData.IsEligibleForFoil) || eff.Rarity >= Rarity.UR1;
                }
                var tempData = new CardData
                {
                    monsterType = fallback.monsterType,
                    borderType = ECardBorderType.Base,
                    expansionType = ECardExpansionType.Tetramon,
                    isFoil = isFoil
                };
                Plugin.SetPProperty(cardUI, "m_CardData", tempData);
                ReplacingCards.ApplyWankulCardVisuals(cardUI, tempData, wCard);
                return;
            }

            // Fallback générique si la carte est synthétique (deck de secours)
            cardUI.SetCardUI(fallback);
            Sprite spr = CardAdapter.GetCardSprite(duelCard);
            if (spr != null && cardUI.m_CardFrontImage != null)
            {
                cardUI.m_CardFrontImage.sprite = spr;
                cardUI.m_CardFrontImage.gameObject.SetActive(true);
            }
        }

        private CardData GetOrCreateFallbackCardData()
        {
            if (_fallbackCardData == null)
            {
                _fallbackCardData = new CardData
                {
                    monsterType = EMonsterType.PiggyA,
                    borderType = ECardBorderType.Base,
                    expansionType = ECardExpansionType.Tetramon,
                    isFoil = false
                };
            }
            return _fallbackCardData;
        }

        private static Collider AttachHitCollider(InteractableCard3d card3d)
        {
            Vector3 size = new Vector3(0.085f, 0.12f, 0.015f);
            if (card3d.m_BoxCollider != null)
            {
                Vector3 src = card3d.m_BoxCollider.size;
                if (src.sqrMagnitude > 0.0001f)
                {
                    size = new Vector3(
                        Mathf.Max(src.x, 0.08f),
                        Mathf.Max(src.y, 0.11f),
                        Mathf.Max(src.z, 0.016f)
                    );
                }
            }

            var hitGo = new GameObject("DuelCardHitBox");
            hitGo.layer = IgnoreRaycastLayer;
            hitGo.transform.SetParent(card3d.transform, false);
            var box = hitGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            return box;
        }
    }
}
