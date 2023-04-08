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
  openAIPrompt: string,
  openAIResponse: string,
  generateButtonDisabled: boolean,
  cards: MagicCard[],
  currentError: string,
}

const defaultPrompt:string = "Generate me one Magic: The Gathering card from the Dominaria plane."

export class MTGCardGenerator extends React.Component<MTGCardGeneratorProps, MTGCardGeneratorState> {
  constructor(props: MTGCardGeneratorProps) {
    super(props);
    this.state = {
      openAIPrompt: '',
      openAIResponse: '',
      generateButtonDisabled: false,
      cards: [TutorialCard],
      currentError: '',
    };

    this.handleSubmit = this.handleSubmit.bind(this);
    this.handleChangeInput = this.handleChangeInput.bind(this);
  }

  getLoadingClassName() : string{
    return this.state.generateButtonDisabled ? "loadingAnimation loadingIcon" : "loadingIcon";
  }

  handleChangeInput(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({ openAIPrompt: event.target.value });
  }

  handleSubmit() {
    this.setState({ generateButtonDisabled: true, currentError: "" })

    var userPrompt = this.state.openAIPrompt

    GenerateMagicCardRequest(userPrompt).then(cards => {
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
          <input type="text" className="userInputPrompt" placeholder='is from the Dominaria plane' onChange={this.handleChangeInput} value={this.state.openAIPrompt} />
        </label>
        <p></p>
        <table>
          <tr>
            <td>
              <button className="generateButton" type="submit" onClick={() => this.handleSubmit()} disabled={this.state.generateButtonDisabled}>Generate!</button>
            </td>
            <td>
              <img className={this.getLoadingClassName()} src={loadingIcon} />
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
