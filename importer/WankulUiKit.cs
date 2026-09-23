using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WankulCrazyPlugin.importer
{
    /// <summary>Petits helpers UGUI partages par l'ecran de chargement et l'ecran d'erreur.</summary>
    internal static class WankulUiKit
    {
        private static Font _font;
        private static Sprite _whiteSprite;

        public static Font GetFont()
        {
            if (_font == null)
            {
                try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            }
            if (_font == null)
            {
                try { _font = Font.CreateDynamicFontFromOSFont("Arial", 16); } catch { }
            }
            return _font;
        }

        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    Texture2D t = Texture2D.whiteTexture;
                    _whiteSprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                }
                return _whiteSprite;
            }
        }

        public static Canvas SetupCanvas(GameObject go, int sortingOrder)
        {
            Canvas canvas = go.GetComponent<Canvas>();
            if (canvas == null) canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void PlaceCentered(RectTransform rt, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
        }

        public static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = WhiteSprite;
            img.color = color;
            return img;
        }

        public static Text CreateText(Transform parent, string name, string content, int size, FontStyle style, TextAnchor align, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            Font font = GetFont();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            t.text = content;
            return t;
        }

        public static Button CreateButton(Transform parent, string label, Color color, UnityAction onClick)
        {
            Image img = CreateImage(parent, "Btn_" + label, color);
            Button btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(Mathf.Min(color.r * 1.2f, 1f), Mathf.Min(color.g * 1.2f, 1f), Mathf.Min(color.b * 1.2f, 1f), 1f);
            colors.pressedColor = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            Text t = CreateText(img.transform, "Text", label, 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(t.rectTransform);
            return btn;
        }
    }
}
