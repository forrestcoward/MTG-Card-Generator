import React, { useState } from 'react';
import { Card, Row, Col, Button } from 'antd';
import { useDrag } from '@use-gesture/react';
import { EventMessage, EventType, PublicClientApplication } from '@azure/msal-browser';
import { CardDisplay, MagicCard } from './Card';
import { Loader } from './Loader';
import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react';
import { CardBattleResult, GenerateCardBattle } from './CallAPI';

import "./mana.min.css";
import "./mtg-card.css";
import "./app.css";
import { setCardContainerSize } from './Utility';
import { DislikeFilled, DislikeOutlined, LikeOutlined } from '@ant-design/icons';


interface ComparisonControlProps {
    element1: MagicCard;
    element2: MagicCard;
}

interface BattleWindowProps {
  msalInstance: PublicClientApplication;
}

interface BattleWindowState {
  cardA: MagicCard | undefined;
  cardAId: string;
  cardB: MagicCard | undefined;
  cardBId: string;
  loading: boolean;
}

export class BattleWindow extends React.Component<BattleWindowProps, BattleWindowState> {
  constructor(props: BattleWindowProps) {
    super(props);

    this.state = {
      cardA: undefined,
      cardAId: "",
      cardB: undefined,
      cardBId: "",
      loading: true
    };

    setCardContainerSize();
    this.generateCardBattle();

    this.props.msalInstance.addEventCallback((message: EventMessage) => {
      if (message.eventType === EventType.LOGIN_SUCCESS) {
        //this.generateCardBattle();
      }
    });
  }

  generateCardBattle() {
    GenerateCardBattle(this.props.msalInstance).then((cardGenerationRecords) => {
      var cardA = new MagicCard(cardGenerationRecords[0].magicCards[0]);
      var cardB = new MagicCard(cardGenerationRecords[1].magicCards[0]);
      this.setState({cardA: cardA, cardAId: cardGenerationRecords[0].id, cardB: cardB, cardBId: cardGenerationRecords[1].id, loading: false})
    }).catch((error) => {
      console.log(error)
    })
  }

  declareCardBattleResult(winnerId: string, loserId: string) {
    CardBattleResult(winnerId, loserId, this.props.msalInstance).then(() => {
      this.setState({loading: true})
      this.generateCardBattle()
    }).catch((error) => {
      console.log(error)
    })
  }

  render() {
    var loading = 
    <div style={{padding: "20px"}}>
      <h1>
        Generating battle...
      </h1>
      <Loader />
    </div>

    //var display = <ComparisonControl element1={<CardDisplay card={this.state.cardA!} />} element2={<CardDisplay card={this.state.cardB!} />} />

    //var pageDisplay = this.state.loading ? loading : <ComparisonControl element1={this.state.cardA!} element2={this.state.cardB!} />

    var userCardDisplay = <div></div>
if (this.state.cardA && this.state.cardB)
{
  userCardDisplay = 
  <div>
    <h1 style={{justifyContent: "center"}}>It's time to duel...</h1>
    <div style={{display: "flex", flexWrap: "wrap", justifyContent: "center"}}>
      <div style={{display: "flex"}}>
        <div className="cardContainer" key={`card-container-${this.state.cardA.id}`}>
          <CardDisplay key={`card-display-${this.state.cardA.id}`} card={this.state.cardA} showCardMenu={false} />
          <Button onClick={this.declareCardBattleResult.bind(this, this.state.cardAId, this.state.cardBId)} style={{ marginTop: '10px', justifyContent: 'center', height:"50px" }}>
            <LikeOutlined style={{fontSize: '40px'}} />
                          </Button>
      </div>

      </div>
      <div style={{display: "flex"}}>
      <div className="cardContainer" key={`card-container-${this.state.cardB.id}`}>
        <CardDisplay key={`card-display-${this.state.cardB.id}`} card={this.state.cardB} showCardMenu={false} />
        <Button onClick={this.declareCardBattleResult.bind(this, this.state.cardBId, this.state.cardAId)} style={{ marginTop: '10px', height:"50px", justifyContent: "center" }}>
        <LikeOutlined style={{fontSize: '40px'}} />
                        </Button>
      </div>
      </div>
    </div>
    </div>

    let singleCardDispay =
    <div>
      <div className="cardContainer" key={`card-container-${this.state.cardA.id}`}>
          <CardDisplay key={`card-display-${this.state.cardA.id}`} card={this.state.cardA} showCardMenu={false} />
          <Button onClick={this.declareCardBattleResult.bind(this, this.state.cardAId, this.state.cardBId)} style={{ marginTop: '10px', justifyContent: 'center', height:"50px" }}>
            <LikeOutlined style={{fontSize: '40px'}} />
          </Button>
          <Button onClick={this.declareCardBattleResult.bind(this, this.state.cardAId, this.state.cardBId)} style={{ marginTop: '10px', justifyContent: 'center', height:"50px" }}>
            <DislikeOutlined style={{fontSize: '40px'}} />
          </Button>
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

const ComparisonControl: React.FC<ComparisonControlProps> = ({ element1, element2 }) => {
    const [selected, setSelected] = useState<number | null>(null);

    const bind = useDrag(({ swipe: [swipeX] }) => {
        if (swipeX === 1) { // swiped right
            setSelected(1);
        } else if (swipeX === -1) { // swiped left
            setSelected(2);
        }
    }); // Adjust swipeVelocity as needed

    return (
        <Row gutter={16}>
            <Col span={12}>
                <div {...bind()}>
                    <Card>
                      <div className="cardContainer" key={`card-container-${element1.id}`}>
                        <CardDisplay key={`card-display-${element1.id}`} card={element1} showCardMenu={false} />
                      </div>
                    </Card>
                    <Button onClick={() => setSelected(1)} style={{ marginTop: '10px' }}>
                          Vote for Element 1
                      </Button>
                </div>
            </Col>
            <Col span={12}>
                <div {...bind()}>
                    <Card>
                       <div className="cardContainer" key={`card-container-${element2.id}`}>
                        <CardDisplay key={`card-display-${element2.id}`} card={element2} showCardMenu={false} />
                      </div>
                    </Card>
                    <Button onClick={() => setSelected(2)} style={{ marginTop: '10px' }}>
                            Vote for Element 2
                        </Button>
                </div>
            </Col>
        </Row>
    );
};

export default ComparisonControl;