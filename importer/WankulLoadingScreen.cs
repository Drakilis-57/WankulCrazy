using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.importer
{
    public class WankulLoadingScreen : MonoBehaviour
    {
        private static WankulLoadingScreen _instance;
        private Canvas _canvas;
        private Text _statusText;
        private Image _progressBarFill;

        /// <summary>True tant que les textures des cartes sont en cours de chargement.
        /// A utiliser pour bloquer l'ouverture de boosters / de l'album.</summary>
        public static bool IsLoading { get; private set; }

        // Budget de temps par frame (ms) : plus fluide qu'un nombre d'images fixe.
        private const float FrameBudgetMs = 12f;
        // Une carte qui met plus longtemps que ca est loggee.
        private const long SlowCardMs = 250;

        public static void ShowAndStartLoading(List<WankulCardData> cards, Action onComplete)
        {
            if (IsLoading)
            {
                Plugin.Logger?.LogWarning("[WankulLoadingScreen] Chargement deja en cours, demande ignoree.");
                return;
            }

            if (_instance == null)
            {
                GameObject go = new GameObject("WankulLoadingScreen", typeof(RectTransform));
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<WankulLoadingScreen>();
            }

            IsLoading = true;
            _instance.StartCoroutine(_instance.LoadCardsCoroutine(cards, onComplete));
        }

        private void Awake()
        {
            CreateUI();
        }

        private void OnDestroy()
        {
            IsLoading = false;
            if (_instance == this) _instance = null;
        }

        private void CreateUI()
        {
            _canvas = WankulUiKit.SetupCanvas(gameObject, 9999);

            Image bg = WankulUiKit.CreateImage(transform, "Background", new Color(0.08f, 0.08f, 0.10f, 0.96f));
            WankulUiKit.Stretch(bg.rectTransform);

            Text title = WankulUiKit.CreateText(transform, "Title", "WANKUL CRAZY - CHARGEMENT DES CARTES",
                42, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.95f, 0.85f, 0.35f, 1f));
            WankulUiKit.PlaceCentered(title.rectTransform, new Vector2(0, 80), new Vector2(1000, 70));

            _statusText = WankulUiKit.CreateText(transform, "Status", "Initialisation...",
                22, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            WankulUiKit.PlaceCentered(_statusText.rectTransform, new Vector2(0, 10), new Vector2(800, 40));

            Image barBg = WankulUiKit.CreateImage(transform, "ProgressBarBg", new Color(0.18f, 0.18f, 0.22f, 1f));
            WankulUiKit.PlaceCentered(barBg.rectTransform, new Vector2(0, -50), new Vector2(600, 26));

            _progressBarFill = WankulUiKit.CreateImage(barBg.transform, "ProgressBarFill", new Color(0.3f, 0.75f, 0.35f, 1f));
            _progressBarFill.type = Image.Type.Filled;
            _progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            _progressBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _progressBarFill.fillAmount = 0f;
            WankulUiKit.Stretch(_progressBarFill.rectTransform);

            _canvas.enabled = false;
        }

        private IEnumerator LoadCardsCoroutine(List<WankulCardData> cards, Action onComplete)
        {
            if (cards == null || cards.Count == 0)
            {
                IsLoading = false;
                onComplete?.Invoke();
                yield break;
            }

            _canvas.enabled = true;
            _progressBarFill.fillAmount = 0f;
            _statusText.text = "Preparation du chargement...";
            yield return null;

            string pluginPath = Plugin.GetPluginPath();
            int total = cards.Count;
            int loaded = 0;
            int missing = 0;
            int failed = 0;
            var totalWatch = System.Diagnostics.Stopwatch.StartNew();
            var frameWatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < total; i++)
            {
                var cardWatch = System.Diagnostics.Stopwatch.StartNew();
                LoadCardTextures(cards[i], pluginPath, ref missing, ref failed);
                if (cardWatch.ElapsedMilliseconds > SlowCardMs)
                {
                    Plugin.Logger?.LogWarning($"[WankulLoadingScreen] Carte lente ({cardWatch.ElapsedMilliseconds} ms) : {cards[i].Title} ({cards[i].TexturePath})");
                }

                loaded++;

                if (frameWatch.ElapsedMilliseconds >= FrameBudgetMs || loaded == total)
                {
                    float progress = (float)loaded / total;
                    _progressBarFill.fillAmount = progress;
                    _statusText.text = $"Chargement des cartes : {loaded} / {total} ({(int)(progress * 100)}%)";
                    yield return null;
                    frameWatch.Restart();
                }
            }

            Plugin.LogInfo($"[WankulLoadingScreen] {total} cartes traitees en {totalWatch.ElapsedMilliseconds} ms ({missing} sans fichier texture, {failed} en echec).");
            if (missing > 0)
            {
                Plugin.Logger?.LogWarning($"[WankulLoadingScreen] {missing} carte(s) ont un TexturePath introuvable dans data/.");
            }

            _statusText.text = "Chargement termine !";
            yield return new WaitForSeconds(0.3f);

            _canvas.enabled = false;
            IsLoading = false;
            onComplete?.Invoke();
        }

        private static void LoadCardTextures(WankulCardData card, string pluginPath, ref int missing, ref int failed)
        {
            if (string.IsNullOrEmpty(card.TexturePath)) return;

            string texturePath = ResolveExistingPath(Path.Combine(pluginPath, "data", card.TexturePath));
            string maskPath = ResolveMaskPath(card, pluginPath);

            try
            {
                if (texturePath != null)
                {
                    Texture2D texture = LoadTexture(texturePath);
                    if (texture != null)
                    {
                        card.Texture = texture;
                        card.Sprite = CreateSprite(texture);
                    }
                    else
                    {
                        failed++;
                        Plugin.Logger?.LogWarning($"Texture illisible : {card.TexturePath}");
                    }
                }
                else
                {
                    missing++;
                    Plugin.LogDebug($"Texture introuvable : {Path.Combine(pluginPath, "data", card.TexturePath)}");
                }

                // Le masque est optionnel : son absence est normale.
                bool isFoil = IsCardFoil(card);
                if (maskPath != null)
                {
                    Texture2D mask = LoadTexture(maskPath);
                    if (mask != null)
                    {
                        card.TextureMask = mask;
                        card.SpriteMask = CreateSprite(mask);
                    }
                    else if (isFoil)
                    {
                        Plugin.Logger?.LogWarning($"[Foil] Le masque a echoue au chargement pour la carte foil {card.Title} (#{card.Number}): {maskPath}");
                    }
                }
                else if (isFoil)
                {
                    Plugin.Logger?.LogWarning($"[Foil] Aucun masque trouve pour la carte foil {card.Title} (#{card.Number})");
                }
            }
            catch (Exception ex)
            {
                failed++;
                Plugin.Logger?.LogWarning($"Texture load failed for {card.TexturePath}: {ex.Message}");
            }
        }

        public static bool IsCardFoil(WankulCardData card)
        {
            if (card is EffigyCardData effigyCard)
            {
                var rarityData = RaritiesManager.GetRarity(effigyCard.RarityId);
                if ((rarityData != null && rarityData.IsEligibleForFoil) || effigyCard.Rarity >= Rarity.UR1)
                {
                    return true;
                }
            }
            return false;
        }

        public static string ResolveMaskPath(WankulCardData card, string pluginPath)
        {
            if (string.IsNullOrEmpty(card.TexturePath)) return null;

            // 1. Chemin direct relatif (data/masks/<TexturePath>)
            string directPath = Path.Combine(pluginPath, "data/masks", card.TexturePath);
            string resolvedDirect = ResolveExistingPath(directPath);
            if (resolvedDirect != null) return resolvedDirect;

            // 2. Résolution dynamique selon la saison dans data/masks/textures/{prefix}_{card.Number}.png
            int num = card.NumberInt;
            if (num > 0)
            {
                string season = !string.IsNullOrEmpty(card.SeasonId) ? card.SeasonId : "";
                string texturePath = card.TexturePath.Replace('\\', '/');

                int? prefix = null;
                if (season == "S01" || texturePath.Contains("Origins/"))
                {
                    prefix = 150 + (num - 151); // 151 -> 150_151.png, 180 -> 179_180.png
                }
                else if (season == "S02" || texturePath.Contains("Campus/"))
                {
                    prefix = 305 + (num - 126); // 126 -> 305_126.png, 155 -> 334_155.png
                }
                else if (season == "S03" || texturePath.Contains("Battle/"))
                {
                    prefix = 516 + (num - 151); // 151 -> 516_151.png, 180 -> 545_180.png
                }
                else if (season == "S04" || texturePath.Contains("Stellar/"))
                {
                    prefix = 696 + (num - 151); // 151 -> 696_151.png, 180 -> 725_180.png
                }

                if (prefix.HasValue && prefix.Value >= 0)
                {
                    string seasonMaskPath = Path.Combine(pluginPath, "data/masks/textures", $"{prefix.Value}_{card.Number}.png");
                    string resolvedSeason = ResolveExistingPath(seasonMaskPath);
                    if (resolvedSeason != null) return resolvedSeason;
                }

                // Fallback générique : {num-1}_{num}.png
                string fallbackIdPath = Path.Combine(pluginPath, "data/masks/textures", $"{num - 1}_{card.Number}.png");
                string resolvedFallback = ResolveExistingPath(fallbackIdPath);
                if (resolvedFallback != null) return resolvedFallback;
            }

            return null;
        }

        public static string ResolveExistingPath(string path)
        {
            if (File.Exists(path)) return path;

            string ext = Path.GetExtension(path);
            if (string.IsNullOrEmpty(ext)) return null;

            if (ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                string altJpg = Path.ChangeExtension(path, ".jpg");
                if (File.Exists(altJpg)) return altJpg;

                string altJpeg = Path.ChangeExtension(path, ".jpeg");
                if (File.Exists(altJpeg)) return altJpeg;
            }
            else if (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                string altPng = Path.ChangeExtension(path, ".png");
                if (File.Exists(altPng)) return altPng;
            }

            return null;
        }

        private static Texture2D LoadTexture(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            // markNonReadable : la copie CPU est liberee, ~2x moins de RAM. Ces textures ne sont jamais relues par GetPixels.
            if (!texture.LoadImage(bytes, true))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private static Sprite CreateSprite(Texture2D texture)
        {
            // FullRect : evite la generation d'un mesh de contour (Tight) pour chaque sprite.
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
    }
}
