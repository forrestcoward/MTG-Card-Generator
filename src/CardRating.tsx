import React from 'react';
import { Rate, Table, Tag } from 'antd';
import { EventMessage, EventType, PublicClientApplication } from '@azure/msal-browser';
import { CardDisplay, CardGenerationRecord, MagicCard } from './Card';
import { Loader } from './Loader';
import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react';
import { GetCardToRate, RateCard, GetTopCards, GetUserInfo } from './CallAPI';

import "./mana.min.css";
import "./mtg-card.css";
import "./app.css";
import { setCardContainerSize } from './Utility';
import { CardPreview } from './CardPreview';
import { User } from './User';

interface CardRatingProps {
  msalInstance: PublicClientApplication;
}

interface CardRatingState {
  card: MagicCard | undefined;
  cardId: string;
  loading: boolean;
  errorMessage: string;
  cardWidth: number;
  topCards: CardGenerationRecord[];
  user: User | undefined;
}

export class CardRating extends React.Component<CardRatingProps, CardRatingState> {
  constructor(props: CardRatingProps) {
    super(props);

    let width = setCardContainerSize();
    this.state = {
      card: undefined,
      cardId: "",
      errorMessage: "",
      topCards: [],
      loading: true,
      cardWidth: width,
      user: undefined,
    };

    this.props.msalInstance.addEventCallback((message: EventMessage) => {
      if (message.eventType === EventType.LOGIN_SUCCESS) {
        this.getRandomCard();
        this.getTopCards();
      }
    });
  }

  componentDidMount(): void {
    this.getRandomCard();
    this.getTopCards();
  }

  getUserInfo() {
    GetUserInfo(this.props.msalInstance).then((user) => {
      this.setState({user: user})
    }).catch((error) => {
      this.setState({errorMessage: error.message})
      console.log("Error in getUserInfo: " + error)
    })
  }

  getRandomCard() {
    this.getUserInfo();
    GetCardToRate(this.props.msalInstance).then((record) => {
      var card = new MagicCard(record!.card);
      this.setState({card: card, cardId: record!.id, loading: false, errorMessage: ""})
    }).catch((error) => {
      this.setState({errorMessage: error.message, loading: false})
      console.log("Error in getRandomCard: " + error)
    })
  }

  getTopCards() {
    GetTopCards(this.props.msalInstance).then((records) => {
      this.setState({topCards: records})
    }).catch((error) => {
      this.setState({ loading: false })
      console.log("Error in getTopCards: " + error)
    })
  }

  rateCard(cardId: string, rating: number) {
    RateCard(cardId, rating, this.props.msalInstance).then(() => {
      this.setState({loading: true})
      this.getRandomCard()
    }).catch((error) => {
      this.setState({ loading: false })
      console.log("Error in rateCard: " + error)
    })
  }

  render() {
    var loadingElement = 
      <div style={{padding: "20px"}}>
        <h1>
          Retrieving a card to rate...
        </h1>
        <Loader />
      </div>

    var errorElement =
      <div style={{padding: "20px"}}>
          {this.state.errorMessage}
      </div>

    type DataSourceItem = {
      card: JSX.Element;
    };

    let dataSource: DataSourceItem[] = [];

    const columns = [
      {
        title: 'Card',
        dataIndex: 'card',
      },
    ];

    this.state.topCards.forEach((cardRecord, index) => {
      let entry = {
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
        <div style={{display: "flex", flexWrap: "wrap", justifyContent: "center", backgroundColor:"whitesmoke"}}>
          <table>
            <tbody>
              <tr style={{justifyContent:"center", display:"grid", marginBottom:"-25px"}}>
                <td style={{maxWidth:"500px", margin:"5px"}}>
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
                  <h4>Submit Your Rating:</h4>
                </td>
              </tr>
              { this.state.user &&
              <tr style={{textAlign: "center"}}>
                <td>
                  <Rate className='card-rating' style={{marginTop: "0px", width: `${this.state.cardWidth}px`}} onChange={(rating) => this.rateCard(this.state.cardId, rating)} ></Rate>
                </td>
              </tr>
              }
              <tr style={{display:"grid", marginTop:"10px"}}>
                <td style={{textAlign:"right"}}>
                  <Tag style={{fontSize: "10px", marginLeft:"0"}} color="geekblue">You've Rated {this.state.user != undefined ? this.state.user.numberOfCardsRated : 0} Cards</Tag>
                </td>
              </tr>
              <tr style={{justifyContent:"center", marginTop: "5px", display:"grid", backgroundColor: "#857dff0f"}}>
                <td>
                  <h2>Top 100 User Cards</h2>
                </td>
              </tr>
              <tr>
                <td>
                  <Table showHeader={false} className='leaderboard-table' bordered={true} dataSource={dataSource} columns={columns} pagination={{pageSize: 20, showSizeChanger:false}} />
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    }

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
            <h1>Please login.</h1>
          </div>
        </UnauthenticatedTemplate>
      </div>
      )
  }
}