using Newtonsoft.Json;

namespace MTG.CardGenerator.Models
{
    // Represents a generated Magic: The Gathering card from OpenAI's LLM.
    public class BasicCard
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("manaCost")]
        public string ManaCost { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("text")]
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
        // Reason why the LLM genearted the card.
        [JsonProperty("explanation")]
        public string Explanation { get; set; }
        // A funny explanation of why the LLM generated the card.
        [JsonProperty("funnyExplanation")]
        public string FunnyExplanation { get; set; }
        [JsonProperty("userPrompt")]
        // The user prompt that generated this card.
        public string UserPrompt { get; set; }
    }
}
