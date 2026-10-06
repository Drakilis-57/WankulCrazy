using System;
using System.Collections.Generic;

namespace WankulCrazy.Duel.Engine;

/// <summary>
/// Configuration des règles d'une partie de duel Wankul TCG.
/// Les valeurs par défaut sont PROVISOIRES et ajustables selon les formats de tournoi/jeu.
/// </summary>
public sealed class DuelRules
{
    // PROVISOIRE : Taille de la main initiale au lancement du duel (défaut : 5)
    public int StartingHandSize { get; }

    // PROVISOIRE : Limite maximale de cartes en main (null = pas de limite)
    public int? MaxHandSize { get; }

    // PROVISOIRE : Mulligan désactivé par défaut à l'étape 1
    public bool MulliganEnabled { get; }

    // PROVISOIRE : Le premier joueur pioche 2 cartes dès son tour 1
    public bool FirstPlayerDrawsOnTurn1 { get; }

    // PROVISOIRE : Nombre de cartes piochées normalement au début de chaque tour
    public int CardsDrawnPerTurn { get; }

    // PROVISOIRE : Nombre de terrains à scorer pour remporter la partie
    public int TerrainsToWin { get; }

    // PROVISOIRE : Nombre maximum de personnages pouvant être joués par tour
    public int MaxCharactersPerTurn { get; }

    // PROVISOIRE : Nombre maximum de terrains simultanés sur le plateau
    public int MaxTerrainsOnBoard { get; }

    // PROVISOIRE : Seuil en dessous duquel la pose d'un terrain est obligatoire (0 ou 1 terrain en jeu)
    public int MinTerrainsThreshold { get; }

    // Seuil de force pour déclencher un score automatique sans carte scoreur (11 points = 110 force, ou 11 selon l'échelle)
    public int AutoScoreForceThreshold { get; }

    public DuelRules(
        int startingHandSize = 5,
        int? maxHandSize = null,
        bool mulliganEnabled = false,
        bool firstPlayerDrawsOnTurn1 = true,
        int cardsDrawnPerTurn = 2,
        int terrainsToWin = 5,
        int maxCharactersPerTurn = 4,
        int maxTerrainsOnBoard = 3,
        int minTerrainsThreshold = 1,
        int autoScoreForceThreshold = 110)
    {
        StartingHandSize = startingHandSize;
        MaxHandSize = maxHandSize;
        MulliganEnabled = mulliganEnabled;
        FirstPlayerDrawsOnTurn1 = firstPlayerDrawsOnTurn1;
        CardsDrawnPerTurn = cardsDrawnPerTurn;
        TerrainsToWin = terrainsToWin;
        MaxCharactersPerTurn = maxCharactersPerTurn;
        MaxTerrainsOnBoard = maxTerrainsOnBoard;
        MinTerrainsThreshold = minTerrainsThreshold;
        AutoScoreForceThreshold = autoScoreForceThreshold;
    }
}
