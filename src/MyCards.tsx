import { EventMessage, EventType, PublicClientApplication } from "@azure/msal-browser";
import React from "react";
import { CardDisplay, MagicCard } from "./Card";
import { GetUserMagicCards } from "./CallAPI";

import "./mtg-card.css";
import "./app.css";
import { setCardContainerSize } from "./Utility";
import { AuthenticatedTemplate, UnauthenticatedTemplate } from "@azure/msal-react";
import { Loader } from "./Loader";

export interface MyCardsProps{
  msalInstance: PublicClientApplication
}

export interface MyCardsState {
 cards: MagicCard[],
 loading: boolean,
}

export class MyCards extends React.Component<MyCardsProps, MyCardsState> {
  constructor(props: MyCardsProps) {
    super(props);
    this.state = {
      cards: [],
      loading: true,
    };

    setCardContainerSize();
    this.getUserCards();

    this.props.msalInstance.addEventCallback((message: EventMessage) => {
      if (message.eventType === EventType.LOGIN_SUCCESS) {
        this.getUserCards();
      }
    });
  }

  getUserCards() {
    GetUserMagicCards(this.props.msalInstance).then((cards) => {
      cards.forEach(card => {
        // Never use the temporary image because it might not exist anymore. Can make this better in the future.
        card.temporaryImageUrl = card.imageUrl
      })
      this.setState({cards: cards, loading: false})
    }).catch((error) => {
      console.log(error)
    })
  }

  render() {
    var loading = 
    <div style={{padding: "20px"}}>
      <h1>
        Loading your cards...
      </h1>
      <Loader />
    </div>

    var userCardDisplay =
    <div>
      <div className="cardsContainer">
      {
        this.state.cards.map(card => (
          <div className="cardContainer" key={`card-container-${card.id}`}>
            <CardDisplay key={`card-display-${card.id}`} card={card} />
          </div>
        ))
      }
      </div>
    </div>

    var pageDisplay = this.state.loading ? loading : userCardDisplay;

    return (
      <div>
          <AuthenticatedTemplate>
            {pageDisplay}
          </AuthenticatedTemplate>
          <UnauthenticatedTemplate>
            <div style={{margin: "10px"}}>
              <h1>Please login to see the history of your generated cards.</h1>
            </div>
          </UnauthenticatedTemplate>
        </div>
    )
  }
}