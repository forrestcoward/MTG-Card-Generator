import React from "react";

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

enum ColorIdentity {
  White = "w",
  Blue = "u",
  Black = "b",
  Green = "g",
  Red = "r",
  Colorless = "c",
  Azorius = "uw",
  Dimir = "ub",
  Rakdos = "br",
  Gruul = "rg",
  Selesnya = "wg",
  Orzhov = "wb",
  Izzet = "ur",
  Golgari = "bg",
  Boros = "rw",
  Simic = "ug",
  ThreePlusColored = "mc",
  Unknown = "unknown",
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

const manaTokens : string[] = ["{G}", "{W}", "{U}", "{B}", "{R}", "{0}", "{1}", "{2}", "{3}", 
"{4}", "{5}", "{6}", "{7}", "{8}", "{9}", "{10}", "{C}", "{T}", "{X}"]

const manaTokenToCssCharacter : Record<string, string> = {
  "{G}": "g",
  "{W}": "w",
  "{U}": "u",
  "{B}": "b",
  "{R}": "r",
  "{0}": "0",
  "{1}": "1",
  "{2}": "2",
  "{3}": "3",
  "{4}": "4",
  "{5}": "5",
  "{6}": "6",
  "{7}": "7",
  "{8}": "8",
  "{9}": "9",
  "{10}": "10",
  "{C}": "c",
  "{T}": "tap",
  "{X}": "x",
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
  setNumberDisplay: string

  constructor(card: BasicCard) {
    this.name = card.name
    this.manaCost = card.manaCost
    this.type = card.type
    this.typeLine = card.typeLine
    this.text = card.text
    this.rawOracleText = card.rawOracleText
    this.modifiedOracleText = card.modifiedOracleText
    this.power = 0
    this.toughness = 0
    this._colorIdentity = ""
    this.flavorText = card.flavorText
    this.pt = card.pt
    this.rarity = card.rarity
    this.imageUrl = card.imageUrl
    this.setNumberDisplay = getRandomInt(0, 451) + "/" + 451
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
  get cardFrameTypeLineClassName()  {
    return "frame-type-line frame-" + this.manaCssClassPostfix + " frame-type-line-" + this.manaCssClassPostfix
  }

  get cardFrameHeaderClassName() {
    return "frame-header frame-" + this.manaCssClassPostfix + " frame-header-" + this.manaCssClassPostfix
  }

  get cardFrameTextBoxClassName() {
    return "frame-text-box frame-text-box-" + this.manaCssClassPostfix
  }
  
  get cardFrameArtClassName() {
    return "frame-art frame-art-" + this.manaCssClassPostfix
  }

  get rarityDisplay() {
    var token = ""
    switch (this.rarity.toLocaleLowerCase()) {
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
    if (type.includes("legendary creature") || type.includes("creature")) return CardType.Creature
    if (type == "instant") return CardType.Instant;
    if (type == "sorcery") return CardType.Sorcery;
    if (type.includes("artifact")) return CardType.Artifact;
    if (type.includes("enchantment")) return CardType.Enchantment;
    if (type.includes("land")) return CardType.Land;
    return CardType.Unknown;
  }

  get textDisplay() {
    return this.rawOracleText.split('\n').map(line => this.addManaHtmlToCardTextLine(line));
  }

  // Gets the CSS postfix to create class names that are differentiated by color identity.
  private get manaCssClassPostfix() {
    return this.colorIdentity;
  }

  // Transforms text representations of mana symbols from OpenAI into HTML that can render those mana symbols using the mana project (https://github.com/andrewgioia/mana).
  private addManaHtmlToCardTextLine(line: string) {
    manaTokens.forEach(token => {
      line = line.replaceAll(token, `<i class=\"${MagicCard.getManaClassName(token)}\"></i>`)
    })

    return line
  }

  // Gets the matching mana project (https://github.com/andrewgioia/mana) CSS token for <i></i> based on the mana token string.
  static getManaClassName(manaToken: string) {
    return `ms ms-${manaTokenToCssCharacter[manaToken]}`
  }
}

function getRandomInt(min: number, max: number) {
  min = Math.ceil(min);
  max = Math.floor(max);
  return Math.floor(Math.random() * (max - min) + min);
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
              <div className={card.cardFrameHeaderClassName}>
                <h1 className="name">{card.name}</h1>
                <div className="mana-symbols">
                  {card.manaCostTokens.map((manaCostToken, i) => (
                    <i key={card.name + "-manaToken-"+ i} className={MagicCard.getManaClassName(manaCostToken) + " manaCost"} id="mana-icon"></i>
                  ))}
                </div>
              </div>
              <img className={card.cardFrameArtClassName} src={card.imageUrl} />
              <div className={card.cardFrameTypeLineClassName}>
                <h1 className="type">{card.typeLine}</h1>
                <div className="mana-symbols">
                  <i className="ms ms-dfc-ignite" id="mana-icon"></i>
                </div>
              </div>
              <div className={card.cardFrameTextBoxClassName}>
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