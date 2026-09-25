# 🛠️ Guide des Commandes de Build — WankulCrazy

Pour éviter de taper `powershell .\build.ps1 -Mode ...`, deux commandes directes ont été créées à la racine du projet : **`dev`** et **`release`**.

---

## 1. Mode Développement : `dev`

Dans n'importe quel terminal à la racine du projet :
```cmd
dev
```
*(ou double-clic sur [`dev.cmd`](file:///c:/Users/elias/Downloads/WankulCrazy/dev.cmd) depuis l'explorateur Windows)*

### Ce qu'il fait :
1. Compile le projet en Release (`dist/WankulCrazy/WankulCrazyPlugin.dll`).
2. Copie immédiatement le `.dll` mis à jour directement dans le jeu :
   `E:\jeux\TCG Card Shop Simulator\BepInEx\plugins\WankulCrazy\WankulCrazyPlugin.dll`.
3. T'avertit si le jeu est ouvert sans faire crasher la commande.

---

## 2. Mode Release (Package Joueur) : `release`

Dans n'importe quel terminal à la racine du projet :
```cmd
release
```
*(ou double-clic sur [`release.cmd`](file:///c:/Users/elias/Downloads/WankulCrazy/release.cmd))*

### Ce qu'il fait :
1. Compile une version propre du projet.
2. Prépare un dossier `WankulCrazy/` avec :
   - `WankulCrazyPlugin.dll`
   - `INSTALL.txt` et `LICENCE.txt`
   - L'intégralité du dossier `data/` (cartes de toutes les saisons, sprites, textures, raretés, masques, etc.).
3. Nettoie automatiquement les fichiers de dev (`save_*.json`, cartes de test, etc.).
4. Génère un fichier **`.zip` prêt à être distribué** dans le dossier [`releases/`](file:///c:/Users/elias/Downloads/WankulCrazy/releases/) :
   `releases/WankulCrazy_Release_YYYYMMDD_HHMMSS.zip`.

---

## 3. Structure du Zip pour le joueur final

Le joueur n'a qu'à extraire le zip dans son dossier de mods :
```text
TCG Card Shop Simulator/
└── BepInEx/
    └── plugins/
        └── WankulCrazy/
            ├── WankulCrazyPlugin.dll
            ├── INSTALL.txt
            ├── LICENCE.txt
            └── data/
                ├── seasons.json
                ├── rarities.json
                ├── cards/
                ├── sprites/
                ├── masks/
                ├── patchtextures/
                └── names/
```
