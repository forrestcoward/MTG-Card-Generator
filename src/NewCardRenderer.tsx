import React, { useRef, useEffect } from 'react';
import { MagicCard } from './Card';
import CreateMTGCard from './card-rendering/CreateMTGCard';

// Import the card rendering styles
import './card-rendering/style.css';

interface NewCardRendererProps {
  card: MagicCard;
  cardWidth: number;
  showCardMenu?: boolean;
  allowImageUpdate?: boolean;
  allowImagePreview?: boolean;
  allowEdits?: boolean;
}

// Convert MagicCard to the format expected by the new renderer
function convertMagicCardToNewFormat(card: MagicCard): any {
  return {
    name: card.name,
    mana_cost: card.manaCost,
    type_line: card.typeLine,
    oracle_text: card.oracleText,
    power: card.power?.toString() || '',
    toughness: card.toughness?.toString() || '',
    flavor_text: card.flavorText,
    rarity: card.rarity?.toLowerCase() || 'common',
    colors: card.colorIdentity ? [card.colorIdentity.toUpperCase()] : [],
    color_identity: card.colorIdentity ? [card.colorIdentity.toUpperCase()] : [],
    image_uris: {
      art_crop: card.imageUrl || card.temporaryImageUrl || ''
    },
    set: 'GEN',
    collector_number: '001',
    artist: 'AI Generated',
    lang: 'en'
  };
}


export const NewCardRenderer: React.FC<NewCardRendererProps> = ({ 
  card, 
  cardWidth, 
  showCardMenu = true,
  allowImageUpdate = true,
  allowImagePreview = true,
  allowEdits = true 
}) => {
  const containerRef = useRef<HTMLDivElement>(null);
  // Prepare converted data and scale
  const convertedCard = convertMagicCardToNewFormat(card);
  const scale = cardWidth / 440; // Adjust scale based on desired width

  const downloadCard = async () => {
  const cardElement = containerRef.current?.querySelector('.mtg-card') as HTMLElement;
    if (!cardElement) return;

    // Load html-to-image if not already loaded
    if (!(window as any).htmlToImage) {
      try {
        await new Promise<void>((resolve, reject) => {
          const script = document.createElement('script');
          script.src = 'https://cdnjs.cloudflare.com/ajax/libs/html-to-image/1.11.11/html-to-image.js';
          script.onload = () => resolve();
          script.onerror = () => reject(new Error('Failed to load html-to-image'));
          document.head.appendChild(script);
        });
      } catch (error) {
        console.error('Failed to load html-to-image library:', error);
        alert('Failed to load image export library');
        return;
      }
    }

    // Use html-to-image to download the card
    const htmlToImage = (window as any).htmlToImage;
    try {
      const dataUrl = await htmlToImage.toPng(cardElement, {
        quality: 1.0,
        pixelRatio: 2,
        backgroundColor: 'white'
      });
      
      const link = document.createElement('a');
      link.download = `${card.name.replace(/[^a-z0-9]/gi, '_').toLowerCase()}.png`;
      link.href = dataUrl;
      link.click();
    } catch (error) {
      console.error('Error generating PNG:', error);
      alert('Error generating PNG. Please try again.');
    }
  };

  return (
    <div style={{ position: 'relative' }}>
        <div ref={containerRef} style={{ width: 'fit-content' }}>
          <CreateMTGCard card={convertedCard} scale={scale} renderMargin={1} />
        </div>
    </div>
  );
};
