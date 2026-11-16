using System;
using System.Collections.Generic;
using WankulCrazy.Duel.Engine;
using Xunit;

namespace WankulCrazyPlugin.Tests
{
    [Collection("StaticStateTestCollection")]
    public class DuelComboTests
    {
        private class TestRandom : IRandom
        {
            public int Next(int maxValue) => 0;
            public int Next(int minValue, int maxValue) => minValue;
            public void Shuffle<T>(IList<T> list) { }
        }

        private DuelEngine CreateEngine()
        {
            var rules = new DuelRules(startingHandSize: 5, cardsDrawnPerTurn: 1, autoScoreForceThreshold: 110);
            return new DuelEngine(new TestRandom(), rules);
        }

        [Fact]
        public void PlayingComboCard_NextToOpeningGem_ActivatesCombo()
        {
            var engine = CreateEngine();
            var deck = new List<DuelCard>();
            for(int i = 0; i < 20; i++) deck.Add(new DuelCard($"p1_{i}", "Test", CardKind.Character, 10));
            engine.StartDuel(deck, deck, PlayerId.Player1);
            engine.StartTurn();

            // On a besoin d'un terrain
            var terrain = new DuelCard("t1", "Terrain", CardKind.Terrain, 0);
            engine.State.AddCardToHand(PlayerId.Player1, terrain);
            engine.PlayTerrain(PlayerId.Player1, "t1", 0);
            engine.StartTurn(); // Active le terrain

            var openingCard = new DuelCard("open1", "Open", CardKind.Character, force: 10, hasOpeningGem: true);
            var comboCard = new DuelCard("combo1", "Combo", CardKind.Character, force: 10, comboEffectIds: new[] { "boost_self_20" }, hasClosingGem: true);

            engine.State.AddCardToHand(PlayerId.Player1, openingCard);
            engine.State.AddCardToHand(PlayerId.Player1, comboCard);

            engine.PlayCharacter(PlayerId.Player1, "open1", 0);
            engine.PlayCharacter(PlayerId.Player1, "combo1", 0); // Va s'insérer à l'index 1 (après open1)

            var slot = engine.State.Slots[0];
            Assert.Equal(2, slot.CharactersP1.Count);

            // Force totale attendue: 10 + (10 + 20) = 40
            int force = slot.GetForce(PlayerId.Player1);
            Assert.Equal(40, force);
        }

        [Fact]
        public void PlayingComboCard_First_DoesNotActivateCombo()
        {
            var engine = CreateEngine();
            var deck = new List<DuelCard>();
            for(int i = 0; i < 20; i++) deck.Add(new DuelCard($"p1_{i}", "Test", CardKind.Character, 10));
            engine.StartDuel(deck, deck, PlayerId.Player1);
            engine.StartTurn();

            var terrain = new DuelCard("t1", "Terrain", CardKind.Terrain, 0);
            engine.State.AddCardToHand(PlayerId.Player1, terrain);
            engine.PlayTerrain(PlayerId.Player1, "t1", 0);
            engine.StartTurn(); // Active le terrain

            var comboCard = new DuelCard("combo1", "Combo", CardKind.Character, force: 10, comboEffectIds: new[] { "boost_self_20" }, hasClosingGem: true);

            engine.State.AddCardToHand(PlayerId.Player1, comboCard);

            engine.PlayCharacter(PlayerId.Player1, "combo1", 0); // Index 0

            var slot = engine.State.Slots[0];
            Assert.Single(slot.CharactersP1);

            // Force attendue: 10 (pas de combo car index 0)
            int force = slot.GetForce(PlayerId.Player1);
            Assert.Equal(10, force);
        }

