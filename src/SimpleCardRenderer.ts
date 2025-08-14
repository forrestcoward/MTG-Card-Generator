// Simple card renderer converted from the card-rendering folder
// This provides a basic implementation that can be directly imported

export interface CardData {
  name: string;
  mana_cost: string;
  type_line: string;
  oracle_text: string;
  power?: string;
  toughness?: string;
  flavor_text?: string;
  rarity: string;
  colors: string[];
  color_identity: string[];
  image_uris: {
    art_crop: string;
  };
  set: string;
  collector_number: string;
  artist: string;
  lang: string;
}

// Mana symbol mapping
const manaSymbols: Record<string, string> = {
  '{W}': '⚪',
  '{U}': '🔵',
  '{B}': '⚫',
  '{R}': '🔴',
  '{G}': '🟢',
  '{C}': '◇',
  '{T}': '⭮',
  '{X}': 'X',
  '{0}': '0',
  '{1}': '1',
  '{2}': '2',
  '{3}': '3',
  '{4}': '4',
  '{5}': '5',
  '{6}': '6',
  '{7}': '7',
  '{8}': '8',
  '{9}': '9',
  '{10}': '10',
};

function parseManaCost(manaCost: string): string {
  return manaCost.replace(/{[^}]+}/g, (match) => {
    return manaSymbols[match] || match;
  });
}

function parseOracleText(text: string): string {
  // Replace mana symbols with unicode equivalents
  let parsed = text.replace(/{[^}]+}/g, (match) => {
    return manaSymbols[match] || match;
  });
  
  // Wrap reminder text in italics
  parsed = parsed.replace(/\([^)]+\)/g, (match) => `<i>${match}</i>`);
  
  return parsed;
}

function getColorFromIdentity(colors: string[]): string {
  if (colors.length === 0) return 'colorless';
  if (colors.length === 1) return colors[0].toLowerCase();
  if (colors.length === 2) return 'gold';
  return 'multicolor';
}

export function createSimpleCard(card: CardData, scale: number = 1): HTMLElement {
  const cardElement = document.createElement('div');
  cardElement.className = 'simple-mtg-card';
  
  const colorClass = getColorFromIdentity(card.colors);
  cardElement.classList.add(`card-${colorClass}`);
  
  // Apply scaling
  cardElement.style.transform = `scale(${scale})`;
  cardElement.style.transformOrigin = 'top left';
  
  const cardHTML = `
    <div class="card-frame">
      <div class="card-header">
        <div class="card-name">${card.name}</div>
        <div class="card-mana-cost">${parseManaCost(card.mana_cost)}</div>
      </div>
      
      <div class="card-art" style="background-image: url('${card.image_uris.art_crop}');">
      </div>
      
      <div class="card-type-line">
        <div class="card-type">${card.type_line}</div>
        <div class="card-set-info">${card.set}</div>
      </div>
      
      <div class="card-text-box">
        <div class="card-oracle-text">${parseOracleText(card.oracle_text)}</div>
        ${card.flavor_text ? `<div class="card-flavor-text"><i>${card.flavor_text}</i></div>` : ''}
      </div>
      
      ${card.power && card.toughness ? `
        <div class="card-pt-box">
          <div class="card-pt">${card.power}/${card.toughness}</div>
        </div>
      ` : ''}
      
      <div class="card-footer">
        <div class="card-collector-info">${card.collector_number} ${card.rarity.charAt(0).toUpperCase()}</div>
        <div class="card-artist">Art: ${card.artist}</div>
      </div>
    </div>
  `;
  
  cardElement.innerHTML = cardHTML;
  
  return cardElement;
}

// CSS styles for the simple card
export const cardStyles = `
.simple-mtg-card {
  width: 440px;
  height: 630px;
  border-radius: 15px;
  overflow: hidden;
  position: relative;
  font-family: 'Matrix', 'Arial', sans-serif;
  box-shadow: 0 4px 8px rgba(0,0,0,0.3);
  margin: 10px;
}

.card-frame {
  width: 100%;
  height: 100%;
  position: relative;
  display: flex;
  flex-direction: column;
  border: 2px solid #333;
  border-radius: 15px;
  background: linear-gradient(135deg, #f5f5f5 0%, #e0e0e0 100%);
}

.card-colorless .card-frame { background: linear-gradient(135deg, #f0f0f0 0%, #d0d0d0 100%); }
.card-w .card-frame { background: linear-gradient(135deg, #fffbd5 0%, #f7f0c7 100%); }
.card-u .card-frame { background: linear-gradient(135deg, #aae1ff 0%, #7cc7e8 100%); }
.card-b .card-frame { background: linear-gradient(135deg, #ccc2c0 0%, #a6998a 100%); }
.card-r .card-frame { background: linear-gradient(135deg, #ffb3a7 0%, #e8967a 100%); }
.card-g .card-frame { background: linear-gradient(135deg, #9bd3ae 0%, #7ab085 100%); }
.card-gold .card-frame { background: linear-gradient(135deg, #ffd95a 0%, #e6c547 100%); }
.card-multicolor .card-frame { background: linear-gradient(135deg, #ffd95a 0%, #e6c547 100%); }

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 15px;
  background: rgba(0,0,0,0.1);
  border-bottom: 1px solid #666;
}

.card-name {
  font-size: 18px;
  font-weight: bold;
  color: #000;
  text-shadow: 1px 1px 2px rgba(255,255,255,0.8);
}

.card-mana-cost {
  font-size: 16px;
  font-weight: bold;
  color: #000;
  text-shadow: 1px 1px 2px rgba(255,255,255,0.8);
}

.card-art {
  flex: 1;
  background-size: cover;
  background-position: center;
  background-repeat: no-repeat;
  border-top: 1px solid #666;
  border-bottom: 1px solid #666;
  min-height: 200px;
}

.card-type-line {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 15px;
  background: rgba(0,0,0,0.1);
  border-bottom: 1px solid #666;
}

.card-type {
  font-size: 14px;
  font-weight: bold;
  color: #000;
}

.card-set-info {
  font-size: 12px;
  color: #666;
}

.card-text-box {
  padding: 15px;
  flex: 1;
  background: rgba(255,255,255,0.9);
  overflow-y: auto;
}

.card-oracle-text {
  font-size: 13px;
  line-height: 1.4;
  color: #000;
  margin-bottom: 10px;
  white-space: pre-line;
}

.card-flavor-text {
  font-size: 12px;
  color: #666;
  font-style: italic;
  margin-top: 10px;
  border-top: 1px solid #ddd;
  padding-top: 8px;
}

.card-pt-box {
  position: absolute;
  bottom: 50px;
  right: 15px;
  background: rgba(255,255,255,0.9);
  border: 2px solid #333;
  border-radius: 8px;
  padding: 5px 10px;
}

.card-pt {
  font-size: 16px;
  font-weight: bold;
  color: #000;
}

.card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 15px;
  background: rgba(0,0,0,0.1);
  border-top: 1px solid #666;
}

.card-collector-info {
  font-size: 10px;
  color: #666;
}

.card-artist {
  font-size: 10px;
  color: #666;
}
`;
