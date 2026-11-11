using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using WankulCrazyPlugin.utils;

namespace WankulCrazyPlugin.importer
{
    /// <summary>
    /// Pont HTTP local permettant à Antigravity d'interroger et de contrôler le jeu en temps réel :
    /// - GET  /status          : Vérifie que le jeu et le pont tournent
    /// - GET  /held_item       : Retourne les détails complets de l'objet en main (mesh, matériaux, UVs)
    /// - GET  /target          : Retourne l'objet visé au centre de l'écran (filtre intelligent)
    /// - POST /reload_textures : Force le rechargement à chaud de toutes les textures Wankul
    /// - POST /screenshot      : Déclenche une capture d'écran Unity et renvoie le chemin du fichier
    /// - POST /dump_mesh       : Exporte le mesh 3D et ses coordonnées UV exactes en fichier .obj (supporte isReadable == false)
    /// - POST /dump_texture    : Exporte n'importe quelle Texture en PNG via RenderTexture.Blit
    /// - POST /render_item     : Studio Photo 3D isolé 4 vues (Face, 3/4 Iso, Dessus, Dos) en grille 2x2
    /// - GET  /logs            : Retourne les 200 derniers logs Unity/BepInEx filtrables par level et keyword
    /// </summary>
    public class WankulGameBridge : MonoBehaviour
    {
        private const int Port = 8765;
        private const int MaxLogCapacity = 200;

        private static WankulGameBridge s_Instance;
        private static HttpListener s_Listener;
        private static Thread s_ListenerThread;
        private static bool s_IsRunning = false;

        // File d'actions synchronisées sur le thread principal d'Unity
        private static readonly ConcurrentQueue<Action> s_MainThreadQueue = new ConcurrentQueue<Action>();

        // Buffer circulaire thread-safe des 200 derniers logs
        private struct LogRecord
        {
            public string Timestamp;
            public string Level;
            public string Message;
            public string StackTrace;
        }

        private static readonly object s_LogLock = new object();
        private static readonly LogRecord[] s_LogRingBuffer = new LogRecord[MaxLogCapacity];
        private static int s_LogWriteIndex = 0;
        private static int s_LogCount = 0;
        private static bool s_LogSubscribed = false;

