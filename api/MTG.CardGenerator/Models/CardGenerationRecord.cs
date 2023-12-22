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
        [JsonProperty("userSuppliedKey")]
        public bool UserSupplied { get; set; }
        [JsonProperty("estimatedCost")]
        public double EstimatedCost { get; set; }
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonProperty("host")]
        public string Host { get; set; }
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
        [JsonProperty("magicCards")]
        public MagicCard[] MagicCards { get; set; }
        [JsonProperty("card")]
        public MagicCard Card => MagicCards[0];
        [JsonProperty("rating")]
        public CardRating Rating { get; set; }
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
            return new CardGenerationRecordResponse
            {
                Id = cardGenerationRecord.Id,
                // Pass the id onto each card, too.
                Card = new MagicCardResponse(cardGenerationRecord.Card, cardGenerationRecord.Id),
                Rating = cardGenerationRecord.Rating,
            };
        }
    }
}
