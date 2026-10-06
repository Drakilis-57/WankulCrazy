# Spécifications & Design UI : Duel Wankul TCG & WankulDebugScreen

Document de référence pour l'architecture visuelle, l'expérience utilisateur (UX), les composants d'interface (UI) et les chartes graphiques de :
1. **L'écran de Duel Wankul TCG (DuelView2D / WankulDuelUI)**
2. **L'écran d'erreur et de rapport de crash (WankulDebugScreen)**
3. **Le Design System partagé (WankulUiKit)**

---

## 1. Principes Directeurs & Charte Graphique

### 1.1 Identité Visuelle Wankul
- **Thématique globale** : Contraste élevé, tons sombres profonds (Slate / Dark Navy) relevés d'accents vifs et saturés (Or Wankul `#FFE080`, Cyan Joueur `#66D9EF`, Rouge Adversaire/Danger `#FF5555`).
- **Typographie** : Police système robuste avec fallback propre (`Arial.ttf` / Unity Dynamic Font). Tailles hiérarchisées (28px Titres majeurs, 20-22px Sous-titres & scores, 12-14px Cartes & détails).
- **Rendu & Shaders** : uGUI pur en `ScreenSpace-Overlay` avec `CanvasScaler` adaptatif 1920x1080 (match height/width 0.5f). Aucune dépendance sur des shaders non sécurisés (règle anti-Standard shader).
- **Lisibilité & Accessibilité** : Cartouches semi-opaques (alpha 0.85 - 0.95) derrière tous les textes critiques sur fond de plateau ou de carte, outlinings nets pour la mise en évidence des éléments interactifs.

### 1.2 Palette de Couleurs Système

| Rôle | Hex / RGBA | Utilisation |
|---|---|---|
| **Fond Écran (Overlay)** | `#0A0D14` / `rgba(10, 13, 20, 0.95)` | Assombrissement d'ambiance et focus UI |
| **Bandeaux & Panneaux** | `#1F242E` / `rgba(31, 36, 46, 1.0)` | Cartouches principaux, conteneurs |
| **Accent Or / Wankul** | `#FFE080` / `rgba(255, 224, 128, 1.0)` | Titres de terrains, victoires, scores maîtres |
| **Joueur 1 (Ami / P1)** | `#55C5FF` / `rgba(85, 197, 255, 1.0)` | Forces alliées, contours cartes jouables |
| **Joueur 2 (Ennemi / P2)** | `#FF6666` / `rgba(255, 102, 102, 1.0)` | Forces ennemies, zones de confrontation adverse |
| **Validation / Valide** | `#33F266` / `rgba(51, 242, 102, 0.95)` | Halo de sélection de slot cible cliquable |
| **Bouton Fin de Tour** | `#B32E38` / `rgba(179, 46, 56, 0.95)` | Bouton d'action principal de fin de phase |
| **Bouton Quitter / Danger** | `#732429` / `rgba(115, 36, 41, 0.92)` | Forfeit, quitter l'application |
| **Actions Secondaires** | `#4073BF` / `rgba(64, 115, 191, 1.0)` | Boutons Copier, Interactions utilitaires |

---

## 2. Écran de Duel Wankul TCG (`DuelView2D`)

### 2.1 Disposition Spatiale (Layout 1920x1080)

L'écran est structuré en 3 bandes horizontales distinctes assurant une lecture de haut en bas :

```
+-----------------------------------------------------------------------------------------+
| [ TOP BAR ]  Score: JOUEUR 0/5 - ADVERSAIRE 0/5  |  Tour 1  |  Phase  |     [ Quitter ] |
+-----------------------------------------------------------------------------------------+
| [ MESSAGE BANNER ] "Sélectionnez un Terrain pour jouer votre carte..."                 |
+-----------------------------------------------------------------------------------------+
|                                    ZONE DU PLATEAU                                      |
|                                                                                         |
|   +-----------------------+   +-----------------------+   +-----------------------+     |
|   | SLOT 1 : ADVERSAIRE   |   | SLOT 2 : ADVERSAIRE   |   | SLOT 3 : ADVERSAIRE   |     |
|   | Persos adverses       |   | Persos adverses       |   | Persos adverses       |     |
|   | Force: 0              |   | Force: 120            |   | Force: 0              |     |
|   +-----------------------+   +-----------------------+   +-----------------------+     |
|   | [ILLUST] TERRAIN 1    |   | [ILLUST] TERRAIN 2    |   | [ILLUST] TERRAIN 3    |     |
|   | "La Ferme"            |   | "La Plage"            |   | "Le Bar"              |     |
|   | Statut: Actif         |   | Statut: INACTIF (90°) |   | Statut: Validé P1     |     |
|   +-----------------------+   +-----------------------+   +-----------------------+     |
|   | SLOT 1 : VOS PERSOS   |   | SLOT 2 : VOS PERSOS   |   | SLOT 3 : VOS PERSOS   |     |
|   | Persos alliés         |   | Persos alliés         |   | Persos alliés         |     |
|   | Force: 80             |   | Force: 0              |   | Force: 150            |     |
|   +-----------------------+   +-----------------------+   +-----------------------+     |
|                                                                                         |
+-----------------------------------------------------------------------------------------+
| [ MAIN DU JOUEUR (HAND) ]                                         [ BOUTON FIN DE TOUR ]|
| [Carte 1] [Carte 2] [Carte 3] [Carte 4] [Carte 5] ...             | FIN DU TOUR        ||
| (Survol: grossissement & tooltip info)                            | ► Passer           ||
+-----------------------------------------------------------------------------------------+
```

