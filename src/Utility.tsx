// Dynamically adjust card sizes.
export function setCardContainerSize(minWidth: number = 440) {
  const cardContainerClass = '.card-container'
  const cardContainerRule = findCSSRule(cardContainerClass)

  // Scale the card entirely based on the card width.
  const vw = Math.max(document.documentElement.clientWidth || 0, window.innerWidth || 0)
  const cardWidth = Math.min(minWidth, vw - 16)
  const cardHeight = ((cardWidth * 3.6) / 2.5)

  if (cardContainerRule) {
    cardContainerRule.style.width  = `${cardWidth}px`
    cardContainerRule.style.height = `${cardHeight}px`
  }

  return cardWidth
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

export function isMobileDevice() {
  var userAgent = navigator.userAgent;
  return /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini/i.test(userAgent);
}

export function getRandomInt(min: number, max: number) {
  min = Math.ceil(min);
  max = Math.floor(max);
  return Math.floor(Math.random() * (max - min) + min);
}

export function getChildrenClientOffsetHeight(element: HTMLElement, vertical: boolean = true) {
  let height = 0
  for (let i = 0; i < element.children.length; i++) {
    if (vertical) {
      height += (element.children[i] as HTMLElement).offsetHeight
    } else {
      height = Math.max(height, (element.children[i] as HTMLElement).offsetHeight)
    }
  }
  return height
}

// Adjust the text size of innerContainer so it fits a certain ratio within container.
export function adjustTextHeightBasedOnClientHeight(container: HTMLElement, innerContainer: HTMLElement, fit: number, minFontSize: number = 4) {
  let fontSize = window.getComputedStyle(innerContainer, null).getPropertyValue('font-size')
  let fontSizeFloat = parseFloat(fontSize)

  if (innerContainer.clientHeight == 0) {
    return
  }

  while (innerContainer.clientHeight / container.clientHeight < fit) {
    fontSizeFloat += .5
    innerContainer.style.fontSize = fontSizeFloat + "px"
  }

  while (innerContainer.clientHeight / container.clientHeight > fit) {
    fontSizeFloat -= .5
    innerContainer.style.fontSize = fontSizeFloat + "px"

    if (fontSizeFloat <= minFontSize) {
      break
    }
  }

  // Shrink by 1 extra for better fits.
  innerContainer.style.fontSize = (fontSizeFloat - 1) + "px"
}

export function adjustTextHeightBasedOnChildrenClientOffsetHeight(container: HTMLElement, innerContainer: HTMLElement, fit: number, minFontSize: number = 4, vertical: boolean) {
  let fontSize = window.getComputedStyle(innerContainer, null).getPropertyValue('font-size')
  let fontSizeFloat = parseFloat(fontSize)

  if (innerContainer.children.length == 0 || getChildrenClientOffsetHeight(innerContainer, vertical) == 0) {
    return
  }

  while (getChildrenClientOffsetHeight(innerContainer, vertical) / container.clientHeight < fit) {
    fontSizeFloat++
    innerContainer.style.fontSize = fontSizeFloat + "px"
  }

  while (getChildrenClientOffsetHeight(innerContainer, vertical) / container.clientHeight > fit) {
    fontSizeFloat--
    innerContainer.style.fontSize = fontSizeFloat + "px"

    if (fontSizeFloat <= minFontSize) {
      break
    }
  }
}

export async function copyTextToClipboard(text: string): Promise<void> {
  try {
    await navigator.clipboard.writeText(text);
  } catch (err) {
    console.error('Failed to copy text to clipboard', err);
  }
}

export function getBaseUrl(): string {
  return `${window.location.protocol}//${window.location.host}`;
}