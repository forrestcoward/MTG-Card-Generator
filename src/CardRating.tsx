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
import { CardPreview } from './CardPreview';
import { ColumnType } from 'antd/es/table';

interface CardRatingProps {
  msalInstance: PublicClientApplication;
}

interface CardRatingState {
  card: MagicCard | undefined;
  cardId: string;
  loading: boolean;
  cardWidth: number;
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
      cardWidth: width
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
      rank: JSX.Element;
      score: string;
      card: JSX.Element;
    };

    const dataSource: DataSourceItem[] = [];

    const columns = [
      {
        title: 'Rank',
        dataIndex: 'rank',
        key: 'rank',
        align: 'center' as ColumnType<DataSourceItem>['align'],
      },
      {
        title: 'Rating',
        dataIndex: 'score',
        key: 'score',
        align: 'center' as ColumnType<DataSourceItem>['align'],
      },
      {
        title: 'Card',
        dataIndex: 'card',
        key: 'card',
      },
    ];

    this.state.topCards.forEach((card, index) => {
      let entry = {
        rank: <div><h3>{index + 1}</h3></div>,
        score: card.rating.averageScore.toFixed(2),
        card: 
          <div key={`leaderboard-card-container-${card.id}`}>
            <CardPreview key={`leaderboard-card-${card.id}`} card={new MagicCard(card.magicCards[0])} cardWidth={this.state.cardWidth} />
        </div>
      }
      dataSource.push(entry);
    })

    const rateCardButtonStyle : React.CSSProperties= { marginTop: '10px', height:"50px", justifyContent: "center" }
    const rateCardIconStyle : React.CSSProperties= { fontSize: '45px' }

    var userCardDisplay = <div></div>
    if (this.state.card)
    {
      userCardDisplay = 
      <div>
        <div style={{display: "flex", flexWrap: "wrap", justifyContent: "center"}}>
          <table>
            <tbody>
              <tr style={{justifyContent:"center", display:"grid", marginBottom:"-25px"}}>
                <td>
                  <h3>Prompt: "<i>{this.state.card.userPrompt}</i>"</h3>
                </td>
              </tr>
              <tr>
                <td>
                  <div className="cardContainer" key={`card-container-${this.state.card.id}`} style={{marginTop:"15px"}}>
                    <CardDisplay key={`card-display-${this.state.card.id}`} card={this.state.card} showCardMenu={false} cardWidth={this.state.cardWidth} />
                  </div>
                </td>
              </tr>
              <tr style={{textAlign: "center"}}>
                <td>
                  <Button onClick={this.rateCard.bind(this, this.state.cardId, 1)} type="text" style={rateCardButtonStyle}>
                    <Bs1Circle className="anticon" style={rateCardIconStyle} />
                  </Button>
                  <Button onClick={this.rateCard.bind(this, this.state.cardId, 2)} type="text" style={rateCardButtonStyle}>
                    <Bs2Circle className="anticon" style={rateCardIconStyle} />
                  </Button>
                  <Button onClick={this.rateCard.bind(this, this.state.cardId, 3)} type="text" style={rateCardButtonStyle}>
                    <Bs3Circle className="anticon" style={rateCardIconStyle} />
                  </Button>
                  <Button onClick={this.rateCard.bind(this, this.state.cardId, 4)} type="text" style={rateCardButtonStyle}>
                    <Bs4Circle className="anticon" style={rateCardIconStyle} />
                  </Button>
                  <Button onClick={this.rateCard.bind(this, this.state.cardId, 5)} type="text" style={rateCardButtonStyle}>
                    <Bs5Circle className="anticon" style={rateCardIconStyle} />
                  </Button>
                </td>
              </tr>
              <tr>

              </tr>
              <tr style={{justifyContent:"center", marginTop: "20px", display:"grid"}}>
                <h2>Top Rated Cards</h2>
              </tr>
              <tr>
                <td>
                  <Table className='leaderboard-table' bordered={true} dataSource={dataSource} columns={columns} pagination={{pageSize: 20}} />;
                </td>
              </tr>
            </tbody>
          </table>
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