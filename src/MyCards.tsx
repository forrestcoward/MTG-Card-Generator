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
 cardWidth: number,
 loading: boolean,
 errorMessage: string,
}

export class MyCards extends React.Component<MyCardsProps, MyCardsState> {
  constructor(props: MyCardsProps) {
    super(props);

    var width = setCardContainerSize();
    this.state = {
      cards: [],
      loading: true,
      cardWidth: width,
      errorMessage: "",
    };

    this.getUserCards();

    this.props.msalInstance.addEventCallback((message: EventMessage) => {
      if (message.eventType === EventType.LOGIN_SUCCESS) {
        this.getUserCards();
      }
    });
  }

  getUserCards() {
    GetUserMagicCards(this.props.msalInstance).then((records) => {
      this.setState({cards: records.map(x => new MagicCard(x.card)), errorMessage: "", loading: false})
    }).catch((error) => {
      this.setState({errorMessage: error.message, loading: false})
      console.log(error)
    })
  }

  render() {
    var loadingElement = 
      <div style={{padding: "20px"}}>
        <h1>
          Loading your cards...
        </h1>
        <Loader />
      </div>

    var errorElement =
      <div style={{padding: "20px"}}>
          {this.state.errorMessage}
      </div>

    var userCardDisplay =
      <div>
        <div className="cardsContainer">
        {
          this.state.cards.map(card => (
            <div className="cardContainer" key={`card-container-${card.id}`}>
              <CardDisplay key={`card-display-${card.id}`} card={card} showCardMenu={true} cardWidth={this.state.cardWidth} allowImagePreview={true} allowEdits={true}/>
            </div>
          ))
        }
        </div>
      </div>

    var pageDisplay = userCardDisplay;
    if (this.state.errorMessage) {
      pageDisplay = errorElement
    } else if (this.state.loading) {
      pageDisplay = loadingElement
    }

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