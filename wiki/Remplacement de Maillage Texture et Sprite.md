# Remplacement du maillage, de la texture et du sprite

> **Fichiers sources pertinents**
> * [importer/OBJImporter.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/OBJImporter.cs)
> * [importer/PatchTexturesImporter.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs)
> * [importer/WankulLoadingScreen.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/WankulLoadingScreen.cs)
> * [patch/WindowsPosters.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/patch/WindowsPosters.cs)
> * [utils/MaterialDiagnostics.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs)
> * [utils/ShaderUtils.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/ShaderUtils.cs)
> * [utils/TextureUtils.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/TextureUtils.cs)
> * [utils/obj/CharWordReader.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/CharWordReader.cs)
> * [utils/obj/OBJLoader.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/OBJLoader.cs)
> * [utils/obj/OBJLoaderHelper.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/OBJLoaderHelper.cs)
> * [utils/obj/OBJObjectBuilder.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/OBJObjectBuilder.cs)
> * [utils/obj/SplitMode.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/SplitMode.cs)
> * [utils/obj/StringExtensions.cs](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/StringExtensions.cs)

## Objectif et portée

Cette page détaille l'architecture d'injection d'actifs et de contenu responsable du remplacement des maillages, des textures, des sprites et des matériaux Vanilla dans *WankulCrazy*.Il couvre l'importateur d'actifs d'analyse de répertoire (`OBJImporter`), la pile d'analyseurs de streaming personnalisé `.obj` (`utils/obj`), le pipeline de correctifs de texture (`PatchTexturesImporter`), la sécurité des shaders d'exécution (`ShaderUtils`), les assistants de manipulation de texture (`TextureUtils`) et les diagnostics d'exécution des matériaux (`MaterialDiagnostics`).

---

## 1. Importateur OBJ et mise en cache des actifs (OBJImporter.cs)

La classe `OBJImporter` gère les modèles 3D personnalisés et les textures placées dans le répertoire de données du mod.Il initialise les chemins de fichiers, analyse les répertoires et met en cache les actifs au démarrage pour éviter les goulots d'étranglement d'E/S à l'exécution [importer/OBJImporter.cs L16-L182](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/OBJImporter.cs#L16-L182)

### Analyse et stockage des répertoires

