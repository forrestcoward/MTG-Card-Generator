import React from 'react';
import { CardDisplay, CardGenerationRecord, MagicCard } from './Card';
import { PublicClientApplication } from '@azure/msal-browser';
import { GetCard } from './CallAPI';
import { setCardContainerSize } from './Utility';

interface ViewCardProps {
  msalInstance: PublicClientApplication;
}

interface ViewCardState {
  card: CardGenerationRecord | null
  cardWidth: number
  cardId: string | null
}

export class ViewCard extends React.Component<ViewCardProps, ViewCardState> {
  constructor(props:ViewCardProps) {
    super(props);
    const width = setCardContainerSize();
    const queryParams = new URLSearchParams(location.search);
    const cardId = queryParams.get('id'); 
    this.state = {
      card: null,
      cardWidth: width,
      cardId: cardId
    };

    this.fetchCard()
  }

  fetchCard() {
    if (this.state.cardId) {
      GetCard(this.props.msalInstance, this.state.cardId).then((card) => {
        if (card) {
          this.setState({card: card})
        }
      });
    }
  }

  render() {
    return (
      <div>
        { this.state.card &&
          <div className="cardsContainer">
            <CardDisplay card={new MagicCard(this.state.card.card)} allowEdits={true} allowImagePreview={true} cardWidth={this.state.cardWidth} showCardMenu={true} allowImageUpdate={false} />
          </div>
        }
      </div>
    )
  }
}