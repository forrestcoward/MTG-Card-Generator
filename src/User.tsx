export interface User {
  userName: string;
  numberOfCardsGenerated: number;
  numberOfCardsRated: number;
  numberOfFreeCardsGeneratedToday: number;
  numberOfFreeCardsAllowedPerDay: number;
  totalCostOfCardsGeneratedToday: number;
  remainingDailySpendLimit: number;
  dailySpendLimit: number;
}
