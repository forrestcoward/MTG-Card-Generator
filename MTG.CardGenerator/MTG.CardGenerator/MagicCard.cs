using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace MTG.CardGenerator
{
    public enum ColorIdentity
    {
        White,
        Blue,
        Black,
        Green,
        Red,
        Colorless,
        Azorius,
        Dimir,
        Rakdos,
        Gruul,
        Selesnya,
        Orzhov,
        Izzet,
        Golgari,
        Boros,
        Simic,
        ThreePlusColored,
        Unknown
    }
    public enum CardType
    {
        Creature,
        Instant,
        Sorcery,
        Enchantment,
        Artifact,
        Land,
        Unknown
    }

    public enum CardProperty
    {
        Flashback,
        Unearth,
        GainsEffectWhenCastFromGraveyard,
        Unknown,
    }

    // Defines the rules needed for a CardProperty with respect to a MagicCard.
    public class CardPropertyRule
    {
        public CardProperty CardProperty { get; set; }
        public string[] IncludesRules { get; set; } = Array.Empty<string>();
        public string MechanicMatchRule { get; set; } = string.Empty;

        // The legal card types for this rule e.g. A card with flashback cannot be a creature.
        public CardType[] LegalTypes { get; set; } = Array.Empty<CardType>();

        // The set of properties a card must have if it is has this rule e.g. a card that gains an effect when cast from the graveyard should have a keyword, like flashback, that allows it to be cast from the graveyard.
        public CardProperty[] MustHaveProperties { get; set; } = Array.Empty<CardProperty>();

        // Tests if the rule applies to the line of oracle text.
        public static bool Applies(ParsedOracleTextLine cardTextLine, CardPropertyRule rule)
        {
            foreach (var stringMatch in rule.IncludesRules)
            {
                var match = stringMatch.Replace("{name}", cardTextLine.ParentCard.Name);
                if (cardTextLine.ParentCard.RawOracleText.ToLower().Contains(match.ToLower()))
                {
                    return true;
                }
            }

            if (!string.IsNullOrWhiteSpace(rule.MechanicMatchRule))
            {
                if (cardTextLine.Mechanic.ToLower() == rule.MechanicMatchRule.ToLower())
                {
                    return true;
                }
            }

            return false;
        }

        public static CardPropertyRule[] WellKnownPropertyRules = new CardPropertyRule[]
        {
            new CardPropertyRule()
            {
                CardProperty = CardProperty.GainsEffectWhenCastFromGraveyard,
                IncludesRules = new string[]
                {
                    "If you cast this spell from your graveyard",
                    "If this spell was cast from your graveyard",
                    "If {name} was cast from your graveyard"
                },
                MustHaveProperties = new[] { CardProperty.Flashback, CardProperty.Unearth }
            },
            new CardPropertyRule()
            {
                CardProperty = CardProperty.Flashback,
                MechanicMatchRule = "Flashback",
                LegalTypes = new[] { CardType.Instant, CardType.Sorcery }
            },
            new CardPropertyRule()
            {
                CardProperty = CardProperty.Unearth,
                MechanicMatchRule = "Unearth",
                LegalTypes= new[] { CardType.Artifact, CardType.Creature }
            },
        };
    }

    /// <summary>
    /// Represents a line of oracle text, including which properties CardProperties it was able to extract.
    /// </summary>
    public class ParsedOracleTextLine
    {
        public static Regex CardRulesRegex = new(@"(?<mechanicName>[a-zA-Z -.0-9/’']+)?[ ]?[—-]?(?<cost>((\{[0123456789UWGBRT]\}|[a-zA-Z 1-9]*)[,]?[ ]?)*)[:,.]?[ ]?(?<effect>.*)");

        public MagicCard ParentCard { get; }
        public string Mechanic { get; }
        public string Cost { get; }
        public string Effect { get; }
        public CardPropertyRule[] CardPropertyRules { get; }
        public CardProperty[] Properties { get; }

        public ParsedOracleTextLine(string oracleTextLine, MagicCard parentCard)
        {
            ParentCard = parentCard;
            var match = CardRulesRegex.GetNamedGroupsMatches(oracleTextLine);
            Mechanic = match["mechanicName"].Trim();
            Cost = match["cost"].Trim();
            Effect = match["effect"].Trim();

            CardPropertyRules = CardPropertyRule.WellKnownPropertyRules.Where(rule => CardPropertyRule.Applies(this, rule)).ToArray();
            Properties = CardPropertyRules.Select(criteria => criteria.CardProperty).ToArray();
        }
    }

    /// <summary>
    /// Represents a modified Magic: The Gathering card we return to the user.
    /// </summary>
    public class MagicCard
    {
        public string Name { get; }
        public string ManaCost { get; set; }
        public string TypeLine { get; }
        [JsonConverter(typeof(StringEnumConverter))]
        public CardType Type { get; set; }
        public string RawOracleText { get; }
        public string ModifiedOracleText { get; set; }
        public string FlavorText { get; }
        public string Rarity { get; }
        [JsonProperty("pt")]
        public string PowerAndToughness { get; set; }
        public int Power { get; set; }
        public int Toughness { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ColorIdentity ColorIdentity { get; set; }

        [JsonIgnore]
        public ParsedOracleTextLine[] ParsedOracleTextLines { get; }

        [JsonIgnore]
        // A list of keywords that were parsed. This is not exhaustive.
        public CardProperty[] Properties { get; }

        [JsonIgnore]
        public List<CardPropertyRule> ViolatedRules { get; }

        public MagicCard(OpenAIMagicCard card)
        {
            Name = card.Name;
            if (!string.IsNullOrWhiteSpace(card.ManaCost))
            {
                ManaCost = card.ManaCost;
            }
            else
            {
                ManaCost = string.Empty;
            }
            RawOracleText = card.OracleText;
            FlavorText = card.FlavorText;
            Rarity = card.Rarity;
            ColorIdentity = GetColorIdentity(card.ManaCost);
            PowerAndToughness = card.PowerAndToughness;
            TypeLine = card.Type;

            ParsedOracleTextLines = RawOracleText.Split('\n').Select(oracleTextLine => new ParsedOracleTextLine(oracleTextLine, this)).ToArray();

            Properties = ParsedOracleTextLines.SelectMany(x => x.Properties).Distinct().ToArray();

            ViolatedRules = new List<CardPropertyRule>();

            foreach (var parsedOracleTextLine in ParsedOracleTextLines)
            {
                foreach (var rule in parsedOracleTextLine.CardPropertyRules)
                {
                    if (!rule.LegalTypes.Contains(Type))
                    {
                        // Card contains a property that is not allowed on its type.
                        // e.g. flashback is not allowed on creatures.
                        ViolatedRules.Add(rule);
                    }

                    if (!rule.MustHaveProperties.Any(mustHaveProperty => !Properties.Contains(mustHaveProperty)))
                    {
                        // This rule requires a property not present on the rest of the card.
                        ViolatedRules.Add(rule);
                    }
                }
            }
        }

        private static ColorIdentity GetColorIdentity(string manaCostTokens) {
            var white = false;
            var blue = false;
            var black = false;
            var red = false;
            var green = false;

            if (manaCostTokens.Contains("{W}")) white = true;
            if (manaCostTokens.Contains("{U}")) blue = true;
            if (manaCostTokens.Contains("{B}")) black = true;
            if (manaCostTokens.Contains("{R}")) red = true;
            if (manaCostTokens.Contains("{G}")) green = true;

            if (white && !blue && !black && !red && !green) return ColorIdentity.White;
            if (!white && blue && !black && !red && !green) return ColorIdentity.Blue;
            if (!white && !blue && black && !red && !green) return ColorIdentity.Black;
            if (!white && !blue && !black && red && !green) return ColorIdentity.Red;
            if (!white && !blue && !black && !red && green) return ColorIdentity.Green;
            if (!white && !blue && !black && !red && !green) return ColorIdentity.Colorless;

            if (white && blue && !black && !red && !green) return ColorIdentity.Azorius;
            if (!white && blue && black && !red && !green) return ColorIdentity.Dimir;
            if (!white && !blue && black && red && !green) return ColorIdentity.Rakdos;
            if (!white && !blue && !black && red && green) return ColorIdentity.Gruul;
            if (white && !blue && !black && !red && green) return ColorIdentity.Selesnya;
            if (white && !blue && black && !red && !green) return ColorIdentity.Orzhov;
            if (!white && blue && !black && red && !green) return ColorIdentity.Izzet;
            if (!white && !blue && black && !red && green) return ColorIdentity.Golgari;
            if (white && !blue && !black && red && !green) return ColorIdentity.Boros;
            if (!white && blue && !black && !red && green) return ColorIdentity.Simic;

            return ColorIdentity.ThreePlusColored;
        }
    }
}
