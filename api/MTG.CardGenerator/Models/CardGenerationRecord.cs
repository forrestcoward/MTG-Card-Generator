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

    public class CardBattle
    {
        [JsonProperty("victories")]
        public int Victories { get; set; }
        [JsonProperty("defeats")]
        public int Defeats { get; set; }
        [JsonProperty("mostRecent")]
        public DateTime MostRecent { get; set; }
    }

    public class CardGenerationRecord
    {
        public string id { get; set; }
        public GenerationMetaData generationMetadata { get; set; }
        public UserMeta user { get; set; }
        public MagicCard[] magicCards { get; set; }
        public CardBattle cardBattle { get; set; }
        public CardRating rating { get; set; }
    }

    public class GenerationMetaData
    {
        public string userPrompt { get; set; }
        public string systemPrompt { get; set; }
        public string imagePrompt { get; set; }
        public double temperature { get; set; }
        public int tokensUsed { get; set; }
        public string model { get; set; }
        public string imageSize { get; set; }
        public string imageModel { get; set; }
        public string openAIResponse { get; set; }
        public bool includeExplanation { get; set; }
        public bool userSupliedKey { get; set; }
        public double estimatedCost { get; set; }
        public DateTime timestamp { get; set; }
    }

    public class UserMeta
    {
        public string userName { get; set; }
        public string userSubject { get; set; }
    }
}
