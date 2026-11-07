using System;
using System.IO;
using HarmonyLib;
using UnityEngine;
using WankulCrazyPlugin.utils;

namespace WankulCrazyPlugin.patch
{
    /// <summary>
    /// Protège LoadingScreen et ScreenRatioScaler contre les NRE, et injecte le visuel
    /// Wankul sur la carte 3D tournante de l'écran de chargement.
    /// </summary>
    public static class SceneLifecyclePatches
    {
        private static Texture2D s_WankulLoadingFront;
        private static Texture2D s_WankulLoadingBack;
        private static Sprite s_WankulLoadingFrontSprite;
        private static Sprite s_WankulLoadingBackSprite;

        public static Sprite GetWankulLoadingFrontSprite()
        {
            EnsureTexturesLoaded();
            return s_WankulLoadingFrontSprite;
        }

        public static Sprite GetWankulLoadingBackSprite()
        {
            EnsureTexturesLoaded();
            return s_WankulLoadingBackSprite;
        }

        public static void OpenScreenPostfix()
        {
            try
            {
                var screen = CSingleton<LoadingScreen>.Instance;
                if (screen != null)
                {
                    if (screen.gameObject.GetComponent<WankulLoadingScreenWatcher>() == null)
                    {
                        screen.gameObject.AddComponent<WankulLoadingScreenWatcher>();
                    }
                    ApplyWankulLoadingCard(screen.gameObject);
                }
                ApplyToAllLoadingObjects();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[LoadingScreenPatch] Erreur lors de l'application de la carte Wankul : {ex.Message}");
            }
        }

        public static void OnPressLoadGamePrefix()
        {
            try
            {
                ApplyToAllLoadingObjects();
            }
            catch { }
        }

        private static void EnsureTexturesLoaded()
        {
            string pluginPath = Plugin.GetPluginPath();
            if (s_WankulLoadingFront == null)
            {
                string frontPath = Path.Combine(pluginPath, "data", "patchtextures", "shared1", "T_3dCardModel.png");
                if (File.Exists(frontPath))
                {
                    s_WankulLoadingFront = TextureUtils.LoadTexture(frontPath);
                    if (s_WankulLoadingFront != null)
                    {
                        s_WankulLoadingFront.name = "WankulLoadingFront";
                        s_WankulLoadingFront.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                        UnityEngine.Object.DontDestroyOnLoad(s_WankulLoadingFront);
                        s_WankulLoadingFrontSprite = Sprite.Create(s_WankulLoadingFront, new Rect(0, 0, s_WankulLoadingFront.width, s_WankulLoadingFront.height), new Vector2(0.5f, 0.5f));
                        s_WankulLoadingFrontSprite.name = "WankulLoadingFrontSprite";
                        UnityEngine.Object.DontDestroyOnLoad(s_WankulLoadingFrontSprite);
                    }
                }
            }

            if (s_WankulLoadingBack == null)
            {
                string backPath = Path.Combine(pluginPath, "data", "patchtextures", "shared0", "CardBack.png");
                if (!File.Exists(backPath))
                {
                    backPath = Path.Combine(pluginPath, "data", "patchtextures", "shared1", "T_CardBackMesh.png");
                }

                if (File.Exists(backPath))
                {
                    s_WankulLoadingBack = TextureUtils.LoadTexture(backPath);
                    if (s_WankulLoadingBack != null)
                    {
                        s_WankulLoadingBack.name = "WankulLoadingBack";
                        s_WankulLoadingBack.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                        UnityEngine.Object.DontDestroyOnLoad(s_WankulLoadingBack);
                        s_WankulLoadingBackSprite = Sprite.Create(s_WankulLoadingBack, new Rect(0, 0, s_WankulLoadingBack.width, s_WankulLoadingBack.height), new Vector2(0.5f, 0.5f));
                        s_WankulLoadingBackSprite.name = "WankulLoadingBackSprite";
                        UnityEngine.Object.DontDestroyOnLoad(s_WankulLoadingBackSprite);
                    }
                }
            }
        }

