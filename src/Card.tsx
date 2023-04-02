import React from "react";

declare module 'react' {
  interface HTMLAttributes<T> extends AriaAttributes, DOMAttributes<T> {
    // extends React's HTMLAttributes
    custom?: string;
  }
}

enum ColorIdentity {
  White,
  Blue,
  Black,
  Green,
  Red,
  Colorless,
  Azorius,
  Dimir,
  Rakdos,
  Gruul,
  Selesnya,
  Orzhov,
  Izzet,
  Golgari,
  Boros,
  Simic,
  ThreePlusColored,
  Unknown
}

enum CardType {
  Creature,
  Instant,
  Sorcery,
  Enchantment,
  Artifact,
  Land,
  Unknown
}

export interface BasicCard {
  name: string,
  manaCost: string,
  typeLine: string,
  type: string,
  text: string,
  rawOracleText: string,
  modifiedOracleText: string,
  power: number,
  toughness: number,
  colorIdentity: string
  pt: string,
  flavorText: string,
  rarity: string,
  imageUrl: string,
}

export class MagicCard {
  name: string
  manaCost: string
  typeLine: string
  type: string
  text: string
  rawOracleText: string
  modifiedOracleText: string
  power: number
  toughness: number
  _colorIdentity: string
  pt: string
  flavorText: string
  rarity: string
  imageUrl: string

