using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.importer
{
    /// <summary>
    /// Payload transféré du producteur I/O d'arrière-plan vers le consommateur (main thread Unity).
    /// Regroupe de manière atomique la carte, sa texture principale et son masque foil éventuel.
    /// </summary>
    internal sealed class CardTexturePayload
    {
        public WankulCardData Card;
        public string TexturePath;
        public byte[] TextureBytes;
        public string MaskPath;
        public byte[] MaskBytes;
        public bool HasTextureError;
        public string ErrorMessage;

        public void Reset()
        {
            Card = null;
            TexturePath = null;
            TextureBytes = null;
            MaskPath = null;
            MaskBytes = null;
            HasTextureError = false;
            ErrorMessage = null;
        }
    }

    public class WankulLoadingScreen : MonoBehaviour
    {
        private static WankulLoadingScreen _instance;
        private Canvas _canvas;
        private Text _statusText;
        private Image _progressBarFill;

        /// <summary>True tant que les textures des cartes sont en cours de chargement.
        /// A utiliser pour bloquer l'ouverture de boosters / de l'album.</summary>
        public static bool IsLoading { get; private set; }

        // Budget de temps par frame (ms) pour garantir la fluidité UGUI du main thread Unity.
        private const float FrameBudgetMs = 12f;
        // Une carte dont le traitement dépasse ce seuil est loggée en avertissement.
        private const long SlowCardMs = 250;
        // Capacité bornée du buffer producteur/consommateur (limite l'empreinte RAM à ~3-5 Mo en moyenne tout en évitant la famine).
        private const int BoundedQueueCapacity = 32;
        // Nombre de threads I/O parallèles pour saturer le débit disque sans bloquer le thread principal.
        private const int MaxIoWorkers = 3;

        // Gestion de l'annulation (changement de scène, destruction du composant, fermeture)
        private CancellationTokenSource _cts;
        private BlockingCollection<CardTexturePayload> _queue;

        #region Pool de Payloads (Réduction GC)

        private static readonly ConcurrentQueue<CardTexturePayload> _payloadPool = new ConcurrentQueue<CardTexturePayload>();

        private static CardTexturePayload RentPayload()
        {
            if (_payloadPool.TryDequeue(out var payload))
            {
                return payload;
            }
            return new CardTexturePayload();
        }

        private static void ReturnPayload(CardTexturePayload payload)
        {
            if (payload == null) return;
            payload.Reset();
            if (_payloadPool.Count < 64)
            {
                _payloadPool.Enqueue(payload);
            }
        }

        #endregion

        #region Cache d'Index de Fichiers (Élimination des 10 000 syscalls File.Exists)

        /// <summary>
        /// Initialise l'index des fichiers de data/ en mémoire une seule fois via WankulFileIndex.
        /// Insensible à la casse et aux séparateurs '/' ou '\'.
        /// </summary>
        public static void EnsureFileIndexInitialized(string pluginPath) => WankulFileIndex.EnsureInitialized(pluginPath);

        /// <summary>
        /// Invalide le cache d'index de fichiers (utile lors d'ajouts dynamiques de fichiers).
        /// </summary>
        public static void InvalidateFileIndex() => WankulFileIndex.Invalidate();

        public static string ResolveExistingPath(string path) => WankulFileIndex.ResolveExistingPath(path);

        public static string ResolveMaskPath(WankulCardData card, string pluginPath) => WankulFileIndex.ResolveMaskPath(card, pluginPath);

        public static bool IsCardFoil(WankulCardData card) => WankulFileIndex.IsCardFoil(card);

        #endregion

        #region Point d'Entrée & Cycle de Vie MonoBehaviour

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
            CleanupResources();
            IsLoading = false;
            if (_instance == this) _instance = null;
        }

        private void CleanupResources()
        {
            try
            {
                if (_cts != null && !_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                }
            }
            catch { }

            if (_queue != null)
            {
                try
                {
                    while (_queue.TryTake(out var p))
                    {
                        ReturnPayload(p);
                    }
                    _queue.Dispose();
                }
                catch { }
                _queue = null;
            }

            if (_cts != null)
            {
                try
                {
                    _cts.Dispose();
                }
                catch { }
                _cts = null;
            }
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

        #endregion

        #region Pipeline Asynchrone Producer / Consumer

        /// <summary>
        /// Producteur multi-thread exécuté en arrière-plan sur le ThreadPool (Task.Run).
        /// Indexe data/ puis lit en parallèle les octets bruts PNG/JPG sans bloquer le main thread Unity.
        /// BoundedQueueCapacity borne la mémoire vive occupée par les tampons en attente.
        /// </summary>
        private static void ProduceCardPayloads(
            List<WankulCardData> cards,
            string pluginPath,
            BlockingCollection<CardTexturePayload> queue,
            CancellationToken ct)
        {
            try
            {
                EnsureFileIndexInitialized(pluginPath);

                int total = cards.Count;
                if (total == 0) return;

                int sharedCursor = -1;
                int workerCount = Math.Min(MaxIoWorkers, Math.Max(1, Math.Min(total, Environment.ProcessorCount / 2)));

                if (workerCount <= 1)
                {
                    ProducerWorkerLoop(cards, pluginPath, queue, ref sharedCursor, ct);
                }
                else
                {
                    var workers = new Task[workerCount];
                    for (int w = 0; w < workerCount; w++)
                    {
                        workers[w] = Task.Run(() => ProducerWorkerLoop(cards, pluginPath, queue, ref sharedCursor, ct), CancellationToken.None);
                    }
                    Task.WaitAll(workers);
                }
            }
            catch (OperationCanceledException)
            {
                // Arrêt propre en cas de cancellation (changement de scène, destruction)
            }
            catch (AggregateException aex)
            {
                bool allCanceled = true;
                foreach (var inner in aex.Flatten().InnerExceptions)
                {
                    if (!(inner is OperationCanceledException))
                    {
                        allCanceled = false;
                        break;
                    }
                }
                if (!allCanceled)
                {
                    Plugin.Logger?.LogError($"[WankulLoadingScreen] Producer task error: {aex.InnerException?.Message ?? aex.Message}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[WankulLoadingScreen] Producer task fatal error: {ex.Message}");
            }
            finally
            {
                try
                {
                    queue.CompleteAdding();
                }
                catch { }
            }
        }

        private static void ProducerWorkerLoop(
            List<WankulCardData> cards,
            string pluginPath,
            BlockingCollection<CardTexturePayload> queue,
            ref int sharedCursor,
            CancellationToken ct)
        {
            int count = cards.Count;
            while (!ct.IsCancellationRequested)
            {
                int i = Interlocked.Increment(ref sharedCursor);
                if (i >= count) break;

                var card = cards[i];
                var payload = RentPayload();
                bool enqueued = false;

                try
                {
                    payload.Card = card;

                    // Si la carte a déjà sa texture et son sprite prêts (ex: second passage), passer rapidement
                    if (card.Texture != null && card.Sprite != null && (!IsCardFoil(card) || card.TextureMask != null))
                    {
                        payload.TexturePath = card.TexturePath;
                        queue.Add(payload, ct);
                        enqueued = true;
                        continue;
                    }

                    if (string.IsNullOrEmpty(card.TexturePath))
                    {
                        queue.Add(payload, ct);
                        enqueued = true;
                        continue;
                    }

                    // Résolution O(1) via l'index en mémoire (0 syscall disque)
                    string texturePath = ResolveExistingPath(Path.Combine(pluginPath, "data", card.TexturePath));
                    payload.TexturePath = texturePath;

                    string maskPath = ResolveMaskPath(card, pluginPath);
                    payload.MaskPath = maskPath;

                    // Lecture disque I/O en tâche d'arrière-plan
                    if (texturePath != null)
                    {
                        try
                        {
                            payload.TextureBytes = File.ReadAllBytes(texturePath);
                        }
                        catch (Exception ex)
                        {
                            payload.HasTextureError = true;
                            payload.ErrorMessage = $"Error reading texture file {texturePath}: {ex.Message}";
                        }
                    }

                    // Lecture du masque foil en tâche d'arrière-plan
                    if (maskPath != null && !ct.IsCancellationRequested)
                    {
                        try
                        {
                            payload.MaskBytes = File.ReadAllBytes(maskPath);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Logger?.LogWarning($"[WankulLoadingScreen] Error reading mask {maskPath}: {ex.Message}");
                        }
                    }

                    // Ajout bloquant dans la collection bornée (backpressure automatique si le buffer est plein)
                    queue.Add(payload, ct);
                    enqueued = true;
                }
                finally
                {
                    if (!enqueued)
                    {
                        ReturnPayload(payload);
                    }
                }
            }
        }

        /// <summary>
        /// Coroutine consommatrice sur le thread principal d'Unity.
        /// Dépile les octets déjà en mémoire, appelle Texture2D.LoadImage(markNonReadable: true) et Sprite.Create,
        /// puis met à jour la barre de progression de manière fluide à chaque frame.
        /// </summary>
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
            int lastUiLoaded = -1;

            _cts = new CancellationTokenSource();
            var ct = _cts.Token;
            _queue = new BlockingCollection<CardTexturePayload>(BoundedQueueCapacity);

            // Démarrage du pool de producteurs I/O sur le ThreadPool (inclut l'indexation hors main thread)
            Task.Run(() => ProduceCardPayloads(cards, pluginPath, _queue, ct), ct);

            var totalWatch = Stopwatch.StartNew();
            var frameWatch = Stopwatch.StartNew();

            while (!ct.IsCancellationRequested && (!_queue.IsCompleted || _queue.Count > 0))
            {
                if (this == null || !gameObject.activeInHierarchy)
                {
                    // L'objet Unity a été détruit (changement de scène ou arrêt)
                    CleanupResources();
                    yield break;
                }

                // Dépiler tout ce qui est prêt dans le budget de la frame courante (12 ms)
                while (!ct.IsCancellationRequested &&
                       frameWatch.Elapsed.TotalMilliseconds < FrameBudgetMs &&
                       _queue.TryTake(out CardTexturePayload payload))
                {
                    long cardStartTicks = frameWatch.ElapsedTicks;
                    ProcessCardPayload(payload, ref missing, ref failed);
                    double cardMs = (frameWatch.ElapsedTicks - cardStartTicks) * 1000.0 / Stopwatch.Frequency;

                    if (cardMs > SlowCardMs && payload.Card != null)
                    {
                        Plugin.Logger?.LogWarning($"[WankulLoadingScreen] Carte lente ({cardMs:F1} ms) : {payload.Card.Title} ({payload.Card.TexturePath})");
                    }

                    ReturnPayload(payload);
                    loaded++;
                }

                // Mettre à jour l'UI à chaque frame où au moins une carte a progressé (progression 100% fluide)
                if (loaded != lastUiLoaded)
                {
                    lastUiLoaded = loaded;
                    float progress = total > 0 ? (float)loaded / total : 1f;
                    _progressBarFill.fillAmount = progress;
                    _statusText.text = $"Chargement des cartes : {loaded} / {total} ({(int)(progress * 100)}%)";
                }

                if (!ct.IsCancellationRequested && (!_queue.IsCompleted || _queue.Count > 0))
                {
                    yield return null;
                    frameWatch.Restart();
                }
            }

            if (ct.IsCancellationRequested)
            {
                Plugin.Logger?.LogWarning("[WankulLoadingScreen] Chargement interrompu (changement de scene ou destruction).");
            }
            else
            {
                Plugin.LogInfo($"[WankulLoadingScreen] {total} cartes traitees en {totalWatch.ElapsedMilliseconds} ms ({missing} sans fichier texture, {failed} en echec).");
                if (missing > 0)
                {
                    Plugin.Logger?.LogWarning($"[WankulLoadingScreen] {missing} carte(s) ont un TexturePath introuvable dans data/.");
                }

                _statusText.text = "Chargement termine !";
                yield return new WaitForSecondsRealtime(0.2f);
            }

            CleanupResources();

            if (_canvas != null) _canvas.enabled = false;
            IsLoading = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Traite un payload sur le main thread Unity : création de Texture2D et Sprite.
        /// Synchronise de manière atomique la texture principale et le masque foil pour éviter tout décalage.
        /// </summary>
        private static void ProcessCardPayload(CardTexturePayload payload, ref int missing, ref int failed)
        {
            if (payload == null || payload.Card == null) return;
            var card = payload.Card;

            if (string.IsNullOrEmpty(card.TexturePath)) return;

            // 1. Texture principale
            if (card.Texture == null || card.Sprite == null)
            {
                if (payload.HasTextureError || payload.TextureBytes == null)
                {
                    if (payload.TexturePath == null)
                    {
                        missing++;
                        Plugin.LogDebug($"Texture introuvable : {card.TexturePath}");
                    }
                    else
                    {
                        failed++;
                        if (!string.IsNullOrEmpty(payload.ErrorMessage))
                        {
                            Plugin.Logger?.LogWarning($"[WankulLoadingScreen] {payload.ErrorMessage}");
                        }
                    }
                }
                else
                {
                    try
                    {
                        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        // markNonReadable : true libère la mémoire CPU dès l'envoi sur GPU (~2x moins de RAM).
                        if (texture.LoadImage(payload.TextureBytes, true))
                        {
                            texture.wrapMode = TextureWrapMode.Clamp;
                            texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                            card.Texture = texture;
                            card.Sprite = CreateSprite(texture);
                        }
                        else
                        {
                            failed++;
                            UnityEngine.Object.Destroy(texture);
                            Plugin.Logger?.LogWarning($"Texture illisible (LoadImage failed) : {card.TexturePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Plugin.Logger?.LogWarning($"Texture load exception for {card.TexturePath}: {ex.Message}");
                    }
                }
            }

            // 2. Masque Foil (strictement synchronisé avec la carte principale)
            bool isFoil = IsCardFoil(card);
            if (payload.MaskBytes != null && (card.TextureMask == null || card.SpriteMask == null))
            {
                try
                {
                    Texture2D mask = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (mask.LoadImage(payload.MaskBytes, true))
                    {
                        mask.wrapMode = TextureWrapMode.Clamp;
                        mask.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                        card.TextureMask = mask;
                        card.SpriteMask = CreateSprite(mask);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(mask);
                        if (isFoil)
                        {
                            Plugin.Logger?.LogWarning($"[Foil] Le masque a echoue au chargement pour la carte foil {card.Title} (#{card.Number}): {payload.MaskPath}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (isFoil)
                    {
                        Plugin.Logger?.LogWarning($"[Foil] Exception lors du chargement du masque pour {card.Title}: {ex.Message}");
                    }
                }
            }
            else if (isFoil && card.TextureMask == null)
            {
                if (payload.MaskPath != null)
                {
                    Plugin.Logger?.LogWarning($"[Foil] Le masque a echoue au chargement pour la carte foil {card.Title} (#{card.Number}): {payload.MaskPath}");
                }
                else
                {
                    Plugin.LogDebug($"[Foil] Aucun masque trouve pour la carte foil {card.Title} (#{card.Number})");
                }
            }
        }

        #endregion

        #region Helpers de Création Directe (Compatibilité & Fallback)

        public static Texture2D LoadTexture(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            string resolved = ResolveExistingPath(path);
            if (resolved == null) return null;

            try
            {
                byte[] bytes = File.ReadAllBytes(resolved);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes, true))
                {
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                return texture;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[WankulLoadingScreen] LoadTexture failed for {path}: {ex.Message}");
                return null;
            }
        }

        public static Sprite CreateSprite(Texture2D texture)
        {
            if (texture == null) return null;
            // FullRect évite la génération d'un maillage de contour polygonal serré (Tight)
            Sprite s = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return s;
        }

        #endregion
    }
}
