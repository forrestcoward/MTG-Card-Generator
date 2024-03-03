using Newtonsoft.Json;
using System;

namespace MTG.CardGenerator.Models
{
    public class CardRating
    {
        [JsonProperty("numberOfVotes")]
        public int NumberOfVotes { get; set; }
        [JsonProperty("totalScore")]
        public long TotalScore { get; set; }
        [JsonProperty("averageScore")]
        public double AverageScore { get; set; }
        [JsonProperty("mostRecent")]
        public DateTime MostRecent { get; set; }
    }

    public class GenerationMetaData
    {
        [JsonProperty("userPrompt")]
        public string UserPrompt { get; set; }
        [JsonProperty("systemPrompt")]
        public string SystemPrompt { get; set; }
        [JsonProperty("imagePrompt")]
        public string ImagePrompt { get; set; }
        [JsonProperty("temperature")]
        public double Temperature { get; set; }
        [JsonProperty("tokensUsed")]
        public int TokensUsed { get; set; }
        [JsonProperty("model")]
        public string Model { get; set; }
        [JsonProperty("imageSize")]
        public string ImageSize { get; set; }
        [JsonProperty("imageModel")]
        public string ImageModel { get; set; }
        [JsonProperty("imageStyle")]
        public string ImageStyle { get; set; }
        [JsonProperty("openAIResponse")]
        public string OpenAIResponse { get; set; }
        [JsonProperty("includeExplanation")]
        public bool IncludeExplanation { get; set; }
        [JsonProperty("extraCreative")]
        public bool ExtraCreative { get; set; }
        [JsonProperty("userSuppliedKey")]
        public bool UserSupplied { get; set; }
        [JsonProperty("estimatedCost")]
        public double EstimatedCost { get; set; }
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonProperty("host")]
        public string Host { get; set; }
        [JsonProperty("origin")]
        public string Origin { get; set; }
    }

    /// <summary>
    /// Represents a card record in the database.
    /// </summary>
    public class CardGenerationRecord
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("generationMetadata")]
        public GenerationMetaData GenerationMetadata { get; set; }
        [JsonProperty("user")]
        public CardUserMetadata User { get; set; }
        [JsonProperty("card")]
        public MagicCard Card { get; set; }
        [JsonProperty("rating")]
        public CardRating Rating { get; set; }
        [Obsolete("No longer used but exists on old records. Newer records will have Card set.")]
        [JsonProperty("magicCards")]
        public MagicCard[] MagicCards { get; set; }
    }

    /// <summary>
    /// Represents a card record returned through the API.
    /// </summary>
    public class CardGenerationRecordResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("card")]
        public MagicCardResponse Card { get; set; }
        [JsonProperty("rating")]
        public CardRating Rating { get; set; }

        public static CardGenerationRecordResponse FromDatabaseRecord(CardGenerationRecord cardGenerationRecord)
        {
            MagicCardResponse card;
            if (cardGenerationRecord.Card == null)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                card = new MagicCardResponse(cardGenerationRecord.MagicCards[0], cardGenerationRecord.Id);
#pragma warning restore CS0618 // Type or member is obsolete
            }
            else
            {
                // All newer records will have just a single card set.
                card = new MagicCardResponse(cardGenerationRecord.Card, cardGenerationRecord.Id);
            }

            return new CardGenerationRecordResponse
            {
                // Pass the id onto each card, too.
                Id = cardGenerationRecord.Id,
                Card = card,
                Rating = cardGenerationRecord.Rating,
            };
        }
    }
}
