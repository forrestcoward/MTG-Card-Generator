import React from 'react';
import { CardDisplay, MagicCard  } from './Card';
import { SearchMagicCardsRequest } from './CallAPI';
import { setCardContainerSize } from './Utility';
import { PublicClientApplication } from '@azure/msal-browser';

import "./mtg-card.css";
import "./app.css";

// @ts-ignore
import loadingIcon from './card-backgrounds/staff.png'

export interface SearchCardsProps { 
  msalInstance: PublicClientApplication;
}

const defaultPrompt:string = ""

export interface SearchCardsState {
  prompt: string,
  response: string,
  isLoading: boolean,
  cards: MagicCard[],
  currentError: string,
  userName: string,
  cardWidth: number
}

export class SearchCards extends React.Component<SearchCardsProps, SearchCardsState> {
  constructor(props: SearchCardsProps) {
    super(props);
    let width = setCardContainerSize();
    this.state = {
      prompt: '',
      response: '',
      isLoading: false,
      cards: [],
      currentError: '',
      userName: '',
      cardWidth: width
    };

    this.handleSubmit = this.handleSubmit.bind(this);
    this.handleChangeInput = this.handleChangeInput.bind(this);
  }

  getLoadingClassName() : string{
    return this.state.isLoading ? "loadingAnimation loadingIcon" : "loadingIcon";
  }

  handleChangeInput(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({ prompt: event.target.value });
  }

  handleSubmit() {
    this.setState({ isLoading: true, currentError: "" })
    let query = this.state.prompt

    SearchMagicCardsRequest(query, this.props.msalInstance).then(cards => {
      this.setState({
        response: JSON.stringify(cards),
        cards: cards,
        isLoading: false
      })

    }).catch((error: Error) => {
      this.setState({ isLoading: false, currentError: error.message })
    });
  }

  render() {
    return (
      <div>
        <div className="outerContainer">
          <div className="container">
            <p>Search other user's generated cards...</p>
            <label>
              <input type="text" className="userInputPrompt" placeholder={defaultPrompt} onChange={this.handleChangeInput} value={this.state.prompt} />
            </label>
            <p></p>
            <table>
              <tbody>
                <tr>
                  <td>
                    <button className="generateButton" type="submit" onClick={() => this.handleSubmit()} disabled={this.state.isLoading}>Search!</button>
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
                <CardDisplay key={`card-display-${card.id}`} card={card} showCardMenu={true} cardWidth={this.state.cardWidth} />
              </div>
            ))
          }
          </div>
        </div>
      </div>
    );
  }
}
