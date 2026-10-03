#nullable enable
using System;
using System.Collections.Generic;

namespace WankulCrazy.Duel.Engine;

/// <summary>
/// Intelligence artificielle pour le moteur de duel Wankul TCG.
/// N'utilise aucun composant Unity et n'appelle que l'API publique de DuelEngine.
/// </summary>
public sealed class DuelAI
{
    public PlayerId Player { get; }
    private readonly IRandom _rng;

    public DuelAI(PlayerId player, IRandom? rng = null)
    {
        Player = player;
        _rng = rng ?? new SystemRandomAdapter();
    }

    /// <summary>
    /// Exécute les actions du tour de l'IA jusqu'à EndTurn.
    /// </summary>
    public void PlayTurn(DuelEngine engine)
    {
        if (engine.State.IsGameOver) return;
        if (engine.State.ActivePlayer != Player) return;

        // 1. Pose obligatoire de terrain si requis
        if (engine.State.TerrainRequirementPending)
        {
            TryPlayRequiredTerrain(engine);
            if (engine.State.TerrainRequirementPending)
            {
                // Si toujours pending (cas anormal sans terrain trouvé), on ne peut pas continuer
                return;
            }
        }

        // 2. Pose facultative d'un terrain si on en a plusieurs en main et qu'il y a un slot libre
        TryPlayOptionalTerrain(engine);

        // 3. Poser des personnages normaux sur des terrains existants
        PlayCharactersToReinforce(engine);

        // 4. Poser un Scoreur si on mène sur un terrain actif
        TryPlayScoreurToWin(engine);

        // 5. Terminer le tour
        if (!engine.State.IsGameOver && engine.State.ActivePlayer == Player)
        {
            engine.EndTurn(Player);
        }
    }

    private void TryPlayRequiredTerrain(DuelEngine engine)
    {
        var hand = engine.State.GetHand(Player);
        DuelCard? terrain = null;
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].Kind == CardKind.Terrain)
            {
                terrain = hand[i];
                break;
            }
        }

        if (terrain == null) return;

        int freeSlot = FindFirstFreeSlot(engine);
        if (freeSlot != -1)
        {
            engine.PlayTerrain(Player, terrain.Id, freeSlot);
        }
    }

    private void TryPlayOptionalTerrain(DuelEngine engine)
    {
        if (engine.State.TerrainsInPlayCount >= engine.Rules.MaxTerrainsOnBoard) return;

        var hand = engine.State.GetHand(Player);
        int terrainCountInHand = 0;
        DuelCard? candidate = null;

        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].Kind == CardKind.Terrain)
            {
                terrainCountInHand++;
                if (candidate == null) candidate = hand[i];
            }
        }

        // Si on a plus d'1 terrain en main et qu'il y a de la place sur le plateau
        if (terrainCountInHand > 1 && candidate != null)
        {
            int freeSlot = FindFirstFreeSlot(engine);
            if (freeSlot != -1)
            {
                engine.PlayTerrain(Player, candidate.Id, freeSlot);
            }
        }
    }

    private void PlayCharactersToReinforce(DuelEngine engine)
    {
        var availableSlots = GetSlotsWithTerrain(engine);
        if (availableSlots.Count == 0) return;

        while (!engine.State.IsGameOver &&
               engine.State.CharactersPlayedThisTurn < engine.Rules.MaxCharactersPerTurn)
        {
            var hand = engine.State.GetHand(Player);
            DuelCard? bestChar = null;

            // On cherche un personnage régulier (non scoreur)
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].Kind == CardKind.Character && !hand[i].IsScoreur)
                {
                    if (bestChar == null || hand[i].Force > bestChar.Force)
                    {
                        bestChar = hand[i];
                    }
                }
            }

            if (bestChar == null) break;

            // Choix du slot : préférer un terrain où l'adversaire a des forces, ou le premier disponible
            int targetSlot = ChooseBestSlotForCharacter(engine, availableSlots);
            if (targetSlot == -1) break;

            var result = engine.PlayCharacter(Player, bestChar.Id, targetSlot);
            if (!result.Success) break;
        }
    }

    private void TryPlayScoreurToWin(DuelEngine engine)
    {
        if (engine.State.IsGameOver) return;
        if (engine.State.CharactersPlayedThisTurn >= engine.Rules.MaxCharactersPerTurn) return;

        var hand = engine.State.GetHand(Player);
        DuelCard? scoreur = null;
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].Kind == CardKind.Character && hand[i].IsScoreur)
            {
                scoreur = hand[i];
                break;
            }
        }

        if (scoreur == null) return;

        // Trouver un terrain actif où IA_Force + scoreur.Force > Opponent_Force
        int bestSlotToScore = -1;
        int maxAdvantage = -1;

        for (int i = 0; i < engine.State.Slots.Count; i++)
        {
            var slot = engine.State.Slots[i];
            if (!slot.IsEmpty && slot.IsActive(engine.State.TurnNumber))
            {
                int myForce = slot.GetForce(Player) + scoreur.Force;
                int oppForce = slot.GetForce(Player.Opponent());

                if (myForce > oppForce)
                {
                    int advantage = myForce - oppForce;
                    if (advantage > maxAdvantage)
                    {
                        maxAdvantage = advantage;
                        bestSlotToScore = i;
                    }
                }
            }
        }

        if (bestSlotToScore != -1)
        {
            engine.PlayCharacter(Player, scoreur.Id, bestSlotToScore);
        }
    }

    private static int FindFirstFreeSlot(DuelEngine engine)
    {
        for (int i = 0; i < engine.State.Slots.Count; i++)
        {
            if (engine.State.Slots[i].IsEmpty) return i;
        }
        return -1;
    }

    private static List<int> GetSlotsWithTerrain(DuelEngine engine)
    {
        var list = new List<int>();
        for (int i = 0; i < engine.State.Slots.Count; i++)
        {
            if (!engine.State.Slots[i].IsEmpty) list.Add(i);
        }
        return list;
    }

    private int ChooseBestSlotForCharacter(DuelEngine engine, List<int> availableSlots)
    {
        if (availableSlots.Count == 0) return -1;

        // Préférer un terrain actif où l'adversaire a des forces pour rivaliser
        int bestSlot = availableSlots[0];
        int highestOppForce = -1;

        for (int i = 0; i < availableSlots.Count; i++)
        {
            int slotIdx = availableSlots[i];
            var slot = engine.State.Slots[slotIdx];
            int oppForce = slot.GetForce(Player.Opponent());
            if (oppForce > highestOppForce)
            {
                highestOppForce = oppForce;
                bestSlot = slotIdx;
            }
        }

        return bestSlot;
    }
}
