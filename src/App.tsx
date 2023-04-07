import React from 'react';
import { BasicCard, CardDisplay, MagicCard  } from './Card';
import { GenerateMagicCardRequest } from './OpenAI';
import "./mtg-card.css";
import "./app.css";
// @ts-ignore
import tutorialCardWizardImage from './card-backgrounds/wizard.png'
// @ts-ignore
import loadingStaffIcon from './card-backgrounds/staff.png'

export interface MTGCardGeneratorProps { }

export interface MTGCardGeneratorState {
  openAIPrompt: string,
  openAIResponse: string,
  generateButtonDisabled: boolean,
  cards: MagicCard[],
  currentError: string,
}

const defaultPrompt:string = "Generate me one Magic: The Gathering card from the Dominaria plane."

const _tutorialCard:BasicCard = {
  name: "The Magic: The Gathering Card Creator",
  manaCost: "{6}",
  typeLine: "Legendary Creature — Artificer God",
  type: "Artifact",
  rawOracleText: "Haste, Hexproof\n{T}: Enter a prompt above and hit \"Generate!\" to generate a unique Magic: The Gathering card into my spell book",
  text: "",
  modifiedOracleText: "",
  colorIdentity: "Colorless",
  pt: "6/6",
  power: 6,
  toughness: 6,
  flavorText: "\"I recall the huge design teams employed to devise even the simplest cards. Even the most intelligent of designers will never hope to match the execution and creativity of modern machines. I respect them only as much as they have paved the way, but we will not be looking backwards.\"\n - The Creator",
  rarity: "Mythic",
  imageUrl: tutorialCardWizardImage,
}

const tutorialCard:MagicCard = new MagicCard(_tutorialCard);

export class MTGCardGenerator extends React.Component<MTGCardGeneratorProps, MTGCardGeneratorState> {
  constructor(props: MTGCardGeneratorProps) {
    super(props);
    this.state = {
      openAIPrompt: defaultPrompt,
      openAIResponse: '',
      generateButtonDisabled: false,
      cards: [tutorialCard],
      currentError: '',
    };

    this.handleChange = this.handleChange.bind(this);
    this.handleSubmit = this.handleSubmit.bind(this);
    this.handleChangeInput = this.handleChangeInput.bind(this);
  }

  getLoadingClassName() : string{
    if (this.state.generateButtonDisabled) {
      return "wizardStaffLoading"
    } else {
      return ""
    }
  }

  handleChange(event: React.ChangeEvent<HTMLTextAreaElement>) {
    this.setState({ openAIPrompt: event.target.value });
  }

  handleChangeInput(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({ openAIPrompt: event.target.value });
  }

  handleSubmit() {
    this.setState({ generateButtonDisabled: true, currentError: "" })

    var userPrompt = this.state.openAIPrompt

    GenerateMagicCardRequest(userPrompt).then(cards => {

      /*
      const promises: Promise<any>[] = [];
      cards.forEach(card => {
        var prompt = card.openAIImagePrompt;
        promises.push(MakeOpenAIImageCreateRequest(config.OpenAIApiKey, prompt).then(imageUrl => {
          card.imageUrl = imageUrl;
        }));
      });

      Promise.all(promises).then(() => {
        this.setState({
          openAIResponse: JSON.stringify(cards),
          cards: [...cards, ...this.state.cards],
          generateButtonDisabled: false
        })
      })
      */

      this.setState({
        openAIResponse: JSON.stringify(cards),
        cards: [...cards, ...this.state.cards],
        generateButtonDisabled: false
      })

    }).catch((error: Error) => {
      this.setState({ generateButtonDisabled: false, currentError: error.message + ": " + error.stack })
    });
  }

  render() {
    return (
      <div className="outerContainer">
        <div className="container">
        <p>Generate me a Magic: The Gathering card that...</p>
        <label>
          <input type="text" className="userPrompt" onChange={this.handleChangeInput} value={this.state.openAIPrompt} height={"100px"} />
        </label>
        <p></p>
        <table>
          <tr>
            <td>
              <button className="generateButton" type="submit" onClick={() => this.handleSubmit()} disabled={this.state.generateButtonDisabled}>Generate!</button>
            </td>
            <td>
              <img className={this.getLoadingClassName()} src={loadingStaffIcon} width={"50px"} height={"50px"} />
            </td>
          </tr>
        </table>
        <h2 style={{ color: 'red' }}>{this.state.currentError}</h2>
        <textarea value={this.state.openAIResponse} readOnly={true} rows={30} cols={120} hidden={true} />
        </div>
        <div className="cardsContainer">
        {
          this.state.cards.map(card => (
            <div className="cardContainer" key={card.name + -"display"}>
              <CardDisplay key={card.name} card={card} />
            </div>
          ))
        }
        </div>
      </div>
    );
  }
}
