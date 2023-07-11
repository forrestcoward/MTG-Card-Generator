using System;

namespace MTG.CardGenerator.Models
{
    public class User
    {
        public string id => userSubject;
        public string userName { get; set; }
        public string userSubject { get; set; }
        public long numberOfCardsGenerated { get; set; } = 0;
        public DateTime? lastActiveTime { get; set; } = null;
        public long totalCostOfCardsGenerated { get; set; } = 0;
    }
}
