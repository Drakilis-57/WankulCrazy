1. **Mise à jour du modèle `DuelCard` et du parseur `CardAdapter`** :
   - Ajouter `HasOpeningGem` et `HasClosingGem` dans `DuelCard`.
   - Ajouter `ComboEffectIds` pour les effets de combo à `DuelCard`.
   - Mettre à jour `CardAdapter.ToDuelCard` pour parser `EffigyCardData.Combo` et détecter si la carte a un combo (et déterminer ses effets de combo), configurant ainsi les propriétés de gemmes en conséquence.

2. **Mise à jour de `TerrainSlot`, `DuelState` et `DuelEngine` pour le positionnement** :
   - Modifier `TerrainSlot.AddCharacter` pour accepter un `insertIndex` : `internal void AddCharacter(PlayerId player, DuelCard card, int insertIndex = -1)` qui insère au bon endroit dans `_charactersP1` / `_charactersP2`.
   - Modifier `DuelState.AddCharacterToSlot` et `MoveCharacter` de la même façon (ajouter `insertIndex = -1`).
   - Mettre à jour `DuelEngine.PlayCharacter` pour prendre `int insertIndex = -1` (par défaut, à la fin).
   - Mettre à jour `DuelEngine.MoveCharacter` pour prendre `int insertIndex = -1`.

3. **Logique de Combo et événements `DuelEngine`** :
   - Créer `IsComboActive(PlayerId player, int slotIndex, int characterIndex)` dans `DuelEngine`.
   - Créer `ComboFormedEvent` et `ComboBrokenEvent` dans `DuelEvents.cs` avec leurs paramètres.
   - Modifier `TerrainSlot` pour appliquer les effets de force des COMBO : dans `GetForce`, au lieu d'itérer bêtement, on vérifie si un combo est actif pour la carte en cours.
   - Si un effet modifie autre chose que la force, on le déclenche au moment où `ComboFormedEvent` est émis (pour l'instant la plupart des combos Wankul ciblent la force ou donnent des bonus d'effets immédiats, on gère les effets via `EffectRegistry`).

4. **Refonte de l'interface graphique `DuelView2D`** :
   - Ajuster l'affichage des personnages pour qu'ils soient positionnés horizontalement avec un visuel de connexion (gemme) s'il y a un combo (on peut ajouter une image / icône inter-carte ou modifier les layouts existants dans `DuelView2D.cs`).

5. **Création des Tests Unitaires** :
   - Ajouter `DuelComboTests.cs` dans `WankulCrazyPlugin.Tests`.
   - Vérifier le jeu, l'insertion, le retrait, et la condition de l'index 0.

6. **Pre-commit et soumission** :
   - Compléter les étapes de pré-commit pour s'assurer des tests, des vérifications, des revues et de la réflexion.
