# Directives d'architecture & Modding TCG Card Shop Simulator

## 1. Gestion des Objets Custom & Catalogue Boutique
- **Idempotence & Anti-doublons** : Toujours vérifier si un `EItemType` ou une licence `RestockData` existe déjà dans les listes (`m_ShownItemType`, `m_RestockDataList`, etc.) avant de l'ajouter. Ne jamais faire de `Add` sans garde `!Contains`.
- **Répartition stricte par onglet** :
  - `m_ShownItemType` : Boosters, Displays, Starters/Precons uniquement.
  - `m_ShownFigurineItemType` : Figurines, Peluches, Caleçons.
  - `m_ShownAccessoryItemType` : Tapis de jeu (Playmats), Classeurs (Binders), Sleeves.
- **Positionnement thématique** :
  - Positionner les objets custom normaux immédiatement après les items normaux du jeu de niveau équivalent (ex: après Battle / `EpicCardBox`).
  - Positionner les variantes Taux de drop immédiatement après les variantes Taux originales (`DestinyEpicCardBox`).
  - Aligner l'ordre de `m_RestockDataList` sur celui du catalogue pour une progression cohérente.

## 2. Configuration des Prix & Niveaux
- **Licences & Réputation** : Configuré dans `data/customitems/restockDataList.json` (`licenseShopLevelRequired` et `licensePrice`).
- **Prix de base fournisseur & marges** : Configuré dans `data/customitems/ItemDataList.json` (`baseCost`, `marketPriceMinPercent`, `marketPriceMaxPercent`).
- **Cartes individuelles (Singles)** : Géré dynamiquement par algorithme dans `patch/CardPrice.cs`.

## 3. Débogage & Rendu
- Si un comportement de shop ou de licence diverge, privilégier des logs exhaustifs de l'ordre des listes réelles au runtime plutôt que de simples suppositions.
- Prêter attention au mapping des textures 3D (ne pas assigner une texture de Display sur un modèle de Booster).
