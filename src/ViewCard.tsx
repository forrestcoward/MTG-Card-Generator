import React, { useEffect } from 'react';
import { CardDisplay, CardGenerationRecord, MagicCard } from './Card';
import { GetCard } from './CallAPI';
import { setCardContainerSize } from './Utility';
import { useMsal } from '@azure/msal-react';

export function ViewCard() {
  const [card, setCard] = React.useState<CardGenerationRecord | null>(null);
  const [cardWidth] = React.useState(setCardContainerSize());
  const [cardId] = React.useState<string | null>(new URLSearchParams(location.search).get('id'));
  const { instance: msalInstance } = useMsal();

  useEffect(() => {
    fetchCard()
  }, []);

  function fetchCard() {
    if (cardId) {
      GetCard(msalInstance, cardId).then((card) => {
        if (card) {
          setCard(card);
        }
      });
    }
  }

  return (
    <div>
      { card &&
        <div className="cardsContainer">
          <CardDisplay 
            card={new MagicCard(card.card)} 
            allowEdits={true} 
            allowImagePreview={true} 
            cardWidth={cardWidth} 
            showCardMenu={true} 
            allowImageUpdate={false} />
        </div>
      }
    </div>
  )
}