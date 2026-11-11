using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace WankulCrazyPlugin.Tests
{
    public class CustomAssetIntegrityTests
    {
        private static string GetProjectRootPath()
        {
            string? rootPath = AppDomain.CurrentDomain.BaseDirectory;
            while (rootPath != null && !File.Exists(Path.Combine(rootPath, "WankulCrazyPlugin.sln")))
            {
                rootPath = Directory.GetParent(rootPath)?.FullName;
            }
            if (rootPath == null)
            {
                rootPath = Directory.GetCurrentDirectory();
            }
            return rootPath;
        }

        [Fact]
        public void CustomItems_ItemDataList_AllIconsExist()
        {
            string rootPath = GetProjectRootPath();
            string jsonPath = Path.Combine(rootPath, "data", "customitems", "ItemDataList.json");
            Assert.True(File.Exists(jsonPath), $"ItemDataList.json not found at: {jsonPath}");

            string iconsDir = Path.Combine(rootPath, "data", "customitems", "icons");
            Assert.True(Directory.Exists(iconsDir), $"Icons directory not found at: {iconsDir}");

            string jsonContent = File.ReadAllText(jsonPath);
            JArray items = JArray.Parse(jsonContent);

            var missingIcons = new List<string>();

            foreach (JToken token in items)
            {
                if (token is not JObject item) continue;

                string itemType = (string?)item["itemType"] ?? "Unknown";
                string? iconFileName = (string?)item["icon"];

                Assert.False(string.IsNullOrWhiteSpace(iconFileName), $"Item '{itemType}' has a null or empty icon field.");

                string iconPath = Path.Combine(iconsDir, iconFileName!);
                if (!File.Exists(iconPath))
                {
                    missingIcons.Add($"Item '{itemType}': icon '{iconFileName}' missing at '{iconPath}'");
                }
            }

            Assert.True(missingIcons.Count == 0,
                $"Missing icon files for custom items:\n{string.Join("\n", missingIcons)}");
        }

        [Fact]
        public void CustomItems_ItemMeshDataList_AllTexturesExist()
        {
            string rootPath = GetProjectRootPath();
            string jsonPath = Path.Combine(rootPath, "data", "customitems", "itemMeshDataList.json");
            Assert.True(File.Exists(jsonPath), $"itemMeshDataList.json not found at: {jsonPath}");

            string texturesDir = Path.Combine(rootPath, "data", "customitems", "textures");
            Assert.True(Directory.Exists(texturesDir), $"Textures directory not found at: {texturesDir}");

            string jsonContent = File.ReadAllText(jsonPath);
            JArray meshes = JArray.Parse(jsonContent);

            var missingTextures = new List<string>();

            foreach (JToken token in meshes)
            {
                if (token is not JObject mesh) continue;

                string itemType = (string?)mesh["itemType"] ?? (string?)mesh["name"] ?? "Unknown";
                string? textureFileName = (string?)mesh["texture"];

                if (string.IsNullOrWhiteSpace(textureFileName))
                {
                    continue;
                }

                string texturePath = Path.Combine(texturesDir, textureFileName!);
                if (!File.Exists(texturePath))
                {
                    missingTextures.Add($"Mesh entry '{itemType}': texture '{textureFileName}' missing at '{texturePath}'");
                }
            }

            Assert.True(missingTextures.Count == 0,
                $"Missing texture files for mesh entries:\n{string.Join("\n", missingTextures)}");
        }

        [Fact]
        public void PatchTextures_CodeReferences_AllPngLiteralsExist()
        {
            string rootPath = GetProjectRootPath();

            string[] sourceRelativePaths = new[]
            {
                Path.Combine("importer", "PatchTexturesImporter.cs"),
                Path.Combine("patch", "InteractionPlayerControllerPatch.cs")
            };

            // Regex matches: Path.Combine(Plugin.GetPluginPath(), "data", ... )
            Regex pathCombineRegex = new Regex(
                @"Path\.Combine\s*\(\s*Plugin\.GetPluginPath\(\)\s*,\s*(""data""[^)]+)\)",
                RegexOptions.Compiled);

            Regex stringLiteralRegex = new Regex(@"""([^""]+)""", RegexOptions.Compiled);

            var missingFiles = new List<string>();
            int testedPathCount = 0;

            foreach (string relSourcePath in sourceRelativePaths)
            {
                string fullSourcePath = Path.Combine(rootPath, relSourcePath);
                Assert.True(File.Exists(fullSourcePath), $"Source file not found at: {fullSourcePath}");

                string code = File.ReadAllText(fullSourcePath);
                MatchCollection matches = pathCombineRegex.Matches(code);

                foreach (Match match in matches)
                {
                    string argsBlock = match.Groups[1].Value;
                    MatchCollection stringMatches = stringLiteralRegex.Matches(argsBlock);

                    var pathSegments = new List<string>();
                    foreach (Match strMatch in stringMatches)
                    {
                        pathSegments.Add(strMatch.Groups[1].Value);
                    }

                    if (pathSegments.Count == 0) continue;

                    string lastSegment = pathSegments[pathSegments.Count - 1];
                    if (!lastSegment.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    testedPathCount++;
                    string targetFilePath = Path.Combine(rootPath, Path.Combine(pathSegments.ToArray()));

                    if (!File.Exists(targetFilePath))
                    {
                        missingFiles.Add($"{relSourcePath} references missing file: '{targetFilePath}'");
                    }
                }
            }

            Assert.True(testedPathCount > 0, "No Path.Combine PNG literal references were extracted.");
            Assert.True(missingFiles.Count == 0,
                $"Missing PNG assets referenced in source code:\n{string.Join("\n", missingFiles)}");
        }
    }
}
