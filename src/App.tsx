import React from 'react';
import { Card, CardDisplay, getCardImagePrompt, setCardManaCostTokens } from './Card';
import { MakeOpenAIChatComletionRequest, MakeOpenAIImageCreateRequest } from './OpenAI';
import "./mtg-card.css";
const config = require('../config.json');

export interface MTGCardGeneratorProps {
}

export interface MTGCardGeneratorState {
  openAIPrompt: string,
  cardUpgradeRules: string
  openAIResponse: string,
  generateButtonDisabled: boolean,
  cards: Card[]
}

const systemPrompt:string = `You are an assistant who works as a Magic: The Gathering card designer. You like complex cards with interesting mechanics.

The output must also follow the Magic "color pie" design rules. For each card, your output be JSON which includes "name", "manaCost", "type", "text", "flavorText", "pt", and "rarity" fields.

Do not explain the card, only give JSON. If you generate multiple cards, each card should be placed in a JSON array field named "cards".
`

const cardUpgradeSystemPrompt:string = `You are an assistant who works as a Magic: The Gathering card designer. You will receive a JSON dictionary called "cards" which will contain a list of Magic: The Gathering cards. Each card will be a JSON dictionary which contains the fields "name", "manaCost", "type", "text", "flavorText", "pt", "rarity", and "imageUrl", which represent the card. You will also receive a JSON array called "rules" which is a list of rules you must ensure each card's "text" follows. If a card does not match one or more of the rules, the card's text (the "text" field) should be modified so that all rules are followed. If all rules are followed, do not modify the card's "text" at all. For each card, add a new JSON field called "updateExplanation" explaining how you applied the rules to the card.

You will return only JSON which is the modified list of cards. Do not return the the ruless. Do not provide any other output except for the JSON. Do not explain your decisions.`

const defaultCardUpgradeRules:string = `{
  "rules": [
    "A turn cannot have an upkeep but a player can.",
    "If a card has a mana cost that is variable by paying {X}, or an ability where the player can pay a variable amount by paying {X}, the effect gained by paying {X} should scale based on the amount of mana paid for X.",
    "If a card references its flashback cost, it should explicitly state the amount of mana that flashback costs. The flashback cost usually costs more than the mana cost of the card.",
    "Making the card become an artifact should cost at most 1 colorless mana.",
    "A card cannot have an effect that creates a card."
  ]
}`

export class MTGCardGenerator extends React.Component<MTGCardGeneratorProps, MTGCardGeneratorState> {
  constructor(props: MTGCardGeneratorProps) {
    super(props);
    this.state = {
      openAIPrompt: 'Generate me two Magic: The Gathering cards from the Dominaria plane.',
      cardUpgradeRules: defaultCardUpgradeRules,
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

  handleUpgradeTextChange(event: React.ChangeEvent<HTMLTextAreaElement>) {
    this.setState({ cardUpgradeRules: event.target.value });
  }

  handleSubmit(event: React.FormEvent<HTMLFormElement>) {

    this.setState({ generateButtonDisabled: true })

    MakeOpenAIChatComletionRequest(config.OpenAIApiKey, this.state.openAIPrompt, systemPrompt).then(resp => {
      var cards: Card[] = JSON.parse(resp)["cards"]

      cards.forEach(card => {
        setCardManaCostTokens(card)
      });

      const promises: Promise<any>[] = [];
      cards.forEach(card => {
        var prompt = getCardImagePrompt(card);
        promises.push(MakeOpenAIImageCreateRequest(config.OpenAIApiKey, prompt).then(imageUrl => {
          card.imageUrl = imageUrl;
        }));
      });

      var rules = JSON.parse(this.state.cardUpgradeRules)

      var userPrompt = {
        "rules": rules["rules"],
        "cards": cards
      }

      promises.push(MakeOpenAIChatComletionRequest(config.OpenAIApiKey, JSON.stringify(userPrompt), cardUpgradeSystemPrompt).then(resp => {
        let upgradedCards = JSON.parse(resp)["cards"]
        for (var i = 0; i < upgradedCards.length; i++) {
          cards[i].upgradedCard = upgradedCards[i]
        }
      }));

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
        <p>Rules to follow when upgrading the card:</p>
        <label>
          <textarea value={this.state.cardUpgradeRules} onChange={this.handleUpgradeTextChange} rows={10} cols={120} />
        </label>
        <p></p>
        <input type="submit" value="Generate" disabled={this.state.generateButtonDisabled} />
        <p></p>
        <textarea value={this.state.openAIResponse} readOnly={true} rows={30} cols={120} hidden={false} />
        {
          this.state.cards.map(card => (
            <div>
              <CardDisplay key={card.name} card={card} />
              <CardDisplay key={card.name + "-upgraded"} card={card.upgradedCard} />
              <p>
                {card.upgradedCard.updateExplanation}
              </p>
            </div>
          ))
        }
      </form>
    );
  }
}
