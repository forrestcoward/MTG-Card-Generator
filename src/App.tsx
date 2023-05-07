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
  settings: SettingGroup[],
  cards: MagicCard[],
  currentError: string,
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
const defaultModel:string= "gpt-4"

const modelSettings = [
  { name: "GPT 4", id: "gpt-4", value: false, group: "model-settings", description: "The most advanced model to date. Will generate the most unique cards, but is slower than other models." },
  { name: "GPT 3.5", id: "gpt-3.5", value: true, group: "model-settings", description: "Less powerful than GPT 4, but faster and less expensive. The default."},
]

const modelSettingsGroup : SettingGroup = {
  name: "Model",
  description: "Which langauge model to use when generating Magic cards.",
  settings: modelSettings,
}

const cardGenerationSettings = [
  { name: "Explain Yourself", id: "setting-provide-explanation", value: false, description: "Include an explanation below the card explaining the reasoning behind its creation. The AI can be even be quite funny! This will slow down card generation and will not apply to cards generated without this setting on." },
]

const cardGenerationSettingsGroup : SettingGroup = {
  name: "Card Generation",
  description: "Settings which affect what information to generate.",
  settings: cardGenerationSettings,
}

export class MTGCardGenerator extends React.Component<MTGCardGeneratorProps, MTGCardGeneratorState> {
  constructor(props: MTGCardGeneratorProps) {
    super(props);
    this.state = {
      prompt: '',
      response: '',
      isLoading: false,
      isSettingsOpen: false,
      settings: [modelSettingsGroup, cardGenerationSettingsGroup],
      cards: [TutorialCard],
      currentError: '',
    };

    this.handleSubmit = this.handleSubmit.bind(this);
    this.handleChangeInput = this.handleChangeInput.bind(this);
    this.setCardContainerSize();
  }

  allSettings() : Setting[] {
    return this.state.settings.map(settingGroup => settingGroup.settings).flat();
  }

  showCardExplanations() : boolean {
    return this.allSettings().find(setting => setting.id == "setting-provide-explanation")?.value ?? true;
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

  setCardContainerSize() {
    const cardContainerClass = '.card-container';
    const cardContainerRule = findCSSRule(cardContainerClass);
    const cardMetaClass = '.card-meta'
    const cardMetaRule = findCSSRule(cardMetaClass);
    // Scale the card entirely based on the card width.
    const vw = Math.max(document.documentElement.clientWidth || 0, window.innerWidth || 0)
    const cardWidth = Math.min(440, vw)
    const cardHeight = ((cardWidth * 3.6) / 2.5);

    if (cardContainerRule) {
      cardContainerRule.style.width  = `${cardWidth}px`;
      cardContainerRule.style.height = `${cardHeight}px`;

      // Card explainations.
      if (cardMetaRule) {
        cardMetaRule.style.width  = `${cardWidth-24}px`;
      }
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
    let modelSetting = this.allSettings().find(setting => setting.value == true && setting?.group == "model-settings")
    if (modelSetting) {
      model = modelSetting.id
    }

    GenerateMagicCardRequest(userPrompt, model, this.showCardExplanations()).then(cards => {
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
          settings={this.state.settings} 
          onModelSettingsChange={this.handleSettingUpdate} 
          isOpen={this.state.isSettingsOpen} 
          onClose={this.toggleIsSettingsOpen} />
      </div>
    );
  }
}
