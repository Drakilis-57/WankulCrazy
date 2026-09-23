using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WankulCrazyPlugin.importer
{
    public class WankulDebugScreen : MonoBehaviour
    {
        private const string ModTag = "WankulCrazyPlugin";
        private const int MaxDisplayedStackChars = 2500;

        private static WankulDebugScreen _instance;
        private static bool _isDisplaying = false;

        private Canvas _canvas;
        private Text _titleText;
        private Text _errorSummaryText;
        private Text _stackTraceText;

        // Derniere erreur affichee (source de verite pour le bouton "Copier")
        private string _lastTitle = "";
        private string _lastMessage = "";
        private string _lastStack = "";
        private int _currentHash;

        private readonly HashSet<int> _ignoredHashes = new HashSet<int>();
        private bool _ignoreAll = false;

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
            _instance.TryDisplay(title, message, stackTrace);
        }

        private void Awake()
        {
            CreateUI();
            Application.logMessageReceived += HandleUnityLog;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleUnityLog;
            if (_instance == this) _instance = null;
            _isDisplaying = false;
        }

        private void Update()
        {
            // Le jeu recapture le curseur a chaque frame : on le libere tant que l'ecran est visible.
            if (_isDisplaying)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        private static bool IsFromMod(string logString, string stackTrace)
        {
            return (stackTrace != null && stackTrace.Contains(ModTag))
                || (logString != null && logString.Contains(ModTag));
        }

        private void HandleUnityLog(string logString, string stackTrace, LogType type)
        {
            if (_isDisplaying || _ignoreAll) return;

            if (logString.Contains("application is closing") || logString.Contains("Cancel creating"))
            {
                return;
            }

            bool isException = type == LogType.Exception;
            bool isCriticalError = type == LogType.Error && (
                logString.Contains("NullReferenceException") ||
                logString.Contains("ArgumentOutOfRangeException") ||
                logString.Contains("IndexOutOfRangeException")
            );

            if (!isException && !isCriticalError) return;

            // Uniquement les erreurs qui viennent de notre mod (pas le jeu de base / les autres mods)
            if (!IsFromMod(logString, stackTrace)) return;

            TryDisplay(isException ? "EXCEPTION DETECTEE" : "ERREUR CRITIQUE DETECTEE", logString, stackTrace);
        }

        private void TryDisplay(string title, string message, string stackTrace)
        {
            if (_ignoreAll) return;

            int hash = (title + "|" + message + "|" + stackTrace).GetHashCode();
            if (_ignoredHashes.Contains(hash)) return;
            if (_isDisplaying) return;

            _currentHash = hash;
            DisplayError(title, message, stackTrace);
        }

        private void CreateUI()
        {
            _canvas = WankulUiKit.SetupCanvas(gameObject, 10000);

            Image bg = WankulUiKit.CreateImage(transform, "BgOverlay", new Color(0.04f, 0.04f, 0.06f, 0.95f));
            WankulUiKit.Stretch(bg.rectTransform);

            Image panel = WankulUiKit.CreateImage(transform, "MainPanel", new Color(0.12f, 0.12f, 0.16f, 1f));
            WankulUiKit.PlaceCentered(panel.rectTransform, Vector2.zero, new Vector2(1200, 720));

            Image topBar = WankulUiKit.CreateImage(panel.transform, "TopBar", new Color(0.85f, 0.2f, 0.2f, 1f));
            WankulUiKit.Place(topBar.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -10), Vector2.zero);

            _titleText = WankulUiKit.CreateText(panel.transform, "TitleText", "WANKUL CRAZY - ERREUR DETECTEE",
                28, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.35f, 0.35f, 1f));
            WankulUiKit.Place(_titleText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -80), new Vector2(-40, -20));

            _errorSummaryText = WankulUiKit.CreateText(panel.transform, "SummaryText", "Une exception est survenue.",
                18, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            WankulUiKit.Place(_errorSummaryText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -190), new Vector2(-40, -90));

            Image stackBox = WankulUiKit.CreateImage(panel.transform, "StackBox", new Color(0.06f, 0.06f, 0.08f, 1f));
            WankulUiKit.Place(stackBox.rectTransform, Vector2.zero, Vector2.one, new Vector2(30, 100), new Vector2(-30, -200));

            _stackTraceText = WankulUiKit.CreateText(stackBox.transform, "StackTraceText", "Details indisponibles.",
                14, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.8f, 0.8f, 0.85f, 1f));
            _stackTraceText.verticalOverflow = VerticalWrapMode.Truncate;
            WankulUiKit.Place(_stackTraceText.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10));

            AddButton(panel.transform, 0, "Copier l'erreur", new Color(0.25f, 0.45f, 0.75f, 1f), OnCopyClicked);
            AddButton(panel.transform, 1, "Ignorer", new Color(0.35f, 0.65f, 0.35f, 1f), OnIgnoreClicked);
            AddButton(panel.transform, 2, "Ignorer tout", new Color(0.55f, 0.55f, 0.30f, 1f), OnIgnoreAllClicked);
            AddButton(panel.transform, 3, "Quitter le jeu", new Color(0.75f, 0.25f, 0.25f, 1f), OnQuitClicked);

            _canvas.enabled = false;
        }

        private static void AddButton(Transform parent, int column, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            Button btn = WankulUiKit.CreateButton(parent, label, color, onClick);
            const int columns = 4;
            WankulUiKit.Place((RectTransform)btn.transform,
                new Vector2((float)column / columns, 0), new Vector2((float)(column + 1) / columns, 0),
                new Vector2(20, 24), new Vector2(-20, 72));
        }

        private void DisplayError(string title, string message, string stackTrace)
        {
            _isDisplaying = true;
            _lastTitle = title ?? "";
            _lastMessage = message ?? "";
            _lastStack = string.IsNullOrEmpty(stackTrace) ? "Aucune trace de pile." : stackTrace;

            _titleText.text = _lastTitle;
            _errorSummaryText.text = _lastMessage;
            _stackTraceText.text = _lastStack.Length > MaxDisplayedStackChars
                ? _lastStack.Substring(0, MaxDisplayedStackChars) + "\n[... tronque : utilisez \"Copier l'erreur\" pour la trace complete]"
                : _lastStack;

            _canvas.enabled = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void Hide()
        {
            _isDisplaying = false;
            _canvas.enabled = false;
        }

        private void OnCopyClicked()
        {
            GUIUtility.systemCopyBuffer =
                "=== WANKUL CRAZY ERROR REPORT ===\n" +
                $"Titre: {_lastTitle}\n" +
                $"Erreur: {_lastMessage}\n" +
                $"StackTrace:\n{_lastStack}\n" +
                "================================";

            // Retour visuel sans toucher aux donnees de l'erreur
            _titleText.text = _lastTitle + "  (copie dans le presse-papier)";
        }

        private void OnIgnoreClicked()
        {
            _ignoredHashes.Add(_currentHash);
            Hide();
        }

        private void OnIgnoreAllClicked()
        {
            _ignoreAll = true;
            Hide();
        }

        private void OnQuitClicked()
        {
            Application.Quit();
        }
    }
}