        public static void ApplyToAllLoadingObjects()
        {
            try
            {
                EnsureTexturesLoaded();

                // 1. LoadingScreen singleton
                if (CSingleton<LoadingScreen>.Instance != null)
                {
                    ApplyWankulLoadingCard(CSingleton<LoadingScreen>.Instance.gameObject);
                }

                // 2. SaveLoadGameSlotSelectScreen (écran de sauvegarde/chargement)
                if (CSingleton<SaveLoadGameSlotSelectScreen>.Instance != null)
                {
                    var saveScreen = CSingleton<SaveLoadGameSlotSelectScreen>.Instance;
                    if (saveScreen.m_SavingGameScreen != null)
                    {
                        ApplyWankulLoadingCard(saveScreen.m_SavingGameScreen);
                    }
                }

                // 3. Tous les sprites nommés CardBack dans les composants Image
                var allImages = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Image>();
                foreach (var img in allImages)
                {
                    if (img == null || img.gameObject == null) continue;
                    string name = img.name ?? "";
                    string spriteName = img.sprite != null ? img.sprite.name : "";
                    if (spriteName.Equals("CardBack", StringComparison.OrdinalIgnoreCase) ||
                        name.IndexOf("CardBack", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (s_WankulLoadingBackSprite != null)
                        {
                            img.sprite = s_WankulLoadingBackSprite;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[LoadingScreenPatch] Erreur dans ApplyToAllLoadingObjects : {ex.Message}");
            }
        }

        public static void ApplyWankulLoadingCard(GameObject root)
        {
            if (root == null) return;
            EnsureTexturesLoaded();

            // 1. Remplacement sur les composants UI Image (pour les cartes 2D animées)
            var images = root.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (var img in images)
            {
                if (img == null) continue;
                string imgName = img.name ?? "";
                string spriteName = img.sprite != null ? img.sprite.name : "";

                bool isBack = imgName.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              spriteName.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0;

                bool isCard = imgName.IndexOf("Card", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              spriteName.IndexOf("Card", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              imgName.IndexOf("Front", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              spriteName.IndexOf("Front", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              spriteName.IndexOf("IconOnly", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isBack && s_WankulLoadingBackSprite != null)
                {
                    img.sprite = s_WankulLoadingBackSprite;
                    Plugin.Logger.LogInfo($"[LoadingScreenPatch] Sprite Back Wankul appliqué sur UI.Image '{imgName}' (ancien sprite: '{spriteName}').");
                }
                else if (isCard && !isBack && s_WankulLoadingFrontSprite != null)
                {
                    img.sprite = s_WankulLoadingFrontSprite;
                    Plugin.Logger.LogInfo($"[LoadingScreenPatch] Sprite Front Wankul appliqué sur UI.Image '{imgName}' (ancien sprite: '{spriteName}').");
                }
            }

            // 2. Remplacement sur les RawImage
            var rawImages = root.GetComponentsInChildren<UnityEngine.UI.RawImage>(true);
            foreach (var rImg in rawImages)
            {
                if (rImg == null) continue;
                string rName = rImg.name ?? "";
                string texName = rImg.texture != null ? rImg.texture.name : "";

                bool isBack = rName.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              texName.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0;

                bool isCard = rName.IndexOf("Card", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              texName.IndexOf("Card", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isBack && s_WankulLoadingBack != null)
                {
                    rImg.texture = s_WankulLoadingBack;
                    Plugin.Logger.LogInfo($"[LoadingScreenPatch] Texture Back Wankul appliquée sur RawImage '{rName}'.");
                }
                else if (isCard && s_WankulLoadingFront != null)
                {
                    rImg.texture = s_WankulLoadingFront;
                    Plugin.Logger.LogInfo($"[LoadingScreenPatch] Texture Front Wankul appliquée sur RawImage '{rName}'.");
                }
            }

            // 3. Remplacement sur les Renderer 3D traditionnels (MeshRenderer, etc.)
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                Material[] mats = r.sharedMaterials;
                if (mats == null || mats.Length == 0) continue;

                bool modified = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    if (m == null) continue;

                    string matName = m.name ?? "";
                    string goName = r.gameObject.name ?? "";

                    bool isBack = matName.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  goName.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0;

                    bool isCard = matName.IndexOf("Card", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  goName.IndexOf("Card", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  matName.IndexOf("3d", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  matName.IndexOf("Model", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!isCard && !isBack) continue;

                    Texture2D targetTex = isBack ? s_WankulLoadingBack : s_WankulLoadingFront;
                    if (targetTex == null) continue;

                    Material safeMat = ShaderUtils.CreateSafeMaterial(ShaderUtils.EnsureNotBrokenStandard(m));
                    if (safeMat.HasProperty("_BaseColorMap")) safeMat.SetTexture("_BaseColorMap", targetTex);
                    if (safeMat.HasProperty("_BaseMap")) safeMat.SetTexture("_BaseMap", targetTex);
                    if (safeMat.HasProperty("_MainTex")) safeMat.SetTexture("_MainTex", targetTex);
                    safeMat.hideFlags |= HideFlags.DontUnloadUnusedAsset;

                    mats[i] = safeMat;
                    modified = true;
                }

                if (modified)
                {
                    r.sharedMaterials = mats;
                    Plugin.Logger.LogInfo($"[LoadingScreenPatch] Carte Wankul appliquée sur Renderer '{r.gameObject.name}' ({mats.Length} slot(s)).");
                }
            }
        }

        public static void ApplyWankulLoadingCard(LoadingScreen screen)
        {
            if (screen == null) return;
            ApplyWankulLoadingCard(screen.gameObject);
        }

        public static void FinishLoadingPrefix()
        {
            try
            {
                ApplyToAllLoadingObjects();
            }
            catch { }
        }

        public static bool CloseScreenPrefix()
        {
            try
            {
                if (CSingleton<LoadingScreen>.Instance == null)
                {
                    return false;
                }
                ApplyToAllLoadingObjects();
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static bool ScreenRatioScalerInitPrefix(ScreenRatioScaler __instance)
        {
            if (__instance == null)
            {
                return false;
            }

            try
            {
                if (CSingleton<InputManager>.Instance == null)
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Surveille l'écran de chargement en continu chaque frame (LateUpdate) pour s'assurer
    /// que les sprites/materials restent 100% Wankul et ne soient jamais écrasés par les visuels vanilla
    /// lors du chargement à 100% ou du fade out.
    /// </summary>
    public class WankulLoadingScreenWatcher : MonoBehaviour
    {
        private LoadingScreen m_LoadingScreen;
        private UnityEngine.UI.Image m_FrontCardImage;

        private void Awake()
        {
            m_LoadingScreen = GetComponent<LoadingScreen>();
        }

        private void LateUpdate()
        {
            if (m_LoadingScreen == null || m_LoadingScreen.m_ScreenGrp == null) return;
            if (!m_LoadingScreen.m_ScreenGrp.activeSelf) return;

            Sprite wankulFront = SceneLifecyclePatches.GetWankulLoadingFrontSprite();
            if (wankulFront == null) return;

            if (m_FrontCardImage == null || m_FrontCardImage.sprite != wankulFront)
            {
                SceneLifecyclePatches.ApplyWankulLoadingCard(m_LoadingScreen.gameObject);

                var images = m_LoadingScreen.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                foreach (var img in images)
                {
                    if (img != null && (img.name == "CardFront" || img.name == "CardBG"))
                    {
                        m_FrontCardImage = img;
                        break;
                    }
                }
            }
        }
    }
}
