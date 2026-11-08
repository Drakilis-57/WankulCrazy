using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace WankulCrazyPlugin.importer
{
    /// <summary>
    /// Pont HTTP local permettant à Antigravity d'interroger et de contrôler le jeu en temps réel :
    /// - GET  /status          : Vérifie que le jeu et le pont tournent
    /// - GET  /held_item       : Retourne les détails complets de l'objet en main (mesh, matériaux, UVs)
    /// - GET  /target          : Retourne l'objet visé au centre de l'écran (filtre intelligent)
    /// - POST /reload_textures : Force le rechargement à chaud de toutes les textures Wankul
    /// - POST /screenshot      : Déclenche une capture d'écran Unity et renvoie le chemin du fichier
    /// - POST /dump_mesh       : Exporte le mesh 3D et ses coordonnées UV exactes en fichier .obj
    /// </summary>
    public class WankulGameBridge : MonoBehaviour
    {
        private const int Port = 8765;
        private static WankulGameBridge s_Instance;
        private static HttpListener s_Listener;
        private static Thread s_ListenerThread;
        private static bool s_IsRunning = false;

        // File d'actions synchronisées sur le thread principal d'Unity
        private static readonly ConcurrentQueue<Action> s_MainThreadQueue = new ConcurrentQueue<Action>();

        // Cache des champs de InteractionPlayerController
        private static readonly FieldInfo s_HoldItemListField = typeof(InteractionPlayerController).GetField("m_HoldItemList", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo s_HoldSprayItemField = typeof(InteractionPlayerController).GetField("m_CurrentHoldSprayItem", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo s_HoldingBoxField = typeof(InteractionPlayerController).GetField("m_CurrentHoldingBox", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        public static void Initialize()
        {
            if (s_Instance != null) return;

            GameObject go = new GameObject("WankulGameBridge");
            UnityEngine.Object.DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<WankulGameBridge>();

            StartServer();
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
                        jsonResponse = await EnqueueMainThread(() => DumpHeldMeshObj(req.QueryString["name"]));
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
                    sb.Append($"\"vertex_count\":{m.vertexCount},");
                    sb.Append($"\"submesh_count\":{m.subMeshCount},");

                    Vector2[] uvs = m.uv;
                    if (uvs != null && uvs.Length > 0)
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
                        sb.Append($"\"uv_bounds\":{{\"u_min\":{minU:F4},\"u_max\":{maxU:F4},\"v_min\":{minV:F4},\"v_max\":{maxV:F4}}},");
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

        private static string DumpHeldMeshObj(string requestedName)
        {
            var controller = CSingleton<InteractionPlayerController>.Instance;
            Mesh mesh = null;

            if (controller != null)
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

            if (mesh == null && !string.IsNullOrEmpty(requestedName))
            {
                var allMeshes = Resources.FindObjectsOfTypeAll<Mesh>();
                mesh = Array.Find(allMeshes, m => m != null && m.name.Equals(requestedName, StringComparison.OrdinalIgnoreCase));
            }

            if (mesh == null)
            {
                return "{\"success\":false,\"error\":\"Aucun mesh 3D trouvé pour l'export OBJ\"}";
            }

            string outDir = Path.Combine(Plugin.GetPluginPath(), "mesh_dumps");
            Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, $"{mesh.name}.obj");

            StringBuilder objSb = new StringBuilder();
            objSb.AppendLine($"# Export OBJ Wankul Game Bridge");
            objSb.AppendLine($"# Mesh: {mesh.name}");
            objSb.AppendLine($"# Vertices: {mesh.vertexCount}");

            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                objSb.AppendLine($"v {vertices[i].x:F6} {vertices[i].y:F6} {vertices[i].z:F6}");
            }

            Vector2[] uvs = mesh.uv;
            if (uvs != null)
            {
                for (int i = 0; i < uvs.Length; i++)
                {
                    objSb.AppendLine($"vt {uvs[i].x:F6} {uvs[i].y:F6}");
                }
            }

            Vector3[] normals = mesh.normals;
            if (normals != null)
            {
                for (int i = 0; i < normals.Length; i++)
                {
                    objSb.AppendLine($"vn {normals[i].x:F6} {normals[i].y:F6} {normals[i].z:F6}");
                }
            }

            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                objSb.AppendLine($"g submesh_{sub}");
                objSb.AppendLine($"usemtl Material_{sub}");
                int[] triangles = mesh.GetTriangles(sub);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int i0 = triangles[i] + 1;
                    int i1 = triangles[i + 1] + 1;
                    int i2 = triangles[i + 2] + 1;
                    objSb.AppendLine($"f {i0}/{i0}/{i0} {i1}/{i1}/{i1} {i2}/{i2}/{i2}");
                }
            }

            File.WriteAllText(outPath, objSb.ToString(), Encoding.UTF8);
            Plugin.Logger.LogInfo($"[WankulGameBridge] Mesh '{mesh.name}' exporté avec succès vers {outPath} ({mesh.vertexCount} sommets) !");

            return $"{{\"success\":true,\"mesh_name\":\"{EscapeJson(mesh.name)}\",\"path\":\"{outPath.Replace("\\", "/")}\",\"vertex_count\":{mesh.vertexCount},\"submesh_count\":{mesh.subMeshCount}}}";
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
