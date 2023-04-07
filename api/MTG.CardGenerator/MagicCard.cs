﻿using Newtonsoft.Json;
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
        [JsonProperty("name")]
        public string Name { get; }
        [JsonProperty("manaCost")]
        public string ManaCost { get; set; }
        [JsonProperty("typeLine")]
        public string TypeLine { get; }
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("type")]
        public CardType Type { get; set; }
        [JsonProperty("rawOracleText")]
        public string RawOracleText { get; }
        [JsonProperty("modifiedOracleText")]
        public string ModifiedOracleText { get; set; }
        [JsonProperty("flavorText")]
        public string FlavorText { get; }
        [JsonProperty("rarity")]
        public string Rarity { get; }
        [JsonProperty("pt")]
        public string PowerAndToughness { get; set; }
        [JsonProperty("power")]
        public string Power { get; set; }
        [JsonProperty("toughness")]
        public string Toughness { get; set; }

        [JsonProperty("colorIdentity")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ColorIdentity ColorIdentity { get; set; }

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonIgnore]
        public string OpenAIImagePrompt
        {
            get
            {
                var prompt = $"{Name}: {FlavorText}";

                if (Type == CardType.Creature)
                {
                    prompt = $"An image of '{Name}', a '{Type}', Greg Kutkowski style, digital art";
                }

                if (Type == CardType.Instant || Type == CardType.Sorcery || Type == CardType.Enchantment || Type == CardType.Artifact)
                {
                    prompt = $"An image of '{Name}', that illustrates the following description: {FlavorText}. The image should be Greg Rutkowski style, digital art.";
                }

                if (Type == CardType.Enchantment || Type == CardType.Artifact)
                {
                    prompt = $"An image of '{Name}' that illustrates the following description: {FlavorText}. The image should be Greg Rutkowski style, digital art.";
                }

                return prompt;
            }
        }

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
            ManaCost = !string.IsNullOrWhiteSpace(card.ManaCost) ? card.ManaCost : string.Empty;
            RawOracleText = card.OracleText;
            FlavorText = card.FlavorText;
            Rarity = card.Rarity;
            ColorIdentity = GetColorIdentity(card.ManaCost);
            TypeLine = card.Type;
            Type = GetCardType(TypeLine);
            ImageUrl = string.Empty;

            if (string.IsNullOrWhiteSpace(card.PowerAndToughness) &&
                (!string.IsNullOrWhiteSpace(card.Power) && !string.IsNullOrWhiteSpace(card.Toughness)))
            {
                PowerAndToughness = $"{card.Power}/{card.Toughness}";
            }
            else if (!string.IsNullOrWhiteSpace(card.PowerAndToughness))
            {
                PowerAndToughness = card.PowerAndToughness;
            }

            if (!string.IsNullOrWhiteSpace(card.Power))
            {
                Power = card.Power;
            }

            if (!string.IsNullOrWhiteSpace(card.Toughness))
            {
                Toughness = card.Toughness;
            }

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

        private static CardType GetCardType(string typeLine)
        {
            var type = typeLine.ToLower();

            if (type.Contains("legendary creature") || type.Contains("creature"))
            {
                return CardType.Creature;
            }

            if (type == "instant")
            {
                return CardType.Instant;
            }

            if (type == "sorcery")
            {
                return CardType.Sorcery;
            }

            if (type.Contains("artifact"))
            {
                return CardType.Artifact;
            }

            if (type.Contains("enchantment"))
            {
                return CardType.Enchantment;
            }

            if (type.Contains("land"))
            {
                return CardType.Land;
            }

            return CardType.Unknown;
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