import React from 'react';
import { CardDisplay, MagicCard  } from './Card';
import { GenerateMagicCardRequest, MakeOpenAIImageCreateRequest } from './OpenAI';
import "./mtg-card.css";
const config = require('../config.json');

export interface MTGCardGeneratorProps { }

export interface MTGCardGeneratorState {
  openAIPrompt: string,
  openAIResponse: string,
  generateButtonDisabled: boolean,
  cards: MagicCard[],
  currentError: string
}

const defaultPrompt:string = "Generate me one Magic: The Gathering card from the Dominaria plane."

export class MTGCardGenerator extends React.Component<MTGCardGeneratorProps, MTGCardGeneratorState> {
  constructor(props: MTGCardGeneratorProps) {
    super(props);
    this.state = {
      openAIPrompt: defaultPrompt,
      openAIResponse: '',
      generateButtonDisabled: false,
      cards: [],
      currentError: '',
    };

    this.handleChange = this.handleChange.bind(this);
    this.handleSubmit = this.handleSubmit.bind(this);
  }

  handleChange(event: React.ChangeEvent<HTMLTextAreaElement>) {
    this.setState({ openAIPrompt: event.target.value });
  }

  handleSubmit() {
    this.setState({ generateButtonDisabled: true, currentError: "" })

    var userPrompt = this.state.openAIPrompt + "\n Only return JSON. Do not explain yourself."

    GenerateMagicCardRequest(userPrompt).then(cards => {
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
    }).catch((error: Error) => {
      this.setState({ generateButtonDisabled: false, currentError: error.message + ": " + error.stack })
    });
  }

  render() {
    return (
      <div>
        <p>Please tell me what type of Magic: The Gathering card you would like me to generate:</p>
        <label>
          <textarea value={this.state.openAIPrompt} onChange={this.handleChange} rows={10} cols={120} />
        </label>
        <p></p>
        <button type="submit" onClick={() => this.handleSubmit()} disabled={this.state.generateButtonDisabled}>Generate</button>
        <h2 style={{ color: 'red' }}>{this.state.currentError}</h2>
        <p></p>
        Here is the latest API response:
        <p></p>
        <textarea value={this.state.openAIResponse} readOnly={true} rows={30} cols={120} hidden={false} />
        {
          this.state.cards.map(card => (
            <div key={card.name + -"display"}>
              <CardDisplay key={card.name} card={card} />
            </div>
          ))
        }
      </div>
    );
  }
}
