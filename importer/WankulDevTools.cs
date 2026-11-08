using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace WankulCrazyPlugin.importer
{
    /// <summary>
    /// Outils de débogage et d'itération rapide en jeu :
    /// - F8 : Inspecteur 3D (Objets en main + Raycast intelligent au viseur)
    /// - F9 : Capture d'écran automatique enregistrée dans BepInEx/plugins/WankulCrazy/screenshots/
    /// - F10 : Rechargement à chaud (Hot-Reload) de toutes les textures Wankul
    /// </summary>
    public class WankulDevTools : MonoBehaviour
    {
        private static WankulDevTools s_Instance;

        // Cache statique des champs de InteractionPlayerController (conformité règle modding n°4)
        private static readonly FieldInfo s_HoldItemListField = typeof(InteractionPlayerController).GetField("m_HoldItemList", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo s_HoldSprayItemField = typeof(InteractionPlayerController).GetField("m_CurrentHoldSprayItem", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo s_HoldingBoxField = typeof(InteractionPlayerController).GetField("m_CurrentHoldingBox", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private bool m_InspectorEnabled = false;
        private string m_StatusMessage = "";
        private float m_StatusTimer = 0f;
        private Vector2 m_ScrollPos = Vector2.zero;

        private GUIStyle m_BoxStyle;
        private GUIStyle m_HeaderStyle;
        private GUIStyle m_LabelStyle;
        private GUIStyle m_CrosshairStyle;
        private GUIStyle m_NotificationStyle;

        public static void Initialize()
        {
            if (s_Instance == null)
            {
                GameObject go = new GameObject("WankulDevTools");
                UnityEngine.Object.DontDestroyOnLoad(go);
                s_Instance = go.AddComponent<WankulDevTools>();
                Plugin.Logger.LogInfo("[WankulDevTools] Initialisé avec succès (F8: Inspecteur, F9: Screenshot, F10: Hot-Reload) !");
            }
        }

        private void Update()
        {
            if (m_StatusTimer > 0f)
            {
                m_StatusTimer -= Time.unscaledDeltaTime;
            }

            // F8 : Bascule de l'inspecteur 3D
            if (Input.GetKeyDown(KeyCode.F8))
            {
                m_InspectorEnabled = !m_InspectorEnabled;
                ShowNotification(m_InspectorEnabled ? "🔍 Inspecteur 3D ACTIVÉ (F8)" : "🔍 Inspecteur 3D DÉSACTIVÉ (F8)");
            }

            // Déverrouillage de la souris avec Alt Gauche pour scroller dans l'inspecteur
            if (m_InspectorEnabled && Input.GetKey(KeyCode.LeftAlt))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // F9 : Capture d'écran automatique
            if (Input.GetKeyDown(KeyCode.F9))
            {
                CaptureScreenshot();
            }

            // F10 : Hot-Reload des textures
            if (Input.GetKeyDown(KeyCode.F10))
            {
                ReloadTextures();
            }
        }

        private void ShowNotification(string message, float duration = 3.5f)
        {
            m_StatusMessage = message;
            m_StatusTimer = duration;
        }

        private void CaptureScreenshot()
        {
            try
            {
                string dir = Path.Combine(Plugin.GetPluginPath(), "screenshots");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"screenshot_{timestamp}.png";
                string fullPath = Path.Combine(dir, filename);

                ScreenCapture.CaptureScreenshot(fullPath);
                string msg = $"📷 Screenshot sauvegardé : {filename}";
                ShowNotification(msg, 4f);
                Plugin.Logger.LogInfo($"[WankulDevTools] {msg} ({fullPath})");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[WankulDevTools] Échec de la capture d'écran: {ex.Message}");
                ShowNotification($"❌ Erreur Screenshot: {ex.Message}", 4f);
            }
        }

        private void ReloadTextures()
        {
            try
            {
                ShowNotification("⏳ Rechargement des textures...", 1f);
                int count = PatchTexturesImporter.ReloadAllTextures();
                ShowNotification($"⚡ Textures rechargées ! ({count} éléments actualisés)", 4f);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[WankulDevTools] Échec du rechargement des textures: {ex.Message}");
                ShowNotification($"❌ Erreur Hot-Reload: {ex.Message}", 4f);
            }
        }

        private void InitStyles()
        {
            if (m_BoxStyle != null) return;

            Texture2D blackTex = new Texture2D(1, 1);
            blackTex.SetPixel(0, 0, new Color(0.04f, 0.04f, 0.06f, 0.95f));
            blackTex.Apply();

            Texture2D notifTex = new Texture2D(1, 1);
            notifTex.SetPixel(0, 0, new Color(0.12f, 0.18f, 0.24f, 0.96f));
            notifTex.Apply();

            m_BoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = blackTex, textColor = Color.white },
                padding = new RectOffset(14, 14, 14, 14),
                alignment = TextAnchor.UpperLeft,
                fontSize = 13
            };

            m_HeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) }
            };

            m_LabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                richText = true,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.92f, 0.92f) }
            };

            m_CrosshairStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.2f, 1f, 0.4f, 0.9f) }
            };

            m_NotificationStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = notifTex, textColor = Color.white },
                padding = new RectOffset(16, 16, 8, 8),
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
        }

        private void OnGUI()
        {
            InitStyles();

            // 1. Bannière de notification temporaire
            if (m_StatusTimer > 0f && !string.IsNullOrEmpty(m_StatusMessage))
            {
                float notifWidth = 560f;
                float notifHeight = 42f;
                float notifX = (Screen.width - notifWidth) * 0.5f;
                GUI.Box(new Rect(notifX, 20f, notifWidth, notifHeight), m_StatusMessage, m_NotificationStyle);
            }

            // 2. Inspecteur 3D (F8)
            if (m_InspectorEnabled)
            {
                DrawInspector();
            }
        }

        private void DrawInspector()
        {
            // Viseur au centre de l'écran
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            GUI.Label(new Rect(cx - 15f, cy - 15f, 30f, 30f), "+", m_CrosshairStyle);

            StringBuilder sb = new StringBuilder();

            // A. Inspecter l'objet en main en priorité
            bool hasHeldObject = false;
            var controller = CSingleton<InteractionPlayerController>.Instance;
            if (controller != null)
            {
                try
                {
                    var holdList = s_HoldItemListField?.GetValue(controller) as List<Item>;
                    if (holdList != null && holdList.Count > 0 && holdList[0] != null)
                    {
                        hasHeldObject = true;
                        AppendObjectDetails(sb, holdList[0].gameObject, $"✋ OBJET EN MAIN ({holdList.Count} en pile)");
                        sb.AppendLine();
                    }

                    if (!hasHeldObject)
                    {
                        var sprayItem = s_HoldSprayItemField?.GetValue(controller) as Item;
                        if (sprayItem != null)
                        {
                            hasHeldObject = true;
                            AppendObjectDetails(sb, sprayItem.gameObject, "✋ DÉODORANT / SPRAY EN MAIN");
                            sb.AppendLine();
                        }
                    }

                    if (!hasHeldObject)
                    {
                        var holdingBox = s_HoldingBoxField?.GetValue(controller) as InteractablePackagingBox;
                        if (holdingBox != null)
                        {
                            hasHeldObject = true;
                            AppendObjectDetails(sb, holdingBox.gameObject, "✋ CARTON DE LIVRAISON EN MAIN");
                            sb.AppendLine();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"[WankulDevTools] Erreur inspection main: {ex.Message}");
                }
            }

            // B. Raycast intelligent au centre de l'écran (filtre les bloqueurs invisibles)
            Camera cam = Camera.main != null ? Camera.main : Camera.current;
            if (cam != null)
            {
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit? bestHit = GetBestRaycastHit(ray, 30f);

                if (bestHit.HasValue)
                {
                    RaycastHit hit = bestHit.Value;
                    AppendObjectDetails(sb, hit.collider.gameObject, $"🎯 OBJET AU VISEUR ({hit.distance:F2}m)");
                }
                else if (!hasHeldObject)
                {
                    sb.AppendLine("<i>Aucun objet en main et aucun objet 3D détecté au centre de l'écran.</i>");
                    sb.AppendLine("<i>(Prenez un objet en main ou visez directement un rayon/article).</i>");
                }
            }
            else if (!hasHeldObject)
            {
                sb.AppendLine("<i>Caméra principale introuvable.</i>");
            }

            sb.AppendLine();
            sb.AppendLine("<color=#85929e>Raccourcis : [Alt Gauche] Libérer souris | [F8] Masquer | [F9] Screenshot | [F10] Hot-Reload</color>");

            // Boîte d'affichage sur le côté gauche (placée sous l'heure et le jour natifs)
            float boxX = 15f;
            float boxY = 110f;
            float boxWidth = 550f;
            float boxHeight = 440f;

            GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "", m_BoxStyle);
            GUI.Label(new Rect(boxX + 14f, boxY + 8f, boxWidth - 28f, 26f), "🛠️ WANKUL DEVTOOLS — INSPECTEUR 3D", m_HeaderStyle);

            // Zone défilable pour afficher tous les détails sans coupure
            float viewWidth = boxWidth - 32f;
            float contentHeight = Mathf.Max(boxHeight - 50f, sb.Length * 0.7f + 60f);

            m_ScrollPos = GUI.BeginScrollView(
                new Rect(boxX + 14f, boxY + 36f, boxWidth - 28f, boxHeight - 44f),
                m_ScrollPos,
                new Rect(0, 0, viewWidth - 16f, contentHeight)
            );
            GUI.Label(new Rect(0, 0, viewWidth - 16f, contentHeight), sb.ToString(), m_LabelStyle);
            GUI.EndScrollView();
        }

        private RaycastHit? GetBestRaycastHit(Ray ray, float maxDistance)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
            if (hits == null || hits.Length == 0) return null;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            RaycastHit? candidateWithRenderer = null;

            foreach (var hit in hits)
            {
                var col = hit.collider;
                if (col == null || col.isTrigger) continue;

                string colName = col.gameObject.name;
                string layerName = LayerMask.LayerToName(col.gameObject.layer);

                // Ignorer les bloqueurs de navigation, obstacles invisibles, triggers et navmesh
                if (colName.IndexOf("NavBlocker", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    colName.IndexOf("NavMesh", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    colName.IndexOf("Pathfinding", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    colName.IndexOf("Obstacle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    colName.IndexOf("Door_Obstacle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    layerName.Equals("NavMeshBlocker", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Cible prioritaire absolue : objet qui contient un Item ou un UI_PriceTag
                if (col.GetComponentInParent<Item>() != null || col.GetComponentInParent<UI_PriceTag>() != null)
                {
                    return hit;
                }

                // Cible secondaire : objet avec un Renderer actif
                if (candidateWithRenderer == null)
                {
                    var rend = col.GetComponent<Renderer>() ?? col.GetComponentInParent<Renderer>();
                    if (rend != null && rend.enabled)
                    {
                        candidateWithRenderer = hit;
                    }
                }
            }

            return candidateWithRenderer;
        }

        private void AppendObjectDetails(StringBuilder sb, GameObject go, string header)
        {
            if (go == null) return;

            sb.AppendLine($"<b><color=#f4d03f>{header}</color></b> : <color=#5dade2>{go.name}</color>");

            // 1. Composant Item
            Item item = go.GetComponentInParent<Item>();
            if (item != null)
            {
                sb.AppendLine($"  <b>Type Item :</b> <color=#58d68d>{item.GetItemType()}</color>");
                if (item.m_MeshFilter != null && item.m_MeshFilter.sharedMesh != null)
                {
                    sb.AppendLine($"  <b>Mesh :</b> {item.m_MeshFilter.sharedMesh.name} ({item.m_MeshFilter.sharedMesh.vertexCount} sommets)");
                }
            }

            // 2. Composant UI_PriceTag
            UI_PriceTag priceTag = go.GetComponentInParent<UI_PriceTag>();
            if (priceTag != null)
            {
                sb.AppendLine($"  <b>PriceTag :</b> <color=#e67e22>{priceTag.GetItemType()}</color>");
                if (priceTag.m_InstancedRenderer != null && priceTag.m_InstancedRenderer.sharedMaterial != null)
                {
                    var mat = priceTag.m_InstancedRenderer.sharedMaterial;
                    Texture mainTex = mat.mainTexture;
                    string texInfo = mainTex != null ? $"{mainTex.name} ({mainTex.width}x{mainTex.height})" : "null";
                    sb.AppendLine($"  <b>Material :</b> {mat.name} | <b>Atlas :</b> {texInfo}");
                }
            }

            // 3. Renderer et Matériaux
            List<Renderer> renderers = new List<Renderer>();
            if (item != null)
            {
                if (item.m_Mesh != null) renderers.Add(item.m_Mesh);
                if (item.m_MeshSecondary != null) renderers.Add(item.m_MeshSecondary);
            }
            if (renderers.Count == 0)
            {
                var r = go.GetComponent<Renderer>() ?? go.GetComponentInChildren<Renderer>() ?? go.GetComponentInParent<Renderer>();
                if (r != null) renderers.Add(r);
            }

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                Material[] mats = renderer.materials;
                sb.AppendLine($"  <b>Renderer :</b> {renderer.name} ({renderer.GetType().Name})");
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    if (m == null) continue;

                    string shaderName = m.shader != null ? m.shader.name : "null";
                    Texture mainTex = null;
                    if (m.HasProperty("_MainTex")) mainTex = m.GetTexture("_MainTex");
                    else if (m.HasProperty("_BaseMap")) mainTex = m.GetTexture("_BaseMap");

                    string texDesc = mainTex != null ? $"{mainTex.name} ({mainTex.width}x{mainTex.height})" : "aucune";
                    sb.AppendLine($"    <b>Slot {i} :</b> {m.name}");
                    sb.AppendLine($"       Shader: <color=#85c1e9>{shaderName}</color> | Texture: <color=#f5b041>{texDesc}</color>");
                }
            }
        }
    }
}
