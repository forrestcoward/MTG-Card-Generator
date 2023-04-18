export function findCSSRule(selector: string): CSSStyleRule | null {
  const sheets = document.styleSheets;
  const sheetsLength = sheets.length;

  for (let i = 0; i < sheetsLength; i++) {
    let sheet = sheets[i];
    let rules = sheet.cssRules;

    for (let j = 0; j < rules.length; j++) {
      let rule = rules[j];
      if (rule instanceof CSSStyleRule && rule.selectorText === selector) {
        return rule;
      }
    }
  }

  return null;
}