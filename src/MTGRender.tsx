import React, { useRef, useEffect, useState } from 'react';
import { MagicCard } from './Card';
import { Button, Dropdown, Tooltip } from 'antd';
import { 
  CaretDownFilled, 
  CloudDownloadOutlined, 
  CopyOutlined, 
  InfoCircleOutlined, 
  QuestionCircleOutlined, 
  ZoomInOutlined, 
  ZoomOutOutlined 
} from '@ant-design/icons';
import { copyTextToClipboard, isMobileDevice } from './Utility';

// Import the card rendering styles
import './card-rendering/style.css';
import CreateMTGCardComponent from './card-rendering/CreateMTGCardComponent';
import type { Card } from './card-rendering/script';

interface MTGRenderProps {
  card: MagicCard;
  cardWidth: number;
  showCardMenu?: boolean;
  allowImageUpdate?: boolean;
  allowImagePreview?: boolean;
  allowEdits?: boolean;
}

// Convert MagicCard to the format expected by the new renderer
function convertMagicCardToNewFormat(card: MagicCard): Card {
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


export const MTGRenderDisplay : React.FC<MTGRenderProps> = ({ 
  card, 
  cardWidth, 
  showCardMenu = true,
  allowImageUpdate = true,
  allowImagePreview = true,
  allowEdits = true 
}) => {
  const containerRef = useRef<HTMLDivElement>(null);
  
  // State management for the card menu
  const [currentWidth, setCurrentWidth] = useState(cardWidth);
  const [increaseSizeAllowed, setIncreaseSizeAllowed] = useState(true);
  const [decreaseSizeAllowed, setDecreaseSizeAllowed] = useState(true);
  const [actualSize, setActualSize] = useState({ width: cardWidth, height: cardWidth * 1.4 });
  
  // Prepare converted data and scale
  const convertedCard = convertMagicCardToNewFormat(card);
  const scale = currentWidth * 1.7 / cardWidth;

  // Measure actual size after transform
  useEffect(() => {
    const measureActualSize = () => {
      const cardElement = document.getElementById(`mtgcardrender-${card.id}`);
      const cardMenu = document.getElementById(`mtgcardrender-menu-${card.id}`);
      if (cardElement && cardMenu) {
        const cardRect = cardElement.getBoundingClientRect();
        const menuRect = cardMenu.getBoundingClientRect();
        const newHeight = cardRect.height + menuRect.height;
        setActualSize({ width: cardRect.width, height: newHeight });
      } else if (cardElement) {
        const cardRect = cardElement.getBoundingClientRect();
        setActualSize({ width: cardRect.width, height: cardRect.height });
      }
    };

    // Measure after a short delay to ensure rendering is complete
    const timeoutId = setTimeout(measureActualSize, 100);
    return () => clearTimeout(timeoutId);
  }, [currentWidth, card.id]);

  // Initialize size toggles
  useEffect(() => {
    updateSizeToggles(currentWidth);
  }, [currentWidth]);

  const updateSizeToggles = (newWidth: number) => {
    if (newWidth <= 330) {
      setIncreaseSizeAllowed(true);
      setDecreaseSizeAllowed(false);
    } else {
      setIncreaseSizeAllowed(true);
      setDecreaseSizeAllowed(true);
    }
  };

  const increaseCardSize = (increase: number = 30) => {
    const newWidth = currentWidth + increase;
    setCurrentWidth(newWidth);
    updateSizeToggles(newWidth);
  };

  const decreaseCardSize = (decrease: number = 30) => {
    const newWidth = currentWidth - decrease;
    setCurrentWidth(newWidth);
    updateSizeToggles(newWidth);
  };

  const handleArtDownload = () => {
    const img = card.temporaryImageUrl ? card.temporaryImageUrl : card.imageUrl;
    if (img) {
      window.open(img, '_blank');
    }
  };

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

  // Menu configuration
  const cardIsTutorialCard = card.databaseId === "-1";
  const cardMenuIconFontSize = "40px";
  const optionsMenuItemStyle: React.CSSProperties = { marginLeft: 5 };
  const optionsMenuIconStyle: React.CSSProperties = { fontSize: 30 };
  const showSizeAdjustmentButtons = !isMobileDevice() || Math.max(document.documentElement.clientWidth || 0, window.innerWidth || 0) > 900;

  const menuItems = [
    {
      key: '1',
      label: (
        <Tooltip placement="left">
          <CloudDownloadOutlined style={optionsMenuIconStyle} />
          <span style={optionsMenuItemStyle}>Download Card</span>
        </Tooltip>
      ),
      onClick: downloadCard
    },
    {
      key: '2',
      label: (
        <Tooltip placement="left">
          <CloudDownloadOutlined style={optionsMenuIconStyle} />
          <span style={optionsMenuItemStyle}>Download Art</span>
        </Tooltip>
      ),
      onClick: handleArtDownload
    }
  ];

  const copyLinkMenuItem = {
    key: '3',
    label: (
      <div>
        <CopyOutlined style={optionsMenuIconStyle} />
        <span style={optionsMenuItemStyle}>Copy Link</span>
      </div>
    ),
    onClick: async () => {
      await copyTextToClipboard(card.getCardUrl())
    }
  };

  if (card.databaseId) {
    menuItems.push(copyLinkMenuItem);
  }

  return (
    <div 
      style={{
        paddingTop: 20,
        width: actualSize.width,
        height: actualSize.height,
      }}
    >
      <div 
        style={{ 
          width: actualSize.width,
          height: actualSize.height,
        }}
      >
        <div id={`mtgcardrender-${card.id}`} ref={containerRef} style={{ width: 'fit-content', transform: `scale(${scale})`, transformOrigin: 'top left' }}>
          <CreateMTGCardComponent 
            key={`${card.id}-${currentWidth}`} 
            card={convertedCard} 
            scale={1} 
            renderMargin={1} 
          />
        </div>
                {true && (
          <div id={`mtgcardrender-menu-${card.id}`}>
            <Tooltip title={<div><b>Prompt: </b>{card.userPrompt}</div>} placement="left">
              <QuestionCircleOutlined style={{fontSize: cardMenuIconFontSize, marginLeft: 3}} />
            </Tooltip>
            
            {card.explanation && (
              <Tooltip 
                title={
                  <div>
                    <b>About This Card</b>
                    <br/>
                    <br/>
                    {card.explanation}
                    <br/>
                    <br/>
                    {card.funnyExplanation}
                  </div>
                } 
                overlayInnerStyle={{width: 320, backgroundColor: 'rgba(0, 0, 0, 0.93)'}} 
                placement="left"
              >
                <InfoCircleOutlined style={{fontSize: cardMenuIconFontSize, marginLeft: 3}} />
              </Tooltip>
            )}
            
            {showSizeAdjustmentButtons && (
              <Button type="text" size="middle" shape="circle" disabled={!increaseSizeAllowed}>
                <ZoomInOutlined 
                  onClick={() => increaseCardSize(60)} 
                  style={{fontSize: cardMenuIconFontSize, marginRight: -4}} 
                />
              </Button>
            )}
            
            {showSizeAdjustmentButtons && (
              <Button type="text" size="middle" shape="circle" disabled={!decreaseSizeAllowed}>
                <ZoomOutOutlined 
                  onClick={() => decreaseCardSize(60)} 
                  style={{fontSize: cardMenuIconFontSize, marginRight: -7}} 
                />
              </Button>
            )}
            
            <Dropdown menu={{items: menuItems}}>
              <a className="ant-dropdown-link" onClick={e => e.preventDefault()}>
                <CaretDownFilled style={{fontSize: cardMenuIconFontSize}} />
              </a>
            </Dropdown>
          </div>
        )}
      </div>

    </div>
  );
};
