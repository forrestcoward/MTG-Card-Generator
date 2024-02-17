import React from 'react';
import { CardDisplay, MagicCard  } from './Card';
import { GenerateMagicCardRequest } from './CallAPI';
import { isMobileDevice, setCardContainerSize } from './Utility';
import { TutorialCard } from './TutorialCard';
import PopOutSettingsMenu from './PopOutSettingsMenu';
import { PublicClientApplication } from '@azure/msal-browser';

import "./mana.min.css";
import "./mtg-card.css";
import "./app.css";

// @ts-ignore
import loadingIcon from './card-backgrounds/staff.png'
// @ts-ignore
import settingsIcon from './card-backgrounds/settings.png'

export interface MTGCardGeneratorProps { 
  msalInstance: PublicClientApplication;
}

export interface MTGCardGeneratorState {
  prompt: string,
  response: string,
  isLoading: boolean,
  isSettingsOpen: boolean,
  settings: SettingGroup[],
  userOpenAIKey: string,
  cards: MagicCard[],
  currentError: string,
  userName: string,
  defaultCardWidth: number,
}

export interface SettingGroup {
  name: string;
  description: string;
  settings: Setting[];
}

export interface Setting {
  name: string;
  id: string;
  value: boolean;
  description: string;
  group?: string;
}

const defaultPrompt:string = "is from the Dominaria plane."
const defaultModel:string= "gpt-4-turbo-preview"

const modelSettings = [
  { name: "GPT 4", id: "gpt-4-turbo-preview", value: false, group: "model-settings", description: "The most advanced model, but slower." },
  { name: "GPT 3.5", id: "gpt-3.5", value: true, group: "model-settings", description: "Less intelligent than GPT 4, but faster."},
]

const modelSettingsGroup : SettingGroup = {
  name: "Model",
  description: "Language model to use.",
  settings: modelSettings,
}

const cardGenerationSettings = [
  { name: "Explain Yourself", id: "setting-provide-explanation", value: false, description: "Explain why the card was generated. The AI can be even be quite funny! May slow down card generation." },
  { name: "High Quality Images", id: "setting-high-quality-images", value: false, description: "Generate the highest quality art using the state of the art. May slow down card generation considerably." },
]

const cardGenerationSettingsGroup : SettingGroup = {
  name: "Card Generation",
  description: "Customize card generation.",
  settings: cardGenerationSettings,
}

export class MTGCardGenerator extends React.Component<MTGCardGeneratorProps, MTGCardGeneratorState> {
  constructor(props: MTGCardGeneratorProps) {
    super(props);
    let width = setCardContainerSize();
    this.state = {
      prompt: '',
      response: '',
      isLoading: false,
      isSettingsOpen: false,
      settings: [modelSettingsGroup, cardGenerationSettingsGroup],
      userOpenAIKey: '',
      cards: [TutorialCard],
      currentError: '',
      userName: '',
      defaultCardWidth: width
    };

    this.handleSubmit = this.handleSubmit.bind(this);
    this.handleChangeInput = this.handleChangeInput.bind(this);
    this.handleCardWidthChanged = this.handleCardWidthChanged.bind(this);
    setCardContainerSize();
  }

  allSettings() : Setting[] {
    return this.state.settings.map(settingGroup => settingGroup.settings).flat();
  }

  showCardExplanations() : boolean {
    return this.allSettings().find(setting => setting.id == "setting-provide-explanation")?.value ?? true;
  }

  highQualityImages() : boolean {
    return this.allSettings().find(setting => setting.id == "setting-high-quality-images")?.value ?? false;
  }

  toggleIsSettingsOpen = () => {
    this.setState({ isSettingsOpen: !this.state.isSettingsOpen });
  };

