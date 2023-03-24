type CaptureGroupDictionary = { [key: string]: string };

function extractCaptureGroups(regExp: RegExp, input: string): CaptureGroupDictionary | null {
  const result: CaptureGroupDictionary = {};

  // Check if the RegExp has any capture groups
  if (!/\((?!\?:)/.test(regExp.source)) {
    return null;
  }

  // Extract named capture group names
  const groupNames = Array.from(regExp.source.matchAll(/\(\?<(\w+)>/g)).map(m => m[1]);

  // Make sure the RegExp has the global flag to enable multiple matches
  const globalRegExp = new RegExp(regExp.source, regExp.flags.includes('g') ? regExp.flags : regExp.flags + 'g');

  // Iterate through all matches in the input string
  let match: RegExpExecArray | null;
  while ((match = globalRegExp.exec(input)) !== null) {
    // Iterate through all capture groups in the current match
    for (let i = 1; i < match.length; i++) {
      const captureGroupName = groupNames[i - 1] || `group${i}`;
      if (!result[captureGroupName]) {
        result[captureGroupName] = match[i];
      } else {
        result[captureGroupName] += match[i];
      }
    }
  }

  return result;
}
// Usage example:
var regExp = /```(?<cards>.*)```.*/gm
var input = '``` { "c"... is not valid JSON The response was: ``` { "cards": [ { "name": "Tolarian Grand Archmage", "manaCost": "{3}{U}{U}", "type": "Legendary Creature — Human Wizard", "text": "Flash, flying, hexproof, trample\nWhenever Tolarian Grand Archmage deals damage to a player, draw that many cards.\n{2}{U}: Target creature gets -X/-X until end of turn where X is the number of cards in your hand.\n{3}{U}{U}{U}: Exile target permanent.", "flavorText": "Knowledge is power.", "pt": "3/3", "rarity": "Mythic Rare" } ] } ```';
var result = extractCaptureGroups(regExp, input);
console.log(result);

regExp = /(?<manaCostToken>\{[1,2,3,4,5,6,7,8,9,10,W,G,U,B,R]\})/mg
input = 'Flashback {2}{G}{G}';
result = extractCaptureGroups(regExp, input);
console.log(result);