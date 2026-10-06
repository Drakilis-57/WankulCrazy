# Wankul TCG - Règles Officielles & Spécifications du Mini-Jeu

Ce document synthétise les règles complètes du jeu de cartes à collectionner officiel **Wankul TCG** (créé par Wankil Studio / Laink & Terracid) en vue du remplacement du mini-jeu de cartes vanilla (Tetramon) dans *TCG Card Shop Simulator*.

---

## 1. Vue d'Ensemble & Objectifs

Une partie de Wankul oppose deux joueurs en duel stratégique autour d'arènes d'affrontement appelées **Terrains**.

### Conditions de Victoire
Pour gagner la partie, un joueur doit remplir **l'une des deux conditions** suivantes :
1. **Gagner 5 Duels** : Être le premier joueur à remporter 5 combats sur les cartes Terrain ("scorer" 5 terrains).
2. **Meule / Épuisement de Deck** : Vider la pioche adverse. Si un joueur doit piocher ou défausser une carte depuis une pioche vide, il perd immédiatement la partie.

---

## 2. Structure et Règles de Deckbuilding

- **Taille réglementaire d'un deck** : 50 cartes (les formats d'initiation / rapides peuvent utiliser 30 cartes).
- **Répartition type d'un deck** :
  - **10 cartes Terrain**
  - **40 cartes Personnage**
- **Restriction des Scoreurs** : Un deck ne peut pas contenir plus de **5 cartes Personnage dotées de la capacité *Scoreur***.

---

## 3. Typologie des Cartes

### 3.1 Cartes Terrain
- Représentent les zones et lieux d'affrontement.
- **Limite de présence** : Au maximum **3 cartes Terrain en jeu** simultanément sur la table.
- **Règle d'arrivée en jeu** : Un terrain posé lors du tour en cours **ne peut jamais être scoré durant ce même tour**. Il entre en jeu incliné (à 90°) pour signaler qu'il n'est pas encore éligible au duel, et sera redressé au début du tour suivant.
- Peuvent comporter des règles passives globales ou des modificateurs spécifiques (bonus de force à certaines catégories, effets à la résolution, etc.).

### 3.2 Cartes Personnage
- Possèdent :
  - Un nom et une identité (Laink, Terracid, ou autres personnalités de l'univers Wankil).
  - Une valeur de **Force / Puissance** numérique.
  - Un type / affinité / tag éventuel.
  - Des effets spéciaux (À l'arrivée en jeu, Permanents, Réactions).
- Sont jouées sur un **Terrain précis** et apportent leur valeur de force au camp du joueur sur ce terrain.

### 3.3 Rôle Spécial : Les Scoreurs & Déclenchement de Score
- Personnages possédant le mot-clé ou la capacité **Scoreur**.
- Lorsqu'un Scoreur est posé sur un terrain éligible (qui n'a pas été posé durant ce tour), il **déclenche la résolution immédiate du duel** sur ce terrain.
- **Score automatique (11 points)** : Lorsqu'un joueur totalise au moins **11 points** (110 de Force) de Personnages sur un terrain actif, il peut déclencher le score automatique du terrain durant son tour, sans nécessiter de carte Scoreur.

### 3.4 Règles d'Or du Combat & Résolution des Scores
- **Règle d'or** : 
  - Pour déclencher un score, il doit y avoir au moins 1 Personnage sur le Terrain.
  - Pour **gagner** un score, un joueur doit avoir au moins 1 Personnage sur le Terrain. Un Personnage avec 0 en Force l'emporte si en face il n'y a aucun Personnage.
- **En cas d'égalité** :
  - Si les deux joueurs ont un total de Force identique, **"l'attaquant"** (celui qui a déclenché le score, via un Scoreur ou un score automatique) **est considéré comme perdant**.
  - Le défenseur est déclaré gagnant, remporte le terrain et déclenche l'effet "Le Gagnant", tandis que l'attaquant déclenche l'effet "Le Perdant".
- **Après le score** :
  - Une fois les effets "Le Gagnant" puis "Le Perdant" résolus, chaque joueur défausse ses Personnages.
  - Le propriétaire du terrain défausse ensuite ce terrain.
---

## 4. Déroulement d'un Tour de Jeu

Chaque joueur actif suit scrupuleusement les 3 phases de jeu suivantes :

### Phase 1 : Pioche (Draw Phase)
- Le joueur actif pioche **2 cartes** de son deck.

### Phase 2 : Vérification des Terrains (Terrain Check Phase)
1. Le joueur actif compte le nombre de terrains actuellement présents sur la table :
   - S'il y a **0 ou 1 Terrain** en jeu, le joueur **doit obligatoirement en poser un** depuis sa main.
   - S'il n'a aucun Terrain en main, il procède au **dépilage** de sa pioche : il révèle les cartes une par une depuis le dessus de son deck jusqu'à trouver un Terrain. Il place ce terrain en jeu et replace les autres cartes révélées sous son deck.
2. Les terrains posés au tour précédent par l'adversaire sont redressés (ils deviennent actifs et éligibles au duel).

### Phase 3 : Pose & Actions (Action / Play Phase)
- **Limite de pose de Personnages** : Le joueur peut poser un maximum de **4 cartes Personnage** par tour depuis sa main, réparties sur les terrains de son choix.
- **Pose de Terrains** : Le joueur peut également poser des cartes Terrain supplémentaires gratuitement depuis sa main, tant que le total de terrains en jeu ne dépasse pas la limite de 3.
- **Déclenchement d'un Duel (Scoring)** :
  - Si un joueur active un effet de scoring ou pose un **Scoreur** sur un terrain redressé :
    1. On additionne la Force totale des personnages du Joueur 1 présents sur ce terrain (en tenant compte des bonus/effets).
    2. On additionne la Force totale des personnages du Joueur 2 présents sur ce même terrain.
    3. Le joueur ayant la force la plus élevée **remporte le duel** et gagne 1 point de victoire. En cas d'égalité, les règles d'arbitrage de l'effet s'appliquent (ou aucun point marqué selon le terrain).
    4. **Nettoyage** : Tous les personnages engagés sur ce terrain et le terrain lui-même sont envoyés dans leurs défausses respectives.

---

## 5. Spécifications Techniques pour l'Intégration Modding

Pour remplacer le mini-jeu vanilla (Tetramon) dans *TCG Card Shop Simulator* :

### Modèle de Données
- Adapter la lecture des cartes depuis `data/wankulCards.json` pour extraire :
  - Le type de carte (`Terrain` vs `Personnage`).
  - La valeur de force/puissance.
  - La présence du trait `Scoreur`.
  - Le texte d'effet à parser ou exécuter.

### Gestionnaire de Partie (`WankulBattleManager`)
- **State Machine** :
  - `TurnState` : `Draw` -> `TerrainCheck` -> `PlayAction` -> `DuelResolution` -> `EndTurn`.
- **Plateau de Jeu (Board)** :
  - 3 Slots centraux de Terrains (`TerrainSlot[3]`).
  - Pour chaque slot : 2 zones d'assignation de cartes (Zone Joueur vs Zone Adversaire/IA).
  - Zones Pioche, Défausse et Main pour chaque joueur.
- **Compteurs de Match** :
  - `PlayerScore` / `OpponentScore` (objectif 5 points).
  - `CardsPlayedThisTurn` (max 4 personnages).
