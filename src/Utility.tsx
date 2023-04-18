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