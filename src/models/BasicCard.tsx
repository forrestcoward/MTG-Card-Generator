export interface BasicCard {
  name: string,
  manaCost: string,
  typeLine: string,
  type: string,
  text: string,
  oracleText: string,
  power: number,
  toughness: number,
  colorIdentity: string
  pt: string,
  flavorText: string,
  rarity: string,
  imageUrl: string,
  temporaryImageUrl: string,
  userPrompt: string,
  explanation: string,
  funnyExplanation: string,
  id: string // Database id for lookup.
  alternativeImageUrls: string[]
  url: string
}
