using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace MTG.CardGenerator.Models
{
    internal static class APIResponses
    {
        internal static string GetCardResponse(CardGenerationRecord card)
        {
            return JsonConvert.SerializeObject(CardGenerationRecordResponse.FromDatabaseRecord(card));
        }

        internal static string GetCardsResponse(IEnumerable<CardGenerationRecord> cards)
        {
            return JsonConvert.SerializeObject(cards.Select(x => CardGenerationRecordResponse.FromDatabaseRecord(x)));
        }
    }
}
