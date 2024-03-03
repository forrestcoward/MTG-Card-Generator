import { EventMessage, EventType } from "@azure/msal-browser";
import React, { useEffect, useState } from "react";
import { CardDisplay, MagicCard } from "./Card";
import { GetUserMagicCards } from "./CallAPI";

import "./mtg-card.css";
import "./app.css";
import { setCardContainerSize } from "./Utility";
import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from "@azure/msal-react";
import { Loader } from "./Loader";
import { SettingFilled } from "@ant-design/icons";
import { Button, Tooltip } from "antd";
import PopOutSettingsMenu from "./PopOutSettingsMenu";

export function MyCards() {
  const [cards, setCards] = React.useState<MagicCard[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [errorMessage, setErrorMessage] = React.useState("");
  const [cardWidth ] = React.useState(setCardContainerSize());
  const [settings] = useState([]);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const { instance: msalInstance } = useMsal();

  useEffect(() => {
    getUserCards();

    msalInstance.addEventCallback((message: EventMessage) => {
      if (message.eventType === EventType.LOGIN_SUCCESS) {
        getUserCards();
      }
    });
  }, [])

  function getUserCards() {
    GetUserMagicCards(msalInstance).then((records) => {
      setCards(records.map(x => new MagicCard(x.card)));
      setErrorMessage("");
      setLoading(false);
    }).catch((error) => {
      setErrorMessage(error.message);
      setLoading(false);
      console.error(error)
    })
  }

  const loadingElement = 
    <div style={{padding:20}}>
      <h1>
        Loading your cards...
      </h1>
      <Loader />
    </div>

  const errorElement =
    <div style={{padding:20}}>
        {errorMessage}
    </div>

  const userCardDisplay =
    <div>
      <div style={{position:'absolute', right:0, margin:10, marginRight:20}}>
        <Tooltip title="Settings">
          <Button icon={<SettingFilled className="spinningIcon" />} type="text" onClick={() => setIsSettingsOpen(true)} className='settingButton' />
        </Tooltip>
      </div>

      <div className="cardsContainer">
      {
        cards.map(card => (
          <div className="cardContainer" key={`card-container-${card.id}`}>
            <CardDisplay 
              key={`card-display-${card.id}`} 
              card={card} showCardMenu={true} 
              cardWidth={cardWidth} 
              allowImagePreview={true} 
              allowEdits={true} 
              allowImageUpdate={true} />
          </div>
        ))
      }
      </div>
      <PopOutSettingsMenu 
        settings={settings}
        onModelSettingsChange={() => {}}
        isOpen={isSettingsOpen} 
        onClose={()=> {setIsSettingsOpen(false)}} />
    </div>

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
        <div style={{margin:10}}>
          <h1>Please login.</h1>
        </div>
      </UnauthenticatedTemplate>
    </div>
  )
}