### 2.2 Composants & Interactions du Duel

#### A. Le Bandeau Supérieur (`TopBar`)
- **Position** : `anchorMin: (0, 0.88)`, `anchorMax: (1, 1.0)`.
- **Informations affichées** :
  - Identifiants de match et score sous format course aux points : `WANKUL TCG | JOUEUR : X / 5 — ADVERSAIRE : Y / 5`.
  - Indicateur de Tour courant et joueur actif (`VOTRE TOUR` / `TOUR DE L'ADVERSAIRE`).
  - Bouton `Quitter` sécurisé (rouge sombre avec outline clair) ancré à droite pour abandonner la partie proprement sans bloquer l'état du jeu.

#### B. Les 3 Colonnes de Terrains (`BoardArea` & `Slot_0..2`)
- **Dimensions & Ancrages** : Zone centrale occupant `Y: 0.32` à `0.86`, divisée en 3 colonnes égales de 33.3% chacune.
- **Section Haute (Adversaire)** :
  - En-tête rouge clair : `ADVERSAIRE` + indicateur dynamique de force cumulée `Force: XXX`.
  - Liste textuelle/icônes des cartes Personnages posées par l'adversaire sur ce terrain.
- **Section Centrale (Carte Terrain)** :
  - Fond sombre texturé recevant la texture réelle de la carte Terrain Wankul (`_slotTerrainImages`).
  - **Rotation dynamique** : Si le terrain est déclaré **Inactif** par les règles de jeu, l'illustration pivote à **90°** (`transform.localEulerAngles = new Vector3(0, 0, 90)`), mimant la pose couchée physique.
  - Cartouche de titre supérieur (`TitlePanel`) pour nommer explicitement le lieu (ex: "La Forêt", "Terrain Neutre").
  - Pastille d'état inférieur (`StatusPanel`) indiquant la situation du lieu (Ex: "Contesté", "Inactif", "Capturé").
- **Section Basse (Vos Personnages)** :
  - En-tête bleu clair : `VOS PERSOS` + indicateur dynamique de force cumulée `Force: XXX`.
  - Liste des personnages déployés par le joueur et leurs éventuels bonus d'effets.
- **Retour Interactif (Sélection & Ciblage)** :
  - Dès qu'une carte jouable est sélectionnée en main, les slots légaux s'illuminent via un outline vert fluo pulsant (`#33F266`, distance `(4, -4)`).
  - Un clic sur un slot valide déclenche l'action moteur correspondante (`PlayTerrainAction` ou `PlayCharacterAction`).

#### C. La Main du Joueur (`BottomArea` & `HandContainer`)
- **Position** : `Y: 0.02` à `0.30`.
- **Organisation** : `HorizontalLayoutGroup` avec alignement centré, distribution flexible des cartes de la main.
- **Comportement Carte en Main** :
  - Affichage de la carte (Texture Wankul HD avec ratio carte respecté).
  - Badge indiquant le Coût, le Type (Terrain / Personnage) et la Force de base.
  - **État Hover (Survol)** : Élévation en Y (+20px) et mise en avant visuelle (`transform.SetAsLastSibling`).
  - **État Sélectionné** : Cadre doré pulsant, affichage du message d'aide central invitant à cliquer sur l'un des 3 terrains cibles.
  - Un second clic sur la même carte désélectionne la carte et éteint les halos de ciblage des slots.

#### D. Le Bouton de Fin de Tour (`EndTurnBtn`)
- **Position** : Ancré en bas à droite (`X: 0.84` à `0.985`, `Y: 0.20` à `0.80` de la `BottomArea`).
- **Aspect** : Pavé bordeaux profond (`#B32E38`), outline or chaud (`#FFE080`).
- **Texte** : `FIN DU TOUR \n ► Passer`.
- **Verrouillage** : Grisé et non-interactif pendant le tour adverse ou la résolution d'actions/animations AI.

