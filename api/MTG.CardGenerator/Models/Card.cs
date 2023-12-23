using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace MTG.CardGenerator.Models
{
    /// <summary>
    /// Represents a generated card from OpenAI's LLM.
    /// </summary>
    public class BasicCard
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("manaCost")]
        public string ManaCost { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("oracleText")]
        public string OracleText { get; set; }
        [JsonProperty("flavorText")]
        public string FlavorText { get; set; }
        [JsonProperty("rarity")]
        public string Rarity { get; set; }
        [JsonProperty("pt")]
        public string PowerAndToughness { get; set; }
        // Sometimes OpenAI returns power separately, despite being told to return "pt" as one field.
        [JsonProperty("power")]
        public string Power { get; set; }
        // Sometimes OpenAI returns toughness separately, despite being told to return "pt" as one field.
        [JsonProperty("toughness")]
        public string Toughness { get; set; }
        [JsonProperty("explanation")]
        public string Explanation { get; set; }
        [JsonProperty("funnyExplanation")]
        public string FunnyExplanation { get; set; }
        [JsonProperty("userPrompt")]
        public string UserPrompt { get; set; }
    }

    // Represents a generated card in the database.
    public class MagicCard
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("manaCost")]
        public string ManaCost { get; set; }
        [JsonProperty("typeLine")]
        public string TypeLine { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("type")]
        public CardType Type { get; set; }
        [JsonProperty("rawOracleText")]
        public string RawOracleText { get; set; }
        [JsonProperty("oracleText")]
        public string OracleText { get; set; }
        [JsonProperty("flavorText")]
        public string FlavorText { get; set; }
        [JsonProperty("rarity")]
        public string Rarity { get; set; }
        [JsonProperty("pt")]
        public string PowerAndToughness { get; set; }
        [JsonProperty("colorIdentity")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ColorIdentity ColorIdentity { get; set; }
        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }
        [JsonProperty("temporaryImageUrl")]
        public string TemporaryImageUrl { get; set; }
        [JsonProperty("userPrompt")]
        public string UserPrompt { get; set; }
        [JsonProperty("explanation")]
        public string Explanation { get; set; }
        [JsonProperty("funnyExplanation")]
        public string FunnyExplanation { get; set; }
    }

    /// <summary>
    /// Represents a generated card that we return through API responses.
    /// </summary>
    public class MagicCardResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("manaCost")]
        public string ManaCost { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("type")]
        public CardType Type { get; set; }
        [JsonProperty("typeLine")]
        public string TypeLine { get; set; }
        [JsonProperty("oracleText")]
        public string OracleText { get; set; }
        [JsonProperty("flavorText")]
        public string FlavorText { get; set; }
        [JsonProperty("colorIdentity")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ColorIdentity ColorIdentity { get; set; }
        [JsonProperty("rarity")]
        public string Rarity { get; set; }
        [JsonProperty("pt")]
        public string PowerAndToughness { get; set; }
        [JsonProperty("explanation")]
        public string Explanation { get; set; }
        [JsonProperty("funnyExplanation")]
        public string FunnyExplanation { get; set; }
        [JsonProperty("userPrompt")]
        public string UserPrompt { get; set; }
        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }
        [JsonProperty("temporaryImageUrl")]
        public string TemporaryImageUrl { get; set; }
        [JsonProperty("url")]
        public string Url
        {
            get
            {
                var websiteUrl = Environment.GetEnvironmentVariable(Constants.WebsiteUrl);
                if (string.IsNullOrWhiteSpace(websiteUrl))
                {
                    return string.Empty;
                }

                return $"{websiteUrl}Card?id={Id}";
            }
        }

        public MagicCardResponse(MagicCard card, string id, bool includeTemporaryImage = false)
        {
            this.Id = id;
            this.Name = card.Name;
            this.ManaCost = card.ManaCost;
            this.Type = card.Type;
            this.TypeLine = card.TypeLine;
            this.FlavorText = card.FlavorText;
            this.ColorIdentity = card.ColorIdentity;
            this.Rarity = card.Rarity;
            this.PowerAndToughness = card.PowerAndToughness;
            this.Explanation = card.Explanation;
            this.FunnyExplanation = card.FunnyExplanation;
            this.UserPrompt = card.UserPrompt.Replace(@"%20", " ");

            // The temporary image is the one provided by OpenAI and only lasts a short amount of time.
            // For all use cases except the initial generate, use the permanent image we store ourselves.
            this.ImageUrl = card.ImageUrl;
            this.TemporaryImageUrl = string.Empty;
            if (includeTemporaryImage || string.IsNullOrWhiteSpace(card.ImageUrl))
            {
                this.TemporaryImageUrl = card.TemporaryImageUrl;
            }

            this.OracleText = !string.IsNullOrWhiteSpace(card.OracleText) ? card.OracleText : card.RawOracleText;
            PostProcessOracleText();
        }

        /// <summary>
        /// Correct bad text already stored in the database.
        /// </summary>
        private void PostProcessOracleText()
        {
            // Fix incorrectly parsed new lines.
            if (this.OracleText.Contains("\\n"))
            {
                this.OracleText = this.OracleText.Replace("\\n", "\n");
            }

            // Add new lines for activiated abilities.
            this.OracleText = MagicCardParser.AddNewlineToActivatedAbilities(this.OracleText);
        }
    }
}
