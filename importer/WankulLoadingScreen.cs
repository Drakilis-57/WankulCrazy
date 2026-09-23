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
        private GameObject _overlay;
        private Text _titleText;
        private Text _statusText;
        private Image _progressBarFill;

        public static void ShowAndStartLoading(List<WankulCardData> cards, Action onComplete)
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("WankulLoadingScreen");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<WankulLoadingScreen>();
            }

            _instance.StartCoroutine(_instance.LoadCardsCoroutine(cards, onComplete));
        }

        private void Awake()
        {
            CreateUI();
        }

        private void CreateUI()
        {
            _overlay = new GameObject("Overlay");
            _overlay.transform.SetParent(transform, false);

            _canvas = _overlay.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999; // Toujours au premier plan absolu

            CanvasScaler scaler = _overlay.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _overlay.AddComponent<GraphicRaycaster>();

            // Fond sombre opaque
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(_overlay.transform, false);
            Image bg = bgObj.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.10f, 0.96f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            Font defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (defaultFont == null)
            {
                try { defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16); } catch { }
            }

            // Titre Wankul TCG
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(_overlay.transform, false);
            _titleText = titleObj.AddComponent<Text>();
            if (defaultFont != null) _titleText.font = defaultFont;
            _titleText.fontSize = 42;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = new Color(0.95f, 0.85f, 0.35f, 1f); // Jaune/Doré Wankul
            _titleText.text = "WANKUL CRAZY - CHARGEMENT DES CARTES";
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 80);
            titleRect.sizeDelta = new Vector2(1000, 70);

            // Texte de statut (ex: Chargement 240 / 895 cartes...)
            GameObject statusObj = new GameObject("Status");
            statusObj.transform.SetParent(_overlay.transform, false);
            _statusText = statusObj.AddComponent<Text>();
            if (defaultFont != null) _statusText.font = defaultFont;
            _statusText.fontSize = 22;
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.color = Color.white;
            _statusText.text = "Initialisation...";
            RectTransform statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.anchoredPosition = new Vector2(0, 10);
            statusRect.sizeDelta = new Vector2(800, 40);

            // Barre de progression - Fond
            GameObject barBgObj = new GameObject("ProgressBarBg");
            barBgObj.transform.SetParent(_overlay.transform, false);
            Image barBg = barBgObj.AddComponent<Image>();
            barBg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
            RectTransform barBgRect = barBgObj.GetComponent<RectTransform>();
            barBgRect.anchoredPosition = new Vector2(0, -50);
            barBgRect.sizeDelta = new Vector2(600, 24);

            // Barre de progression - Remplissage
            GameObject barFillObj = new GameObject("ProgressBarFill");
            barFillObj.transform.SetParent(barBgObj.transform, false);
            _progressBarFill = barFillObj.AddComponent<Image>();
            _progressBarFill.color = new Color(0.3f, 0.75f, 0.35f, 1f); // Vert vif
            _progressBarFill.type = Image.Type.Filled;
            _progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            _progressBarFill.fillAmount = 0f;
            RectTransform barFillRect = barFillObj.GetComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.sizeDelta = Vector2.zero;

            _overlay.SetActive(false);
        }

        private IEnumerator LoadCardsCoroutine(List<WankulCardData> cards, Action onComplete)
        {
            if (cards == null || cards.Count == 0)
            {
                onComplete?.Invoke();
                yield break;
            }

            _overlay.SetActive(true);

            string pluginPath = Plugin.GetPluginPath();
            int total = cards.Count;
            int loaded = 0;
            const int batchSize = 12; // Décompresse 12 images par frame pour fluidité maximale sans freeze Windows

            for (int i = 0; i < total; i++)
            {
                var card = cards[i];
                if (!string.IsNullOrEmpty(card.TexturePath))
                {
                    string texturepath = Path.Combine(pluginPath, "data", card.TexturePath);
                    string texturepathmask = Path.Combine(pluginPath, "data/masks", card.TexturePath);

                    try
                    {
                        if (File.Exists(texturepath))
                        {
                            byte[] bytes = File.ReadAllBytes(texturepath);
                            Texture2D texture = new Texture2D(2, 2);
                            if (texture.LoadImage(bytes))
                            {
                                texture.wrapMode = TextureWrapMode.Clamp;
                                card.Texture = texture;
                                card.Sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                            }
                        }

                        if (File.Exists(texturepathmask))
                        {
                            byte[] maskBytes = File.ReadAllBytes(texturepathmask);
                            Texture2D texturemask = new Texture2D(2, 2);
                            if (texturemask.LoadImage(maskBytes))
                            {
                                texturemask.wrapMode = TextureWrapMode.Clamp;
                                card.TextureMask = texturemask;
                                card.SpriteMask = Sprite.Create(texturemask, new Rect(0, 0, texturemask.width, texturemask.height), new Vector2(0.5f, 0.5f));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger?.LogWarning($"Texture load failed for {card.TexturePath}: {ex.Message}");
                    }
                }

                loaded++;

                // Rendre la main à Unity et Windows toutes les 'batchSize' images pour garder l'interface fluide
                if (loaded % batchSize == 0 || loaded == total)
                {
                    float progress = (float)loaded / total;
                    if (_progressBarFill != null) _progressBarFill.fillAmount = progress;
                    if (_statusText != null) _statusText.text = $"Chargement des cartes : {loaded} / {total} ({(int)(progress * 100)}%)";
                    yield return null; // Prochaine frame (évite le "Ne répond pas")
                }
            }

            if (_statusText != null) _statusText.text = "Chargement terminé !";
            yield return new WaitForSeconds(0.3f);

            _overlay.SetActive(false);
            onComplete?.Invoke();
        }
    }
}
