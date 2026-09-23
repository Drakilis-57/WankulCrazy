using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace WankulCrazyPlugin.importer
{
    public class WankulDebugScreen : MonoBehaviour
    {
        private static WankulDebugScreen _instance;
        private Canvas _canvas;
        private GameObject _overlay;
        private Text _titleText;
        private Text _errorSummaryText;
        private Text _stackTraceText;

        public static void Initialize()
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("WankulDebugScreen", typeof(RectTransform));
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<WankulDebugScreen>();
            }
        }

        public static void Show(string title, string message, string stackTrace = null)
        {
            Initialize();
            _instance.DisplayError(title, message, stackTrace);
        }

        private void Awake()
        {
            CreateUI();
            Application.logMessageReceived += HandleUnityLog;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleUnityLog;
        }

        private static bool _isDisplaying = false;

        private void HandleUnityLog(string logString, string stackTrace, LogType type)
        {
            if (_isDisplaying) return;

            // Ignore les logs de fermeture normale du jeu
            if (logString.Contains("application is closing") || logString.Contains("Cancel creating"))
            {
                return;
            }

            // Intercepte les exceptions fatales (LogType.Exception) ou les erreurs avec StackTrace d'exception
            bool isFatalException = type == LogType.Exception;
            bool isFatalError = type == LogType.Error && (
                logString.Contains("Exception") ||
                logString.Contains("NullReference") ||
                logString.Contains("ArgumentOutOfRange") ||
                logString.Contains("IndexOutOfRange")
            );

            if (isFatalException || isFatalError)
            {
                _isDisplaying = true;
                string title = isFatalException ? "CRASH DETECTE (Exception)" : "ERREUR CRITIQUE DÉTECTÉE";
                DisplayError(title, logString, stackTrace);
            }
        }

        private void CreateUI()
        {
            _overlay = this.gameObject;

            _canvas = _overlay.GetComponent<Canvas>();
            if (_canvas == null) _canvas = _overlay.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10000; // Au-dessus de tout

            CanvasScaler scaler = _overlay.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = _overlay.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            if (_overlay.GetComponent<GraphicRaycaster>() == null)
                _overlay.AddComponent<GraphicRaycaster>();

            Texture2D whiteTex = Texture2D.whiteTexture;
            Sprite whiteSprite = Sprite.Create(whiteTex, new Rect(0, 0, whiteTex.width, whiteTex.height), new Vector2(0.5f, 0.5f));

            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
            {
                try { font = Font.CreateDynamicFontFromOSFont("Arial", 16); } catch { }
            }

            // Fond sombre semi-transparent
            GameObject bgObj = new GameObject("BgOverlay", typeof(RectTransform));
            bgObj.transform.SetParent(_overlay.transform, false);
            Image bg = bgObj.AddComponent<Image>();
            bg.sprite = whiteSprite;
            bg.color = new Color(0.04f, 0.04f, 0.06f, 0.95f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Panneau central
            GameObject panelObj = new GameObject("MainPanel", typeof(RectTransform));
            panelObj.transform.SetParent(_overlay.transform, false);
            Image panelImg = panelObj.AddComponent<Image>();
            panelImg.sprite = whiteSprite;
            panelImg.color = new Color(0.12f, 0.12f, 0.16f, 1f);
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(1200, 720);
            panelRect.anchoredPosition = Vector2.zero;

            // Bordure supérieure rouge
            GameObject topBarObj = new GameObject("TopBar", typeof(RectTransform));
            topBarObj.transform.SetParent(panelObj.transform, false);
            Image topBarImg = topBarObj.AddComponent<Image>();
            topBarImg.sprite = whiteSprite;
            topBarImg.color = new Color(0.85f, 0.2f, 0.2f, 1f);
            RectTransform topBarRect = topBarObj.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0, 1);
            topBarRect.anchorMax = new Vector2(1, 1);
            topBarRect.pivot = new Vector2(0.5f, 1);
            topBarRect.sizeDelta = new Vector2(0, 10);
            topBarRect.anchoredPosition = Vector2.zero;

            // Titre d'erreur
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));
            titleObj.transform.SetParent(panelObj.transform, false);
            _titleText = titleObj.AddComponent<Text>();
            if (font != null) _titleText.font = font;
            _titleText.fontSize = 28;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleLeft;
            _titleText.color = new Color(1f, 0.35f, 0.35f, 1f);
            _titleText.text = "WANKUL CRAZY - ERREUR CRITIQUE DÉTECTÉE";
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(40, 310);
            titleRect.sizeDelta = new Vector2(1120, 50);

            // Message d'erreur
            GameObject summaryObj = new GameObject("SummaryText", typeof(RectTransform));
            summaryObj.transform.SetParent(panelObj.transform, false);
            _errorSummaryText = summaryObj.AddComponent<Text>();
            if (font != null) _errorSummaryText.font = font;
            _errorSummaryText.fontSize = 18;
            _errorSummaryText.fontStyle = FontStyle.Bold;
            _errorSummaryText.alignment = TextAnchor.UpperLeft;
            _errorSummaryText.color = Color.white;
            _errorSummaryText.text = "Une exception est survenue.";
            RectTransform summaryRect = summaryObj.GetComponent<RectTransform>();
            summaryRect.anchoredPosition = new Vector2(40, 240);
            summaryRect.sizeDelta = new Vector2(1120, 80);

            // Cadre StackTrace
            GameObject stackBoxObj = new GameObject("StackBox", typeof(RectTransform));
            stackBoxObj.transform.SetParent(panelObj.transform, false);
            Image stackBoxImg = stackBoxObj.AddComponent<Image>();
            stackBoxImg.sprite = whiteSprite;
            stackBoxImg.color = new Color(0.06f, 0.06f, 0.08f, 1f);
            RectTransform stackBoxRect = stackBoxObj.GetComponent<RectTransform>();
            stackBoxRect.anchoredPosition = new Vector2(0, -30);
            stackBoxRect.sizeDelta = new Vector2(1120, 380);

            // Texte StackTrace
            GameObject stackTextObj = new GameObject("StackTraceText", typeof(RectTransform));
            stackTextObj.transform.SetParent(stackBoxObj.transform, false);
            _stackTraceText = stackTextObj.AddComponent<Text>();
            if (font != null) _stackTraceText.font = font;
            _stackTraceText.fontSize = 14;
            _stackTraceText.alignment = TextAnchor.UpperLeft;
            _stackTraceText.color = new Color(0.8f, 0.8f, 0.85f, 1f);
            _stackTraceText.text = "Détails de l'erreur indisponibles.";
            RectTransform stackTextRect = stackTextObj.GetComponent<RectTransform>();
            stackTextRect.anchorMin = Vector2.zero;
            stackTextRect.anchorMax = Vector2.one;
            stackTextRect.sizeDelta = new Vector2(-20, -20);
            stackTextRect.anchoredPosition = Vector2.zero;

            // Boutons d'action
            CreateButton(panelObj, whiteSprite, font, "📋 Copier l'erreur", new Vector2(-360, -300), new Vector2(240, 48), new Color(0.25f, 0.45f, 0.75f, 1f), OnCopyClicked);
            CreateButton(panelObj, whiteSprite, font, "⏩ Ignorer et Continuer", new Vector2(0, -300), new Vector2(240, 48), new Color(0.35f, 0.65f, 0.35f, 1f), OnIgnoreClicked);
            CreateButton(panelObj, whiteSprite, font, "❌ Quitter le jeu", new Vector2(360, -300), new Vector2(240, 48), new Color(0.75f, 0.25f, 0.25f, 1f), OnQuitClicked);

            if (_canvas != null) _canvas.enabled = false;
        }

        private void CreateButton(GameObject parent, Sprite sprite, Font font, string label, Vector2 pos, Vector2 size, Color btnColor, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject("Btn_" + label, typeof(RectTransform));
            btnObj.transform.SetParent(parent.transform, false);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = sprite;
            btnImg.color = btnColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            ColorBlock colors = btn.colors;
            colors.highlightedColor = btnColor * 1.2f;
            colors.pressedColor = btnColor * 0.8f;
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchoredPosition = pos;
            btnRect.sizeDelta = size;

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(btnObj.transform, false);
            Text t = textObj.AddComponent<Text>();
            if (font != null) t.font = font;
            t.fontSize = 16;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = label;

            RectTransform tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
        }

        private void DisplayError(string title, string message, string stackTrace)
        {
            if (_titleText != null) _titleText.text = title;
            if (_errorSummaryText != null) _errorSummaryText.text = message;
            if (_stackTraceText != null) _stackTraceText.text = string.IsNullOrEmpty(stackTrace) ? "Aucune trace de pile." : stackTrace;

            if (_canvas != null) _canvas.enabled = true;

            // Rétablir le curseur de la souris visible et déverrouillé
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void OnCopyClicked()
        {
            string fullReport = $"=== WANKUL CRAZY ERROR REPORT ===\n" +
                                $"Titre: {_titleText?.text}\n" +
                                $"Erreur: {_errorSummaryText?.text}\n" +
                                $"StackTrace:\n{_stackTraceText?.text}\n" +
                                $"================================";

            GUIUtility.systemCopyBuffer = fullReport;
            if (_errorSummaryText != null)
            {
                _errorSummaryText.text = "✓ Rapport copié dans le presse-papier ! Vous pouvez le coller avec Ctrl+V.";
            }
        }

        private void OnIgnoreClicked()
        {
            _isDisplaying = false;
            if (_canvas != null) _canvas.enabled = false;
        }

        private void OnQuitClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
