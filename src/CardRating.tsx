import React from 'react';
import { Button, Table } from 'antd';
import { EventMessage, EventType, PublicClientApplication } from '@azure/msal-browser';
import { CardDisplay, CardGenerationRecord, MagicCard } from './Card';
import { Loader } from './Loader';
import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react';
import { GetRandomCard, RateCard, TopCards } from './CallAPI';

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
  defaultCardWidth: number;
  topCards: CardGenerationRecord[];
}

export class CardRating extends React.Component<CardRatingProps, CardRatingState> {
  constructor(props: CardRatingProps) {
    super(props);

    let width = setCardContainerSize();
    this.state = {
      card: undefined,
      cardId: "",
      topCards: [],
      loading: true,
      defaultCardWidth: width
    };

    this.props.msalInstance.addEventCallback((message: EventMessage) => {
      if (message.eventType === EventType.LOGIN_SUCCESS) {
        this.getRandomCard();
      }
    });
  }

  componentDidMount(): void {
    this.getRandomCard();
    this.getTopCards();
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

  getTopCards() {
    TopCards(this.props.msalInstance).then((cards) => {
      cards.forEach(c => {
        c.magicCards[0].temporaryImageUrl = c.magicCards[0].imageUrl
      })
      this.setState({topCards: cards})
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

type DataSourceItem = {
  rank: number;
  score: number; // or another appropriate type depending on what card.rating.averageScore returns
  card: JSX.Element;
};

const dataSource: DataSourceItem[] = [];

  const columns = [
    {
      title: 'Rank',
      dataIndex: 'rank',
      key: 'rank',
    },
    {
      title: 'Score',
      dataIndex: 'score',
      key: 'score',
    },
    {
      title: 'Card',
      dataIndex: 'card',
      key: 'card',
    },
  ];

  this.state.topCards.forEach((card, index) => {
    let entry = {
      rank: index,
      score: card.rating.averageScore,
      card: 
        <div className="cardContainer" key={`card-container-${card.id}`}>
          <CardDisplay key={`card-display-${card.id}`} card={new MagicCard(card.magicCards[0])} showCardMenu={false} defaultCardWidth={400} />
       </div>
    }
    dataSource.push(entry);
  })

    var userCardDisplay = <div></div>
    if (this.state.card)
    {
      userCardDisplay = 
      <div>
        <div style={{display: "flex", flexWrap: "wrap", justifyContent: "center"}}>
          <table>
            <tbody>
              <tr>
                <td>
                  <div className="cardContainer" key={`card-container-${this.state.card.id}`}>
                    <CardDisplay key={`card-display-${this.state.card.id}`} card={this.state.card} showCardMenu={false} defaultCardWidth={this.state.defaultCardWidth} />
                  </div>
                </td>
              </tr>
              <tr style={{textAlign: "center"}}>
                <td>
                  <Button onClick={this.rateCard.bind(this, this.state.cardId, 1)} type="text" style={{ marginTop: '10px', height:"50px", justifyContent: "center" }}>
                    <Bs1Circle className="anticon" style={{fontSize: '45px'}} />
                  </Button>
                  <Button onClick={this.rateCard.bind(this, this.state.cardId, 2)} type="text" style={{ marginTop: '10px', height:"50px", justifyContent: "center" }}>
                    <Bs2Circle className="anticon" style={{fontSize: '45px'}} />
                  </Button>
                  <Button onClick={this.rateCard.bind(this, this.state.cardId, 3)} type="text" style={{ marginTop: '10px', height:"50px", justifyContent: "center" }}>
                    <Bs3Circle className="anticon" style={{fontSize: '45px'}} />
                  </Button>
                  <Button onClick={this.rateCard.bind(this, this.state.cardId, 4)} type="text" style={{ marginTop: '10px', height:"50px", justifyContent: "center" }}>
                    <Bs4Circle className="anticon" style={{fontSize: '45px'}} />
                  </Button>
                  <Button onClick={this.rateCard.bind(this, this.state.cardId, 5)} type="text" style={{ marginTop: '10px', height:"50px", justifyContent: "center" }}>
                    <Bs5Circle className="anticon" style={{fontSize: '45px'}} />
                  </Button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div style={{textAlign: "center"}}>
        <Table bordered={true} dataSource={dataSource} columns={columns} />;
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