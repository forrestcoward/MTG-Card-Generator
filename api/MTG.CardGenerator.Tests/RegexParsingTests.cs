using Xunit;

namespace MTG.CardGenerator.Tests
{
    public class RegexParseTests
    {
        [Fact]
        public void EffectWithMultipleCosts()
        {
            var match = CardTextLine.CardRulesRegex.GetNamedGroupsMatches("{1}, {T}, Sacrifice Basilica Skullbomb: Draw a card.");
            Assert.Equal(string.Empty, match["mechanicName"]);
            Assert.Equal("{1}, {T}, Sacrifice Basilica Skullbomb", match["cost"]);
            Assert.Equal("Draw a card.", match["effect"]);

            match = CardTextLine.CardRulesRegex.GetNamedGroupsMatches("SacAndDraw - {1}, {T}, Sacrifice Basilica Skullbomb: Draw a card.");
            Assert.Equal("SacAndDraw - ", match["mechanicName"]);
            Assert.Equal("{1}, {T}, Sacrifice Basilica Skullbomb", match["cost"]);
            Assert.Equal("Draw a card.", match["effect"]);
        }

        [Fact]
        public void EffectWithCostWithNoMechanic()
        {
            var match = CardTextLine.CardRulesRegex.GetNamedGroupsMatches("{2}{R}{R}, Exile Flameblast Phoenix from your graveyard: Create a red Instant card named Flameblast with \"Flameblast deals 4 damage to any target.\" Then exile Flameblast. Activate this ability only any time you could cast a sorcery.");
            Assert.Equal(string.Empty, match["mechanicName"]);
            Assert.Equal("{2}{R}{R}, Exile Flameblast Phoenix from your graveyard", match["cost"]);
            Assert.Equal("Create a red Instant card named Flameblast with \"Flameblast deals 4 damage to any target.\" Then exile Flameblast. Activate this ability only any time you could cast a sorcery.", match["effect"]);
        }

        [Fact]
        public void EffectWithCostWithMechanic()
        {
            var match = CardTextLine.CardRulesRegex.GetNamedGroupsMatches("Flashback {2}{U}{U}, Sacrifice Memory Amulet: Target player shuffles their graveyard into their library, then draws a card.");
            Assert.Equal("Flashback ", match["mechanicName"]);
            Assert.Equal("{2}{U}{U}, Sacrifice Memory Amulet", match["cost"]);
            Assert.Equal("Target player shuffles their graveyard into their library, then draws a card.", match["effect"]);
        }

        [Fact]
        public void SimpleEffect()
        {
            var match = CardTextLine.CardRulesRegex.GetNamedGroupsMatches("Draw a card.");
            Assert.Equal("Draw a card.", match["mechanicName"]);
            Assert.Equal(string.Empty, match["cost"]);
            Assert.Equal(string.Empty, match["effect"]);
        }


        [Fact]
        public void SimpleEffect2()
        {
            var card = "When Ambulatory Edifice enters the battlefield, you may pay 2 life. When you do, target creature gets -1/-1 until end of turn.";
            var match = CardTextLine.CardRulesRegex.GetNamedGroupsMatches(card);
            Assert.Equal(card, match["mechanicName"]);
            Assert.Equal(string.Empty, match["cost"]);
            Assert.Equal(string.Empty, match["effect"]);
        }

        [Fact(Skip = "Valid test, but correct behavior not implemented")]
        public void EffectWithoutManaCost()
        {
            var card = "Discard two cards: Create a 2/2 black Zombie creature token.";
            var match = CardTextLine.CardRulesRegex.GetNamedGroupsMatches(card);
            Assert.Equal(string.Empty, match["mechanicName"]);
            Assert.Equal("Discard two cards", match["cost"]);
            Assert.Equal("Create a 2/2 black Zombie creature token.", match["effect"]);
        }

        [Fact]
        public void FlashbackMechanic()
        {
            var card = "Flashback {4}{R}";
            var match = CardTextLine.CardRulesRegex.GetNamedGroupsMatches(card);
            Assert.Equal("Flashback ", match["mechanicName"]);
            Assert.Equal("{4}{R}", match["cost"]);
            Assert.Equal(string.Empty, match["effect"]);

            card = "Flashback {4}{R} (You may cast this card from the graveyard for its flashback cost)";
            match = CardTextLine.CardRulesRegex.GetNamedGroupsMatches(card);
            Assert.Equal("Flashback ", match["mechanicName"]);
            Assert.Equal("{4}{R} ", match["cost"]);
            Assert.Equal("(You may cast this card from the graveyard for its flashback cost)", match["effect"]);
        }

        [Fact]
        public void FlashbackMechanic2()
        {
            var card = "Flashback—{1}{G}, Pay 3 life. (You may cast this card from your graveyard for its flashback cost. Then exile it.)";
            var match = CardTextLine.CardRulesRegex.GetNamedGroupsMatches(card);
            Assert.Equal("Flashback", match["mechanicName"]);
            Assert.Equal("{1}{G}, Pay 3 life", match["cost"]);
            Assert.Equal("(You may cast this card from your graveyard for its flashback cost. Then exile it.)", match["effect"]);
        }

        [Fact(Skip = "Valid test, but correct behavior not implemented")]
        public void EffectThatReferencesAManaCost()
        {
            var card = "Draw two cards, then exile this card from your graveyard. You may cast this card from exile by paying {3} rather than paying its mana cost.";
            var match = CardTextLine.CardRulesRegex.GetNamedGroupsMatches(card);
            Assert.Equal(card, match["mechanicName"]);
            Assert.Equal(string.Empty, match["cost"]);
            Assert.Equal(string.Empty, match["effect"]);
        }
    }
}