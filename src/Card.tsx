import React from "react";

declare module 'react' {
  interface HTMLAttributes<T> extends AriaAttributes, DOMAttributes<T> {
    // extends React's HTMLAttributes
    custom?: string;
  }
}

export interface Card {
  name: string,
  manaCost: string,
  manaCostTokens: string[],
  type: string,
  text: string,
  flavorText: string,
  pt: string,
  rarity: string,
  reason: string,
  funnyReason: string,
  imageUrl: string,
  upgradedCard: Card,
  updateExplanation: string,
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
  ThreePlusColored
}

function getCardColorIdentity(card: Card) {
  var white = false;
  var blue = false;
  var black = false;
  var red = false;
  var green = false;

  if (card.manaCostTokens.includes("{W}")) { white = true }
  if (card.manaCostTokens.includes("{U}")) { blue = true }
  if (card.manaCostTokens.includes("{B}")) { black = true }
  if (card.manaCostTokens.includes("{R}")) { red = true }
  if (card.manaCostTokens.includes("{G}")) { green = true }

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

function getCardRarityDisplay(card: Card) {
  var token = ""
  switch (card.rarity.toLowerCase()) {
    case "common": token = "C"; break;
    case "uncommon": token = "U"; break;
    case "rare": token = "R"; break;
    case "mythic": token = "M"; break;
    case "mythic rare": token = "M"; break;
  }
  return token;
}

function getCardSetNumberDisplay(max: number = 451) {
  return getRandomInt(0, max) + "/" + max
}

function getRandomInt(min: number, max: number) {
  min = Math.ceil(min);
  max = Math.floor(max);
  return Math.floor(Math.random() * (max - min) + min); // The maximum is exclusive and the minimum is inclusive
}

function splitCardText(card: Card) {
  var lines = card.text.split('\n')
  for (var i = 0; i < lines.length; i++) {
    lines[i] = manaCostTokenToManaSymbolHtml(lines[i]);
  }

  return lines;
}

function getCardManaToken(card: Card) {
  var identity = getCardColorIdentity(card);
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

function manaCostTokenToManaSymbolHtml(line: string) {
  line = line.replaceAll("{G}", "<i class=\"ms ms-g\"></i>")
  line = line.replaceAll("{W}", "<i class=\"ms ms-w\"></i>")
  line = line.replaceAll("{U}", "<i class=\"ms ms-u\"></i>")
  line = line.replaceAll("{B}", "<i class=\"ms ms-b\"></i>")
  line = line.replaceAll("{R}", "<i class=\"ms ms-r\"></i>")
  line = line.replaceAll("{0}", "<i class=\"ms ms-0\"></i>")
  line = line.replaceAll("{1}", "<i class=\"ms ms-1\"></i>")
  line = line.replaceAll("{2}", "<i class=\"ms ms-2\"></i>")
  line = line.replaceAll("{3}", "<i class=\"ms ms-3\"></i>")
  line = line.replaceAll("{4}", "<i class=\"ms ms-4\"></i>")
  line = line.replaceAll("{5}", "<i class=\"ms ms-5\"></i>")
  line = line.replaceAll("{6}", "<i class=\"ms ms-6\"></i>")
  line = line.replaceAll("{7}", "<i class=\"ms ms-7\"></i>")
  line = line.replaceAll("{8}", "<i class=\"ms ms-8\"></i>")
  line = line.replaceAll("{9}", "<i class=\"ms ms-9\"></i>")
  line = line.replaceAll("{10}", "<i class=\"ms ms-10\"></i>")
  line = line.replaceAll("{C}", "<i class=\"ms ms-c\"></i>")
  line = line.replaceAll("{T}", "<i class=\"ms ms-tap\"></i>")
  return line
}

function manaCostTokenToManaSymbolClass(manaToken: string) {
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
  return ""
}

export function getCardImagePrompt(card: Card) {
  var prompt = card.name + ": " + card.flavorText

  if (card.type.toLowerCase().includes("legendary creature") || card.type.toLowerCase().includes("creature")) {
    prompt = "An image of '" + card.name + "', a " + card.type + " ,  Greg Kutkowski style, digital art";
    //prompt = "An image of '" + card.name + "' within a fantasy world with some action in the same style as the painting Starry Night."
  }

  if (card.type.toLowerCase().includes("instant") || card.type.toLowerCase().includes("sorcery")) {
    prompt = "An image of '" + card.name + "' that illustrates the following: " + card.flavorText + ". Greg Rutkowski style, digital art"
  }

  if (card.type.toLowerCase().includes("enchantment") || card.type.toLowerCase().includes("artifact")) {
    prompt = "An image of '" + card.name + "' that illustrates the following: " + card.flavorText + ". Greg Rutkowski style, digital art"
  }

  return prompt;
}

export function setCardManaCostTokens(card: Card) {
  // Bug: sometimes GPT returns just "1G" or "2RR" for mana cost and not "{1}{G}" or "{2}{R}{R}".
  // if (!card.manaCost.startsWith("{") && !card.manaCost.endsWith("}")) {
  //   card.manaCost = "{" + card.manaCost + "}"
  // }
  var r: RegExp = /\{(.*?)\}/g
  var match = card.manaCost.match(r)
  card.manaCostTokens = []
  match?.forEach(m => card.manaCostTokens.push(m))
}

function getCardBackgroundClassName(card: Card) {
  var className = "card-background card-background-";
  var token = getCardManaToken(card);
  return className + token;
}

interface CardDisplayProps {
  card: Card;
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
          <div className={getCardBackgroundClassName(card)}>
            <div className="card-frame">
              <div className="frame-header">
                <h1 className="name">{card.name}</h1>
                <div className="mana-symbols">
                  {card.manaCostTokens.map(manaCostToken => (
                    <i className={manaCostTokenToManaSymbolClass(manaCostToken)} id="mana-icon"></i>
                  ))}
                </div>
              </div>
              <img className="frame-art" src={card.imageUrl} />
              <div className="frame-type-line">
                <h1 className="type">{card.type}</h1>
                <div className="mana-symbols">
                  <i className="ms ms-dfc-ignite" id="mana-icon"></i>
                </div>
              </div>
              <div className="frame-text-box">
                <p className="description ftb-inner-margin">
                  {splitCardText(card).map(line => (
                    <p dangerouslySetInnerHTML={{ __html: line }}>
                    </p>
                  ))}
                </p>
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
                  <p>{getCardSetNumberDisplay()} {getCardRarityDisplay(card)}</p>
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