        // Cache des champs de InteractionPlayerController (conformité règle n°4)
        private static readonly FieldInfo s_HoldItemListField = typeof(InteractionPlayerController).GetField("m_HoldItemList", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo s_HoldSprayItemField = typeof(InteractionPlayerController).GetField("m_CurrentHoldSprayItem", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo s_HoldingBoxField = typeof(InteractionPlayerController).GetField("m_CurrentHoldingBox", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        public static void Initialize()
        {
            if (s_Instance != null) return;

            if (!s_LogSubscribed)
            {
                Application.logMessageReceivedThreaded += OnLogMessageReceivedThreaded;
                s_LogSubscribed = true;
            }

            GameObject go = new GameObject("WankulGameBridge");
            UnityEngine.Object.DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<WankulGameBridge>();

            StartServer();
        }

        private static void OnLogMessageReceivedThreaded(string condition, string stackTrace, LogType type)
        {
            string level;
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    level = "Error";
                    break;
                case LogType.Warning:
                    level = "Warning";
                    break;
                default:
                    level = "Info";
                    break;
            }

            lock (s_LogLock)
            {
                s_LogRingBuffer[s_LogWriteIndex] = new LogRecord
                {
                    Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                    Level = level,
                    Message = condition ?? string.Empty,
                    StackTrace = stackTrace ?? string.Empty
                };
                s_LogWriteIndex = (s_LogWriteIndex + 1) % MaxLogCapacity;
                if (s_LogCount < MaxLogCapacity)
                {
                    s_LogCount++;
                }
            }
        }

        private static void StartServer()
        {
            try
            {
                s_Listener = new HttpListener();
                s_Listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
                s_Listener.Start();
                s_IsRunning = true;

                s_ListenerThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "WankulGameBridge_HttpListener"
                };
                s_ListenerThread.Start();

                Plugin.Logger.LogInfo($"[WankulGameBridge] Serveur local démarré avec succès sur http://127.0.0.1:{Port}/ !");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[WankulGameBridge] Erreur au démarrage du serveur HTTP : {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            s_IsRunning = false;
            if (s_LogSubscribed)
            {
                Application.logMessageReceivedThreaded -= OnLogMessageReceivedThreaded;
                s_LogSubscribed = false;
            }
            try
            {
                s_Listener?.Stop();
                s_Listener?.Close();
            }
            catch { }
        }

        private void Update()
        {
            // Exécuter les requêtes en attente sur le thread principal d'Unity
            int maxPerFrame = 10;
            while (maxPerFrame-- > 0 && s_MainThreadQueue.TryDequeue(out Action action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"[WankulGameBridge] Exception lors de l'exécution sur le main thread : {ex.Message}");
                }
            }
        }

        private static Task<T> EnqueueMainThread<T>(Func<T> function)
        {
            var tcs = new TaskCompletionSource<T>();
            s_MainThreadQueue.Enqueue(() =>
            {
                try
                {
                    T result = function();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }

        private static void ListenLoop()
        {
            while (s_IsRunning && s_Listener != null && s_Listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = s_Listener.GetContext();
                    Task.Run(() => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (s_IsRunning)
                    {
                        Plugin.Logger.LogError($"[WankulGameBridge] Erreur dans ListenLoop : {ex.Message}");
                    }
                }
            }
        }

        private static JObject ReadJsonBody(HttpListenerRequest req)
        {
            if (req == null || !req.HasEntityBody || req.InputStream == null) return null;
            try
            {
                using (var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8))
                {
                    string body = reader.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        return JObject.Parse(body);
                    }
                }
            }
            catch { }
            return null;
        }

        private static string GetParam(HttpListenerRequest req, JObject body, params string[] keys)
        {
            foreach (string key in keys)
            {
                string qVal = req.QueryString[key];
                if (!string.IsNullOrEmpty(qVal)) return qVal;

                if (body != null && body.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken token))
                {
                    string bVal = token?.ToString();
                    if (!string.IsNullOrEmpty(bVal)) return bVal;
                }
            }
            return string.Empty;
        }

        private static async void HandleRequest(HttpListenerContext context)
        {
            HttpListenerRequest req = context.Request;
            HttpListenerResponse res = context.Response;

            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            res.ContentType = "application/json; charset=utf-8";

            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 200;
                res.Close();
                return;
            }

            string path = req.Url.AbsolutePath.ToLowerInvariant().TrimEnd('/');
            JObject bodyJson = ReadJsonBody(req);

            try
            {
                string jsonResponse = "{}";

                switch (path)
                {
                    case "/status":
                        jsonResponse = "{\"status\":\"ok\",\"game\":\"TCG Card Shop Simulator\",\"version\":\"1.07\",\"bridge_port\":" + Port + "}";
                        break;

                    case "/held_item":
                        jsonResponse = await EnqueueMainThread(GetHeldItemJson);
                        break;

                    case "/target":
                        jsonResponse = await EnqueueMainThread(GetTargetItemJson);
                        break;

                    case "/reload_textures":
                        jsonResponse = await EnqueueMainThread(() =>
                        {
                            int count = PatchTexturesImporter.ReloadAllTextures();
                            return $"{{\"success\":true,\"reloaded_items\":{count}}}";
                        });
                        break;

                    case "/screenshot":
                        jsonResponse = await EnqueueMainThread(() =>
                        {
                            string dir = Path.Combine(Plugin.GetPluginPath(), "screenshots");
                            Directory.CreateDirectory(dir);
                            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            string filename = $"screenshot_{timestamp}.png";
                            string fullPath = Path.Combine(dir, filename);
                            ScreenCapture.CaptureScreenshot(fullPath);
                            return $"{{\"success\":true,\"path\":\"{fullPath.Replace("\\", "/")}\",\"filename\":\"{filename}\"}}";
                        });
                        break;

                    case "/dump_mesh":
                        {
                            string meshName = GetParam(req, bodyJson, "name", "mesh_name");
                            jsonResponse = await EnqueueMainThread(() => DumpMeshOnMainThread(meshName));
                        }
                        break;

                    case "/dump_texture":
                        {
                            string texName = GetParam(req, bodyJson, "texture_name", "name");
                            jsonResponse = await EnqueueMainThread(() => DumpTextureOnMainThread(texName));
                        }
                        break;

                    case "/render_item":
                        {
                            string itemType = GetParam(req, bodyJson, "item_type", "item");
                            string meshName = GetParam(req, bodyJson, "mesh_name", "mesh");
                            string sizeStr = GetParam(req, bodyJson, "size");
                            int size = 512;
                            if (!string.IsNullOrEmpty(sizeStr) && int.TryParse(sizeStr, out int parsedSize))
                            {
                                size = Mathf.Clamp(parsedSize, 128, 2048);
                            }
                            jsonResponse = await EnqueueMainThread(() => RenderItemStudioOnMainThread(itemType, meshName, size));
                        }
                        break;

                    case "/logs":
                        {
                            string levelFilter = GetParam(req, bodyJson, "level");
                            string keywordFilter = GetParam(req, bodyJson, "keyword", "search", "q");
                            jsonResponse = GetLogsJson(levelFilter, keywordFilter);
                        }
                        break;

                    default:
                        res.StatusCode = 404;
                        jsonResponse = "{\"error\":\"Route introuvable: " + path + "\"}";
                        break;
                }

                byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
                res.ContentLength64 = buffer.Length;
                res.StatusCode = 200;
                await res.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                res.StatusCode = 500;
                string errJson = $"{{\"error\":\"{EscapeJson(ex.Message)}\"}}";
                byte[] buffer = Encoding.UTF8.GetBytes(errJson);
                res.ContentLength64 = buffer.Length;
                try { await res.OutputStream.WriteAsync(buffer, 0, buffer.Length); } catch { }
            }
            finally
            {
                try { res.Close(); } catch { }
            }
        }

        private static string GetLogsJson(string levelFilter, string keywordFilter)
        {
            List<LogRecord> snapshot = new List<LogRecord>(MaxLogCapacity);
            lock (s_LogLock)
            {
                int start = (s_LogWriteIndex - s_LogCount + MaxLogCapacity) % MaxLogCapacity;
                for (int i = 0; i < s_LogCount; i++)
                {
                    int idx = (start + i) % MaxLogCapacity;
                    snapshot.Add(s_LogRingBuffer[idx]);
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"success\":true,\"logs\":[");
            int matchedCount = 0;

            for (int i = 0; i < snapshot.Count; i++)
            {
                var entry = snapshot[i];
                if (!string.IsNullOrEmpty(levelFilter) &&
                    !entry.Level.Equals(levelFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(keywordFilter))
                {
                    bool inMsg = entry.Message != null && entry.Message.IndexOf(keywordFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                    bool inStack = entry.StackTrace != null && entry.StackTrace.IndexOf(keywordFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!inMsg && !inStack) continue;
                }

                if (matchedCount > 0) sb.Append(",");
                sb.Append("{");
                sb.Append($"\"timestamp\":\"{EscapeJson(entry.Timestamp)}\",");
                sb.Append($"\"level\":\"{EscapeJson(entry.Level)}\",");
                sb.Append($"\"message\":\"{EscapeJson(entry.Message)}\",");
                sb.Append($"\"stack_trace\":\"{EscapeJson(entry.StackTrace)}\"");
                sb.Append("}");
                matchedCount++;
            }

            sb.Append($"],\"count\":{matchedCount},\"buffer_size\":{snapshot.Count}}}");
            return sb.ToString();
        }

        private static string GetHeldItemJson()
        {
            var controller = CSingleton<InteractionPlayerController>.Instance;
            if (controller == null)
            {
                return "{\"found\":false,\"reason\":\"InteractionPlayerController introuvable\"}";
            }

            GameObject targetGo = null;
            Item itemComp = null;
            string holdSource = "none";

            var holdList = s_HoldItemListField?.GetValue(controller) as List<Item>;
            if (holdList != null && holdList.Count > 0 && holdList[0] != null)
            {
                targetGo = holdList[0].gameObject;
                itemComp = holdList[0];
                holdSource = $"m_HoldItemList ({holdList.Count} items)";
            }
            else
            {
                var sprayItem = s_HoldSprayItemField?.GetValue(controller) as Item;
                if (sprayItem != null)
                {
                    targetGo = sprayItem.gameObject;
                    itemComp = sprayItem;
                    holdSource = "m_CurrentHoldSprayItem";
                }
                else
                {
                    var holdingBox = s_HoldingBoxField?.GetValue(controller) as InteractablePackagingBox;
                    if (holdingBox != null)
                    {
                        targetGo = holdingBox.gameObject;
                        holdSource = "m_CurrentHoldingBox";
                    }
                }
            }

            if (targetGo == null)
            {
                return "{\"found\":false,\"reason\":\"Aucun objet en main actuellement\"}";
            }

            return SerializeGameObjectJson(targetGo, itemComp, holdSource);
        }

        private static string GetTargetItemJson()
        {
            Camera cam = Camera.main != null ? Camera.main : Camera.current;
            if (cam == null)
            {
                return "{\"found\":false,\"reason\":\"Camera principale introuvable\"}";
            }

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, 30f);
            if (hits == null || hits.Length == 0)
            {
                return "{\"found\":false,\"reason\":\"Rien à portée de vue (30m)\"}";
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            RaycastHit? bestHit = null;
            foreach (var hit in hits)
            {
                var col = hit.collider;
                if (col == null || col.isTrigger) continue;

                string colName = col.gameObject.name;
                string layerName = LayerMask.LayerToName(col.gameObject.layer);

                if (colName.IndexOf("NavBlocker", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    colName.IndexOf("NavMesh", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    colName.IndexOf("Pathfinding", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    colName.IndexOf("Obstacle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    layerName.Equals("NavMeshBlocker", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (col.GetComponentInParent<Item>() != null || col.GetComponentInParent<UI_PriceTag>() != null)
                {
                    bestHit = hit;
                    break;
                }

                if (bestHit == null)
                {
                    var rend = col.GetComponent<Renderer>() ?? col.GetComponentInParent<Renderer>();
                    if (rend != null && rend.enabled)
                    {
                        bestHit = hit;
                    }
                }
            }

            if (!bestHit.HasValue)
            {
                return "{\"found\":false,\"reason\":\"Aucun objet visible ou interactif dans le viseur\"}";
            }

            var actualHit = bestHit.Value;
            var go = actualHit.collider.gameObject;
            var itemComp = go.GetComponentInParent<Item>();
            return SerializeGameObjectJson(go, itemComp, $"Raycast hit ({actualHit.distance:F2}m)");
        }

        private static string SerializeGameObjectJson(GameObject go, Item item, string sourceDesc)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"found\":true,");
            sb.Append($"\"source\":\"{EscapeJson(sourceDesc)}\",");
            sb.Append($"\"name\":\"{EscapeJson(go.name)}\",");

            if (item != null)
            {
                sb.Append($"\"item_type\":\"{item.GetItemType()}\",");
                if (item.m_MeshFilter != null && item.m_MeshFilter.sharedMesh != null)
                {
                    Mesh m = item.m_MeshFilter.sharedMesh;
                    sb.Append($"\"mesh_name\":\"{EscapeJson(m.name)}\",");
                    sb.Append($"\"is_readable\":{(m.isReadable ? "true" : "false")},");
                    sb.Append($"\"vertex_count\":{m.vertexCount},");
                    sb.Append($"\"submesh_count\":{m.subMeshCount},");

                    if (TryExtractMeshData(m, out _, out Vector2[] uvs, out _, out _) && uvs != null && uvs.Length > 0)
                    {
                        float minU = float.MaxValue, maxU = float.MinValue;
                        float minV = float.MaxValue, maxV = float.MinValue;
                        for (int i = 0; i < uvs.Length; i++)
                        {
                            if (uvs[i].x < minU) minU = uvs[i].x;
                            if (uvs[i].x > maxU) maxU = uvs[i].x;
                            if (uvs[i].y < minV) minV = uvs[i].y;
                            if (uvs[i].y > maxV) maxV = uvs[i].y;
                        }
                        sb.Append(string.Format(CultureInfo.InvariantCulture,
                            "\"uv_bounds\":{{\"u_min\":{0:F4},\"u_max\":{1:F4},\"v_min\":{2:F4},\"v_max\":{3:F4}}},",
                            minU, maxU, minV, maxV));
                    }
                }
            }

            // Composant UI_PriceTag
            UI_PriceTag priceTag = go.GetComponentInParent<UI_PriceTag>();
            if (priceTag != null)
            {
                sb.Append($"\"price_tag_item_type\":\"{priceTag.GetItemType()}\",");
            }

            // Renderers & Matériaux
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

            sb.Append("\"renderers\":[");
            for (int rIdx = 0; rIdx < renderers.Count; rIdx++)
            {
                Renderer rend = renderers[rIdx];
                if (rend == null) continue;
                if (rIdx > 0) sb.Append(",");
                sb.Append("{");
                sb.Append($"\"name\":\"{EscapeJson(rend.name)}\",");
                sb.Append($"\"type\":\"{rend.GetType().Name}\",");
                sb.Append("\"materials\":[");

                Material[] mats = rend.materials;
                for (int mIdx = 0; mIdx < mats.Length; mIdx++)
                {
                    Material mat = mats[mIdx];
                    if (mat == null) continue;
                    if (mIdx > 0) sb.Append(",");
                    sb.Append("{");
                    sb.Append($"\"slot\":{mIdx},");
                    sb.Append($"\"name\":\"{EscapeJson(mat.name)}\",");
                    sb.Append($"\"shader\":\"{EscapeJson(mat.shader != null ? mat.shader.name : "null")}\",");

                    Texture mainTex = null;
                    string texProp = "none";
                    if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
                    {
                        mainTex = mat.GetTexture("_MainTex");
                        texProp = "_MainTex";
                    }
                    else if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null)
                    {
                        mainTex = mat.GetTexture("_BaseMap");
                        texProp = "_BaseMap";
                    }

                    if (mainTex != null)
                    {
                        sb.Append($"\"texture_property\":\"{texProp}\",");
                        sb.Append($"\"texture_name\":\"{EscapeJson(mainTex.name)}\",");
                        sb.Append($"\"texture_width\":{mainTex.width},");
                        sb.Append($"\"texture_height\":{mainTex.height}");
                    }
                    else
                    {
                        sb.Append("\"texture_name\":null");
                    }
                    sb.Append("}");
                }
                sb.Append("]}");
            }
            sb.Append("]}");

            return sb.ToString();
        }

        // =====================================================================
        // MISSION 1.1 : Export OBJ complet (avec fallback GPU isReadable==false)
        // =====================================================================

        private static string DumpMeshOnMainThread(string requestedName)
        {
            var controller = CSingleton<InteractionPlayerController>.Instance;
            Mesh mesh = null;

            if (!string.IsNullOrEmpty(requestedName))
            {
                var allMeshes = Resources.FindObjectsOfTypeAll<Mesh>();
                mesh = Array.Find(allMeshes, m => m != null && m.name.Equals(requestedName, StringComparison.OrdinalIgnoreCase))
                    ?? Array.Find(allMeshes, m => m != null && m.name.IndexOf(requestedName, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (mesh == null && controller != null)
            {
                var holdList = s_HoldItemListField?.GetValue(controller) as List<Item>;
                if (holdList != null && holdList.Count > 0 && holdList[0]?.m_MeshFilter != null)
                {
                    mesh = holdList[0].m_MeshFilter.sharedMesh;
                }
                else
                {
                    var spray = s_HoldSprayItemField?.GetValue(controller) as Item;
                    if (spray?.m_MeshFilter != null)
                    {
                        mesh = spray.m_MeshFilter.sharedMesh;
                    }
                }
            }

            if (mesh == null)
            {
                return "{\"success\":false,\"error\":\"Aucun mesh 3D trouvé pour l'export OBJ\"}";
            }

            if (!TryExtractMeshData(mesh, out Vector3[] vertices, out Vector2[] uvs, out Vector3[] normals, out List<int[]> subMeshes))
            {
                return $"{{\"success\":false,\"error\":\"Impossible de décoder les buffers GPU du mesh '{EscapeJson(mesh.name)}'.\"}}";
            }

            string outDir = Path.Combine(Plugin.GetPluginPath(), "mesh_dumps");
            Directory.CreateDirectory(outDir);
            string safeMeshName = SanitizeFileName(mesh.name);
            string outPath = Path.Combine(outDir, $"{safeMeshName}.obj");

            StringBuilder objSb = new StringBuilder(vertices.Length * 64);
            objSb.AppendLine("# Export OBJ Wankul Game Bridge");
            objSb.AppendLine($"# Mesh: {mesh.name} (isReadable={mesh.isReadable})");
            objSb.AppendLine($"# Vertices: {vertices.Length}");

            for (int i = 0; i < vertices.Length; i++)
            {
                objSb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "v {0:F6} {1:F6} {2:F6}", vertices[i].x, vertices[i].y, vertices[i].z));
            }

            bool hasUvs = uvs != null && uvs.Length == vertices.Length;
            if (hasUvs)
            {
                for (int i = 0; i < uvs.Length; i++)
                {
                    objSb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "vt {0:F6} {1:F6}", uvs[i].x, uvs[i].y));
                }
            }

            bool hasNormals = normals != null && normals.Length == vertices.Length;
            if (hasNormals)
            {
                for (int i = 0; i < normals.Length; i++)
                {
                    objSb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "vn {0:F6} {1:F6} {2:F6}", normals[i].x, normals[i].y, normals[i].z));
                }
            }

            for (int sub = 0; sub < subMeshes.Count; sub++)
            {
                objSb.AppendLine($"g submesh_{sub}");
                objSb.AppendLine($"usemtl Material_{sub}");
                int[] triangles = subMeshes[sub];
                if (triangles == null) continue;

                for (int i = 0; i + 2 < triangles.Length; i += 3)
                {
                    int i0 = triangles[i] + 1;
                    int i1 = triangles[i + 1] + 1;
                    int i2 = triangles[i + 2] + 1;
                    if (hasUvs && hasNormals)
                    {
                        objSb.AppendLine($"f {i0}/{i0}/{i0} {i1}/{i1}/{i1} {i2}/{i2}/{i2}");
                    }
                    else if (hasUvs)
                    {
                        objSb.AppendLine($"f {i0}/{i0} {i1}/{i1} {i2}/{i2}");
                    }
                    else
                    {
                        objSb.AppendLine($"f {i0} {i1} {i2}");
                    }
                }
            }

            File.WriteAllText(outPath, objSb.ToString(), Encoding.UTF8);
            Plugin.Logger.LogInfo($"[WankulGameBridge] Mesh '{mesh.name}' (isReadable={mesh.isReadable}) exporté vers {outPath} ({vertices.Length} sommets, {subMeshes.Count} submeshes) !");

            return $"{{\"success\":true,\"mesh_name\":\"{EscapeJson(mesh.name)}\",\"is_readable\":{(mesh.isReadable ? "true" : "false")},\"path\":\"{outPath.Replace("\\", "/")}\",\"vertex_count\":{vertices.Length},\"submesh_count\":{subMeshes.Count}}}";
        }

        private static bool TryExtractMeshData(
            Mesh mesh,
            out Vector3[] vertices,
            out Vector2[] uvs,
            out Vector3[] normals,
            out List<int[]> subMeshTriangles)
        {
            vertices = null;
            uvs = null;
            normals = null;
            subMeshTriangles = new List<int[]>();

            if (mesh == null) return false;

            // 1. Chemin standard rapide si le mesh est CPU-readable
            if (mesh.isReadable)
            {
                try
                {
                    vertices = mesh.vertices;
                    uvs = mesh.uv;
                    normals = mesh.normals;
                    for (int s = 0; s < mesh.subMeshCount; s++)
                    {
                        subMeshTriangles.Add(mesh.GetTriangles(s));
                    }
                    return vertices != null && vertices.Length > 0;
                }
                catch { }
            }

            // 2. Fallback bas niveau via GraphicsBuffer (GetVertexBuffer / GetIndexBuffer + VertexAttributeDescriptor)
            if (TryExtractFromGpuBuffers(mesh, out vertices, out uvs, out normals, out subMeshTriangles))
            {
                return true;
            }

            // 3. Deuxième fallback via SkinnedMeshRenderer.BakeMesh
            return TryExtractViaBakeMesh(mesh, out vertices, out uvs, out normals, out subMeshTriangles);
        }

        private static bool TryExtractFromGpuBuffers(
            Mesh mesh,
            out Vector3[] vertices,
            out Vector2[] uvs,
            out Vector3[] normals,
            out List<int[]> subMeshTriangles)
        {
            vertices = null;
            uvs = null;
            normals = null;
            subMeshTriangles = new List<int[]>();

            try
            {
                int vCount = mesh.vertexCount;
                if (vCount <= 0) return false;

                VertexAttributeDescriptor[] attrs = mesh.GetVertexAttributes();
                if (attrs == null || attrs.Length == 0) return false;

                // Calculer le stride et l'offset de chaque attribut par stream
                Dictionary<int, int> streamStrides = new Dictionary<int, int>();
                Dictionary<VertexAttribute, (int stream, int offset, VertexAttributeFormat format, int dimension)> attrLocations =
                    new Dictionary<VertexAttribute, (int, int, VertexAttributeFormat, int)>();

                foreach (var a in attrs)
                {
                    int currentOffset = streamStrides.TryGetValue(a.stream, out int s) ? s : 0;
                    int byteSize = GetAttributeFormatSize(a.format) * a.dimension;
                    attrLocations[a.attribute] = (a.stream, currentOffset, a.format, a.dimension);
                    streamStrides[a.stream] = currentOffset + byteSize;
                }

                // Lire tous les streams utilisés depuis le GPU
                Dictionary<int, byte[]> streamBytes = new Dictionary<int, byte[]>();
                foreach (int streamIndex in new List<int>(streamStrides.Keys))
                {
                    int expectedStride = mesh.GetVertexBufferStride(streamIndex);
                    if (expectedStride <= 0) expectedStride = streamStrides[streamIndex];
                    streamStrides[streamIndex] = expectedStride;

                    using (GraphicsBuffer vb = mesh.GetVertexBuffer(streamIndex))
                    {
                        if (vb == null) continue;
                        byte[] raw = new byte[vCount * expectedStride];
                        vb.GetData(raw);
                        streamBytes[streamIndex] = raw;
                    }
                }

                if (!attrLocations.TryGetValue(VertexAttribute.Position, out var posLoc) ||
                    !streamBytes.TryGetValue(posLoc.stream, out byte[] posStream))
                {
                    return false;
                }

                int posStride = streamStrides[posLoc.stream];
                vertices = new Vector3[vCount];
                for (int i = 0; i < vCount; i++)
                {
                    int baseIdx = i * posStride + posLoc.offset;
                    vertices[i] = ReadVector3(posStream, baseIdx, posLoc.format, posLoc.dimension);
                }

                if (attrLocations.TryGetValue(VertexAttribute.Normal, out var normLoc) &&
                    streamBytes.TryGetValue(normLoc.stream, out byte[] normStream))
                {
                    int normStride = streamStrides[normLoc.stream];
                    normals = new Vector3[vCount];
                    for (int i = 0; i < vCount; i++)
                    {
                        int baseIdx = i * normStride + normLoc.offset;
                        normals[i] = ReadVector3(normStream, baseIdx, normLoc.format, normLoc.dimension);
                    }
                }

                if (attrLocations.TryGetValue(VertexAttribute.TexCoord0, out var uvLoc) &&
                    streamBytes.TryGetValue(uvLoc.stream, out byte[] uvStream))
                {
                    int uvStride = streamStrides[uvLoc.stream];
                    uvs = new Vector2[vCount];
                    for (int i = 0; i < vCount; i++)
                    {
                        int baseIdx = i * uvStride + uvLoc.offset;
                        uvs[i] = ReadVector2(uvStream, baseIdx, uvLoc.format, uvLoc.dimension);
                    }
                }

                // Lecture de l'IndexBuffer GPU
                using (GraphicsBuffer ib = mesh.GetIndexBuffer())
                {
                    if (ib == null) return false;
                    bool is32Bit = mesh.indexFormat == IndexFormat.UInt32;
                    int indexStride = is32Bit ? 4 : 2;
                    int totalIndices = ib.count;
                    byte[] rawIndices = new byte[totalIndices * indexStride];
                    ib.GetData(rawIndices);

                    int subCount = Math.Max(1, mesh.subMeshCount);
                    for (int sub = 0; sub < subCount; sub++)
                    {
                        SubMeshDescriptor desc = mesh.GetSubMesh(sub);
                        int start = desc.indexStart;
                        int count = desc.indexCount;
                        int baseVertex = desc.baseVertex;

                        int[] tris = new int[count];
                        for (int k = 0; k < count; k++)
                        {
                            int byteOffset = (start + k) * indexStride;
                            if (byteOffset + indexStride > rawIndices.Length) break;
                            int idxVal = is32Bit
                                ? BitConverter.ToInt32(rawIndices, byteOffset)
                                : BitConverter.ToUInt16(rawIndices, byteOffset);
                            tris[k] = idxVal + baseVertex;
                        }
                        subMeshTriangles.Add(tris);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[WankulGameBridge] TryExtractFromGpuBuffers warning sur '{mesh.name}': {ex.Message}");
                return false;
            }
        }

        private static bool TryExtractViaBakeMesh(
            Mesh mesh,
            out Vector3[] vertices,
            out Vector2[] uvs,
            out Vector3[] normals,
            out List<int[]> subMeshTriangles)
        {
            vertices = null;
            uvs = null;
            normals = null;
            subMeshTriangles = new List<int[]>();

            GameObject tempGo = null;
            Mesh baked = null;
            try
            {
                tempGo = new GameObject("TempMeshBaker");
                var smr = tempGo.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = mesh;
                baked = new Mesh();
                smr.BakeMesh(baked);

                vertices = baked.vertices;
                uvs = baked.uv;
                normals = baked.normals;
                for (int s = 0; s < baked.subMeshCount; s++)
                {
                    subMeshTriangles.Add(baked.GetTriangles(s));
                }
                return vertices != null && vertices.Length > 0;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[WankulGameBridge] TryExtractViaBakeMesh a échoué sur '{mesh.name}': {ex.Message}");
                return false;
            }
            finally
            {
                if (baked != null) UnityEngine.Object.DestroyImmediate(baked);
                if (tempGo != null) UnityEngine.Object.DestroyImmediate(tempGo);
            }
        }

        private static int GetAttributeFormatSize(VertexAttributeFormat format)
        {
            switch (format)
            {
                case VertexAttributeFormat.Float32:
                case VertexAttributeFormat.UInt32:
                case VertexAttributeFormat.SInt32:
                    return 4;
                case VertexAttributeFormat.Float16:
                case VertexAttributeFormat.UNorm16:
                case VertexAttributeFormat.SNorm16:
                case VertexAttributeFormat.UInt16:
                case VertexAttributeFormat.SInt16:
                    return 2;
                case VertexAttributeFormat.UNorm8:
                case VertexAttributeFormat.SNorm8:
                case VertexAttributeFormat.UInt8:
                case VertexAttributeFormat.SInt8:
                    return 1;
                default:
                    return 4;
            }
        }

        private static float ReadComponent(byte[] buffer, int offset, VertexAttributeFormat format)
        {
            if (offset < 0 || offset >= buffer.Length) return 0f;
            switch (format)
            {
                case VertexAttributeFormat.Float32:
                    return offset + 4 <= buffer.Length ? BitConverter.ToSingle(buffer, offset) : 0f;
                case VertexAttributeFormat.Float16:
                    return offset + 2 <= buffer.Length ? Mathf.HalfToFloat(BitConverter.ToUInt16(buffer, offset)) : 0f;
                case VertexAttributeFormat.UNorm16:
                    return offset + 2 <= buffer.Length ? BitConverter.ToUInt16(buffer, offset) / 65535f : 0f;
                case VertexAttributeFormat.SNorm16:
                    return offset + 2 <= buffer.Length ? Mathf.Clamp(BitConverter.ToInt16(buffer, offset) / 32767f, -1f, 1f) : 0f;
                case VertexAttributeFormat.UNorm8:
                    return buffer[offset] / 255f;
                case VertexAttributeFormat.SNorm8:
                    return Mathf.Clamp(((sbyte)buffer[offset]) / 127f, -1f, 1f);
                default:
                    return 0f;
            }
        }

        private static Vector3 ReadVector3(byte[] buffer, int baseOffset, VertexAttributeFormat format, int dimension)
        {
            int elemSize = GetAttributeFormatSize(format);
            float x = dimension >= 1 ? ReadComponent(buffer, baseOffset, format) : 0f;
            float y = dimension >= 2 ? ReadComponent(buffer, baseOffset + elemSize, format) : 0f;
            float z = dimension >= 3 ? ReadComponent(buffer, baseOffset + elemSize * 2, format) : 0f;
            return new Vector3(x, y, z);
        }

        private static Vector2 ReadVector2(byte[] buffer, int baseOffset, VertexAttributeFormat format, int dimension)
        {
            int elemSize = GetAttributeFormatSize(format);
            float u = dimension >= 1 ? ReadComponent(buffer, baseOffset, format) : 0f;
            float v = dimension >= 2 ? ReadComponent(buffer, baseOffset + elemSize, format) : 0f;
            return new Vector2(u, v);
        }

        // =====================================================================
        // MISSION 1.2 : Endpoint POST /dump_texture
        // =====================================================================

        private static string DumpTextureOnMainThread(string textureName)
        {
            if (string.IsNullOrWhiteSpace(textureName))
            {
                return "{\"success\":false,\"error\":\"Le paramètre 'texture_name' est requis.\"}";
            }

            var allTextures = Resources.FindObjectsOfTypeAll<Texture>();
            Texture target = Array.Find(allTextures, t => t != null && t.name.Equals(textureName, StringComparison.Ordinal))
                          ?? Array.Find(allTextures, t => t != null && t.name.Equals(textureName, StringComparison.OrdinalIgnoreCase))
                          ?? Array.Find(allTextures, t => t != null && t.name.IndexOf(textureName, StringComparison.OrdinalIgnoreCase) >= 0);

            if (target == null)
            {
                return $"{{\"success\":false,\"error\":\"Texture '{EscapeJson(textureName)}' introuvable dans Resources.\"}}";
            }

            int width = Math.Max(1, target.width);
            int height = Math.Max(1, target.height);

            string outDir = Path.Combine(Plugin.GetPluginPath(), "texture_dumps");
            Directory.CreateDirectory(outDir);
            string safeName = SanitizeFileName(target.name);
            string outPath = Path.Combine(outDir, $"{safeName}.png");

            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            RenderTexture prevActive = RenderTexture.active;
            Texture2D readable = new Texture2D(width, height, TextureFormat.RGBA32, false);

            try
            {
                Graphics.Blit(target, rt);
                RenderTexture.active = rt;
                readable.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readable.Apply();

                byte[] pngBytes = readable.EncodeToPNG();
                File.WriteAllBytes(outPath, pngBytes);
            }
            finally
            {
                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(rt);
                if (readable != null)
                {
                    UnityEngine.Object.DestroyImmediate(readable);
                }
            }

            Plugin.Logger.LogInfo($"[WankulGameBridge] Texture '{target.name}' ({width}x{height}) exportée vers {outPath} !");
            return $"{{\"success\":true,\"texture_name\":\"{EscapeJson(target.name)}\",\"path\":\"{outPath.Replace("\\", "/")}\",\"width\":{width},\"height\":{height}}}";
        }

        // =====================================================================
        // MISSION 1.3 : Studio Photo 3D isolé POST /render_item (Grille 2x2)
        // =====================================================================

        private static string RenderItemStudioOnMainThread(string itemTypeStr, string meshName, int viewSize)
        {
            Vector3 studioOrigin = new Vector3(0f, -500f, 0f);
            GameObject subjectRoot = null;
            GameObject camGo = null;
            GameObject lightGo = null;
            RenderTexture rt = null;
            Texture2D viewTex = null;
            Texture2D gridTex = null;

            try
            {
                string previewLabel = !string.IsNullOrEmpty(itemTypeStr) ? itemTypeStr : (!string.IsNullOrEmpty(meshName) ? meshName : "HeldItem");
                subjectRoot = CreateStudioSubject(itemTypeStr, meshName, studioOrigin, ref previewLabel);

                if (subjectRoot == null)
                {
                    return $"{{\"success\":false,\"error\":\"Impossible d'instancier le sujet 3D pour '{EscapeJson(previewLabel)}'.\"}}";
                }

                // Calcul des Bounds globaux du sujet
                Renderer[] renderers = subjectRoot.GetComponentsInChildren<Renderer>(true);
                if (renderers == null || renderers.Length == 0)
                {
                    return $"{{\"success\":false,\"error\":\"Aucun Renderer sur le sujet '{EscapeJson(previewLabel)}'.\"}}";
                }

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                Vector3 center = bounds.center;
                float radius = Mathf.Max(0.05f, bounds.extents.magnitude);

                // Caméra et lumière directionnelle temporaires
                camGo = new GameObject("WankulStudioCamera");
                Camera cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.22f, 0.22f, 0.24f, 1f);
                cam.fieldOfView = 35f;
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = Mathf.Max(50f, radius * 20f);
                cam.enabled = false;

                lightGo = new GameObject("WankulStudioLight");
                Light dirLight = lightGo.AddComponent<Light>();
                dirLight.type = LightType.Directional;
                dirLight.color = Color.white;
                dirLight.intensity = 1.25f;
                dirLight.shadows = LightShadows.None;

                rt = RenderTexture.GetTemporary(viewSize, viewSize, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                cam.targetTexture = rt;

                viewTex = new Texture2D(viewSize, viewSize, TextureFormat.RGBA32, false);
                gridTex = new Texture2D(viewSize * 2, viewSize * 2, TextureFormat.RGBA32, false);

                float distance = (radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)) * 1.18f;

                // Les 4 angles demandés :
                // 0: Face (yaw 0°, pitch 0°)          -> Haut-Gauche (x=0, y=viewSize)
                // 1: 3/4 Isométrique (yaw 35°, 25°)   -> Haut-Droite (x=viewSize, y=viewSize)
                // 2: Dessus (yaw 0°, pitch 90°)       -> Bas-Gauche  (x=0, y=0)
                // 3: Dos (yaw 180°, pitch 0°)         -> Bas-Droite  (x=viewSize, y=0)
                (float yaw, float pitch, int destX, int destY)[] views = new[]
                {
                    (0f, 0f, 0, viewSize),
                    (35f, 25f, viewSize, viewSize),
                    (0f, 90f, 0, 0),
                    (180f, 0f, viewSize, 0)
                };

                RenderTexture prevActive = RenderTexture.active;
                try
                {
                    for (int v = 0; v < views.Length; v++)
                    {
                        var view = views[v];
                        Quaternion rot = Quaternion.Euler(view.pitch, view.yaw, 0f);
                        Vector3 offset = rot * (Vector3.back * distance);
                        camGo.transform.position = center + offset;
                        camGo.transform.LookAt(center, Math.Abs(view.pitch) > 85f ? Vector3.forward : Vector3.up);

                        // Aligner la lumière légèrement décalée de la caméra pour donner du relief
                        lightGo.transform.rotation = camGo.transform.rotation * Quaternion.Euler(15f, -20f, 0f);

                        cam.Render();

                        RenderTexture.active = rt;
                        viewTex.ReadPixels(new Rect(0, 0, viewSize, viewSize), 0, 0);
                        viewTex.Apply();

                        Color[] pixels = viewTex.GetPixels();
                        gridTex.SetPixels(view.destX, view.destY, viewSize, viewSize, pixels);
                    }
                }
                finally
                {
                    RenderTexture.active = prevActive;
                }

                gridTex.Apply();

                string outDir = Path.Combine(Plugin.GetPluginPath(), "screenshots");
                Directory.CreateDirectory(outDir);
                string safeName = SanitizeFileName(previewLabel);
                string outPath = Path.Combine(outDir, $"preview_{safeName}.png");

                File.WriteAllBytes(outPath, gridTex.EncodeToPNG());
                Plugin.Logger.LogInfo($"[WankulGameBridge] Studio Photo 3D 2x2 généré : {outPath} ({viewSize * 2}x{viewSize * 2})");

                return $"{{\"success\":true,\"name\":\"{EscapeJson(previewLabel)}\",\"path\":\"{outPath.Replace("\\", "/")}\",\"grid_width\":{viewSize * 2},\"grid_height\":{viewSize * 2}}}";
            }
            finally
            {
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (viewTex != null) UnityEngine.Object.DestroyImmediate(viewTex);
                if (gridTex != null) UnityEngine.Object.DestroyImmediate(gridTex);
                if (camGo != null) UnityEngine.Object.DestroyImmediate(camGo);
                if (lightGo != null) UnityEngine.Object.DestroyImmediate(lightGo);
                if (subjectRoot != null) UnityEngine.Object.DestroyImmediate(subjectRoot);
            }
        }

        private static GameObject CreateStudioSubject(string itemTypeStr, string meshName, Vector3 position, ref string previewLabel)
        {
            // 1. Si item_type est fourni, chercher dans m_ItemMeshDataList / CustomItemsImporter
            if (!string.IsNullOrEmpty(itemTypeStr))
            {
                EItemType itemType = EnumExtensions.SafeParseEItemType(itemTypeStr);
                if (itemType != EItemType.None)
                {
                    previewLabel = itemType.ToString();
                    ItemMeshData meshData = null;
                    var stockList = CSingleton<InventoryBase>.Instance?.m_StockItemData_SO?.m_ItemMeshDataList;
                    int idx = (int)itemType;
                    if (stockList != null && idx >= 0 && idx < stockList.Count)
                    {
                        meshData = stockList[idx];
                    }

                    if (meshData == null && CustomItemsImporter.ItemMeshDataList != null)
                    {
                        meshData = CustomItemsImporter.ItemMeshDataList.FirstOrDefault(
                            m => m != null && EnumExtensions.SafeParseEItemType(m.name) == itemType);
                    }

                    if (meshData != null && meshData.mesh != null)
                    {
                        GameObject root = new GameObject($"StudioSubject_{previewLabel}");
                        root.transform.position = position;

                        var mf = root.AddComponent<MeshFilter>();
                        var mr = root.AddComponent<MeshRenderer>();
                        mf.sharedMesh = meshData.mesh;

                        Material primaryMat = meshData.material;
                        var boxPaths = PatchTexturesImporter.GetBoxTexturePaths();
                        if (boxPaths.TryGetValue(itemType, out string texPath))
                        {
                            Material customMat = PatchTexturesImporter.GetOrCreateBoxMaterial(itemType, texPath, mr);
                            if (customMat != null) primaryMat = customMat;
                        }
                        if (primaryMat == null)
                        {
                            primaryMat = ShaderUtils.CreateSafeMaterial(PatchTexturesImporter.GetSafe2DBaseMaterial());
                        }
                        mr.sharedMaterial = primaryMat;

                        if (meshData.meshSecondary != null)
                        {
                            GameObject secGo = new GameObject("SecondaryMesh");
                            secGo.transform.SetParent(root.transform, false);
                            var secMf = secGo.AddComponent<MeshFilter>();
                            var secMr = secGo.AddComponent<MeshRenderer>();
                            secMf.sharedMesh = meshData.meshSecondary;

                            Material secMat = meshData.materialSecondary ?? primaryMat;
                            EItemType boosterType = PatchTexturesImporter.GetBoosterTypeForDisplay(itemType);
                            if (boosterType != EItemType.None && boxPaths.TryGetValue(boosterType, out string bPath))
                            {
                                Material bMat = PatchTexturesImporter.GetOrCreateBoxMaterial(boosterType, bPath, secMr);
                                if (bMat != null) secMat = bMat;
                            }
                            secMr.sharedMaterial = secMat;
                        }

                        return root;
                    }
                }
            }

            // 2. Si mesh_name est fourni (ou si item_type correspondait à un nom de mesh)
            string searchMesh = !string.IsNullOrEmpty(meshName) ? meshName : itemTypeStr;
            if (!string.IsNullOrEmpty(searchMesh))
            {
                var allMeshes = Resources.FindObjectsOfTypeAll<Mesh>();
                Mesh foundMesh = Array.Find(allMeshes, m => m != null && m.name.Equals(searchMesh, StringComparison.OrdinalIgnoreCase))
                              ?? Array.Find(allMeshes, m => m != null && m.name.IndexOf(searchMesh, StringComparison.OrdinalIgnoreCase) >= 0);

                if (foundMesh != null)
                {
                    previewLabel = foundMesh.name;
                    GameObject root = new GameObject($"StudioSubject_{foundMesh.name}");
                    root.transform.position = position;

                    var mf = root.AddComponent<MeshFilter>();
                    var mr = root.AddComponent<MeshRenderer>();
                    mf.sharedMesh = foundMesh;

                    // Chercher un MeshFilter existant qui utilise ce mesh pour récupérer ses matériaux
                    Material[] existingMats = null;
                    var allFilters = Resources.FindObjectsOfTypeAll<MeshFilter>();
                    foreach (var filter in allFilters)
                    {
                        if (filter != null && filter.sharedMesh == foundMesh)
                        {
                            var r = filter.GetComponent<Renderer>();
                            if (r != null && r.sharedMaterials != null && r.sharedMaterials.Length > 0)
                            {
                                existingMats = r.sharedMaterials;
                                break;
                            }
                        }
                    }

                    if (existingMats != null && existingMats.Length > 0)
                    {
                        mr.sharedMaterials = existingMats;
                    }
                    else
                    {
                        mr.sharedMaterial = ShaderUtils.CreateSafeMaterial(PatchTexturesImporter.GetSafe2DBaseMaterial());
                    }

                    return root;
                }
            }

            // 3. Fallback sur l'objet actuellement tenu en main
            var controller = CSingleton<InteractionPlayerController>.Instance;
            if (controller != null)
            {
                var holdList = s_HoldItemListField?.GetValue(controller) as List<Item>;
                Item held = (holdList != null && holdList.Count > 0) ? holdList[0] : (s_HoldSprayItemField?.GetValue(controller) as Item);
                if (held != null)
                {
                    previewLabel = held.GetItemType().ToString();
                    GameObject clone = UnityEngine.Object.Instantiate(held.gameObject, position, Quaternion.identity);
                    return clone;
                }
            }

            return null;
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unnamed";
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }
}
