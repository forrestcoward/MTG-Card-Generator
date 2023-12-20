export interface User {
  userName: string;
  numberOfCardsGenerated: number;
  numberOfCardsRated: number;
  numberOfFreeCardsGeneratedToday: number;
  allowedFreeCardGenerationsPerDay: number;
}