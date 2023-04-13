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
    public enum CardPropertyOrKeyword
    {
        Flashback,
        Unearth,
        Kicker,
        GainsEffectWhenKicked,
        GainsEffectWhenCastFromGraveyard,
        Unknown,
    }

    // Defines the rules needed for a CardProperty with respect to a MagicCard.
    public class CardRule
    {
        public CardPropertyOrKeyword PropertyOrKeyword { get; set; }
        public string[] IncludesRules { get; set; } = Array.Empty<string>();
        public string MechanicMatchRule { get; set; } = string.Empty;
        public string[] ReminderText { get; set; } = Array.Empty<string>();

        // Adds a mechanic to the card, with a rule and line as context to the violation.
        Action<MagicCard, CardRule, CardTextLine> AddMechanicToCard = null;
        
        // Fixes a card, given the rule it violated and line the violation occured.
        public Action<MagicCard, CardRule, CardTextLine> FixViolation = null;

        // The legal card types for this rule e.g. A card with flashback cannot be a creature.
        public CardType[] LegalTypes { get; set; } = Array.Empty<CardType>();

        // The set of properties a card must have if it is has this rule e.g. a card that gains an effect when cast from the graveyard should have a keyword, like flashback, that allows it to be cast from the graveyard.
        public CardPropertyOrKeyword[] MustHaveProperties { get; set; } = Array.Empty<CardPropertyOrKeyword>();

        // Tests if the rule applies to the line of oracle text.
        public static bool Applies(CardTextLine cardTextLine, CardRule rule)
        {
            foreach (var stringMatch in rule.IncludesRules)
            {
                var match = stringMatch.Replace("{name}", cardTextLine.ParentCard.Name);
                if (cardTextLine.ParentCard.RawOracleText.ToLower().Contains(match.ToLower(), StringComparison.OrdinalIgnoreCase))
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

        public static CardRule[] CardRules = new CardRule[]
        {
            new CardRule()
            {
                PropertyOrKeyword = CardPropertyOrKeyword.GainsEffectWhenCastFromGraveyard,
                IncludesRules = new string[]
                {
                    "If you cast this spell from your graveyard",
                    "If you cast this spell a graveyard",
                    "If you cast this card from your graveyard",
                    "If you cast this card a graveyard",
                    "If you cast {name} from your graveyard",
                    "If you cast {name} from a graveyard",
                    "If this spell was cast from your graveyard",
                    "If this spell was cast from a graveyard",
                    "If this card was cast from your graveyard",
                    "If this card was cast from a graveyard",
                    "If {name} was cast from your graveyard",
                    "If {name} was cast from a graveyard",
                    "If {name} is cast from your graveyard",
                    "If {name} is cast from a graveyard",
                    "You may cast this spell from your graveyard by paying its flashback cost",
                    "You may cast this card from your graveyard by paying its flashback cost",
                    "You may cast {name} from your graveyard by paying its flashback cost",
                    "You may cast it for its flashback cost"
                },
                MustHaveProperties = new[] { CardPropertyOrKeyword.Flashback },
                FixViolation = (card, rule, line) =>
                {
                    // Cards which gain an effect when cast from the graveyard should have Flashback.
                    if (card.Type == CardType.Instant || card.Type == CardType.Sorcery)
                    {
                        // Add Flashback.
                        CardRules.First(rule => rule.PropertyOrKeyword == CardPropertyOrKeyword.Flashback).AddMechanicToCard(card, rule, line);
                    }
                }
            },
            new CardRule()
            {
                PropertyOrKeyword = CardPropertyOrKeyword.GainsEffectWhenKicked,
                IncludesRules = new string[]
                {
                    "If {name} was kicked",
                    "If {name} is kicked",
                    "You may pay the kicker cost",
                    "If you paid the kicker cost",
                    "If this spell was kicked",
                    "If it was kicked"
                },
                MustHaveProperties = new[] { CardPropertyOrKeyword.Kicker },
                FixViolation = (card, rule, line) =>
                {
                    // Add Kicker.
                    CardRules.First(rule => rule.PropertyOrKeyword == CardPropertyOrKeyword.Kicker).AddMechanicToCard(card, rule, line);
                }
            },
            new CardRule()
            {
                PropertyOrKeyword = CardPropertyOrKeyword.Kicker,
                MechanicMatchRule = "Kicker",
                ReminderText = new string[]
                {
                    "(You may pay an additional {cost} as you cast this spell.)",
                    "(You may {cost} in addition to any other costs as you cast this spell)"
                },
                AddMechanicToCard = (card, rule, line) =>
                {
                    var newText = $"Kicker {card.ManaCost}\n";
                    card.RawOracleText = newText + card.RawOracleText;
                },
            },
            new CardRule()
            {
                PropertyOrKeyword = CardPropertyOrKeyword.Flashback,
                MechanicMatchRule = "Flashback",
                LegalTypes = new[] { CardType.Instant, CardType.Sorcery },
                ReminderText = new string[]
                {
                    "Exile this card from your graveyard: Cast it this turn for the its flashback cost",
                    "(You may cast this card from your graveyard for its flashback cost. Then exile it.)"
                },
                AddMechanicToCard = (card, rule, line) =>
                {
                    var newText = $"\nFlashback {card.ManaCost}";
                    card.RawOracleText += newText;
                },
                FixViolation = (card, rule, line) =>
                {
                    // If this is a creature or artifact that has Flashback, give it Unearth instead.
                    if (card.Type == CardType.Creature || card.Type == CardType.Artifact)
                    {
                        var textLines = card.ParsedOracleTextLines.Where(x => !x.Properties.Contains(CardPropertyOrKeyword.Flashback));
                        card.RawOracleText = string.Join('\n', textLines.Select(x => x.ToString()));
                        CardRules.First(rule => rule.PropertyOrKeyword == CardPropertyOrKeyword.Unearth).AddMechanicToCard(card, rule, line);
                    }
                }
            },
            new CardRule()
            {
                PropertyOrKeyword = CardPropertyOrKeyword.Unearth,
                MechanicMatchRule = "Unearth",
                LegalTypes= new[] { CardType.Artifact, CardType.Creature },
                AddMechanicToCard = (card, rule, line) =>
                {
                    // Transform Flashback into Unearth.
                    if (line.Properties.Contains(CardPropertyOrKeyword.Flashback)) {
                        if (rule.ReminderText.Any(reminder => line.CostAndEffectWithoutManaCost.Contains(reminder, StringComparison.OrdinalIgnoreCase)))
                        {
                            var newText = $"\nUnearth {line.Cost}";
                            card.RawOracleText += newText;
                        }
                        else if (!string.IsNullOrWhiteSpace(line.Effect) && !line.Effect.StartsWith($"When {card.Name} enters the battlefield", StringComparison.OrdinalIgnoreCase))
                        {
                            var newText = $"\nUnearth {line.Cost}\nWhen {card.Name} enters the battlefield, {line.Effect.FirstCharToLowerCase()}";
                            card.RawOracleText += newText;
                        }
                        else
                        {
                            var newText = $"\nUnearth {line.Cost}\n{line.Effect}";
                            card.RawOracleText += newText;
                        }
                    }
                }
            },
        };
    }

    /// <summary>
    /// Represents a line of oracle text, including which properties CardProperties it was able to extract.
    /// </summary>
    public class CardTextLine
    {
        private class CardCost
        {
            public string[] Costs { get; }

            public string ManaCost
            {
                get
                {
                    return Costs.FirstOrDefault(x => x.StartsWith("{") && x.EndsWith("}"));
                }
            }

            public string[] CostsWithoutManaCost
            {
                get
                {
                    return Costs.Where(cost => cost != ManaCost).ToArray();
                }
            }

            public CardCost(string costs)
            {
                Costs = costs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }

        public static Regex CardRulesRegex = new(@"(?<mechanicName>[a-zA-Z -.0-9/’']+)?[ ]?[—-]?(?<cost>((\{[0123456789UWGBRT]\}|[a-zA-Z 1-9]*)[,]?[ ]?)*)[:,.]?[ ]?(?<effect>.*)");

        public MagicCard ParentCard { get; }
        public string RawOracleText { get; }
        public string Mechanic { get; }
        public string Cost { get; }
        public string Effect { get; }

        public string CostAndEffectWithoutManaCost
        {
            get
            {
                var costs = new CardCost(Cost);
                if (costs.CostsWithoutManaCost.Any())
                {
                    return $"{string.Join(',', costs.CostsWithoutManaCost)}: {Effect}";
                }
                else
                {
                    return Effect;
                }
            }
        }

        // The rules that apply to this line of text.
        public CardRule[] Rules { get; }
        // The properties that apply to this line of text.
        public CardPropertyOrKeyword[] Properties { get; }

        public CardTextLine(string oracleTextLine, MagicCard parentCard = null)
        {
            RawOracleText = oracleTextLine;
            ParentCard = parentCard;
            var match = CardRulesRegex.GetNamedGroupsMatches(oracleTextLine);
            Mechanic = match["mechanicName"].Trim();
            Cost = match["cost"].Trim();
            Effect = match["effect"].Trim();

            Rules = CardRule.CardRules.Where(rule => CardRule.Applies(this, rule)).ToArray();
            Properties = Rules.Select(criteria => criteria.PropertyOrKeyword).ToArray();
        }

        public override string ToString()
        {
            return RawOracleText;
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
        public string RawOracleText { get; set; }
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
        public CardTextLine[] ParsedOracleTextLines { get; }

        [JsonIgnore]
        // A list of keywords that were parsed. This is not exhaustive.
        public CardPropertyOrKeyword[] Properties { get; }

        [JsonIgnore]
        public List<CardRule> ViolatedRules { get; }

        public static Regex UnbracketedManaCostRegex = new(@"(?<cost>(\s|^)[123456789WUBRG]+(\s|$|:))");

        public static string CorrectUnbracketedManaCosts(string oracleText)
        {
            var unbracketedManaCosts = UnbracketedManaCostRegex.GetAllMatches(oracleText);
            foreach (var unbracketedManaCost in unbracketedManaCosts)
            {
                if (unbracketedManaCost.IsNumericOrWhitespace())
                {
                    // If the detected mana cost is only a number, so no ['W', 'U', 'B', 'R', 'G'], then we do not really know if it is just a number or a mana cost.
                    continue;
                }

                // Otherwise bracket the mana symbols with '{' and '}'.
                oracleText = oracleText.Replace(unbracketedManaCost, unbracketedManaCost.BracketManaSymbols());
            }

            return oracleText;
        }

        public MagicCard(BasicCard card)
        {
            Name = card.Name;
            TypeLine = card.Type;
            Type = GetCardType(TypeLine);
            ManaCost = !string.IsNullOrWhiteSpace(card.ManaCost) ? card.ManaCost : string.Empty;
            ManaCost = FixManaCost(ManaCost);
            RawOracleText = CorrectUnbracketedManaCosts(card.OracleText);
            FlavorText = card.FlavorText;
            Rarity = card.Rarity;
            ColorIdentity = GetColorIdentity(ManaCost);
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

            ParsedOracleTextLines = RawOracleText.Split('\n').Select(oracleTextLine => new CardTextLine(oracleTextLine, this)).ToArray();

            Properties = ParsedOracleTextLines.SelectMany(x => x.Properties).Distinct().ToArray();

            ViolatedRules = new List<CardRule>();

            var violationToTextLine = new Dictionary<CardRule, CardTextLine>();

            // Run the rules.
            foreach (var parsedOracleTextLine in ParsedOracleTextLines)
            {
                foreach (var rule in parsedOracleTextLine.Rules)
                {
                    if (rule.LegalTypes.Any() &&
                        !rule.LegalTypes.Contains(Type))
                    {
                        // Card contains a property that is not allowed on its type.
                        // e.g. flashback is not allowed on creatures.
                        ViolatedRules.Add(rule);
                        violationToTextLine[rule] = parsedOracleTextLine;
                    }

                    if (rule.MustHaveProperties.Any() && 
                        rule.MustHaveProperties.Any(mustHaveProperty => !Properties.Contains(mustHaveProperty)))
                    {
                        // This rule requires a property not present on the rest of the card.
                        // e.g. a card gains an additional effect when cast from the graveyard but does not have flashback or unearth.
                        ViolatedRules.Add(rule);
                        violationToTextLine[rule] = parsedOracleTextLine;
                    }
                }
            }

            // Correct the rules.
            foreach (var rule in ViolatedRules)
            {
                if (rule.FixViolation != null)
                {
                    rule.FixViolation(this, rule, violationToTextLine[rule]);
                }
            }
        }

        private static CardType GetCardType(string typeLine)
        {
            if (string.IsNullOrWhiteSpace(typeLine))
            {
                return CardType.Unknown;
            }

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

        private string FixManaCost(string manaCost)
        {
            // Lands should have no cost, and GPT frequently creates land cards with cost.
            if (Type == CardType.Land)
            {
                return string.Empty;
            }

            // Generated mana costs often have no brackets at all. If so, just add brackets around each mana character.
            if (!manaCost.Contains('{') && !manaCost.Contains('}'))
            {
                return manaCost.BracketManaSymbols();
            }

            if (!manaCost.IsBracketed())
            {
                manaCost = manaCost.Replace("}", "");
                manaCost = manaCost.Replace("{", "");
                return manaCost.BracketManaSymbols();
            }

            return manaCost;
        }
    }
}
