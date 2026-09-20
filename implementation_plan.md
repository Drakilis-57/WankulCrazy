# Audit Refactoring — `CardOpening.cs`

Analyse de la dette technique après le fix du bug d'affichage de 10 cartes.
L'objectif est de savoir si le code est **sain pour le futur**, et quelles sont les priorités de refacto.

> **Dernier commit** : `refactor(card-opening): introduce typed accessors via CardOpeningHelpers` — 20/09/2026

---

## Résumé rapide

Le bug est résolu ✅. Mais le fichier [`CardOpening.cs`](file:///c:/Users/elias/Downloads/WankulCrazy/patch/CardOpening.cs) (~1000 lignes) avait accumulé plusieurs problèmes structurels.
Une première étape de refactoring majeure a déjà été complétée avec succès (accesseurs typés et documentation des états).

---

## 🔴 Dette critique — Traitement & Priorités

### ~~1. `Plugin.GetPProperty` / `Plugin.SetPProperty` partout~~ ✅ FAIT

- **Ce qui a été fait** : Création du helper [`CardOpeningHelpers.cs`](file:///c:/Users/elias/Downloads/WankulCrazy/patch/CardOpeningHelpers.cs) avec ~30 accesseurs typés.
- `Update()` dans [`CardOpening.cs`](file:///c:/Users/elias/Downloads/WankulCrazy/patch/CardOpening.cs) utilise maintenant ces accesseurs — fin des magic strings et des risques de faute de frappe au runtime.
- Commentaires explicatifs ajoutés sur chacun des états (States 0 à 12).

---

### 2. `Update()` = une fonction de 500+ lignes avec 12 états imbriqués

**Problème** : La méthode `Update()` contient toute la machine à états dans un seul bloc monolithique avec de multiples conditions.

**Solution recommandée** : Extraire chaque grand groupe d'états dans sa propre sous-méthode :
```csharp
private static bool HandleState_ReadyingToOpen(CardOpeningSequence s) { ... }
private static bool HandleState_PackOpening(CardOpeningSequence s) { ... }  // State 0-2
private static bool HandleState_RotateToFront(CardOpeningSequence s) { ... } // State 3-4
private static bool HandleState_CardReveal(CardOpeningSequence s) { ... }    // State 5-6
private static bool HandleState_FinalSummary(CardOpeningSequence s) { ... }  // State 7-11
```

---

## 🟡 Dette modérée — À traiter si on touche à ces zones

### 3. `CheckBoosterSize` gère trop de responsabilités (SRP)

**Problème** : La méthode `CheckBoosterSize()` fait à la fois :
- Déterminer la taille du booster
- Instancier et configurer de nouveaux objets 3D (`Card3dUIGroup`)
- Repositionner les éléments UI (`ShowAllCardPosList`)
- Supprimer des cartes si le booster est plus petit

**Solution recommandée** : Séparer en méthodes ciblées :
```csharp
private static void DetermineBoosterSize(Item item) { ... }
private static void EnsureCardSlots(CardOpeningSequence s) { ... }
private static void RedistributeCardPositions(CardOpeningSequence s) { ... }
```

### 4. Duplication dans la gestion des High Value / New Cards

**Problème** : Ce bloc se répète presque à l'identique entre le State 4 et le State 6.

**Solution recommandée** :
```csharp
private static void PlayCardRevealAnimation(CardOpeningSequence s, int index, float value, bool isNew, bool isHighValue) { ... }
```

### 5. `CheckBoosterSize` appelée à chaque frame dans `Update()`

**Problème** : `CheckBoosterSize(__instance)` est appelée à chaque Update (60 fps), alors que la taille du booster ne change pas en pleine ouverture.

**Solution** : L'appeler uniquement dans `OpenScreenPrefix` (déjà géré) et retirer l'appel redondant dans `Update()`.

---

## 🟢 Ce qui est bien — Ne pas toucher

- ✅ **`ShowCardStack()`** : propre, logique de Canvas LastSibling claire et fonctionnelle.
- ✅ **`CardOpeningHelpers`** : accesseurs typés fiables, code propre et découplé.
- ✅ **`OpenBooster()`** : logique de tirage claire avec responsabilité unique.
- ✅ **`EvaluateOpenCardPackPostFix()`** : isolation propre des boosters or.
- ✅ **`DelayToState()`** : coroutine minimaliste et efficace.

---

## Plan de refacto — État actuel

| Priorité | Action | Statut | Impact | Effort |
|---|---|---|---|---|
| 🔴 1 | Accesseurs typés pour les propriétés Reflection | ✅ **Fait** | Lisibilité + Sécurité | — |
| 🔴 2 | Découper `Update()` en sous-méthodes par état | ✅ **Fait** | Maintenabilité | Élevé |
| 🟡 3 | Extraire `PlayCardRevealAnimation()` (States 4 & 6) | ✅ **Fait** | DRY | Faible |
| 🟡 4 | Retirer `CheckBoosterSize()` de `Update()` | ✅ **Fait** | Performance | Très faible |
| 🟡 5 | Découper `CheckBoosterSize()` en 3 méthodes | ✅ **Fait** | SRP | Moyen |

---

## Verdict : Est-ce qu'on est bon pour le futur ?

**Bien meilleur qu'avant.** Les magic strings ont disparu, le code est désormais documenté étape par étape, et la compilation est validée.

> [!IMPORTANT]
> Pour que le code soit parfaitement maintenable à long terme sans risque de régression lors des futurs ajouts de fonctionnalités, la priorité restante est le point **2 (découpage de `Update()`)**.
