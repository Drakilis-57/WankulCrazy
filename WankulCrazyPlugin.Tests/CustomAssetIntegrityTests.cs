using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace WankulCrazyPlugin.Tests
{
    /// <summary>
    /// Marks a test that depends on local-only binary/PNG assets excluded by .gitignore.
    /// Runs locally when assets exist and skips explicitly in CI or clean checkouts.
    /// </summary>
    [XunitTestCaseDiscoverer("WankulCrazyPlugin.Tests.LocalAssetFactDiscoverer", "WankulCrazyPlugin.Tests")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class LocalAssetFactAttribute : FactAttribute
    {
    }

    public sealed class LocalAssetFactDiscoverer : FactDiscoverer
    {
        public LocalAssetFactDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }

        protected override IXunitTestCase CreateTestCase(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo factAttribute)
        {
            return new LocalAssetFactTestCase(
                DiagnosticMessageSink,
                discoveryOptions.MethodDisplayOrDefault(),
                discoveryOptions.MethodDisplayOptionsOrDefault(),
                testMethod);
        }
    }

    public sealed class LocalAssetFactTestCase : XunitTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public LocalAssetFactTestCase()
        {
        }

        public LocalAssetFactTestCase(
            IMessageSink diagnosticMessageSink,
            TestMethodDisplay defaultMethodDisplay,
            TestMethodDisplayOptions defaultMethodDisplayOptions,
            ITestMethod testMethod,
            object[]? testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments)
        {
        }

        protected override string GetSkipReason(IAttributeInfo factAttribute)
        {
            return CustomAssetIntegrityTests.GetLocalAssetsSkipReason() ?? base.GetSkipReason(factAttribute);
        }
    }

    public class CustomAssetIntegrityTests
    {
        internal static string? GetLocalAssetsSkipReason()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase))
            {
                return "Local PNG/texture assets are excluded by .gitignore and absent in CI.";
            }

            string rootPath = GetProjectRootPath();
            bool hasAnyAssetDir =
                Directory.Exists(Path.Combine(rootPath, "data", "customitems", "icons"))
                || Directory.Exists(Path.Combine(rootPath, "data", "customitems", "textures"))
                || Directory.Exists(Path.Combine(rootPath, "data", "patchtextures"));

            if (!hasAnyAssetDir)
            {
                return "Local PNG/texture asset directories are absent (excluded by .gitignore).";
            }

            return null;
        }

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

        private static List<(string SourcePath, string TargetPath)> ExtractPatchTexturePngReferences(string rootPath)
        {
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
            var references = new List<(string SourcePath, string TargetPath)>();

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

                    string targetFilePath = Path.Combine(rootPath, Path.Combine(pathSegments.ToArray()));
                    references.Add((relSourcePath, targetFilePath));
                }
            }

            return references;
        }

        [Fact]
        public void PatchTextures_CodeReferences_ExtractsValidPngPaths()
        {
            string rootPath = GetProjectRootPath();
            var references = ExtractPatchTexturePngReferences(rootPath);

            Assert.NotEmpty(references);
            Assert.All(references, r => Assert.EndsWith(".png", r.TargetPath, StringComparison.OrdinalIgnoreCase));
        }

        [LocalAssetFact]
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

        [LocalAssetFact]
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

        [LocalAssetFact]
        public void PatchTextures_CodeReferences_AllPngLiteralsExist()
        {
            string rootPath = GetProjectRootPath();
            var references = ExtractPatchTexturePngReferences(rootPath);

            Assert.True(references.Count > 0, "No Path.Combine PNG literal references were extracted.");

            var missingFiles = references
                .Where(r => !File.Exists(r.TargetPath))
                .Select(r => $"{r.SourcePath} references missing file: '{r.TargetPath}'")
                .ToList();

            Assert.True(missingFiles.Count == 0,
                $"Missing PNG assets referenced in source code:\n{string.Join("\n", missingFiles)}");
        }
    }
}