### 2.3 Système d'Animations & Effets Visuels (VFX)

1. **Déclenchement d'un Combat & Résolution par Scoreur (Barre de Confrontation Dynamique)** :
   - Lorsqu'un Scoreur est posé sur un terrain éligible, l'écran déclenche l'événement de duel :
   - **Jauge de tir à la corde (Tug of War)** : Un module horizontal cinématique apparaît au centre (`ScoreClashBar`).
   - Deux jauges s'affrontent en vis-à-vis : Jauge Bleue P1 (`#55C5FF`) et Jauge Rouge P2 (`#FF6666`) avec affichage chiffré des forces en direct (`Force Totale P1 vs Force Totale P2`).
   - Le curseur central oscille et glisse vers le côté dominant en 0.6s.
   - **Victoire de Terrain** : Éclat doré (`#FFE080`) du côté du vainqueur, mise à jour immédiate du score général (`+1 point`) et retentissement de la fanfare de capture (`DuelSfx.PlayScoreResolved()`).
   - **Aspiration Défausse** : À la fin du clash, le terrain résolu et l'ensemble des personnages engagés s'estompent et volent fluidement vers la boîte de Poubelle/Défausse (`PoubelleSlot`).

2. **Inspection de Carte (Clic droit / Maintien)** :
   - Un clic droit ou clic maintenu sur n'importe quelle carte (en main ou déployée sur un terrain) déploie un **Volet d'Inspection HD** ancré sur le bord droit de l'écran sans bloquer la vue du plateau.
   - Contenu du panneau :
     - Rendu HD agrandi de la carte Wankul.
     - Titre, Rareté, Saison, Type (`Personnage`, `Terrain`, `Scoreur`).
     - Force de base et calcul détaillé des bonus/malus passifs actifs.
     - Description textuelle intégrale de l'effet.
   - Relâcher le clic ou recliquer ferme instantanément le panneau.

3. **Pose de Carte (Transition Discrète & Fluide)** :
   - Pose rapide et réactive en **150ms** : La carte quitte la main avec un micro-zoom et fondu ciblé vers son slot assigné.
   - Ne bloque pas le rythme de jeu du joueur avec des arcs de vol lents, garantissant un gameplay nerveux.
   - Retour sonore léger et sec (`DuelSfx.PlayCardDrop()`).

4. **Pose & Ouverture de Terrain (Révélation Dramatique 90°)** :
   - Lors de la phase obligatoire de début de tour ou d'une pose volontaire :
   - La carte émerge du deck face cachée, glisse au centre du slot vacant, pivote de face avec un éclat lumineux, puis s'incline **à 90° à l'horizontale** pour matérialiser son état *Inactif pour ce tour*.
   - Badge semi-transparent superposé : `INACTIF (COUCHÉ - 90°)`.

5. **Redressement de Terrain (Début de Tour)** :
   - Au début du tour, lors du passage à la phase d'action :
   - Les terrains posés au tour précédent effectuent une rotation fluide de 90° vers la verticale (`0°`).
   - Un liseré lumineux vert/doré confirme que le terrain est désormais **Actif et éligible au duel**.

6. **Changement de Tour (Bannière Cinématique)** :
   - Une bannière centrale semi-transparente glisse horizontalement au milieu de l'écran pendant 0.8s :
     - Tour du Joueur : `VOTRE TOUR` (halo cyan `#55C5FF`, jingle énergique).
     - Tour Adversaire : `TOUR DE L'ADVERSAIRE` (halo rouge bordeaux `#B32E38`).
   - Dès la fin du bandeau, l'animation de pioche des 2 cartes s'exécute vers la main.

---

## 3. Écran d'Erreur & Rapport de Crash (`WankulDebugScreen`)

### 3.1 Objectifs UX
- **Interception non-bloquante** : Détecte les `Exception` critiques et `NullReferenceException` provenant spécifiquement du mod Wankul Crazy (`WankulCrazyPlugin`), sans polluer pour les erreurs tierces du jeu de base.
- **Libération du Curseur** : Déverrouille automatiquement le curseur de la caméra FPS du jeu (`Cursor.visible = true`, `CursorLockMode.None`) à chaque frame tant que le dialogue est ouvert.
- **Outil de Débogage Rapide** : Permet au joueur ou testeur de copier l'intégralité du rapport (titre, message d'erreur et stack trace propre) en un seul clic dans le presse-papier pour transmission instantanée aux développeurs.

### 3.2 Disposition Visuelle & Structure du Modal

