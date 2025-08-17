import { CardFace } from "./CardFace";
import { MagicCard } from "./MagicCard";

export interface TkCard {
  cardFaces: CardFace[];
  layout?: string;
  frameEffects?: string[];
  set?: string;
  setIcon?: string;
  rarity?: string;
  lang?: string;
  artist?: string;
  collectorNumber?: string;
  copyright?: string;

  id: string // DOM id.
  databaseId: string // Database id for lookup.
  userPrompt: string
  explanation: string
  funnyExplanation: string
  url: string
}

export function MagicCardToTkCard(card : MagicCard): TkCard {
  return {
    cardFaces: [
      {
        name: card.name,
        manaCost: card.manaCost,
        type: card.type,
        typeLine: card.typeLine,
        power: card.power,
        toughness: card.toughness,
        pt: card.pt,
        oracleText: card.oracleText,
        flavorText: card.flavorText,
        imageUrl: card.imageUrl,
        temporaryImageUrl: card.temporaryImageUrl,
      }
    ],

    id: card.id,
    databaseId: card.databaseId,
    userPrompt: card.userPrompt,
    explanation: card.explanation,
    funnyExplanation: card.funnyExplanation,
    url: card.url,
    rarity: card.rarity,
    collectorNumber: card.collectorNumber,
    layout: '',
    frameEffects: [''],
    copyright: '',
    setIcon: '',
    set: 'GEN',
    artist: 'AI Generated',
    lang: 'en'
  };
}
