import React from 'react';
import { CardDisplay, MagicCard  } from './Card';
import { GenerateMagicCardRequest } from './OpenAI';
import { findCSSRule } from './Utility';
import { TutorialCard } from './TutorialCard';
import PopOutSettingsMenu from './PopOutSettingsMenu';
import "./mtg-card.css";
import "./app.css";

// @ts-ignore
import loadingIcon from './card-backgrounds/staff.png'
// @ts-ignore
import settingsIcon from './card-backgrounds/settings.png'

export interface MTGCardGeneratorProps { }

export interface MTGCardGeneratorState {
  prompt: string,
  response: string,
  isLoading: boolean,
  isSettingsOpen: boolean,
  modelSettings: Setting[],
  cards: MagicCard[],
  currentError: string,
}

export interface Setting {
  name: string;
  id: string;
  value: boolean;
  description: string;
}

const defaultPrompt:string = "is from the Dominaria plane."
const defaultModel:string= "gpt-4"

export class MTGCardGenerator extends React.Component<MTGCardGeneratorProps, MTGCardGeneratorState> {
  constructor(props: MTGCardGeneratorProps) {
    super(props);
    this.state = {
      prompt: '',
      response: '',
      isLoading: false,
      isSettingsOpen: false,
      modelSettings: [
        { name: "GPT 4", id: "gpt-4", value: false, description: "The most advanced model to date. Will generate the most unique cards, but is slower than other models." },
        { name: "GPT 3.5", id: "gpt-3.5", value: true, description: "Less powerful than GPT 4, but faster and less expensive. The default." },
      ],
      cards: [TutorialCard],
      currentError: '',
    };

    this.handleSubmit = this.handleSubmit.bind(this);
    this.handleChangeInput = this.handleChangeInput.bind(this);
    this.setCardContainerSize();
  }

  toggleIsSettingsOpen = () => {
    this.setState({ isSettingsOpen: !this.state.isSettingsOpen });
  };

  handleModelSettingUpdate = (setting: string, newValue: boolean) => {
    // Update the matching setting.
    let matchingSetting = this.state.modelSettings.find(modelSetting => modelSetting.name == setting)
    if (matchingSetting && matchingSetting.value != true) {
      matchingSetting.value = newValue;
    }

    // Set all other settings to false because only a single model can be selected.
    this.state.modelSettings.forEach((modelSetting) => {
      if (modelSetting.name != setting) {
        modelSetting.value = false;
      }
    });

    this.setState({ modelSettings: this.state.modelSettings });
  };

  setCardContainerSize() {
    const cardContainerClass = '.card-container';
    const cardContainerRule = findCSSRule(cardContainerClass);
    // Scale the card entirely based on the card width.
    const vw = Math.max(document.documentElement.clientWidth || 0, window.innerWidth || 0)
    const cardWidth = Math.min(440, vw)
    const cardHeight = ((cardWidth * 3.6) / 2.5);

    if (cardContainerRule) {
      cardContainerRule.style.width  = `${cardWidth}px`;
      cardContainerRule.style.height = `${cardHeight}px`;
    }

    const cardBackgroundClass = '.card-background';
    const cardBackgroundRule = findCSSRule(cardBackgroundClass);
    if (cardBackgroundRule) {
      cardBackgroundRule.style.height = `${cardHeight - 90}px`;
    }
  }

  getLoadingClassName() : string{
    return this.state.isLoading ? "loadingAnimation loadingIcon" : "loadingIcon";
  }

  handleChangeInput(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({ prompt: event.target.value });
  }

  handleSubmit() {
    this.setState({ isLoading: true, currentError: "" })
    let userPrompt = this.state.prompt

    let model = defaultModel
    let modelSetting = this.state.modelSettings.find(modelSetting => modelSetting.value == true)
    if (modelSetting) {
      model = modelSetting.id
    }

    GenerateMagicCardRequest(userPrompt, model).then(cards => {
      this.setState({
        response: JSON.stringify(cards),
        cards: [...cards, ...this.state.cards],
        isLoading: false
      })

    }).catch((error: Error) => {
      this.setState({ isLoading: false, currentError: error.message + ": " + error.stack })
    });
  }

  render() {
    return (
      <div className="outerContainer">
        <div className="container">
        <p>Generate me a Magic: The Gathering card that...</p>
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
              <CardDisplay key={`card-display-${card.id}`} card={card} />
            </div>
          ))
        }
        </div>
        <PopOutSettingsMenu 
          modelSettings={this.state.modelSettings} 
          onModelSettingsChange={this.handleModelSettingUpdate} 
          isOpen={this.state.isSettingsOpen} 
          onClose={this.toggleIsSettingsOpen} />
      </div>
    );
  }
}
