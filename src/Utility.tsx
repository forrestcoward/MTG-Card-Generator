import { MagicCard } from "./Card";

// Dynamically adjust card sizes
export function setCardContainerSize(minWidth: number = 450) {
  const cardContainerClass = '.card-container';
  const cardContainerRule = findCSSRule(cardContainerClass);

  // Scale the card entirely based on the card width.
  const vw = Math.max(document.documentElement.clientWidth || 0, window.innerWidth || 0)
  const cardWidth = Math.min(minWidth, vw - 16)
  const cardHeight = ((cardWidth * 3.6) / 2.5);

  if (cardContainerRule) {
    cardContainerRule.style.width  = `${cardWidth}px`;
    cardContainerRule.style.height = `${cardHeight}px`;
  }
}

export function findCSSRule(selector: string): CSSStyleRule | null {
  const sheets = document.styleSheets;
  const sheetsLength = sheets.length;

  for (let i = 0; i < sheetsLength; i++) {
    let sheet = sheets[i];
    let rules: CSSRuleList;

    try {
      rules = sheet.cssRules || sheet.rules;
    } catch (e) {
      continue;
    }

    const rulesLength = rules.length;

    for (let j = 0; j < rulesLength; j++) {
      let rule = rules[j];

      if (rule instanceof CSSStyleRule && rule.selectorText === selector) {
        return rule;
      }
    }
  }

  return null;
}

export function getRandomInt(min: number, max: number) {
  min = Math.ceil(min);
  max = Math.floor(max);
  return Math.floor(Math.random() * (max - min) + min);
}

export function logCard(card: MagicCard) {
  console.log(card)
  console.log(JSON.stringify({
    "name": card.name,
    "pt": card.pt,
    "type": card.typeLine,
    "manaCost": card.manaCost,
    "text": card.rawOracleText,
    "rarity": card.rarity,
  }))
}