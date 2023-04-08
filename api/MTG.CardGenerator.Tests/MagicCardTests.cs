﻿using Xunit;

namespace MTG.CardGenerator.Tests
{
    public class MagicCardTests
    {
        [Fact]
        public void CardHasFlashback()
        {
            var _card = new OpenAIMagicCard()
            {
                Name = "TestCard",
                ManaCost = "{1}",
                OracleText = "Discard two cards: Create a 2/2 black Zombie creature token.\nFlashback {1}{B}{B}"
            };

            var card = new MagicCard(_card);
            Assert.Contains(CardProperty.Flashback, card.Properties);
        }

        [Fact]
        public void CardWithEffectFromGraveyardMustHaveFlashback()
        {
            var _card = new OpenAIMagicCard()
            {
                Name = "Fireball",
                ManaCost = "{1}{R}",
                OracleText = "Deal 2 damage to target creature or player. If Fireball was cast from your graveyard, it deals 2 additional damage."
            };

            var card = new MagicCard(_card);
            Assert.Contains(CardProperty.GainsEffectWhenCastFromGraveyard, card.Properties);
            Assert.Single(card.ViolatedRules);
            Assert.Equal(CardProperty.GainsEffectWhenCastFromGraveyard, card.ViolatedRules[0].CardProperty);
        }

        [Fact]
        public void CardParsesManaCostMistakesCorrectly()
        {
            var _card = new OpenAIMagicCard()
            {
                Name = "Fireball",
                ManaCost = "1R",
                OracleText = "Deal 2 damage to target creature or player. If Fireball was cast from your graveyard, it deals 2 additional damage."
            };

            var card = new MagicCard(_card);
            Assert.Equal("{1}{R}", card.ManaCost);
            Assert.Equal(ColorIdentity.Red, card.ColorIdentity);
        }

        [Fact]
        public void CardParsesManaCostMistakesCorrectly2()
        {
            var _card = new OpenAIMagicCard()
            {
                Name = "Fireball",
                ManaCost = "1{R}",
                OracleText = "Deal 2 damage to target creature or player. If Fireball was cast from your graveyard, it deals 2 additional damage."
            };

            var card = new MagicCard(_card);
            Assert.Equal("{1}{R}", card.ManaCost);
            Assert.Equal(ColorIdentity.Red, card.ColorIdentity);
        }

        [Fact]
        public void CardParsesManaCostMistakesCorrectly3()
        {
            var _card = new OpenAIMagicCard()
            {
                Name = "Fireball",
                ManaCost = "{1R}",
                OracleText = "Deal 2 damage to target creature or player. If Fireball was cast from your graveyard, it deals 2 additional damage."
            };

            var card = new MagicCard(_card);
            Assert.Equal("{1}{R}", card.ManaCost);
            Assert.Equal(ColorIdentity.Red, card.ColorIdentity);
        }
    }
}
