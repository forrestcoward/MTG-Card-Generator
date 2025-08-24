using Newtonsoft.Json;

namespace MTG.CardGenerator.Models
{
    /// <summary>
    /// Represents a user record in the database.
    /// </summary>
    public class User
    {
        [JsonProperty("id")]
        public string Id => UserSubject;
        [JsonProperty("userName")]
        public string UserName { get; set; }
        [JsonProperty("userSubject")]
        public string UserSubject { get; set; }
        [JsonProperty("numberOfCardsGenerated")]
        public long NumberOfCardsGenerated { get; set; } = 0;
        [JsonProperty("numberOfCardsRated")]
        public long NumberOfCardsRated { get; set; } = 0;
        [JsonProperty("lastActiveTime")]
        public DateTime? LastActiveTime { get; set; } = null;
        [JsonProperty("isAdmin")]
        public bool IsAdmin { get; set; } = false;
        [JsonProperty("openAIApiKey")]
        public string OpenAIAPIKey { get; set; }
        [JsonProperty("dailySpendLimit")]
        public double DailySpendLimit { get; set; } = -1.0;
        [JsonProperty("totalCostOfCardsGeneratedToday")]
        public double TotalCostOfCardsGeneratedToday { get; set; } = 0.0;
        [JsonProperty("totalCostOfCardsGenerated")]
        public double TotalCostOfCardsGenerated { get; set; } = 0;
        [JsonProperty("numberOfFreeCardsGeneratedToday")]
        public int NumberOfFreeCardsGeneratedToday { get; set; } = 0;
        [JsonProperty("allowedFreeCardGenerationsPerDay")]
        public int AllowedFreeCardGenerationsPerDay { get; set; } = -1;
        [JsonProperty("_etag")]
        public string ETag { get; set; }
    }

    /// <summary>
    /// Represents user information we attach to card generation records so we know who generated the card.
    /// </summary>
    public class CardUserMetadata
    {
        [JsonProperty("userName")]
        public string UserName { get; set; }
        [JsonProperty("userSubject")]
        public string UserSubject { get; set; }
    }
}
