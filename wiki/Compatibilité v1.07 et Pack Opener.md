# Compatibilité v1.07 et Pack Opener

> **Fichiers sources** : `patch/PriceTagAtlasPatch.cs`, `patch/InteractableAutoPackOpenerPatch.cs`, `patch/CardOpening.cs`, `patch/SceneLifecyclePatches.cs`, `importer/PatchTexturesImporter.cs`, `utils/SavesManager.cs`, `libs/Assembly-CSharp.dll`

## Migration Unity 6 / jeu v1.07
- `Assembly-CSharp.dll` mis à jour ; adaptation des patches Harmony, shaders/matériaux instanciés et persistance (`SavesManager`).
- **Textures** : 16 textures (Displays S1..S5, HS, Taux Drop, Deckboxes 1..4) converties vers le gabarit en croix 2048×2048.
- **Billboard** : bloc de personnages redimensionné (~430px) et centré.
- **Étiquettes de rayon** : 86 icônes Wankul injectées dans l'atlas 4096×4096 ; `PriceTagAtlasPatch` route les items custom vers les cellules 224+.
- **Display** : mapping de `CardBoxStaticMesh` sur les textures display (et non booster), correction de l'overdraw des displays à 1 submesh, chemin texture du booster Gold, alias de mesh.

## Ouverture de boosters
- Correction du crash `IndexOutOfRangeException` : synchronisation de `m_CardBaseScaleList` (10 cartes) ; le matériau booster Wankul est conservé pendant l'animation (SkinnedMeshRenderer, nettoyage des MaterialPropertyBlocks).
- Auto Pack Opener : correction du freeze et du `NullReferenceException` en ouverture headless ; `InteractableAutoPackOpenerPatch` transmet le contexte et récupère les machines bloquées ; synchro des workers, calcul d'XP, packs Ascension.

## Cartes gradées
`cards/GradedCardService.cs` : filtre et tri de l'album de cartes gradées (note 1–10, 12 cartes par page de classeur, tri par défaut Prix), prix marché reproduisant la formule vanilla avec bonus de grade, vue rapprochée du slab.

## Économie et progression
- Rééquilibrage des prix Stellar/Legacy ; déblocage des Taux+ vanilla décalé aux niveaux 32–70.
- `CardPrice` : progression S05/HS.

## Divers
- Accents UTF-8 restaurés dans les définitions de cartes ; masques foil Origins (S5 manquants : warnings supprimés) ; `Texture_Calecon` enregistrée.
- Tests CI : les vérifications de PNG locaux sont ignorées en CI (`Skip`) tout en validant les chemins.