  constructor(card: BasicCard) {
    this.name = card.name
    this.manaCost = card.manaCost;
    this.type = card.type;
    this.typeLine = card.typeLine;
    this.text = card.text;
    this.rawOracleText = card.rawOracleText;
    this.modifiedOracleText = card.modifiedOracleText;
    this.power = 0;
    this.toughness = 0;
    this._colorIdentity = "";
    this.flavorText = card.flavorText;
    this.pt = card.pt;
    this.rarity = card.rarity;
    this.imageUrl = "";
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

  get colorIdentity(): ColorIdentity {
    var white = false;
    var blue = false;
    var black = false;
    var red = false;
    var green = false;
  
    var manaCostTokens = this.manaCostTokens;
    if (!this.manaCostTokens) {
      return ColorIdentity.Unknown
    }
  
    if (manaCostTokens.includes("{W}")) { white = true }
    if (manaCostTokens.includes("{U}")) { blue = true }
    if (manaCostTokens.includes("{B}")) { black = true }
    if (manaCostTokens.includes("{R}")) { red = true }
    if (manaCostTokens.includes("{G}")) { green = true }
  
    if (white && !blue && !black && !red && !green) { return ColorIdentity.White }
    if (!white && blue && !black && !red && !green) { return ColorIdentity.Blue }
    if (!white && !blue && black && !red && !green) { return ColorIdentity.Black }
    if (!white && !blue && !black && red && !green) { return ColorIdentity.Red }
    if (!white && !blue && !black && !red && green) { return ColorIdentity.Green }
    if (!white && !blue && !black && !red && !green) { return ColorIdentity.Colorless }
  
    if (white && blue && !black && !red && !green) { return ColorIdentity.Azorius }
    if (!white && blue && black && !red && !green) { return ColorIdentity.Dimir }
    if (!white && !blue && black && red && !green) { return ColorIdentity.Rakdos }
    if (!white && !blue && !black && red && green) { return ColorIdentity.Gruul }
    if (white && !blue && !black && !red && green) { return ColorIdentity.Selesnya }
    if (white && !blue && black && !red && !green) { return ColorIdentity.Orzhov }
    if (!white && blue && !black && red && !green) { return ColorIdentity.Izzet }
    if (!white && !blue && black && !red && green) { return ColorIdentity.Golgari }
    if (white && !blue && !black && red && !green) { return ColorIdentity.Boros }
    if (!white && blue && !black && !red && green) { return ColorIdentity.Simic }
  
    return ColorIdentity.ThreePlusColored;
  }
  
  get cardDivClassName() {
    return "card-background card-background-" + this.manaCssClassPostfix
  }
  
  get rarityDisplay() {
    var token = ""
    switch (this.rarity.toLowerCase()) {
      case "common": token = "C"; break;
      case "uncommon": token = "U"; break;
      case "rare": token = "R"; break;
      case "mythic": token = "M"; break;
      case "mythic rare": token = "M"; break;
    }
    return token;
  }

  get cardType(): CardType {
    var type = this.type.toLocaleLowerCase();

    if (type.includes("legendary creature") || type.includes("creature")) {
      return CardType.Creature
    }

    if (type == "instant") {
      return CardType.Instant;
    }

    if (type == "sorcery") {
      return CardType.Sorcery;
    }

    if (type.includes("artifact")) {
      return CardType.Artifact;
    }

    if (type.includes("enchantment")) {
      return CardType.Enchantment;
    }

    if (type.includes("land")) {
      return CardType.Land;
    }

    return CardType.Unknown;
  }

  get setNumberDisplay() {
    return getRandomInt(0, 451) + "/" + 451
  }

  get openAIImagePrompt() {
    var prompt = this.name + ": " + this.flavorText
  
    if (this.cardType == CardType.Creature) {
      prompt = "An image of '" + this.name + "', a " + this.type + " ,  Greg Kutkowski style, digital art";
      //prompt = "An image of '" + card.name + "' within a fantasy world with some action in the same style as the painting Starry Night."
    }
  
    if (this.cardType == CardType.Instant || this.cardType == CardType.Sorcery) {
      prompt = "An image of '" + this.name + "' that illustrates the following: " + this.flavorText + ". Greg Rutkowski style, digital art"
    }
  
    if (this.cardType == CardType.Enchantment || this.cardType == CardType.Artifact) {
      prompt = "An image of '" + this.name + "' that illustrates the following: " + this.flavorText + ". Greg Rutkowski style, digital art"
    }
  
    return prompt;
  }

  get textDisplay() {
    var lines = this.rawOracleText.split('\n')
    for (var i = 0; i < lines.length; i++) {
      lines[i] = this.addManaHtmlToCardTextLine(lines[i])
    }
  
    return lines;
  }

  // Gets the CSS postfix to create class names that are differentiated by color identity.
  private get manaCssClassPostfix() {
    var identity = this.colorIdentity
    var token = ""
    switch (identity) {
      case ColorIdentity.White: token = "w"; break;
      case ColorIdentity.Blue: token = "u"; break;
      case ColorIdentity.Black: token = "b"; break;
      case ColorIdentity.Red: token = "r"; break;
      case ColorIdentity.Green: token = "g"; break;
      case ColorIdentity.Azorius: token = "uw"; break;
      case ColorIdentity.Dimir: token = "ub"; break;
      case ColorIdentity.Rakdos: token = "br"; break;
      case ColorIdentity.Gruul: token = "rg"; break;
      case ColorIdentity.Selesnya: token = "wg"; break;
      case ColorIdentity.Orzhov: token = "wb"; break;
      case ColorIdentity.Izzet: token = "ur"; break;
      case ColorIdentity.Golgari: token = "bg"; break;
      case ColorIdentity.Boros: token = "rw"; break;
      case ColorIdentity.Simic: token = "gu"; break;
      case ColorIdentity.Colorless: token = "c"; break;
      case ColorIdentity.ThreePlusColored: token = "mc"; break;
    }
  
    return token;
  }

  // Transforms text representations of mana symbols from OpenAI into HTML that can render those mana symbols using the mana project (https://github.com/andrewgioia/mana).
  private addManaHtmlToCardTextLine(line: string) {
    line = line.replaceAll("{G}", `<i class=\"${MagicCard.getManaClassName("{G}")}\"></i>`)
    line = line.replaceAll("{W}", `<i class=\"${MagicCard.getManaClassName("{W}")}\"></i>`)
    line = line.replaceAll("{U}", `<i class=\"${MagicCard.getManaClassName("{U}")}\"></i>`)
    line = line.replaceAll("{B}", `<i class=\"${MagicCard.getManaClassName("{B}")}\"></i>`)
    line = line.replaceAll("{R}", `<i class=\"${MagicCard.getManaClassName("{R}")}\"></i>`)
    line = line.replaceAll("{0}", `<i class=\"${MagicCard.getManaClassName("{0}")}\"></i>`)
    line = line.replaceAll("{1}", `<i class=\"${MagicCard.getManaClassName("{1}")}\"></i>`)
    line = line.replaceAll("{2}", `<i class=\"${MagicCard.getManaClassName("{2}")}\"></i>`)
    line = line.replaceAll("{3}", `<i class=\"${MagicCard.getManaClassName("{3}")}\"></i>`)
    line = line.replaceAll("{4}", `<i class=\"${MagicCard.getManaClassName("{4}")}\"></i>`)
    line = line.replaceAll("{5}", `<i class=\"${MagicCard.getManaClassName("{5}")}\"></i>`)
    line = line.replaceAll("{6}", `<i class=\"${MagicCard.getManaClassName("{6}")}\"></i>`)
    line = line.replaceAll("{7}", `<i class=\"${MagicCard.getManaClassName("{7}")}\"></i>`)
    line = line.replaceAll("{8}", `<i class=\"${MagicCard.getManaClassName("{8}")}\"></i>`)
    line = line.replaceAll("{9}", `<i class=\"${MagicCard.getManaClassName("{9}")}\"></i>`)
    line = line.replaceAll("{10}", `<i class=\"${MagicCard.getManaClassName("{10}")}\"></i>`)
    line = line.replaceAll("{C}", `<i class=\"${MagicCard.getManaClassName("{C}")}\"></i>`)
    line = line.replaceAll("{T}", `<i class=\"${MagicCard.getManaClassName("{T}")}\"></i>`)
    return line
  }

  // Gets the matching mana project (https://github.com/andrewgioia/mana) CSS token for <i></i> based on the mana token string.
  static getManaClassName(manaToken: string) {
    if (manaToken == "{G}") { return "ms ms-g" }
    if (manaToken == "{W}") { return "ms ms-w" }
    if (manaToken == "{U}") { return "ms ms-u" }
    if (manaToken == "{B}") { return "ms ms-b" }
    if (manaToken == "{R}") { return "ms ms-r" }
    if (manaToken == "{0}") { return "ms ms-0" }
    if (manaToken == "{1}") { return "ms ms-1" }
    if (manaToken == "{2}") { return "ms ms-2" }
    if (manaToken == "{3}") { return "ms ms-3" }
    if (manaToken == "{4}") { return "ms ms-4" }
    if (manaToken == "{5}") { return "ms ms-5" }
    if (manaToken == "{6}") { return "ms ms-6" }
    if (manaToken == "{7}") { return "ms ms-7" }
    if (manaToken == "{8}") { return "ms ms-8" }
    if (manaToken == "{9}") { return "ms ms-9" }
    if (manaToken == "{10}") { return "ms ms-10" }
    if (manaToken == "{11}") { return "ms ms-11" }
    if (manaToken == "{12}") { return "ms ms-12" }
    if (manaToken == "{C}") { return "ms ms-c" }
    if (manaToken == "{T}") { return "ms ms-tap" }
    return ""
  }
}
function getRandomInt(min: number, max: number) {
  min = Math.ceil(min);
  max = Math.floor(max);
  return Math.floor(Math.random() * (max - min) + min); // The maximum is exclusive and the minimum is inclusive
}

interface CardDisplayProps {
  card: MagicCard;
}

export class CardDisplay extends React.Component<CardDisplayProps> {
  constructor(props: CardDisplayProps) {
    super(props);
  }

