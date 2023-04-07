import React from 'react';
import { BasicCard, CardDisplay, MagicCard  } from './Card';
import { GenerateMagicCardRequest } from './OpenAI';
import "./mtg-card.css";
import "./app.css";
import wizardImage from './card-backgrounds/wizard.png'

export interface MTGCardGeneratorProps { }

export interface MTGCardGeneratorState {
  openAIPrompt: string,
  openAIResponse: string,
  generateButtonDisabled: boolean,
  cards: MagicCard[],
  currentError: string
}

const defaultPrompt:string = "Generate me one Magic: The Gathering card from the Dominaria plane."

const _tutorialCard:BasicCard = {
  name: "The Magic: The Gathering Card Creator",
  manaCost: "{6}",
  typeLine: "Legendary Creature — Artificer God",
  type: "Artifact",
  text: "",
  rawOracleText: "Haste, Hexproof\n{T}: Enter a prompt above and hit \"Generate!\" to generate a unique Magic: The Gathering card into my spell book",
  modifiedOracleText: "",
  power: 0,
  toughness: 0,
  colorIdentity: "Blue",
  pt: "6/6",
  flavorText: "\"I recall the huge design teams employed to devise even the simplest cards. Even the most intelligent of designers will never hope to match again the execution and creativity of modern machines.\"\n - The Creator",
  rarity: "Mythic",
  imageUrl: wizardImage,
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
          {/*<textarea value={this.state.openAIPrompt} onChange={this.handleChange} rows={10} cols={120} />*/}
          <input type="text" className="userPrompt" onChange={this.handleChangeInput} value={this.state.openAIPrompt} height={"100px"} />
        </label>
        <p></p>
        <button className="generateButton" type="submit" onClick={() => this.handleSubmit()} disabled={this.state.generateButtonDisabled}>Generate!</button>
        <h2 style={{ color: 'red' }}>{this.state.currentError}</h2>
        <textarea value={this.state.openAIResponse} readOnly={true} rows={30} cols={120} hidden={true} />
        </div>
        <div className="cardsContainer">
          {/*
          <div>
            <img src=".\card-backgrounds\staff.png" className="wizardStaffLoading"></img>
          </div>
    */}
        {
          this.state.cards.map(card => (
            <div key={card.name + -"display"}>
              <CardDisplay key={card.name} card={card} />
            </div>
          ))
        }
        </div>
      </div>
    );
  }
}