  handleSettingUpdate = (updatedSetting: string, newValue: boolean) => {
    // Flatten all settings.
    let allSettings = this.allSettings();

    // Update the matching setting.
    let matchingSetting = allSettings.find(setting => setting.name == updatedSetting)
    if (matchingSetting && (matchingSetting.value != true || !matchingSetting.group)) {
      matchingSetting.value = newValue;
    }

    // Set all other settings with the same group to false (if a group is specified).
    // For example, use this to allow only a single model setting.
    allSettings.forEach((setting) => {
      if (setting.name != updatedSetting && setting.group && setting.group == matchingSetting?.group) {
        setting.value = false;
      }
    });

    this.setState({ settings: this.state.settings });
  };

  handleOpenAIKeyChange = (newUserOpenAIKey: string) => {
    this.setState({ userOpenAIKey: newUserOpenAIKey });
  }

  getLoadingClassName() : string{
    return this.state.isLoading ? "loadingAnimation loadingIcon" : "loadingIcon";
  }

  handleChangeInput(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({ prompt: event.target.value });
  }

  handleCardWidthChanged(event: React.ChangeEvent<HTMLInputElement>) {
    var newWidth = parseInt(event.target.value)
    setCardContainerSize(newWidth);
    this.state.cards[0].adjustFontSize();
  }

  handleSubmit() {
    this.setState({ isLoading: true, currentError: "" })
    let userPrompt = this.state.prompt

    let model = defaultModel
    let modelSetting = this.allSettings().find(setting => setting.value == true && setting?.group == "model-settings")
    if (modelSetting) {
      model = modelSetting.id
    }

    GenerateMagicCardRequest(userPrompt, model, this.showCardExplanations(), this.highQualityImages(), this.state.userOpenAIKey, this.props.msalInstance).then(cards => {
      this.setState({
        response: JSON.stringify(cards),
        cards: [...cards, ...this.state.cards],
        isLoading: false
      })

    }).catch((error: Error) => {
      this.setState({ isLoading: false, currentError: error.message })
    });
  }

  render() {
    return (
      <div>
        <div className="outerContainer">
          <div className="container" style={{position: "relative"}}>
            <div className="notification">
              <b>
                <span style={{marginRight:"5px"}}>
                  New:
                </span>
              </b>
              <a className="pulsingLink" href="/RateCards">
                { !isMobileDevice() ? "Rate Other User's Cards!" : "Rate Cards!" } 
              </a>
            </div>
            <p>Generate a Magic: The Gathering card...</p>
            <label>
              <input type="text" className="userInputPrompt" placeholder={defaultPrompt} onChange={this.handleChangeInput} value={this.state.prompt} />
            </label>
            <p></p>
            <table>
              <tbody>
                <tr>
                  <td>
                    <button className="generateButton" type="submit" onClick={() => this.handleSubmit()} disabled={this.state.isLoading}>Generate!</button>
                  </td>
                  <td>
                  <input type="text" className="cardWidthPrompt" onChange={this.handleCardWidthChanged} defaultValue={this.state.defaultCardWidth} style={{display: "none"}}/>
                  </td>
                  <td>
                    <img className={this.getLoadingClassName()} src={loadingIcon} />
                  </td>
                  <td>
                    <div title="Click to open the settings." className="settingsIconDiv">
                      <img  src={settingsIcon} className="settingsIcon" width={40} height={40} onClick={this.toggleIsSettingsOpen} />
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
            <h2 style={{ color: 'red' }}>{this.state.currentError}</h2>
            <textarea value={this.state.response} readOnly={true} rows={30} cols={120} hidden={true} />
          </div>
          <div className="cardsContainer">
          {
            this.state.cards.map(card => (
              <div className="cardContainer" key={`card-container-${card.id}`}>
                <CardDisplay key={`card-display-${card.id}`} card={card} showCardMenu={true} cardWidth={this.state.defaultCardWidth} allowImagePreview={true} allowEdits={true} />
              </div>
            ))
          }
          </div>
          <PopOutSettingsMenu 
            settings={this.state.settings}
            onModelSettingsChange={this.handleSettingUpdate}
            userOpenAIKey={this.state.userOpenAIKey}
            onOpenAIKeyChange={this.handleOpenAIKeyChange}
            isOpen={this.state.isSettingsOpen} 
            onClose={this.toggleIsSettingsOpen} />
        </div>
      </div>
    );
  }
}