```
+-----------------------------------------------------------------------------------------+
|                             OVERLAY SOMBRE (#0A0D10, Alpha 0.95)                        |
|                                                                                         |
|       +-------------------------------------------------------------------------+       |
|       | [BARRE ROUGE CRITIQUE #D93333]                                          |       |
|       | WANKUL CRAZY - ERREUR DETECTEE                                          |       |
|       +-------------------------------------------------------------------------+       |
|       | [RÉSUMÉ ERREUR]                                                         |       |
|       | "NullReferenceException: Object reference not set to an instance..."   |       |
|       +-------------------------------------------------------------------------+       |
|       | [CADRE STACKTRACE SOMBRE]                                               |       |
|       | at WankulCrazyPlugin.duel.DuelView2D.RefreshView () [0x00045]           |       |
|       | at WankulCrazyPlugin.duel.WankulDuelController.Update () [0x00012]      |       |
|       | [... tronqué si > 2500 chars : utiliser "Copier l'erreur"]             |       |
|       |                                                                         |       |
|       +-------------------------------------------------------------------------+       |
|       |  [ Copier l'erreur ]   [ Ignorer ]   [ Ignorer tout ]   [ Quitter jeu ] |       |
|       +-------------------------------------------------------------------------+       |
|                                                                                         |
+-----------------------------------------------------------------------------------------+
```

### 3.3 Boutons d'Action & Comportements

1. **`Copier l'erreur` (Bleu `#4073BF`)** :
   - Formate le message pour Discord / GitHub Issues :
     ```text
     === WANKUL CRAZY ERROR REPORT ===
     Titre: ...
     Erreur: ...
     StackTrace: ...
     ================================
     ```
   - Injecte le bloc dans `GUIUtility.systemCopyBuffer`.
   - Fournit un feedback visuel immédiat en ajoutant `(copié dans le presse-papier)` au titre.
2. **`Ignorer` (Vert doux `#59A659`)** :
   - Enregistre le hash unique de l'erreur dans `_ignoredHashes` pour ne plus jamais afficher cette même occurrence durant la session.
   - Ferme la fenêtre et réactive le gameplay.
3. **`Ignorer tout` (Ocre `#8C8C4D`)** :
   - Désactive totalement l'interception UI d'erreurs pour le reste de la session de jeu (`_ignoreAll = true`).
4. **`Quitter le jeu` (Rouge `#BF4040`)** :
   - Appelle `Application.Quit()` pour sortir proprement si le jeu est dans un état corrompu irrémédiable.

---

## 4. Composants du Design System (`WankulUiKit`)

Le kit d'outils utilitaires statiques (`WankulUiKit.cs`) assure une uniformité technique stricte :

- **`SetupCanvas(GameObject, sortingOrder)`** :
  - Injecte `Canvas` (Overlay), `CanvasScaler` configuré en résolution de référence 1920x1080 et `GraphicRaycaster`.
- **`GetFont()`** :
  - Résolution de police à tolérance de panne : essaie `Arial.ttf` de Resources, fallback sur police dynamique OS, puis fallback sur la première police chargée dans la mémoire Unity.
- **`WhiteSprite`** :
  - Texture 1x1 blanche native réutilisée pour tous les fonds tintés, évitant les artefacts de shaders ou les dépendances de sprites externes.
- **`CreateImage` & `CreateText`** :
  - Instanciation normalisée sans fuite mémoire avec assignation automatique du RectTransform et des propriétés typographiques.
- **`CreateButton`** :
  - Instancie un bouton uGUI complet avec son composant Image, son texte enfant centré et son callback `UnityAction`.
- **`Stretch` / `Place` / `PlaceCentered`** :
  - Helpers d'ancrage responsive pour éliminer les calculs manuels de `offsetMin/Max`.

---

## 5. Bonnes Pratiques & Directives d'Évolution

1. **Séparation Moteur / Vue** :
   - L'UI de duel ne calcule **jamais** de logique de jeu ni de score par elle-même. Elle écoute les événements (`DuelEvents`) de `DuelEngine` et lit `DuelState`.
2. **Gestion des Textures de Cartes** :
   - Les illustrations proviennent des caches haute définition de `WankulCardsData`. Si une texture est manquante, afficher systématiquement le dos de carte officiel ou une texture de fallback documentée, sans lever d'exception.
3. **Responsive & Écrans Larges (Ultrawide 21:9)** :
   - Grâce à l'ancrage horizontal relatif (`anchorMin.x` / `anchorMax.x`) et au mode `CanvasScaler.ScaleWithScreenSize` (0.5), l'UI s'adapte sans déformation du format 16:9 au 21:9.
4. **Audio & Feedback Haptique/Visuel** :
   - Tout clic de bouton ou action sur le plateau doit être soutenu par un son idoine via `DuelSfx` (`PlayCard`, `PlayShuffle`, `StartDuelMusic`).
