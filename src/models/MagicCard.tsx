import { getBaseUrl, getRandomInt } from "../Utility"
import { BasicCard } from "./BasicCard"
import { CardFace } from "./CardFace";
import { ColorIdentity } from "./ColorIdentity"
import { setsHelper } from "../IconHelper"

export class MagicCard {
  cardFaces: CardFace[];
  layout?: string;
  frameEffects?: string[];
  set?: string;
  setIcon?: string;
  setIconUrl?: string;
  rarity: string;
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

  toBasicCard(): BasicCard {
    return {
      name: this.cardFaces[0].name,
      manaCost: this.cardFaces[0].manaCost,
      type: this.cardFaces[0].type,
      typeLine: this.cardFaces[0].typeLine,
      power: this.cardFaces[0].power,
      toughness: this.cardFaces[0].toughness,
      text: this.cardFaces[0].oracleText,
      rarity: this.rarity,
      pt: this.cardFaces[0].pt,
      oracleText: this.cardFaces[0].oracleText,
      flavorText: this.cardFaces[0].flavorText,
      imageUrl: this.cardFaces[0].imageUrl,
      temporaryImageUrl: this.cardFaces[0].temporaryImageUrl,
      id: this.id,
      userPrompt: this.userPrompt,
      explanation: this.explanation,
      funnyExplanation: this.funnyExplanation,
      alternativeImageUrls: this.cardFaces[0].alternativeImageUrls,
      url: this.url
    };
  }

  constructor(card: BasicCard, selectSetIcon: boolean = true) {
    // Parse power and toughness from pt field if individual fields are not provided.
    let power = card.power;
    let toughness = card.toughness;
    
    if (card.pt && (power === undefined || toughness === undefined)) {
      const ptMatch = card.pt.match(/^(\d+)\/(\d+)$/);
      if (ptMatch) {
        power = parseInt(ptMatch[1], 10);
        toughness = parseInt(ptMatch[2], 10);
      }
    }

    // Add the current image to the list of alternative images so it can be selected if newer images are generated.
    var alternativeImageUrls = []
    alternativeImageUrls.push(...card.alternativeImageUrls)
    if (card.imageUrl) {
      alternativeImageUrls.push(card.imageUrl);
    } else if (card.temporaryImageUrl) {
      alternativeImageUrls.push(card.temporaryImageUrl);
    }

    this.cardFaces = [
      {
        name: card.name,
        manaCost: card.manaCost,
        type: card.type,
        typeLine: card.typeLine,
        power: power,
        toughness: toughness,
        pt: card.pt,
        oracleText: card.oracleText,
        flavorText: card.flavorText,
        imageUrl: card.imageUrl,
        temporaryImageUrl: card.temporaryImageUrl,
        alternativeImageUrls: alternativeImageUrls
      }
    ];

    this.id = getRandomInt(0, 1000000000).toString()
    this.databaseId = card.id
    this.userPrompt = card.userPrompt
    this.explanation = card.explanation
    this.funnyExplanation = card.funnyExplanation
    this.url = card.url
    this.rarity = (() => {
      const normalizedRarity = card.rarity?.toLowerCase();
      if (normalizedRarity === 'mythic rare' || normalizedRarity === 'mythic') return 'mythic'
      if (normalizedRarity === 'rare') return 'rare'
      if (normalizedRarity === 'uncommon') return 'uncommon'
      if (normalizedRarity === 'common') return 'common'
      return 'common';
    })();
    this.collectorNumber = getRandomInt(0, 451).toString();
    this.layout = ''
    this.frameEffects = ['']
    this.copyright = ''

    if (selectSetIcon) {
      const setIconInfo = setsHelper.getRandomSetIconInfo(this.rarity);
      this.setIcon = setIconInfo.setCode;
      this.setIconUrl = setIconInfo.iconUrl;
    }

    this.set = 'GEN'
    this.artist = 'AI Generated'
    this.lang = 'en'
  }

  static clone(card: MagicCard, sameId: boolean): MagicCard {
    let basicCard: BasicCard = card.toBasicCard()
    const selectSetIcon = false
    let newCard = new MagicCard(basicCard, selectSetIcon)
    newCard.collectorNumber = card.collectorNumber
    newCard.setIcon = card.setIcon
    newCard.setIconUrl = card.setIconUrl
    if (sameId) {
      newCard.id = card.id
    } else {
      newCard.id = getRandomInt(0, 1000000000).toString()
    }

    newCard.databaseId = card.databaseId
    return newCard
  }

  get face(): CardFace {
    return this.cardFaces[0];
  }

  get manaCostTokens(): string[] {
    return this.getManaCostTokens(this.cardFaces[0].manaCost);
  }

  get cardUrl(): string {
    return getBaseUrl() + "/Card?id=" + this.databaseId
  }

  get colorIdentity(): ColorIdentity {
    var white = false;
    var blue = false;
    var black = false;
    var red = false;
    var green = false;
  
    var manaCostTokens = this.manaCostTokens;
    if (!this.manaCostTokens) return ColorIdentity.Unknown
    if (manaCostTokens.includes("{W}")) white = true 
    if (manaCostTokens.includes("{U}")) blue = true
    if (manaCostTokens.includes("{B}")) black = true
    if (manaCostTokens.includes("{R}")) red = true
    if (manaCostTokens.includes("{G}")) green = true
  
    if ( white && !blue && !black && !red && !green) return ColorIdentity.White
    if (!white &&  blue && !black && !red && !green) return ColorIdentity.Blue
    if (!white && !blue &&  black && !red && !green) return ColorIdentity.Black
    if (!white && !blue && !black &&  red && !green) return ColorIdentity.Red
    if (!white && !blue && !black && !red &&  green) return ColorIdentity.Green
    if (!white && !blue && !black && !red && !green) return ColorIdentity.Colorless
  
    if ( white &&  blue && !black && !red && !green) return ColorIdentity.Azorius
    if (!white &&  blue &&  black && !red && !green) return ColorIdentity.Dimir
    if (!white && !blue &&  black &&  red && !green) return ColorIdentity.Rakdos
    if (!white && !blue && !black &&  red &&  green) return ColorIdentity.Gruul
    if ( white && !blue && !black && !red &&  green) return ColorIdentity.Selesnya
    if ( white && !blue &&  black && !red && !green) return ColorIdentity.Orzhov
    if (!white &&  blue && !black &&  red && !green) return ColorIdentity.Izzet
    if (!white && !blue &&  black && !red &&  green) return ColorIdentity.Golgari
    if ( white && !blue && !black &&  red && !green) return ColorIdentity.Boros
    if (!white &&  blue && !black && !red &&  green) return ColorIdentity.Simic
  
    return ColorIdentity.ThreePlusColored;
  }

  private getManaCostTokens(tokensToParse: string): string[] {
    var r: RegExp = /\{(.*?)\}/g
    var match = tokensToParse.match(r)
    var manaCostTokens:string[] = []
    if (match) {
      match?.forEach(m => manaCostTokens.push(m))
    }
    return manaCostTokens;
  }
}
