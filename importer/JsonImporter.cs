using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BepInEx.Logging;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.importer;

public class JsonImporter
{
    public static void ImportJson()
    {
        string pluginPath = Plugin.GetPluginPath();

        // 1. Load dynamic Seasons & Rarities configuration
        SeasonsManager.LoadFromPluginPath(pluginPath);
        RaritiesManager.LoadFromPluginPath(pluginPath);

        Dictionary<int, WankulCardData> allCardsDict = new Dictionary<int, WankulCardData>();

        int autoAssignedIndex = 900000;
        void RegisterCard(WankulCardData card)
        {
            if (card == null) return;
            if (card.Index <= 0)
            {
                card.Index = ++autoAssignedIndex;
            }
            allCardsDict[card.Index] = card;
        }

        // 2. Load legacy main cards file if present
        string legacyPath = Path.Combine(pluginPath, "data/formated_wankul_cards.json");
        if (File.Exists(legacyPath))
        {
            try
            {
                using (var stream = new FileStream(legacyPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var streamReader = new StreamReader(stream, System.Text.Encoding.UTF8))
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    JToken token = JToken.ReadFrom(jsonReader);
                    List<WankulCardData> cards = DeserializeToken(token);
                    foreach (var card in cards)
                    {
                        RegisterCard(card);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError("Failed to deserialize legacy cards JSON: " + ex.Message);
            }
        }

        // 3. Load multi-file cards from data/cards/ directory if present
        //    (Overwrites any existing legacy cards with the same Index, giving priority to data/cards/)
        string cardsDirectory = Path.Combine(pluginPath, "data/cards");
        if (Directory.Exists(cardsDirectory))
        {
            string[] cardFiles = Directory.GetFiles(cardsDirectory, "*.json", SearchOption.AllDirectories);
            foreach (string filePath in cardFiles)
            {
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var streamReader = new StreamReader(stream, System.Text.Encoding.UTF8))
                    using (var jsonReader = new JsonTextReader(streamReader))
                    {
                        JToken token = JToken.ReadFrom(jsonReader);
                        List<WankulCardData> cards = DeserializeToken(token);
                        foreach (var card in cards)
                        {
                            RegisterCard(card);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"Failed to deserialize cards from {filePath}: {ex.Message}");
                }
            }
        }

        List<WankulCardData> allCards = new List<WankulCardData>(allCardsDict.Values);

        if (allCards.Count > 0)
        {
            CreateCardsData(allCards);
            WankulCardsData wankulCardsData = WankulCardsData.Instance;
            Plugin.LogInfo("Cards data loaded: " + wankulCardsData.cards.Count + " cards");
        }
        else
        {
            Plugin.Logger?.LogError("Failed to deserialize JSON: no cards found across legacy or cards directory.");
        }
    }

    public static List<WankulCardData> DeserializeToken(JToken token)
    {
        List<WankulCardData> cards = new List<WankulCardData>();
        if (token == null) return cards;

        var serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

        if (token.Type == JTokenType.Object)
        {
            JObject jsonObject = (JObject)token;
            if (jsonObject["wankuls"] != null || jsonObject["terrains"] != null || jsonObject["specials"] != null)
            {
                cards.AddRange(DeserializeCardsContainer(jsonObject, serializer));
            }
            else
            {
                var card = DeserializeSingleCard(jsonObject, serializer);
                if (card != null) cards.Add(card);
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            JArray jsonArray = (JArray)token;
            foreach (var item in jsonArray)
            {
                if (item.Type == JTokenType.Object)
                {
                    var card = DeserializeSingleCard((JObject)item, serializer);
                    if (card != null) cards.Add(card);
                }
            }
        }

        return cards;
    }

    private static List<WankulCardData> DeserializeCardsContainer(JObject jsonObject, JsonSerializer serializer)
    {
        List<WankulCardData> cards = new List<WankulCardData>();

        JToken wankulsToken = jsonObject["wankuls"];
        if (wankulsToken != null)
        {
            List<EffigyCardData> wankuls = wankulsToken.ToObject<List<EffigyCardData>>(serializer);
            if (wankuls != null)
            {
                // Hook manuel pour conserver RarityId si fourni dans le JSON car Newtonsoft
                // ignore RarityId via le setter de WankulCardData.Rarity
                if (wankulsToken.Type == JTokenType.Array)
                {
                    JArray array = (JArray)wankulsToken;
                    for (int i = 0; i < wankuls.Count; i++)
                    {
                        if (array[i] is JObject wankulObj && wankulObj["Rarity"] != null)
                        {
                            wankuls[i].RarityId = wankulObj["Rarity"].ToString();
                        }
                    }
                }
                cards.AddRange(wankuls);
            }
        }

        JToken terrainsToken = jsonObject["terrains"];
        if (terrainsToken != null)
        {
            List<TerrainCardData> terrains = terrainsToken.ToObject<List<TerrainCardData>>(serializer);
            if (terrains != null) cards.AddRange(terrains);
        }

        JToken specialsToken = jsonObject["specials"];
        if (specialsToken != null)
        {
            List<SpecialCardData> specials = specialsToken.ToObject<List<SpecialCardData>>(serializer);
            if (specials != null) cards.AddRange(specials);
        }

        return cards;
    }

    private static WankulCardData DeserializeSingleCard(JObject obj, JsonSerializer serializer)
    {
        WankulCardData result = null;
        if (obj["CardType"] != null)
        {
            string cardType = obj["CardType"].ToString();
            if (cardType.Equals("Terrain", StringComparison.OrdinalIgnoreCase))
                result = obj.ToObject<TerrainCardData>(serializer);
            else if (cardType.Equals("Special", StringComparison.OrdinalIgnoreCase))
                result = obj.ToObject<SpecialCardData>(serializer);
            else if (cardType.Equals("Effigy", StringComparison.OrdinalIgnoreCase))
                result = obj.ToObject<EffigyCardData>(serializer);
        }

        if (result == null)
        {
            if (obj["Terrain"] != null)
            {
                result = obj.ToObject<TerrainCardData>(serializer);
            }
            else if (obj["Special"] != null)
            {
                result = obj.ToObject<SpecialCardData>(serializer);
            }
            else if (obj["Effigy"] != null || obj["Rarity"] != null || obj["RarityId"] != null)
            {
                result = obj.ToObject<EffigyCardData>(serializer);
            }
            else
            {
                result = obj.ToObject<WankulCardData>(serializer);
            }
        }

        if (result is EffigyCardData effigy && obj["Rarity"] != null)
        {
            effigy.RarityId = obj["Rarity"].ToString();
        }

        return result;
    }

    private static void CreateCardsData(List<WankulCardData> cards)
    {
        WankulCardsData cardsData = WankulCardsData.Instance;
        cardsData.cards = cards;

        // Si nous sommes dans l'environnement de jeu Unity, utiliser l'écran de chargement asynchrone non bloquant
        if (Application.isPlaying)
        {
            WankulLoadingScreen.ShowAndStartLoading(cardsData.cards, () =>
            {
                Plugin.LogInfo($"[WankulLoadingScreen] Toutes les {cardsData.cards.Count} textures de cartes ont été chargées avec succès !");
            });
            return;
        }

        // Fallback synchrone pour les tests unitaires headless
        string pluginPath = Plugin.GetPluginPath();
        WankulLoadingScreen.EnsureFileIndexInitialized(pluginPath);
        foreach (var card in cardsData.cards)
        {
            if (!string.IsNullOrEmpty(card.TexturePath))
            {
                string texturepath = WankulLoadingScreen.ResolveExistingPath(Path.Combine(pluginPath, "data", card.TexturePath));
                string texturepathmask = WankulLoadingScreen.ResolveMaskPath(card, pluginPath);

                try
                {
                    Texture2D texture = texturepath != null ? LoadTexture(texturepath) : null;
                    Texture2D texturemask = null;

                    bool isFoil = WankulLoadingScreen.IsCardFoil(card);
                    if (texturepathmask != null)
                    {
                        texturemask = LoadTexture(texturepathmask);
                    }

                    if (texture != null)
                    {
                        card.Texture = texture;
                    }
                    else if (texturepath != null)
                    {
                        Plugin.Logger?.LogError("Failed to load texture: " + texturepath);
                    }

                    if (texturemask != null)
                    {
                        card.TextureMask = texturemask;
                    }
                    else if (texturepathmask != null)
                    {
                        Plugin.Logger?.LogError("Failed to load texture mask: " + texturepathmask);
                    }
                    else if (isFoil)
                    {
                        Plugin.LogDebug($"[Foil] Aucun masque trouve pour la carte foil {card.Title} (#{card.Number})");
                    }

                    Sprite sprite = texture != null ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect) : null;
                    Sprite spritemask = null;

                    if (texturemask != null)
                    {
                        spritemask = Sprite.Create(texturemask, new Rect(0, 0, texturemask.width, texturemask.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    }

                    if (sprite != null)
                    {
                        sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                        card.Sprite = sprite;
                    }
                    else if (texture != null)
                    {
                        Plugin.Logger?.LogError("Failed to create sprite: " + texturepath);
                    }

                    if (spritemask != null)
                    {
                        spritemask.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                        card.SpriteMask = spritemask;
                    }
                    else if (texturemask != null)
                    {
                        Plugin.Logger?.LogError("Failed to create sprite mask: " + texturepathmask);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogWarning("Texture/Sprite load skipped: " + ex.Message);
                }
            }
        }
    }

    private static Texture2D LoadTexture(string path)
    {
        string resolved = WankulLoadingScreen.ResolveExistingPath(path);
        if (resolved == null)
        {
            return null;
        }
        byte[] bytes = System.IO.File.ReadAllBytes(resolved);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (texture.LoadImage(bytes, true))
        {
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return texture;
        }
        UnityEngine.Object.Destroy(texture);
        return null;
    }
}
