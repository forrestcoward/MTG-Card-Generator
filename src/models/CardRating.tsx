import { BasicCard } from "./BasicCard"

export interface CardRating {
  numberOfVotes: number,
  totalScore: number,
  averageScore: number,
}

export interface GenerationMetadata {
  model: string,
}

export interface UserRecord {
  username: string,
}

export interface CardGenerationRecords {
  cards: CardGenerationRecord[]
}

export interface CardGenerationRecord {
  id: string // Database id for lookup.
  user: UserRecord
  card: BasicCard
  rating: CardRating
  generationMetadata: GenerationMetadata
}