`OBJImporter` établit des chemins pour les maillages, les noms et les sprites par rapport à `Plugin.GetPluginPath()` [importer/OBJImporter.cs L20-L22](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/OBJImporter.cs#L20-L22)

:

* `data/meshes/` (`path_mes`) : stocke les modèles 3D `.obj` personnalisés.
* `data/sprites/` (`path_spr`) : stocke les textures et sprites `.png` personnalisés.
* `data/names/` (`path_nam`) : stocke les métadonnées ou les configurations de noms.

Les fichiers sont indexés dans des dictionnaires insensibles à la casse (`filePaths_obj` et `filePaths_tex`) pendant `InitFiles()` [importer/OBJImporter.cs L26-L156](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/OBJImporter.cs#L26-L156)

Les textures et les maillages sont préchargés et mis en cache via `CacheTexturesAtStart()` et `CacheMeshesAtStart()` dans `cachedTextures` et `cachedMeshes` [importer/OBJImporter.cs L28-L182](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/OBJImporter.cs#L28-L182)

### Suivi du maillage et de la texture du podium

L'importateur gère des tableaux d'identifiants d'entité de podium vanille (`podium_meshes` et `podium_textures`) à accrocher aux présentoirs de magasin et aux socles de figurines [importer/OBJImporter.cs L34-L95](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/OBJImporter.cs#L34-L95)

```

```

**Sources :** `importer/OBJImporter.cs:16-182`

---

## 2. Pile de chargeur OBJ personnalisée (utils/obj)

L'espace de noms `utils/obj` fournit un analyseur de fichiers Wavefront `.obj` personnalisé et hautes performances, conçu pour éviter la surcharge d'allocation lors du chargement du modèle d'exécution.

### Composants clés de l'analyseur

* **`OBJLoader.cs`** : lit les flux d'entrée ligne par ligne, traite les sommets (`v`), les normales (`vn`), les coordonnées de texture (`vt`) et les faces (`f`) en fonction d'un `SplitMode` sélectionné (`Object`, `Material` ou `None`) [utils/obj/OBJLoader.csL9-L197](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/OBJLoader.cs#L9-L197) [utils/obj/SplitMode.csL7-L13](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/SplitMode.cs#L7-L13)
* **`CharWordReader.cs`** : un lecteur de flux de caractères et de mots à allocation nulle qui traite les tampons de fichiers directement à l'aide de pointeurs de caractères et du suivi d'index (`MoveNext`, `ReadVector`, `ReadFloat`, `ReadInt`) [utils/obj/CharWordReader.csL9-L182](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/CharWordReader.cs#L9-L182)
* **`OBJLoaderHelper.cs`** : fournit des aides mathématiques de bas niveau, notamment `FastFloatParse` et `FastIntParse`, pour une conversion numérique rapide, une configuration de la transparence des matériaux (`EnableMaterialTransparency`) et une génération sécurisée de matériaux de secours [utils/obj/OBJLoaderHelper.csL10-L100](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/OBJLoaderHelper.cs#L10-L100)
* **`StringExtensions.cs`** : étend la manipulation des chaînes avec une méthode `Clean()` pour nettoyer le contenu de la ligne `.obj` en supprimant les espaces et les tabulations supplémentaires [utils/obj/StringExtensions.csL7-L17](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/obj/StringExtensions.cs#L7-L17)

```

```

**Sources :** `utils/obj/OBJLoader.cs:9-198`, `utils/obj/CharWordReader.cs:9-182`, `utils/obj/OBJLoaderHelper.cs:10-100`, `utils/obj/SplitMode.cs:7-13`, `utils/obj/StringExtensions.cs:7-17`

---

## 3. Importateur de textures de patch (PatchTexturesImporter.cs)

`PatchTexturesImporter` applique dynamiquement les remplacements de texture aux ressources du jeu Vanilla en fonction des niveaux de qualité de texture [importer/PatchTexturesImporter.cs L10-L71](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs#L10-L71)

### Pipeline de remplacement et de conversion

Lorsque `ReplaceGameTextures(string textureLevel)` s'exécute, il analyse `data/patchtextures/<textureLevel>` à la recherche de fichiers image de remplacement [importer/PatchTexturesImporter.cs L12-L18](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs#L12-L18)

Pour chaque texture correspondante trouvée dans les ressources chargées de Unity (`Resources.FindObjectsOfTypeAll<Texture>`), `ReplaceTexture(Texture2D original, Texture2D replacement)` est invoqué [importer/PatchTexturesImporter.cs L21-L45](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs#L21-L45)

Il gère :

* **Originaux vides/de dimension nulle :** Génère et copie les données dans une nouvelle instance de texture [importer/PatchTexturesImporter.cs L72-L92](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs#L72-L92)
* **Validation des dimensions :** Rejette les discordances où `original.width != replacement.width` ou la hauteur [importer/PatchTexturesImporter.cs L94-L98](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs#L94-L98)
* **Ajustements du mipmap et du format :** Ajuste automatiquement le nombre de mipmap (`AdjustMipMapLevels`) et convertit les formats de texture (`ConvertTextureFormat` pour les contraintes de compression DXT1/DXT5) [importer/PatchTexturesImporter.csL103-L113](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs#L103-L113)
* **Amélioration du rendu :** Force le filtrage trilinéaire, le niveau anisotrope 16 et le biais mipmap négatif (`-0.5f`) sur les textures remplacées pour un rendu de surface de haute clarté [importer/PatchTexturesImporter.csL119-L123](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs#L119-L123)

Les préfixes de texture personnalisés connus (par exemple, `Texture_Display_`, `Texture_Booster_`, `MonsterStatue_`, `T_CardSleeve`) sont contournés en toute sécurité s'ils ne sont pas présents dans les ressources Vanilla [importer/PatchTexturesImporter.csL50-L64](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/importer/PatchTexturesImporter.cs#L50-L64)

```

```

**Sources :** `importer/PatchTexturesImporter.cs:10-141`

---

## 4. Utilitaires de shader et de texture (ShaderUtils.cs, TextureUtils.cs)

Pour éviter les incompatibilités des pipelines de rendu et le redoutable bug du shader magenta plein écran dans les pipelines de rendu intégrés, URP et HDRP, des classes d'utilitaires spécialisées gèrent les shaders et les textures.

### Sécurité des shaders (ShaderUtils.cs)

L'appel de `Shader.Find("Standard")` échoue ou supprime des variantes dans les pipelines non intégrés, ce qui entraîne des matériaux magenta [utils/ShaderUtils.cs L7-L12](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/ShaderUtils.cs#L7-L12)

`ShaderUtils` résout ce problème en :

* **`GetFallbackLitShader()`** : détecte les pipelines de rendu scriptables actifs (`GraphicsSettings.currentRenderPipeline`) et renvoie `HDRP/Lit`, `Universal Render Pipeline/Lit` ou des solutions de secours intégrées sécurisées comme `Unlit/UnlitWithShadowcaster` ou `Sprites/Default` [utils/ShaderUtils.csL18-L62](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/ShaderUtils.cs#L18-L62)
* **`UsesBrokenStandardShader(Material m)`** : Identifie si un matériau repose sur le shader `"Standard"` cassé [utils/ShaderUtils.cs L70-L73](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/ShaderUtils.cs#L70-L73)
* **`EnsureNotBrokenStandard(Material m)`** : clone et migre les matériaux standards défectueux vers des shaders sûrs tout en préservant les références de texture (`_MainTex`, `_BaseColorMap`, `_BaseMap`) [utils/ShaderUtils.csL80-L98](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/ShaderUtils.cs#L80-L98)
* **`CreateSafeMaterial()`** : instancie les matériaux en toute sécurité à l'aide de shaders validés par pipeline [utils/ShaderUtils.cs L106-L115](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/ShaderUtils.cs#L106-L115)

### Utilitaires de texture (TextureUtils.cs)

* **`MakeTextureReadable(Texture2D texture)`** : utilise un `RenderTexture` temporaire via `Graphics.Blit` pour copier les textures verrouillées par GPU dans des instances `Texture2D` lisibles par le CPU sans détruire les mipmaps ni les autorisations de lecture [utils/TextureUtils.csL10-L29](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/TextureUtils.cs#L10-L29)
* **`LoadTexture(string path)`** : lit les octets d'image brute du disque et les charge dans un objet `Texture2D` [utils/TextureUtils.cs L31-L38](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/TextureUtils.cs#L31-L38)

**Sources :** `utils/ShaderUtils.cs:1-117`, `utils/TextureUtils.cs:1-40`

---

## 5. Diagnostic des matériaux (Material Diagnostics.cs)

`MaterialDiagnostics` sert d'outil de vérification d'exécution pour déboguer l'affectation des shaders et les problèmes de rendu [utils/MaterialDiagnostics.cs L5-L11](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs#L5-L11)

La fonction principale `DumpRenderers(GameObject root, string context)` effectue une inspection récursive de tous les composants `Renderer` sous une cible `GameObject` [utils/MaterialDiagnostics.cs L13-L56](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs#L13-L56)

Pour chaque emplacement de matériau, il enregistre :

* Nom du matériau et référence du shader [utils/MaterialDiagnostics.cs L36-L43](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs#L36-L43)
* Statut de prise en charge des shaders (`shader.isSupported`) [utils/MaterialDiagnostics.cs L37-L43](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs#L37-L43)
* Index de file d'attente de rendu [utils/MaterialDiagnostics.cs L44](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs#L44-L44)
* Vérification de l'affectation des propriétés de texture pour `_BaseColorMap`, `_BaseMap` et `_MainTex` [utils/MaterialDiagnostics.cs L38-L44](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs#L38-L44)
* Type de pipeline de rendu graphique actif [utils/MaterialDiagnostics.cs L54](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs#L54-L54)

Si un shader est nul ou non pris en charge, un journal d'erreurs signale l'emplacement du moteur de rendu comme candidat au rendu magenta [utils/MaterialDiagnostics.cs L47-L50](https://github.com/Drakilis-57/WankulCrazy/blob/57b1f5ed/utils/MaterialDiagnostics.cs#L47-L50)

```

```

**Sources :** `utils/MaterialDiagnostics.cs:1-57`