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

        List<WankulCardData> allCards = new List<WankulCardData>();

        // 2. Load legacy main cards file if present
        string legacyPath = Path.Combine(pluginPath, "data/formated_wankul_cards.json");
        if (File.Exists(legacyPath))
        {
            try
            {
                string jsonContent = File.ReadAllText(legacyPath);
                JToken token = JToken.Parse(jsonContent);
                List<WankulCardData> cards = DeserializeToken(token);
                allCards.AddRange(cards);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError("Failed to deserialize legacy cards JSON: " + ex.Message);
            }
        }

        // 3. Load multi-file cards from data/cards/ directory if present
        string cardsDirectory = Path.Combine(pluginPath, "data/cards");
        if (Directory.Exists(cardsDirectory))
        {
            string[] cardFiles = Directory.GetFiles(cardsDirectory, "*.json", SearchOption.AllDirectories);
            foreach (string filePath in cardFiles)
            {
                try
                {
                    string jsonContent = File.ReadAllText(filePath);
                    JToken token = JToken.Parse(jsonContent);
                    List<WankulCardData> cards = DeserializeToken(token);
                    allCards.AddRange(cards);
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"Failed to deserialize cards from {filePath}: {ex.Message}");
                }
            }
        }

        if (allCards.Count > 0)
        {
            CreateCardsData(allCards);
            WankulCardsData wankulCardsData = WankulCardsData.Instance;
            Plugin.Logger?.LogInfo("Cards data loaded: " + wankulCardsData.cards.Count + " cards");
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
            if (wankuls != null) cards.AddRange(wankuls);
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
        if (obj["Effigy"] != null || obj["Rarity"] != null || obj["RarityId"] != null || (obj["CardType"] != null && obj["CardType"].ToString().Equals("Effigy", StringComparison.OrdinalIgnoreCase)))
        {
            return obj.ToObject<EffigyCardData>(serializer);
        }
        if (obj["Special"] != null || (obj["CardType"] != null && obj["CardType"].ToString().Equals("Special", StringComparison.OrdinalIgnoreCase)))
        {
            return obj.ToObject<SpecialCardData>(serializer);
        }
        if (obj["Terrain"] != null || (obj["CardType"] != null && obj["CardType"].ToString().Equals("Terrain", StringComparison.OrdinalIgnoreCase)))
        {
            return obj.ToObject<TerrainCardData>(serializer);
        }
        return obj.ToObject<WankulCardData>(serializer);
    }

    private static void CreateCardsData(List<WankulCardData> cards)
    {
        WankulCardsData cardsData = WankulCardsData.Instance;
        cardsData.cards = cards;

        string pluginPath = Plugin.GetPluginPath();
        foreach (var card in cardsData.cards)
        {
            if (!string.IsNullOrEmpty(card.TexturePath))
            {
                string texturepath = Path.Combine(pluginPath, "data", card.TexturePath);
                string texturepathmask = Path.Combine(pluginPath, "data/masks", card.TexturePath);

                Texture2D texture = LoadTexture(texturepath);
                Texture2D texturemask = null;

                if (File.Exists(texturepathmask))
                {
                    texturemask = LoadTexture(texturepathmask);
                }

                if (texture != null)
                {
                    card.Texture = texture;
                }
                else
                {
                    Plugin.Logger?.LogError("Failed to load texture: " + texturepath);
                }

                if (texturemask != null)
                {
                    card.TextureMask = texturemask;
                }
                else if (File.Exists(texturepathmask))
                {
                    Plugin.Logger?.LogError("Failed to load texture mask: " + texturepathmask);
                }

                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                Sprite spritemask = null;

                if (texturemask != null)
                {
                    spritemask = Sprite.Create(texturemask, new Rect(0, 0, texturemask.width, texturemask.height), new Vector2(0.5f, 0.5f));
                }

                if (sprite != null)
                {
                    card.Sprite = sprite;
                }
                else
                {
                    Plugin.Logger?.LogError("Failed to create sprite: " + texturepath);
                }

                if (spritemask != null)
                {
                    card.SpriteMask = spritemask;
                }
                else if (texturemask != null)
                {
                    Plugin.Logger?.LogError("Failed to create sprite mask: " + texturepathmask);
                }
            }
        }
    }

    private static Texture2D LoadTexture(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            return null;
        }
        byte[] bytes = System.IO.File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(bytes))
        {
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }
        return null;
    }
}
