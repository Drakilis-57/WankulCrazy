using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WankulCrazyPlugin.cards;
using WankulCrazyPlugin.importer;
using Xunit;

namespace WankulCrazyPlugin.Tests
{
    public class WankulLoadingPipelineTests
    {
        [Fact]
        public void FileIndex_NormalizesPaths_InsensitiveToCaseAndSlashes()
        {
            // Créer un dossier temporaire simulant un data/
            string tempDir = Path.Combine(Path.GetTempPath(), "WankulLoadingTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(tempDir, "data", "cards", "Origins"));
            Directory.CreateDirectory(Path.Combine(tempDir, "data", "masks", "textures"));

            try
            {
                string dummyCardFile = Path.Combine(tempDir, "data", "cards", "Origins", "001_Laink.png");
                File.WriteAllBytes(dummyCardFile, new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }); // PNG magic bytes

                string dummyMaskFile = Path.Combine(tempDir, "data", "masks", "textures", "150_151.png");
                File.WriteAllBytes(dummyMaskFile, new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

                // Initialiser l'index
                WankulFileIndex.Invalidate();
                WankulFileIndex.EnsureInitialized(tempDir);

                // 1. Test casse différente
                string queryUpper = Path.Combine(tempDir, "data", "CARDS", "ORIGINS", "001_LAINK.PNG");
                string resolvedUpper = WankulFileIndex.ResolveExistingPath(queryUpper);
                Assert.NotNull(resolvedUpper);
                Assert.True(File.Exists(resolvedUpper));

                // 2. Test slashes inverses '/'
                string querySlash = tempDir.Replace('\\', '/') + "/data/cards/Origins/001_Laink.png";
                string resolvedSlash = WankulFileIndex.ResolveExistingPath(querySlash);
                Assert.NotNull(resolvedSlash);

                // 3. Test extension alternative (.jpg alors que le fichier est .png)
                string queryJpg = Path.Combine(tempDir, "data", "cards", "Origins", "001_Laink.jpg");
                string resolvedJpg = WankulFileIndex.ResolveExistingPath(queryJpg);
                Assert.NotNull(resolvedJpg);
                Assert.EndsWith(".png", resolvedJpg, StringComparison.OrdinalIgnoreCase);

                // 4. Test fichier inexistant
                string nonExistent = Path.Combine(tempDir, "data", "cards", "Origins", "999_Introuvable.png");
                string resolvedNone = WankulFileIndex.ResolveExistingPath(nonExistent);
                Assert.Null(resolvedNone);
            }
            finally
            {
                WankulFileIndex.Invalidate();
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [Fact]
        public void ResolveMaskPath_ResolvesCorrectPrefixes()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "WankulMaskTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(tempDir, "data", "masks", "textures"));

            try
            {
                // S01: prefix = 150 + (num - 151) -> 151 donne 150_151.png
                string mask151 = Path.Combine(tempDir, "data", "masks", "textures", "150_151.png");
                File.WriteAllBytes(mask151, new byte[] { 1, 2, 3 });

                // S02: prefix = 305 + (num - 126) -> 126 donne 305_126.png
                string mask126 = Path.Combine(tempDir, "data", "masks", "textures", "305_126.png");
                File.WriteAllBytes(mask126, new byte[] { 1, 2, 3 });

                WankulFileIndex.Invalidate();
                WankulFileIndex.EnsureInitialized(tempDir);

                var cardS01 = new EffigyCardData
                {
                    Number = "151",
                    SeasonId = "S01",
                    TexturePath = "Origins/151.png",
                    RarityId = "UR1"
                };

                var cardS02 = new EffigyCardData
                {
                    Number = "126",
                    SeasonId = "S02",
                    TexturePath = "Campus/126.png",
                    RarityId = "UR2"
                };

                string resolvedS01 = WankulFileIndex.ResolveMaskPath(cardS01, tempDir);
                Assert.NotNull(resolvedS01);
                Assert.EndsWith("150_151.png", resolvedS01);

                string resolvedS02 = WankulFileIndex.ResolveMaskPath(cardS02, tempDir);
                Assert.NotNull(resolvedS02);
                Assert.EndsWith("305_126.png", resolvedS02);

                // Carte sans masque disponible
                var cardNoMask = new EffigyCardData
                {
                    Number = "999",
                    SeasonId = "S01",
                    TexturePath = "Origins/999.png",
                    RarityId = "C"
                };
                string resolvedNoMask = WankulFileIndex.ResolveMaskPath(cardNoMask, tempDir);
                Assert.Null(resolvedNoMask);
            }
            finally
            {
                WankulFileIndex.Invalidate();
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        [Fact]
        public void IsCardFoil_DetectsFoilEligibleCards()
        {
            var commonCard = new EffigyCardData { RarityId = "C", Rarity = Rarity.C };
            Assert.False(WankulFileIndex.IsCardFoil(commonCard));

            var ur1Card = new EffigyCardData { RarityId = "UR1", Rarity = Rarity.UR1 };
            Assert.True(WankulFileIndex.IsCardFoil(ur1Card));

            var ur2Card = new EffigyCardData { RarityId = "UR2", Rarity = Rarity.UR2 };
            Assert.True(WankulFileIndex.IsCardFoil(ur2Card));

            var lbCard = new EffigyCardData { RarityId = "LB", Rarity = Rarity.LB };
            Assert.True(WankulFileIndex.IsCardFoil(lbCard));

            // Carte non-effigy (terrain ou special) n'est pas foil
            var terrainCard = new TerrainCardData();
            Assert.False(WankulFileIndex.IsCardFoil(terrainCard));
        }

        [Fact]
        public async Task ProducerConsumer_QueueCancellation_TerminatesCleanly()
        {
            // Vérifie que si le CancellationToken est annulé, la queue bornée se vide et ne bloque aucun thread
            var queue = new BlockingCollection<CardTexturePayload>(boundedCapacity: 4);
            using var cts = new CancellationTokenSource();

            var task = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 100; i++)
                    {
                        if (cts.Token.IsCancellationRequested) break;
                        queue.Add(new CardTexturePayload { ErrorMessage = $"Item_{i}" }, cts.Token);
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    queue.CompleteAdding();
                }
            });

            // Consommer 2 éléments puis annuler
            Assert.True(queue.TryTake(out var p1, 1000));
            Assert.True(queue.TryTake(out var p2, 1000));
            cts.Cancel();

            // Attendre que la tâche productrice se termine
            await task;
            Assert.True(task.IsCompletedSuccessfully);
        }
    }
}
