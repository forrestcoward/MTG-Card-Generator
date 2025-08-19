import { EventMessage, EventType } from "@azure/msal-browser";
import React, { useEffect, useState } from "react";
import { GetUserMagicCards } from "./CallAPI";

import "./mtg-card.css";
import "./app.css";
import { isMobileDevice, setCardContainerSize } from "./Utility";
import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from "@azure/msal-react";
import { Loader } from "./Loader";
import PopOutSettingsMenu from "./PopOutSettingsMenu";
import { SettingsButton } from "./SettingsButton";
import { TkCardRenderer } from "./mtg-tk-card-renderer/TkCardRenderer";
import { CardRenderer } from "./CardRenderer";
import { MagicCard } from "./models/MagicCard";
import { SettingGroup, Setting } from "./MTGCardGenerator";

const cardDisplaySettings = [
  { name: 'Default', id: 'legacy-renderer', value: true, description: '', group: 'display-settings' },
  { name: 'MTGRender.TK', id: 'new-renderer', value: false, description: 'Render cards using the layout seen at https://www.mtgrender.tk', group: 'display-settings' },
]

const cardDisplaySettingsGroup : SettingGroup = {
  name: 'Card Display',
  description: 'Choose how cards are rendered on the page.',
  type: 'radio',
  settings: cardDisplaySettings,
}

export function MyCards() {
  const [cards, setCards] = React.useState<MagicCard[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [errorMessage, setErrorMessage] = React.useState("");
  const [cardWidth ] = React.useState(setCardContainerSize());
  const [settings, setSettings] = useState([cardDisplaySettingsGroup]);
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

  function allSettings(): Setting[] {
    return settings.map(settingGroup => settingGroup.settings).flat();
  }

  function useNewRenderer(): boolean {
    let setting = allSettings().find(setting => setting.value == true && setting?.group == 'display-settings')
    return setting?.id === 'new-renderer'
  }

  function handleSettingUpdate(updatedSetting: string, newValue: boolean) {
    // Flatten all settings.
    var newSettings = [...settings]
    var allSettings = newSettings.map(settingGroup => settingGroup.settings).flat();

    // Update the matching setting.
    let matchingSetting = allSettings.find(setting => setting.name == updatedSetting)
    if (matchingSetting && (matchingSetting.value != true || !matchingSetting.group)) {
      matchingSetting.value = newValue
    }

    // Set all other settings with the same group to false (if a group is specified).
    // For example, use this to allow only a single model setting.
    allSettings.forEach((setting) => {
      if (setting.name != updatedSetting && setting.group && setting.group == matchingSetting?.group) {
        setting.value = false
      }
    });

    setSettings(newSettings);
  };

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
      <div style={{position:'absolute', right:0, margin:10, marginRight:20, display: !isMobileDevice() ? 'block' : 'none'}}>
        <SettingsButton onClick={() => setIsSettingsOpen(true)} />
      </div>

      <div className="cardsContainer">
      {
        cards.map(card => (
          <div className="cardContainer" key={`card-container-${card.id}`}>
            {useNewRenderer() ? (
              <TkCardRenderer 
                key={`mtgrender-card-display-${card.id}`} 
                card={card} 
                showCardMenu={true} 
                cardWidth={cardWidth} 
                allowImagePreview={true} 
                allowEdits={true} 
                allowImageUpdate={true} />
            ) : (
              <CardRenderer 
                key={`card-display-${card.id}`} 
                card={card} 
                showCardMenu={true} 
                cardWidth={cardWidth} 
                allowImagePreview={true} 
                allowEdits={true} 
                allowImageUpdate={true} />
            )}
          </div>
        ))
      }
      </div>
      <PopOutSettingsMenu 
        settings={settings}
        onModelSettingsChange={handleSettingUpdate}
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
