import React from 'react';
import { Card, CardDisplay, getCardImagePrompt, setCardManaCostTokens } from './Card';
import { MakeOpenAIChatComletionRequest, MakeOpenAIImageCreateRequest } from './OpenAI';
import "./mtg-card.css";
const config = require('../config.json');

export interface MTGCardGeneratorProps {
}

export interface MTGCardGeneratorState {
  openAIPrompt: string,
  openAIResponse: string,
  generateButtonDisabled: boolean,
  cards: Card[]
}

const systemPrompt: string = `You are an assistant who works as a Magic: The Gathering card designer. You like complex cards with interesting mechanics.

The output must also follow the Magic "color pie" design rules. For each card, your output be JSON which includes "name", "manaCost", "type", "text", "flavorText", "pt", and "rarity" fields. Also include a JSON field the includes a reason why you generated the card the way you did called "reason". You also should include a JSON field that is a funny reason why you generated the card the way you did called "funnyReason". Do not explain the card, only give JSON. If you generate multiple cards, each card should be placed in a JSON array field named "cards".`

export class MTGCardGenerator extends React.Component<MTGCardGeneratorProps, MTGCardGeneratorState> {
  constructor(props: MTGCardGeneratorProps) {
    super(props);
    this.state = {
      openAIPrompt: 'Generate me two Magic: The Gathering cards from the Dominaria plane.',
      openAIResponse: '',
      generateButtonDisabled: false,
      cards: []
    };

    this.handleChange = this.handleChange.bind(this);
    this.handleSubmit = this.handleSubmit.bind(this);
  }

  handleChange(event: React.ChangeEvent<HTMLTextAreaElement>) {
    this.setState({ openAIPrompt: event.target.value });
  }

  handleSubmit(event: React.FormEvent<HTMLFormElement>) {

    this.setState({ generateButtonDisabled: true })

    MakeOpenAIChatComletionRequest(config.OpenAIApiKey, systemPrompt, this.state.openAIPrompt).then(resp => {
      var cards: Card[] = JSON.parse(resp)["cards"]

      cards.forEach(card => {
        setCardManaCostTokens(card)
      });

      const promises: Promise<void>[] = [];
      cards.forEach(card => {
        var prompt = getCardImagePrompt(card);
        promises.push(MakeOpenAIImageCreateRequest(config.OpenAIApiKey, prompt).then(imageUrl => {
          card.imageUrl = imageUrl;
        }));
      });

      Promise.all(promises).then(() => {
        this.setState({
          openAIResponse: resp,
          cards: [...cards, ...this.state.cards],
          generateButtonDisabled: false
        })
      })
    }).catch((error: Error) => {
      this.setState({ generateButtonDisabled: false })
    });

    event.preventDefault();
  }

  render() {
    return (
      <form onSubmit={this.handleSubmit}>
        <p>Please tell me what type of Magic: The Gathering card you would like me to generate:</p>
        <label>
          <textarea value={this.state.openAIPrompt} onChange={this.handleChange} rows={10} cols={120} />
        </label>
        <p></p>
        <input type="submit" value="Generate" disabled={this.state.generateButtonDisabled} />
        <p></p>
        <textarea value={this.state.openAIResponse} readOnly={true} rows={30} cols={120} hidden={true} />
        {
          this.state.cards.map(card => (
            <CardDisplay key={card.name} card={card} />
          ))
        }
      </form>
    );
  }
}
