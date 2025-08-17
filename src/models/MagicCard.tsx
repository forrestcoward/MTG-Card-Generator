import { getBaseUrl, getRandomInt } from "../Utility"
import { BasicCard } from "./BasicCard"
import { ColorIdentity } from "./ColorIdentity"

export class MagicCard {
  name: string
  manaCost: string
  typeLine: string
  type: string
  text: string
  oracleText: string
  power: number
  toughness: number
  _colorIdentity: string
  pt: string
  flavorText: string
  rarity: string
  imageUrl: string
  temporaryImageUrl: string
  setNumberDisplay: string
  id: string // DOM id.
  databaseId: string // Database id for lookup.
  userPrompt: string
  explanation: string
  funnyExplanation: string
  alternativeImageUrls: string[]
  url: string

  constructor(card: BasicCard) {
    this.name = card.name
    this.manaCost = card.manaCost
    this.type = card.type
    this.typeLine = card.typeLine
    this.text = card.text
    this.oracleText = card.oracleText
    this.power = card.power
    this.toughness = card.toughness
    this._colorIdentity = ""
    this.flavorText = card.flavorText
    this.pt = card.pt
    this.rarity = card.rarity
    this.imageUrl = card.imageUrl
    this.temporaryImageUrl = card.temporaryImageUrl
    this.setNumberDisplay = getRandomInt(0, 451) + "/" + 451
    this.id = getRandomInt(0, 1000000000).toString()
    this.userPrompt = card.userPrompt
    this.explanation = card.explanation
    this.funnyExplanation = card.funnyExplanation
    this.databaseId = card.id
    this.alternativeImageUrls = card.alternativeImageUrls
    this.url = card.url
  }

  static clone(card: MagicCard, sameId: boolean): MagicCard {
    let newCard = new MagicCard(card)
    newCard.setNumberDisplay = card.setNumberDisplay
    if (sameId) {
      newCard.id = card.id
    } else {
      newCard.id = getRandomInt(0, 1000000000).toString()
    }

    newCard.databaseId = card.databaseId
    return newCard
  }

  get manaCostTokens(): string[] {
    return this.getManaCostTokens(this.manaCost);
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

  get getCardUrl(): string {
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
}
