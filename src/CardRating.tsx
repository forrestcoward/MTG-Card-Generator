import React, { useState, useEffect } from 'react';
import { Rate, Table, Tag } from 'antd';
import { EventMessage, EventType } from '@azure/msal-browser';
import { CardDisplay, CardGenerationRecord, MagicCard } from './Card';
import { Loader } from './Loader';
import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react';
import { GetCardToRate, RateCard, GetTopCards, GetUserInfo } from './CallAPI';

import "./mana.min.css";
import "./mtg-card.css";
import "./app.css";
import { setCardContainerSize } from './Utility';
import { CardPreview } from './CardPreview';
import { User } from './User';

export function CardRating() {
  const [card, setCard] = useState<MagicCard|undefined>(undefined);
  const [cardId, setCardId] = useState("");
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");
  const [cardWidth] = useState(setCardContainerSize());
  const [topCards, setTopCards] = useState<CardGenerationRecord[]>([]);
  const [user, setUser] = useState<User|undefined>(undefined);
  const { instance: msalInstance } = useMsal();

  useEffect(() => {
    const eventCallback = (message: EventMessage) => {
      if (message.eventType === EventType.LOGIN_SUCCESS) {
        getRandomCard();
        getTopCards();
      }
    };

    msalInstance.addEventCallback(eventCallback);
    getRandomCard();
    getTopCards();
  }, []);

  const getUserInfo = () => {
    GetUserInfo(msalInstance).then(user => {
      setUser(user);
    }).catch(error => {
      setErrorMessage(error.message);
      console.error("Error in getUserInfo: " + error);
    });
  };

  const getRandomCard = () => {
    getUserInfo();
    GetCardToRate(msalInstance).then(record => {
      const newCard = new MagicCard(record.card);
      setCard(newCard);
      setCardId(record.id);
      setLoading(false);
      setErrorMessage("");
    }).catch(error => {
      setErrorMessage(error.message);
      setLoading(false);
      console.error("Error in getRandomCard: " + error);
    });
  };

  const getTopCards = () => {
    GetTopCards(msalInstance).then(records => {
      setTopCards(records);
    }).catch(error => {
      setLoading(false);
      console.error("Error in getTopCards: " + error);
    });
  };

  const rateCard = (cardId: string, rating: number) => {
    setLoading(true);
    RateCard(cardId, rating, msalInstance).then(() => {
      getRandomCard();
    }).catch(error => {
      setLoading(false);
      console.error("Error in rateCard: " + error);
    });
  };

  const loadingElement = 
    <div style={{padding:20}}>
      <h1>
        Retrieving a card to rate...
      </h1>
      <Loader />
    </div>

  const errorElement =
      <div style={{padding:20}}>
          {errorMessage}
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

  topCards.forEach((cardRecord, index) => {
    let entry = {
      key: cardRecord.id,
      card:
        <table>
          <tbody>
            <tr>
              <td>
                <div>
                  <span style={{fontSize:20, marginRight:5}}><a href={cardRecord.card.url} target="_blank">#{index+1}</a></span>
                  <Rate defaultValue={cardRecord.rating.averageScore} disabled={true} allowHalf={true}></Rate>
                </div>
              </td>
            </tr>
            <tr>
              <td>
                <div>
                  <CardPreview card={new MagicCard(cardRecord.card)} cardWidth={cardWidth} />
                </div>
              </td>
            </tr>
          </tbody>
        </table>
    }
    dataSource.push(entry);
  })

  let userCardDisplay = <div></div>
  if (card)
  {
    userCardDisplay = 
    <div>
      <div style={{display: "flex", flexWrap: "wrap", justifyContent: "center", backgroundColor:"whitesmoke"}}>
        <table>
          <tbody>
            <tr style={{justifyContent:"center", display:"grid", marginBottom:-25}}>
              <td style={{maxWidth:500, margin:5}}>
                <h3>Prompt: "<i>{card.userPrompt}</i>"</h3>
              </td>
            </tr>
            <tr>
              <td>
                <div className="cardContainer" key={`card-rate-container-${card.id}`} style={{marginTop:15}}>
                  <CardDisplay 
                    key={`card-rate-display-${card.id}`} 
                    card={card} 
                    showCardMenu={false} 
                    cardWidth={cardWidth} 
                    allowImagePreview={false} 
                    allowEdits={false} 
                    allowImageUpdate={false} />
                </div>
              </td>
            </tr>
            <tr style={{textAlign: "center"}}>
              <td>
                <h4>Submit Rating For <a href={card.url} target="_blank" style={{textDecoration:"none"}}>{card.name}</a>? (1-5 Stars)</h4>
              </td>
            </tr>
            { user &&
            <tr style={{textAlign: "center"}}>
              <td>
                <Rate className='card-rating' style={{marginTop:0, width: `$cardWidth}px`}} onChange={(rating) => rateCard(cardId, rating)} ></Rate>
              </td>
            </tr>
            }
            <tr style={{display:"grid", marginTop:10}}>
              <td style={{textAlign:"right"}}>
                <Tag style={{fontSize:10, marginLeft:0}} color="geekblue">You've Rated {user != undefined ? user.numberOfCardsRated : 0} Cards</Tag>
              </td>
            </tr>
            <tr style={{justifyContent:"center", marginTop:5, display:"grid", backgroundColor: "#857dff0f"}}>
              <td>
                <h2>Top 100 User Cards</h2>
              </td>
            </tr>
            <tr>
              <td>
                <Table showHeader={false} className='leaderboard-table' bordered={true} dataSource={dataSource} columns={columns} pagination={{pageSize:20, showSizeChanger:false}} />
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  }

  let pageDisplay = userCardDisplay;
  if (errorMessage) {
    pageDisplay = errorElement
  } else if (loading) {
    pageDisplay = loadingElement
  }

  return (
    <div>
      <AuthenticatedTemplate>
        {pageDisplay}
      </AuthenticatedTemplate>
      <UnauthenticatedTemplate>
        <div style={{ margin:10 }}>
          <h1>Please login.</h1>
        </div>
      </UnauthenticatedTemplate>
    </div>
  );
}