  render() {
    let card = this.props.card;
    return (
      <div>
        <div className="card-container">
          <div className={card.cardDivClassName}>
            <div className="card-frame">
              <div className="frame-header">
                <h1 className="name">{card.name}</h1>
                <div className="mana-symbols">
                  {card.manaCostTokens.map((manaCostToken, i) => (
                    <i key={card.name + "-manaToken-"+ i} className={MagicCard.getManaClassName(manaCostToken)} id="mana-icon"></i>
                  ))}
                </div>
              </div>
              <img className="frame-art" src={card.imageUrl} />
              <div className="frame-type-line">
                <h1 className="type">{card.typeLine}</h1>
                <div className="mana-symbols">
                  <i className="ms ms-dfc-ignite" id="mana-icon"></i>
                </div>
              </div>
              <div className="frame-text-box">
                <div className="description ftb-inner-margin">
                  {card.textDisplay.map((line, i) => (
                    <p key={card.name + "-text-" + i} dangerouslySetInnerHTML={{ __html: line }}>
                    </p>
                  ))}
                </div>
                <p className="description">
                </p>
                <p className="flavour-text">
                  {card.flavorText}
                </p>
                <div className="power-and-toughness-frame">
                  <p className="power-and-toughness">
                    {card.pt}
                  </p>
                </div>
              </div>
              <div className="frame-bottom-info inner-margin">
                <div className="fbi-left">
                  <p>{card.setNumberDisplay} {card.rarityDisplay}</p>
                  <p>OpenAI &#x2022; <img className="paintbrush" src="https://image.ibb.co/e2VxAS/paintbrush_white.png" alt="paintbrush icon" />Custom Magic</p>
                </div>
                <div className="fbi-center"></div>
                <div className="fbi-right">
                  &#x99; &amp; &#169; 2023 Chat GPT Turbo
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    )
  }
}