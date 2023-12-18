import React from 'react';
import { Rate, Table } from 'antd';
import { EventMessage, EventType, PublicClientApplication } from '@azure/msal-browser';
import { CardDisplay, CardGenerationRecord, MagicCard } from './Card';
import { Loader } from './Loader';
import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react';
import { GetCardToRate, RateCard, GetTopCards } from './CallAPI';

import "./mana.min.css";
import "./mtg-card.css";
import "./app.css";
import { setCardContainerSize } from './Utility';
import { CardPreview } from './CardPreview';

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
    GetCardToRate(this.props.msalInstance).then((cards) => {
      var card = new MagicCard(cards[0].card);
      card.temporaryImageUrl = card.imageUrl
      this.setState({card: card, cardId: cards[0].id, loading: false})
    }).catch((error) => {
      console.log(error)
    })
  }

  getTopCards() {
    GetTopCards(this.props.msalInstance).then((cards) => {
      cards.forEach(c => {
        c.card.temporaryImageUrl = c.card.imageUrl
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
      //rank: JSX.Element;
      //score: string;
      card: JSX.Element;
    };

    let dataSource: DataSourceItem[] = [];

    const columns = [
      /*
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
      },*/
      {
        title: 'Card',
        dataIndex: 'card',
      },
    ];

    this.state.topCards.forEach((cardRecord, index) => {
      let entry = {
        //rank: <div><h3>{index + 1}</h3></div>,
        //score: cardRecord.rating.averageScore.toFixed(2),
                            //{cardRecord.rating.averageScore.toFixed(2)} / 5
        key: cardRecord.id,
        card:
          <table>
            <tbody>
              <tr>
                <td>
                  <div>
                    <span style={{fontSize: "20px"}}>#{index+1} </span>
                    <Rate defaultValue={cardRecord.rating.averageScore} disabled={true} allowHalf={true}></Rate>
                  </div>
                </td>
              </tr>
              <tr>
                <td>
                  <div>
                    <CardPreview card={new MagicCard(cardRecord.card)} cardWidth={this.state.cardWidth} />
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
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
              <tr style={{justifyContent:"center", display:"grid", marginBottom:"-25px"}}>
                <td>
                  <h3>Prompt: "<i>{this.state.card.userPrompt}</i>"</h3>
                </td>
              </tr>
              <tr>
                <td>
                  <div className="cardContainer" key={`card-rate-container-${this.state.card.id}`} style={{marginTop:"15px"}}>
                    <CardDisplay key={`card-rate-display-${this.state.card.id}`} card={this.state.card} showCardMenu={false} cardWidth={this.state.cardWidth} allowImagePreview={false} allowEdits={false} />
                  </div>
                </td>
              </tr>
              <tr style={{textAlign: "center"}}>
                <td>
                  <Rate className='card-rating' style={{marginTop: "5px", width: `${this.state.cardWidth}px`}} onChange={(rating) => this.rateCard(this.state.cardId, rating)} ></Rate>
                </td>
              </tr>
              <tr>

              </tr>
              <tr style={{justifyContent:"center", marginTop: "10px", display:"grid"}}>
                <td>
                  <h2>Top Rated Cards</h2>
                </td>
              </tr>
              <tr>
                <td>
                  <Table showHeader={false} className='leaderboard-table' bordered={true} dataSource={dataSource} columns={columns} pagination={{pageSize: 20}} style={{width:this.state.cardWidth}} />
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