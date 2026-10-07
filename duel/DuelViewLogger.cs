using System;
using System.Linq;
using WankulCrazy.Duel.Engine;

namespace WankulCrazy.Duel
{
    public static class DuelViewLogger
    {
        public static void LogDuelEvent(IDuelEvent evt)
        {
            Action<string> log = msg => {
                try {
                    var type = Type.GetType("WankulCrazy.Plugin, WankulCrazyPlugin");
                    if (type != null) {
                        var prop = type.GetProperty("Logger", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (prop != null) {
                            var logger = prop.GetValue(null);
                            if (logger != null) {
                                var method = logger.GetType().GetMethod("LogInfo", new[] { typeof(object) });
                                if (method != null) {
                                    method.Invoke(logger, new object[] { msg });
                                    return;
                                }
                            }
                        }
                    }
                } catch { }
                System.Console.WriteLine(msg);
            };

            switch (evt)
            {
                case TurnStartedEvent turnStarted:
                    log($"[Duel] Turn {turnStarted.TurnNumber} started for {turnStarted.ActivePlayer}");
                    break;
                case TerrainRevealedByDigEvent digEvent:
                    log($"[Duel] Terrain Dig: revealed {digEvent.RevealedCardsPutUnderDeck.Count} cards before finding {digEvent.Card.Name}");
                    break;
                case TerrainPlayedEvent terrainPlayed:
                    log($"[Duel] {terrainPlayed.Player} played Terrain '{terrainPlayed.Card.Name}' on Slot {terrainPlayed.SlotIndex}");
                    break;
                case CharacterPlayedEvent charPlayed:
                    log($"[Duel] {charPlayed.Player} played Character '{charPlayed.Card.Name}' on Slot {charPlayed.SlotIndex}");
                    break;
                case EffectTriggeredEvent effectTriggered:
                    log($"[Duel] Effect '{effectTriggered.EffectId}' triggered by '{effectTriggered.SourceCard.Name}' for {effectTriggered.Player}");
                    break;
                case CardsMilledEvent cardsMilled:
                    string cardNames = string.Join(", ", cardsMilled.MilledCards.Select(c => c.Name));
                    log($"[Duel] {cardsMilled.TargetPlayer} milled {cardsMilled.MilledCards.Count} cards: [{cardNames}]");
                    break;
                case CardsDiscardedEvent discarded:
                    string discNames = string.Join(", ", discarded.DiscardedCards.Select(c => c.Name));
                    log($"[Duel] {discarded.Player} discarded {discarded.DiscardedCards.Count} cards: [{discNames}]");
                    break;
                case ScoreResolvedEvent scoreResolved:
                    log($"[Duel] Resolving score on Slot {scoreResolved.SlotIndex}: P1={scoreResolved.ForceP1} vs P2={scoreResolved.ForceP2} -> Winner={scoreResolved.Winner?.ToString() ?? "Draw"}");
                    break;
                case ComboFormedEvent comboFormed:
                    log($"[Duel] Combo formed on Slot {comboFormed.SlotIndex} between '{comboFormed.LeftCard.Name}' and '{comboFormed.RightCard.Name}'");
                    break;
                case ComboBrokenEvent comboBroken:
                    log($"[Duel] Combo broken on Slot {comboBroken.SlotIndex} for card '{comboBroken.CardWithBrokenCombo.Name}'");
                    break;
                case DuelStartedEvent duelStarted:
                    log($"[Duel] Duel Started");
                    break;
                case DeckExhaustedEvent deckExhausted:
                    log($"[Duel] Deck Exhausted for {deckExhausted.Player}");
                    break;
                case TerrainDigFailedEvent digFailed:
                    log($"[Duel] Terrain Dig Failed for {digFailed.Player}");
                    break;
                case DuelEndedEvent duelEnded:
                    log($"[Duel] Duel Ended. Winner: {duelEnded.Winner}. Reason: {duelEnded.Reason}");
                    break;
                case PlayerChoiceRequiredEvent choiceRequired:
                    log($"[Duel] Player {choiceRequired.Player} must make a choice: {choiceRequired.Title}");
                    break;
            }
        }
    }
}
