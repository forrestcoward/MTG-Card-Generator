using System.Text.Json.Serialization;

namespace MTG.CardGenerator.Models
{
    public class ReplicatePredictionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("input")]
        public Dictionary<string, object> Input { get; set; }

        [JsonPropertyName("logs")]
        public string Logs { get; set; }

        [JsonPropertyName("output")]
        public object Output { get; set; }

        [JsonPropertyName("data_removed")]
        public bool DataRemoved { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("status")]
        public ReplicatePredictionStatus Status { get; set; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("urls")]
        public ReplicatePredictionUrls Urls { get; set; }
    }

    public class ReplicatePredictionUrls
    {
        [JsonPropertyName("cancel")]
        public string Cancel { get; set; }

        [JsonPropertyName("get")]
        public string Get { get; set; }

        [JsonPropertyName("stream")]
        public string Stream { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReplicatePredictionStatus
    {
        Starting,
        Processing,
        Succeeded,
        Failed,
        Canceled
    }
}