import React from 'react';
import { CardDisplay, MagicCard  } from './Card';
import { GenerateMagicCardRequest } from './OpenAI';
import { TutorialCard } from './TutorialCard';
import "./mtg-card.css";
import "./app.css";

// @ts-ignore
import loadingIcon from './card-backgrounds/staff.png'

export interface MTGCardGeneratorProps { }

export interface MTGCardGeneratorState {
  prompt: string,
  response: string,
  isLoading: boolean,
  cards: MagicCard[],
  currentError: string,
}

const defaultPrompt:string = "is from the Dominaria plane."

export class MTGCardGenerator extends React.Component<MTGCardGeneratorProps, MTGCardGeneratorState> {
  constructor(props: MTGCardGeneratorProps) {
    super(props);
    this.state = {
      prompt: '',
      response: '',
      isLoading: false,
      cards: [TutorialCard],
      currentError: '',
    };

    this.handleSubmit = this.handleSubmit.bind(this);
    this.handleChangeInput = this.handleChangeInput.bind(this);

    const vw = Math.max(document.documentElement.clientWidth || 0, window.innerWidth || 0)
    // const vh = Math.max(document.documentElement.clientHeight || 0, window.innerHeight || 0)

    const cardContainerClass = '.card-container';
    const cardContainerRule = this.findCSSRule(cardContainerClass);
    // Scale the card entirely based on the card width.
    // Does not work well at the extremes because margins and paddings do not scale
    // and there are issues with the card border background image.
    const cardWidth = 360;
    const cardHeight = ((cardWidth * 3.6) / 2.5);
    if (cardContainerRule) {
      cardContainerRule.style.width  = `${cardWidth}px`;
      cardContainerRule.style.height = `${cardHeight}px`;
    }

    const cardBackgroundClass = '.card-background';
    const cardBackgroundRule = this.findCSSRule(cardBackgroundClass);

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

    var userPrompt = this.state.prompt

    GenerateMagicCardRequest(userPrompt).then(cards => {
      this.setState({
        response: JSON.stringify(cards),
        cards: [...cards, ...this.state.cards],
        isLoading: false
      })

    }).catch((error: Error) => {
      this.setState({ isLoading: false, currentError: error.message + ": " + error.stack })
    });
  }

  findCSSRule(selector: string): CSSStyleRule | null {
    const sheets = document.styleSheets;
    const sheetsLength = sheets.length;
  
    for (let i = 0; i < sheetsLength; i++) {
      let sheet = sheets[i];
      let rules: CSSRuleList;
  
      try {
        rules = sheet.cssRules || sheet.rules;
      } catch (e) {
        continue;
      }
  
      const rulesLength = rules.length;
  
      for (let j = 0; j < rulesLength; j++) {
        let rule = rules[j];
  
        if (rule instanceof CSSStyleRule && rule.selectorText === selector) {
          return rule;
        }
      }
    }
  
    return null;
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
      </div>
    );
  }
}
