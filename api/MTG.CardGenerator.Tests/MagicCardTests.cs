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
        public void CorrectManaCostsInText()
        {
            var card = new MagicCard(new BasicCard()
            {
                Name = "Echoes of the Past",
                ManaCost = "{4}{U}",
                OracleText = "Exile target creature you control. Return that card to the battlefield under your control at the beginning of the next end step.\nFlashback 3U",
                Type = "Instant"
            });

            Assert.True(false);
        }

        [Fact]
        public void CorrectManaCostsInText2()
        {
            var card = new MagicCard(new BasicCard()
            {
                Name = "Echoes of the Past",
                ManaCost = "{4}{U}",
                OracleText = "Exile target creature you control. Return that card to the battlefield under your control at the beginning of the next end step.\nFlashback {3U}",
                Type = "Instant"
            });

            Assert.True(false);
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
        public void CreatureWithFlashbackGetsUnearth()
        {
            var card = new MagicCard(new BasicCard()
            {
                Name = "Memory Amulet",
                ManaCost = "{2}",
                OracleText = "Flashback {2}, Sacrifice Memory Amulet: Target player shuffles their graveyard into their library, then draws a card.",
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
            Assert.Equal("\nUnearth {2}, Sacrifice Memory Amulet\nWhen Memory Amulet enters the battlefield, target player shuffles their graveyard into their library, then draws a card.", card.RawOracleText);
        }

        [Fact]
        public void CreatureWithFlashbackGetsUnearth2()
        {
            var card = new MagicCard(new BasicCard()
            {
                Name = "Temporal Alchemist",
                ManaCost = "{1}{U}",
                OracleText = "Flashback {2}{U}: Tap or untap target permanent. Activate this ability only any time you could cast a sorcery.",
                Type = "Creature - Human Wizard"
            });

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
            Assert.Equal("\nUnearth {2}{U}\nWhen Temporal Alchemist enters the battlefield, tap or untap target permanent. Activate this ability only any time you could cast a sorcery.", card.RawOracleText);
        }

        [Fact]
        public void CreatureWithFlashbackGetsUnearth3()
        {
            var card = new MagicCard(new BasicCard()
            {
                Name = "Soulbound Keeper",
                ManaCost = "{2}{B}",
                OracleText = "Flashback {3}{B}{B}{B} (You may cast this card from your graveyard for its flashback cost. Then exile it.)\nWhen Soulbound Keeper enters the battlefield, each player discards a card.",
                Type = "Creature - Zombie"
            });

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
            Assert.Equal("When Soulbound Keeper enters the battlefield, each player discards a card.\nUnearth {3}{B}{B}{B}", card.RawOracleText);
        }

        [Fact]
        public void CreatureWithFlashbackGetsUnearth4()
        {
            var card = new MagicCard(new BasicCard()
            {
                Name = "Mindwrought Construct",
                ManaCost = "{3}{U}",
                OracleText = "Flashback {3}{U}, Sacrifice an artifact: Return Mindwrought Construct from your graveyard to the battlefield.",
                Type = "Artifact Creature - Construct"
            });

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
            Assert.Equal("\nUnearth {3}{U}, Sacrifice an artifact\nWhen Mindwrought Construct enters the battlefield, return Mindwrought Construct from your graveyard to the battlefield.", card.RawOracleText);
        }

        [Fact]
        public void CreatureWithFlashbackGetsUnearth5()
        {
            var card = new MagicCard(new BasicCard()
            {
                Name = "Goblin Schemer",
                ManaCost = "{1}{R}",
                OracleText = "Flashback {R}{R}: Choose one - Goblin Schemer deals 2 damage to target creature; or Goblin Schemer gains haste and menace until end of turn.",
                Type = "Creature - Goblin Rogue"
            });

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
            Assert.Equal("\nUnearth {R}{R}\nWhen Goblin Schemer enters the battlefield, choose one - Goblin Schemer deals 2 damage to target creature; or Goblin Schemer gains haste and menace until end of turn.", card.RawOracleText);
        }

        [Fact]
        public void CreatureWithFlashbackGetsUnearth6()
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
            Assert.Equal("When Dreadmaw Stalker enters the battlefield, you may discard a card. If you do, target creature gets -2/-2 until end of turn.\nUnearth {4}{B}{B}", card.RawOracleText);
        }

        [Fact]
        public void CreatureWithFlashbackGetsUnearth7()
        {
            var card = new MagicCard(new BasicCard()
            {
                Name = "Unburied Champion",
                ManaCost = "{2}{B}",
                OracleText = "When Unburied Champion enters the battlefield or attacks, you may exile target creature card from a graveyard. If a creature card is exiled this way, Unburied Champion gets +1/+1 until end of turn.\nFlashback {3}{B}{B}{B}",
                Type = "Creature - Zombie Warrior"
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
            Assert.Equal("When Unburied Champion enters the battlefield or attacks, you may exile target creature card from a graveyard. If a creature card is exiled this way, Unburied Champion gets +1/+1 until end of turn.\nUnearth {3}{B}{B}{B}\n", card.RawOracleText);
        }

        [Fact]
        public void CreatureWithFlashbackGetsUnearth8()
        {
            var card = new MagicCard(new BasicCard()
            {
                Name = "Ethereal Sorceress",
                ManaCost = "{2}{U}{U}",
                OracleText = "Flashback {4}{U}{U}{U}: When Ethereal Sorceress enters the battlefield, you may return target instant or sorcery card from your graveyard to your hand. If you do, whenever you cast that card this turn, copy it. You may choose new targets for the copy.",
                Type = "Creature - Spirit Wizard"
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
            Assert.Equal("\nUnearth {4}{U}{U}{U}\nWhen Ethereal Sorceress enters the battlefield, you may return target instant or sorcery card from your graveyard to your hand. If you do, whenever you cast that card this turn, copy it. You may choose new targets for the copy.", card.RawOracleText);
        }
    }
}
