import React from 'react';
import { BasicCard, CardDisplay, CardToBasicCard, MagicCard  } from './Card';
import { MakeOpenAIChatComletionRequest, MakeOpenAIImageCreateRequest } from './OpenAI';
import "./mtg-card.css";
const config = require('../config.json');
// @ts-ignore
import { parseCard } from 'magic-card-parser'

export interface MTGCardGeneratorProps {
}

export interface MTGCardGeneratorState {
  openAIPrompt: string,
  cardUpgradeRules: string
  openAIResponse: string,
  generateButtonDisabled: boolean,
  cards: MagicCard[],
  currentError: string
}

const systemPrompt:string = `You are an assistant who works as a Magic: The Gathering card designer. You like complex cards with interesting mechanics. The cards you generate should obey the Magic "color pie" design rules. The cards you generate should obey the the Magic: The Gathering comphrehensive rules.

You should return a JSON array named "cards" where each entry represents a card you generated for the user based on their request, which must include the "name", "manaCost", "type", "text", "flavorText", "pt", and "rarity" properties only.

Do not explain the cards or explain your reasoning. Only return valid JSON to the user.
`

/*
const cardUpgradeSystemPrompt:string = `You are an assistant who works as a Magic: The Gathering card designer. You will receive a JSON array named "cards" which will contain a list of Magic: The Gathering cards. Each card will be a JSON dictionary which contains the properties "name", "manaCost", "type", "text", "pt", which represent the card. You will also receive a JSON array called "rules" which is a list of rules that must be true for each card, otherwise you must update the card to agree with the rules. If a card does not match one or more of the rules, update the card's text (the "text" property) so that all rules are followed. If all rules are true a card, do not modify that card's JSON at all. For each card, add a new JSON field named "updateExplanation" that explains how you determined if the rules apply to that particular card.

You will only return a JSON array named "cards" which is the modified list of cards. Do not return the JSON array "rules" you received from the user. Finally, do not explain yourself, only return valid JSON for the cards.`
*/

const cardUpgradeSystemPrompt:string = `You are an assistant who works as a Magic: The Gathering card designer. You will receive a JSON dictionary that represents a Magic card continaing the properties "name", "manaCost", "type", "text", "pt". You will also receive a JSON array called "rules" which is a list of rules that must make true when applied to the card. If a card does not match one or more of the rules, update the card's text (the "text" property) so that all rules are followed. If all rules are true for the card, do not make any modifications to the card. Return the card JSON with the updated "text" property, and also add a new JSON field named "updateExplanation" that explains how you determined if the rules applied to that card.

Do not return the JSON array "rules" you received from the user. Finally, do not explain yourself, only return valid JSON for the cards.`

/*
const cardUpgradeSystemPrompt:string = `You are an assistant who works as a Magic: The Gathering card designer. You will receive a JSON dictionary called "cards" which will contain a list of Magic: The Gathering cards. Each card will be a JSON dictionary which contains the fields "name", "manaCost", "type", "text", "flavorText", "pt", "rarity", and "imageUrl", which represent the card. You will also receive a JSON array called "rules" which is a list of rules you must ensure each card's "text" follows. If a card does not match one or more of the rules, the card's text (the "text" field) should be modified so that all rules are followed. If all rules are followed, do not modify the card's "text" at all. For each card, add a new JSON field called "updateExplanation" explaining how you applied the rules to the card.
You will return only JSON which is the modified list of cards. Do not return the the ruless. Do not provide any other output except for the JSON. Do not explain your decisions.`
*/

var defaultCardUpgradeRules:string = `{
  "rules": [
    "A turn cannot have an upkeep but a player can.",
    "If a card has a mana cost that is variable by paying {X}, or an ability where the player can pay a variable amount by paying {X}, the effect gained by paying {X} should scale based on the amount of mana paid for X.",
    "If a card references its flashback cost, it should explicitly state the amount of mana that flashback costs. The flashback cost usually costs more than the mana cost of the card.",
    "Making the card become an artifact should cost at most 1 colorless mana.",
    "A card cannot have an effect that creates a card."
  ]
}`

const defaultPrompt:string = "Generate me one Magic: The Gathering card from the Dominaria plane."

