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
        public double totalCostOfCardsGenerated { get; set; } = 0;
        public int numberOfFreeCardsGeneratedToday { get; set; } = 0;
        public int allowedFreeCardGenerationsPerDay { get; set; } = -1;
        public bool isAdmin { get; set; } = false;
    }
}
