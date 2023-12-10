using Newtonsoft.Json;
using System;

namespace MTG.CardGenerator.Models
{
    public class CardBattle
    {
        [JsonProperty("victories")]
        public int Victories { get; set; }
        [JsonProperty("defeats")]
        public int Defeats { get; set; }
        [JsonProperty("mostRecent")]
        public DateTime MostRecent { get; set; }
    }
}
