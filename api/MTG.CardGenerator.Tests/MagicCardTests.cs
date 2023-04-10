using Xunit;

namespace MTG.CardGenerator.Tests
{
    public class MagicCardTests
    {
        [Fact]
        public void CardHasFlashback()
        {
            var _card = new BasicCard()
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
            var _card = new BasicCard()
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
            var _card = new BasicCard()
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
            var _card = new BasicCard()
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
            var _card = new BasicCard()
            {
                Name = "Fireball",
                ManaCost = "{1R}",
                OracleText = "Deal 2 damage to target creature or player. If Fireball was cast from your graveyard, it deals 2 additional damage."
            };

            var card = new MagicCard(_card);
            Assert.Equal("{1}{R}", card.ManaCost);
            Assert.Equal(ColorIdentity.Red, card.ColorIdentity);
        }

        [Fact]
        public void CreatureCannotHaveFlashbackAndShouldGetUnearth()
        {
            var card = new MagicCard(new BasicCard()
            {
                Name = "Dreadmaw Stalker",
                ManaCost = "{3}{B}",
                OracleText = "Flashback {4}{B}{B} (You may cast this card from your graveyard for its flashback cost. Then exile it.)\nWhen Dreadmaw Stalker enters the battlefield, you may discard a card. If you do, target creature gets -2/-2 until end of turn.",
                Type = "Creature - Zombie Crocodile"
            });

            // Flashback property is violated.
            Assert.Single(card.ViolatedRules);
            Assert.Equal(CardProperty.Flashback, card.ViolatedRules[0].CardProperty);

            // Reparse the card.
            card = new MagicCard(new BasicCard()
            {
                Name = card.Name,
                ManaCost = card.ManaCost,
                OracleText = card.RawOracleText,
                Type = card.TypeLine
            });

            // It should now have Unearth.
            Assert.Empty(card.ViolatedRules);
            Assert.Contains(CardProperty.Unearth, card.Properties);
            Assert.DoesNotContain(CardProperty.Flashback, card.Properties);
        }

        [Fact]
        public void InstantCardWithGraveyardEffectShouldHaveFlashback()
        {
            var card = new MagicCard(new BasicCard()
            {
                Name = "Silent Echoes",
                ManaCost = "{3}{U}{U}",
                OracleText = "Return target creature card from your graveyard to the battlefield. If Silent Echoes was cast from your graveyard, return an additional creature card from your graveyard to the battlefield.",
                Type = "Instant"
            });

            // Gain effect from graveyard is violated because it does not have Flashback.
            Assert.Single(card.ViolatedRules);
            Assert.Equal(CardProperty.GainsEffectWhenCastFromGraveyard, card.ViolatedRules[0].CardProperty);

            // Reparse the card.
            card = new MagicCard(new BasicCard()
            {
                Name = card.Name,
                ManaCost = card.ManaCost,
                OracleText = card.RawOracleText,
                Type = card.TypeLine
            });

            // It should now have Flashback..
            Assert.Empty(card.ViolatedRules);
            Assert.Contains(CardProperty.Flashback, card.Properties);
            Assert.Contains(CardProperty.GainsEffectWhenCastFromGraveyard, card.Properties);
        }

        [Fact]
        public void ArtifactCardWithFlashbackShouldGetUnearthWithSameEffect()
        {
            var card = new MagicCard(new BasicCard()
            {
                Name = "Memory Amulet",
                ManaCost = "{2}",
                OracleText = "Flashback {2}{U}{U}, Sacrifice Memory Amulet: Target player shuffles their graveyard into their library, then draws a card.",
                Type = "Artifact"
            });

            // Flashback property is violated.
            Assert.Single(card.ViolatedRules);
            Assert.Equal(CardProperty.Flashback, card.ViolatedRules[0].CardProperty);

            // Reparse the card.
            card = new MagicCard(new BasicCard()
            {
                Name = card.Name,
                ManaCost = card.ManaCost,
                OracleText = card.RawOracleText,
                Type = card.TypeLine
            });

            // It should now have Unearth.
            Assert.Empty(card.ViolatedRules);
            Assert.Contains(CardProperty.Unearth, card.Properties);
            Assert.Equal("Sacrifice Memory Amulet: Target player shuffles their graveyard into their library, then draws a card.\nUnearth {2}{U}{U} ({2}: Return the card to the battlefield. The creature gains haste. Exile it at the beginning of the next end step or if it would leave the battlefield. Unearth only as a sorcery.)", card.RawOracleText);
        }

        [Fact]
        public void CorrectManaCostsInText()
        {
            var card = new MagicCard(new BasicCard()
            {
                Name = "Echoes of the Past",
                ManaCost = "{4}{U}",
                OracleText = "Exile target creature you control. Return that card to the battlefield under your control at the beginning of the next end step.\nFlashback 3U",
                Type = "Instant"
            });
        }
    }
}