const generateTemperature:number = 1;
const upgradeTemperature:number = .2;

export class MTGCardGenerator extends React.Component<MTGCardGeneratorProps, MTGCardGeneratorState> {
  constructor(props: MTGCardGeneratorProps) {
    super(props);
    this.state = {
      openAIPrompt: defaultPrompt,
      cardUpgradeRules: defaultCardUpgradeRules,
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

  handleUpgradeTextChange(event: React.ChangeEvent<HTMLTextAreaElement>) {
    this.setState({ cardUpgradeRules: event.target.value });
  }

  //handleSubmit(event: React.FormEvent<HTMLFormElement>) {
  handleSubmit() {

    this.setState({ generateButtonDisabled: true, currentError: "" })


    var userPrompt = this.state.openAIPrompt + "\n Only return JSON. Do not explain yourself."

    MakeOpenAIChatComletionRequest(config.OpenAIApiKey, userPrompt, systemPrompt, generateTemperature).then(resp => {
      let _cards: BasicCard[] = []
      let cards: MagicCard[] = [];
      try {
        _cards = JSON.parse(resp)["cards"]
      } catch (e) {
        let error = "Error parsing JSON response from OpenAI: " + e + "\nThe response was:\n" + resp
        console.log(error)
        this.setState({ currentError: error })
      }


      _cards.forEach(_card => {
        // var parse = parseCard({"name": _card.name, "oracle_text": _card.text })
        //if (parse.result[0]) {
        //  console.log(parse.result[0])
        //}

        cards.push(new MagicCard(_card))
      });

      const promises: Promise<any>[] = [];
      cards.forEach(card => {
        var prompt = card.openAIImagePrompt;
        promises.push(MakeOpenAIImageCreateRequest(config.OpenAIApiKey, prompt).then(imageUrl => {
          card.imageUrl = imageUrl;
        }));
      });

      /*
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
      */

      Promise.all(promises).then(() => {
        this.setState({
          openAIResponse: resp,
          cards: [...cards, ...this.state.cards],
          generateButtonDisabled: false
        })
      })
    }).catch((error: Error) => {
      this.setState({ generateButtonDisabled: false, currentError: error.message + ": " + error.stack })
    });

    // event.preventDefault();
  }

  upgradeCard(card: MagicCard) {
    var rules = JSON.parse(this.state.cardUpgradeRules)

    var userPrompt = {
      "rules": rules["rules"],
      "cards": [CardToBasicCard(card)]
    }

    MakeOpenAIChatComletionRequest(config.OpenAIApiKey, JSON.stringify(userPrompt), cardUpgradeSystemPrompt, upgradeTemperature).then(resp => {

      this.setState({ openAIResponse: resp })

      let upgradedCard:BasicCard;
      try {
        upgradedCard = JSON.parse(resp)
      } catch (e) {
        let error = "UpgradeCard error: could not parse response into Card JSON response from OpenAI: " + e + "\nThe response was:\n" + resp
        console.log(error)
        this.setState({ currentError: error })
      }

      let matchingCard = this.state.cards.find(c => c.name == upgradedCard.name)

      if (matchingCard) {
        // @ts-ignore-start
        matchingCard.text = upgradedCard.text
        // @ts-ignore-end
        this.setState({
          cards: this.state.cards,
          generateButtonDisabled: false
        })
      }

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
        <p>Rules to follow when upgrading the card:</p>
        <label>
          <textarea value={this.state.cardUpgradeRules} onChange={(e) => this.handleUpgradeTextChange(e)} rows={10} cols={120} />
        </label>
        <p></p>
        <button type="submit" onClick={() => this.handleSubmit()} disabled={this.state.generateButtonDisabled}>Generate</button>
        <h2 style={{ color: 'red' }}>{this.state.currentError}</h2>
        <p></p>
        Here is my latest response from OpenAI:
        <textarea value={this.state.openAIResponse} readOnly={true} rows={30} cols={120} hidden={false} />
        {
          this.state.cards.map(card => (
            <div key={card.name + -"display"}>
              <CardDisplay key={card.name} card={card} />
              <button onClick={() => this.upgradeCard(card)}>Upgrade</button>;
            </div>
          ))
        }
      </div>
    );
  }
}
