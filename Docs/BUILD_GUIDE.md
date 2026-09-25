Pour lancer les builds facilement, deux raccourcis sont disponibles à la racine :

- En **PowerShell** : `.\dev` ou `.\release`
- En **Invite de commandes (CMD)** : `dev` ou `release`
- Dans **l'Explorateur Windows** : double-clique directement sur [`dev.cmd`](file:///c:/Users/elias/Downloads/WankulCrazy/dev.cmd) ou [`release.cmd`](file:///c:/Users/elias/Downloads/WankulCrazy/release.cmd).

---

## 1. Mode Développement : `.\dev`

Dans ton terminal PowerShell :
```powershell
.\dev
```

### Ce qu'il fait :
1. Compile le projet en Release (`dist/WankulCrazy/WankulCrazyPlugin.dll`).
2. Copie immédiatement le `.dll` mis à jour directement dans ton dossier de jeu (configuré dans [`local.config.json`](file:///c:/Users/elias/Downloads/WankulCrazy/local.config.json)).
3. T'avertit si le jeu est ouvert et verrouille la DLL sans faire planter la console.

---

## 2. Mode Release (Package Joueur) : `.\release`

Dans ton terminal PowerShell :
```powershell
.\release
```
*(ou en invite CMD : `release`, ou double-clic sur [`release.cmd`](file:///c:/Users/elias/Downloads/WankulCrazy/release.cmd))*

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
