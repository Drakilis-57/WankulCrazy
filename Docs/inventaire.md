# 🃏 Inventaire des Cartes - WankulCrazy

Ce document récapitule l'ensemble des cartes recensées dans le projet, classées par saison et par rareté.

---

## 📊 Résumé Global par Saison

| Saison | Nom | Personnages (Effigies) | Terrains | Spéciales | Total | Statut |
| :--- | :--- | :---: | :---: | :---: | :---: | :--- |
| **S01** | **Origins** | 150 | 30 | 0 | **180** | Base officielle |
| **S02** | **Campus** | 130 | 25 | 0 | **155** | Base officielle |
| **S03** | **Battle** | 150 | 30 | 0 | **180** | Base officielle |
| **S04** | **Stellar** | 150 | 30 | 0 | **180** | Intégrée (cartes, boosters, displays, Taux, accessoires) |
| **S05** | **Legacy** | 155 | 30 | 0 | **185** | Intégrée (dont 10 cartes DUO) |
| **HS** | **Hors-Série** | 0 | 0 | 2 | **2** | Seules les 2 cartes spéciales sont dans `data/cards/Specials/` |
| **TOTAL** | | **735** | **145** | **2** | **882** | |

> Comptages vérifiés sur `data/cards/**/*.json`. Les 76 promos HS (SPCAR, SPJV, PGW23…) listées précédemment n'ont aucune donnée JSON dans le dépôt ; leurs raretés existent seulement dans `cards/Rarities.cs`.

---

## 📦 Détail par Saison et par Rareté

### 1. Saison 1 : Origins (`S01`) — 180 Cartes
* **Terrains** : 30 cartes
* **Personnages / Effigies** : 150 cartes
  * **Commune (`C`)** : 50
  * **Peu Commune (`UC`)** : 50
  * **Rare (`R`)** : 20
  * **Ultra Rare 1 (`UR1`)** : 10
  * **Ultra Rare 2 (`UR2`)** : 8
  * **Légendaire Bronze (`LB`)** : 6
  * **Légendaire Argent (`LA`)** : 4
  * **Légendaire Or (`LO`)** : 2

---

### 2. Saison 2 : Campus (`S02`) — 155 Cartes
* **Terrains** : 25 cartes
* **Personnages / Effigies** : 130 cartes
  * **Commune (`C`)** : 40
  * **Peu Commune (`UC`)** : 35
  * **Rare (`R`)** : 25
  * **Ultra Rare 1 (`UR1`)** : 8
  * **Ultra Rare 2 (`UR2`)** : 9
  * **Légendaire Bronze (`LB`)** : 7
  * **Légendaire Argent (`LA`)** : 4
  * **Légendaire Or (`LO`)** : 2

---

### 3. Saison 3 : Battle (`S03`) — 180 Cartes
* **Terrains** : 30 cartes
* **Personnages / Effigies** : 150 cartes
  * **Commune (`C`)** : 50
  * **Peu Commune (`UC`)** : 50
  * **Rare (`R`)** : 20
  * **Ultra Rare 1 (`UR1`)** : 10
  * **Ultra Rare 2 (`UR2`)** : 8
  * **Légendaire Bronze (`LB`)** : 6
  * **Légendaire Argent (`LA`)** : 4
  * **Légendaire Or (`LO`)** : 2

---

### 4. Saison 4 : Stellar (`S04`) — 180 Cartes
* **Terrains** : 30 cartes
* **Personnages / Effigies** : 150 cartes
  * **Commune (`C`)** : 50
  * **Peu Commune (`UC`)** : 50
  * **Rare (`R`)** : 20
  * **Ultra Rare 1 (`UR1`)** : 10
  * **Ultra Rare 2 (`UR2`)** : 8
  * **Légendaire Bronze (`LB`)** : 6
  * **Légendaire Argent (`LA`)** : 4
  * **Légendaire Or (`LO`)** : 2
* *Boosters & items associés déjà référencés :* Booster Stellar, Booster Stellar Taux +, Display Stellar, Booster Gold Stellar, Boxers/Caleçons, Tapis, Classeurs.

---

### 5. Saison 5 : Legacy (`S05`) — 185 Cartes
* **Terrains** : 30 cartes
* **Personnages / Effigies** : 155 cartes
* **Personnages / Effigies par rareté** :
  * **Commune (`C`)** : 50
  * **Peu Commune (`UC`)** : 45
  * **Rare (`R`)** : 20
  * **Ultra Rare 1 (`UR1`)** : 10
  * **Ultra Rare 2 (`UR2`)** : 8
  * **Légendaire Bronze (`LB`)** : 6
  * **Légendaire Argent (`LA`)** : 4
  * **Légendaire Or (`LO`)** : 2
  * **Duo (`DUO`)** : 10
* *Exemple de carte :* GÉODEVINEUR (Terracid · C#46)

---

### 6. Hors-Série (`HS`) — 2 Cartes (données du dépôt)
* **Cartes Spéciales** : 2 (`TICKET D'OR !!!`, `À JETER`)
* Les raretés promo (SPCAR, SPJV, PGW23, SPLEG, NOEL23, PGW24, SPCIV, SPPOP, SPTV, ED, EG, GP) sont déclarées dans `cards/Rarities.cs`, mais aucune carte correspondante n'existe dans `data/cards/`.
