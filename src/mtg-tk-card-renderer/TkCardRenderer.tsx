import React, { useRef, useEffect, useState } from 'react';
import { isMobileDevice } from '../Utility';
import './style.css';
import '../mtg-card.css';
import TKCardCardRendererWrapper from './TkCardRendererWrapper';
import { MagicCard } from '../models/MagicCard';
import { CardMenu } from '../CardMenu';
import { GenerateImageResponse } from '../CallAPI';
import { ImageEditor } from '../ImageEditor';

interface TkCardRendererProps {
  card: MagicCard;
  cardWidth: number;
  showCardMenu?: boolean;
  allowImageUpdate?: boolean;
  allowImagePreview?: boolean;
  allowEdits?: boolean;
}

export const TkCardRenderer : React.FC<TkCardRendererProps> = ({ 
  card, 
  cardWidth, 
  showCardMenu = true,
  allowImageUpdate = true,
  allowImagePreview = false,
  allowEdits = false
}) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const [currentWidth, setCurrentWidth] = useState(cardWidth);
  const [increaseSizeAllowed, setIncreaseSizeAllowed] = useState(true);
  const [decreaseSizeAllowed, setDecreaseSizeAllowed] = useState(true);
  const [actualSize, setActualSize] = useState({ width: cardWidth, height: cardWidth * 1.4 });
  const [actualSizeWithMenu, setActualSizeWithMenu] = useState({ width: cardWidth, height: cardWidth * 1.4 });

  // Image editor state
  const [showImageEditor, setShowImageEditor] = useState(false);
  const [availableImageUrls, setAvailableImageUrls] = useState<GenerateImageResponse[]>(
    card.cardFaces[0].alternativeImageUrls?.map(url => ({ imageUrl: url, temporaryUrl: url })) || []
  );
  const [cardData, setCardData] = useState(card);
  
  // Flavor text visibility state
  const [showFlavorText, setShowFlavorText] = useState(true);

  //const convertedCard = convertMagicCardToNewFormat(card);
  const scale = currentWidth * 1.9 / cardWidth;
  const cardIsTutorialCard = cardData.databaseId === "-1";
  const imageEditorWidth = isMobileDevice() ? currentWidth : currentWidth - 100;

  // For this card renderer, a CSS transform is used to scale the size, but CSS transform does not impact the size of the container objects, 
  // so we update manually to adjust for the transformed size.
  useEffect(() => {
    //const measureActualSize = () => {
      const cardElement = document.getElementById(`mtgcardrender-${cardData.id}`);
      const cardMenu = document.getElementById(`mtgcardrender-menu-${cardData.id}`);

      if (cardElement && cardMenu) {
        const cardRect = cardElement.getBoundingClientRect();
        const menuRect = cardMenu.getBoundingClientRect();
        const newHeight = cardRect.height + menuRect.height;
        setActualSize({ width: cardRect.width, height: cardRect.height });
        setActualSizeWithMenu({ width: cardRect.width, height: newHeight });
      } else if (cardElement) {
        const cardRect = cardElement.getBoundingClientRect();
        setActualSize({ width: cardRect.width, height: cardRect.height });
        setActualSizeWithMenu({ width: cardRect.width, height: cardRect.height });
      }
    //};

    // Measure after a short delay to ensure rendering is complete.
    //const timeoutId = setTimeout(measureActualSize, 0);
    //return () => clearTimeout(timeoutId);
  }, [currentWidth, cardData.id, showFlavorText]);

  // Listen for create image events from the card HTML
  const handleCreateImage = () => {
    setShowImageEditor(true);
  };

  // Initialize size toggles.
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

  const increaseCardSize = (increase: number) => {
    const newWidth = currentWidth + increase;
    setCurrentWidth(newWidth);
    updateSizeToggles(newWidth);
  };

  const decreaseCardSize = (decrease: number) => {
    const newWidth = currentWidth - decrease;
    setCurrentWidth(newWidth);
    updateSizeToggles(newWidth);
  };

  const handleArtDownload = () => {
    const img = cardData.cardFaces[0].temporaryImageUrl ? cardData.cardFaces[0].temporaryImageUrl : cardData.cardFaces[0].imageUrl;
    if (img) {
      window.open(img, '_blank');
    }
  };

  const toggleShowImageEditor = () => {
    setShowImageEditor(!showImageEditor);
  };

  const toggleFlavorText = () => {
    setShowFlavorText(!showFlavorText);
    // Trigger remeasurement after state change
    //setTimeout(() => {
    /*
      const cardElement = document.getElementById(`mtgcardrender-${cardData.id}`);
      const cardMenu = document.getElementById(`mtgcardrender-menu-${cardData.id}`);

      if (cardElement && cardMenu) {
        const cardRect = cardElement.getBoundingClientRect();
        const menuRect = cardMenu.getBoundingClientRect();
        const newHeight = cardRect.height + menuRect.height;
        setActualSize({ width: cardRect.width, height: cardRect.height });
        setActualSizeWithMenu({ width: cardRect.width, height: newHeight });
      } else if (cardElement) {
        const cardRect = cardElement.getBoundingClientRect();
        setActualSize({ width: cardRect.width, height: cardRect.height });
        setActualSizeWithMenu({ width: cardRect.width, height: cardRect.height });
      }
        */
    //}, 0);
  };

  const updateImageUrl = (imageUrls: GenerateImageResponse) => {
    const updatedCard = MagicCard.clone(cardData, true);
    updatedCard.cardFaces[0].imageUrl = imageUrls.imageUrl;
    updatedCard.cardFaces[0].temporaryImageUrl = imageUrls.temporaryUrl;
    setCardData(updatedCard);
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

    const htmlToImage = (window as any).htmlToImage;
    try {
      const dataUrl = await htmlToImage.toPng(cardElement, {
        quality: 1.0,
        pixelRatio: 2,
        backgroundColor: 'white'
      });
      
      const link = document.createElement('a');
      link.download = `${card.cardFaces[0].name.replace(/[^a-z0-9]/gi, '_').toLowerCase()}.png`;
      link.href = dataUrl;
      link.click();
    } catch (error) {
      console.error('Error generating PNG:', error);
      alert('Error generating PNG. Please try again.');
    }
  };

  // Menu configuration.
  const cardMenuIconFontSize = "40px";
  const showSizeAdjustmentButtons = !isMobileDevice() || Math.max(document.documentElement.clientWidth || 0, window.innerWidth || 0) > 900;

  return (
    <div style={{display:"flex", flexWrap:"wrap"}}>
      <div 
        style={{
          paddingTop: 20,
          width: actualSize.width,
          height: actualSizeWithMenu.height,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <div 
          style={{ 
            width: actualSize.width,
            height: actualSize.height,
          }}
        >
          <div id={`mtgcardrender-${cardData.id}`} style={{ 
            width: 'fit-content', 
            transform: `scale(${scale})`, 
            transformOrigin: 'top left'
          }}>
            <TKCardCardRendererWrapper 
              key={`${cardData.id}-${currentWidth}`} 
              card={cardData} 
              scale={1} 
              renderMargin={1}
              showFlavorText={showFlavorText}
              onCreateImageClick={handleCreateImage}
              isImageEditorOpen={showImageEditor}
            />
          </div>
        </div>
          
        {showCardMenu && (
          <CardMenu
            card={cardData}
            showCardMenu={showCardMenu}
            showSizeAdjustmentButtons={showSizeAdjustmentButtons}
            cardMenuIconFontSize={cardMenuIconFontSize}
            increaseSizeAllowed={increaseSizeAllowed}
            decreaseSizeAllowed={decreaseSizeAllowed}
            onIncreaseSize={increaseCardSize}
            onDecreaseSize={decreaseCardSize}
            sizeAdjustmentIncrement={60}
            onDownloadCard={downloadCard}
            onDownloadArt={handleArtDownload}
            menuId={`mtgcardrender-menu-${cardData.id}`}
            allowImageUpdate={allowImageUpdate && !cardIsTutorialCard}
            onToggleImageEditor={toggleShowImageEditor}
            flavorTextVisible={showFlavorText}
            onToggleFlavorText={toggleFlavorText}
          />
        )}
      </div>
      <ImageEditor
        card={cardData}
        isVisible={allowImageUpdate && showImageEditor && !cardIsTutorialCard}
        onClose={() => setShowImageEditor(false)}
        onImageUpdate={updateImageUrl}
        width={imageEditorWidth}
        availableImageUrls={availableImageUrls}
        onAvailableImageUrlsUpdate={setAvailableImageUrls}
      />
    </div>
  );
};
