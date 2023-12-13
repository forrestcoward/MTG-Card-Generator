import React from 'react';
import { Button } from 'antd';
import { EventMessage, EventType, PublicClientApplication } from '@azure/msal-browser';
import { CardDisplay, MagicCard } from './Card';
import { Loader } from './Loader';
import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react';
import { GetRandomCard, RateCard } from './CallAPI';

import "./mana.min.css";
import "./mtg-card.css";
import "./app.css";
import { setCardContainerSize } from './Utility';
import { Bs1Circle, Bs2Circle, Bs3Circle, Bs4Circle, Bs5Circle } from "react-icons/bs";

interface CardRatingProps {
  msalInstance: PublicClientApplication;
}

interface CardRatingState {
  card: MagicCard | undefined;
  cardId: string;
  loading: boolean;
}

export class CardRating extends React.Component<CardRatingProps, CardRatingState> {
  constructor(props: CardRatingProps) {
    super(props);

    this.state = {
      card: undefined,
      cardId: "",
      loading: true
    };

    setCardContainerSize();

    this.props.msalInstance.addEventCallback((message: EventMessage) => {
      if (message.eventType === EventType.LOGIN_SUCCESS) {
        this.getRandomCard();
      }
    });
  }

  componentDidMount(): void {
    this.getRandomCard();
  }

  getRandomCard() {
    GetRandomCard(this.props.msalInstance).then((cards) => {
      var card = new MagicCard(cards[0].magicCards[0]);
      card.temporaryImageUrl = card.imageUrl
      this.setState({card: card, cardId: cards[0].id, loading: false})
    }).catch((error) => {
      console.log(error)
    })
  }

  rateCard(cardId: string, rating: number) {
    RateCard(cardId, rating, this.props.msalInstance).then(() => {
      this.setState({loading: true})
      this.getRandomCard()
    }).catch((error) => {
      console.log(error)
    })
  }

  render() {
    var loading = 
    <div style={{padding: "20px"}}>
      <h1>
        Retrieving card...
      </h1>
      <Loader />
    </div>

    var userCardDisplay = <div></div>
    if (this.state.card)
    {
      userCardDisplay = 
      <div>
        <div style={{display: "flex", flexWrap: "wrap", justifyContent: "center"}}>
          <div style={{display: "flex"}}>
            <div className="cardContainer" key={`card-container-${this.state.card.id}`}>
              <CardDisplay key={`card-display-${this.state.card.id}`} card={this.state.card} showCardMenu={false} />
              <Button onClick={this.rateCard.bind(this, this.state.cardId, 1)} style={{ marginTop: '10px', height:"50px", justifyContent: "center" }}>
                <Bs1Circle style={{fontSize: '40px'}} />
              </Button>
              <Button onClick={this.rateCard.bind(this, this.state.cardId, 2)} style={{ marginTop: '10px', height:"50px", justifyContent: "center" }}>
                <Bs2Circle style={{fontSize: '40px'}} />
              </Button>
              <Button onClick={this.rateCard.bind(this, this.state.cardId, 3)} style={{ marginTop: '10px', height:"50px", justifyContent: "center" }}>
                <Bs3Circle style={{fontSize: '40px'}} />
              </Button>
              <Button onClick={this.rateCard.bind(this, this.state.cardId, 4)} style={{ marginTop: '10px', height:"50px", justifyContent: "center" }}>
                <Bs4Circle style={{fontSize: '40px'}} />
              </Button>
              <Button onClick={this.rateCard.bind(this, this.state.cardId, 5)} style={{ marginTop: '10px', height:"50px", justifyContent: "center" }}>
                <Bs5Circle style={{fontSize: '40px'}} />
              </Button>
            </div>
          </div>
        </div>
      </div>
    }

var pageDisplay = this.state.loading ? loading: userCardDisplay
    return (
      <div>
        <AuthenticatedTemplate>
          {pageDisplay}
        </AuthenticatedTemplate>
        <UnauthenticatedTemplate>
          <div style={{margin: "10px"}}>
            <h1>Please login.</h1>
          </div>
        </UnauthenticatedTemplate>
      </div>
    )
  }
}