        [Fact]
        public void InsertingCard_BetweenCombo_BreaksCombo()
        {
            var engine = CreateEngine();
            var deck = new List<DuelCard>();
            for(int i = 0; i < 20; i++) deck.Add(new DuelCard($"p1_{i}", "Test", CardKind.Character, 10));
            engine.StartDuel(deck, deck, PlayerId.Player1);
            engine.StartTurn();

            var terrain = new DuelCard("t1", "Terrain", CardKind.Terrain, 0);
            engine.State.AddCardToHand(PlayerId.Player1, terrain);
            engine.PlayTerrain(PlayerId.Player1, "t1", 0);
            engine.StartTurn();

            var openingCard = new DuelCard("open1", "Open", CardKind.Character, force: 10, hasOpeningGem: true);
            var comboCard = new DuelCard("combo1", "Combo", CardKind.Character, force: 10, comboEffectIds: new[] { "boost_self_20" }, hasClosingGem: true);
            var blockerCard = new DuelCard("block1", "Blocker", CardKind.Character, force: 10, hasOpeningGem: false, hasClosingGem: false);

            engine.State.AddCardToHand(PlayerId.Player1, openingCard);
            engine.State.AddCardToHand(PlayerId.Player1, comboCard);
            engine.State.AddCardToHand(PlayerId.Player1, blockerCard);

            engine.PlayCharacter(PlayerId.Player1, "open1", 0);
            engine.PlayCharacter(PlayerId.Player1, "combo1", 0); // Index 1

            // Vérification de la force combo actif
            Assert.Equal(40, engine.State.Slots[0].GetForce(PlayerId.Player1));

            // On insère le bloqueur à l'index 1 (entre les deux)
            engine.PlayCharacter(PlayerId.Player1, "block1", 0, insertIndex: 1);

            var slot = engine.State.Slots[0];
            Assert.Equal(3, slot.CharactersP1.Count);
            Assert.Equal("block1", slot.CharactersP1[1].Id);

            // L'open (idx 0) a une opening gem, le block (idx 1) n'a pas de closing gem -> Pas de combo
            // Le block (idx 1) n'a pas d'opening gem, le combo (idx 2) a une closing gem -> Pas de combo
            // Force totale: 10 + 10 + 10 = 30
            Assert.Equal(30, slot.GetForce(PlayerId.Player1));
        }

        [Fact]
        public void ComboColorMismatch_OrangeOpeningAndVioletClosing_DoesNotConnect()
        {
            var orangeOpen = new DuelCard("oo", "OpenOrange", CardKind.Character, force: 3, gemMask: GemMask.OpeningOrange);
            var violetClose = new DuelCard("cv", "CloseViolet", CardKind.Character, force: 4, gemMask: GemMask.ClosingViolet);
            var violetOpen = new DuelCard("ov", "OpenViolet", CardKind.Character, force: 3, gemMask: GemMask.OpeningViolet);

            Assert.False(ComboEvaluator.CanConnect(orangeOpen, violetClose));
            Assert.True(ComboEvaluator.CanConnect(violetOpen, violetClose));
        }

        [Fact]
        public void CardAdapter_ChevalierAndInfecte_DoesNotFormCombo()
        {
            var chevalierData = new WankulCrazyPlugin.cards.EffigyCardData
            {
                Index = 100064,
                Number = "64",
                Title = "CHEVALIER",
                Force = 5,
                Combo = "Défaussez un Personnage en jeu d'une Force de 2 ou moins."
            };
            var infecteData = new WankulCrazyPlugin.cards.EffigyCardData
            {
                Index = 100043,
                Number = "43",
                Title = "INFECTÉ",
                Force = 2,
                Combo = "Reprenez en main la carte du dessus de votre défausse."
            };

            var chevalier = WankulCrazyPlugin.duel.CardAdapter.ToDuelCard(chevalierData);
            var infecte = WankulCrazyPlugin.duel.CardAdapter.ToDuelCard(infecteData);

            Assert.True(chevalier.HasClosingOrangeGem);
            Assert.False(chevalier.HasOpeningGem);
            Assert.True(infecte.HasClosingOrangeGem);
            Assert.False(infecte.HasOpeningGem);
            Assert.False(ComboEvaluator.CanConnect(chevalier, infecte));
        }

        [Fact]
        public void CardAdapter_VendeurConnectsWithBoxeur_NotWithLutin()
        {
            var boxeur = WankulCrazyPlugin.duel.CardAdapter.ToDuelCard(new WankulCrazyPlugin.cards.EffigyCardData
            {
                Index = 100042,
                Number = "42",
                Title = "BOXEUR",
                Force = 3
            });
            var lutin = WankulCrazyPlugin.duel.CardAdapter.ToDuelCard(new WankulCrazyPlugin.cards.EffigyCardData
            {
                Index = 200026,
                Number = "26",
                Title = "LUTIN",
                Force = 2,
                Combo = "Piocher 1 carte."
            });
            var vendeur = WankulCrazyPlugin.duel.CardAdapter.ToDuelCard(new WankulCrazyPlugin.cards.EffigyCardData
            {
                Index = 100035,
                Number = "35",
                Title = "VENDEUR",
                Force = 4,
                Combo = "Scorez un Terrain."
            });

            Assert.True(vendeur.HasClosingVioletGem);
            Assert.False(vendeur.HasClosingOrangeGem);
            Assert.True(lutin.HasOpeningOrangeGem);
            Assert.False(lutin.HasOpeningVioletGem);
            Assert.True(boxeur.HasOpeningVioletGem);

            Assert.False(ComboEvaluator.CanConnect(lutin, vendeur));
            Assert.True(ComboEvaluator.CanConnect(boxeur, vendeur));
        }
    }
}
