import React from "react";
import { CardDisplay, MagicCard } from "./Card";
import Tooltip from "antd/es/tooltip";

interface CardPreviewProps {
  card: MagicCard;
  cardWidth: number;
}

interface CardPreviewState {
  card: MagicCard
}

export class CardPreview extends React.Component<CardPreviewProps, CardPreviewState> {
  constructor(props: CardPreviewProps) {
    super(props);
    this.state = {
      card: MagicCard.clone(props.card, false)
    };
  }

  render() {
    const card = this.props.card;
    return (
      <div>
        <Tooltip mouseEnterDelay={0} mouseLeaveDelay={0} trigger={["hover", "click"]} overlayClassName="antd-no-style-tooltip" overlayInnerStyle={{width: `${this.props.cardWidth}px`}}
        title={<CardDisplay card={this.state.card} cardWidth={this.props.cardWidth} showCardMenu={false} />}>
          <div id={`title-container-${card.id}`} className={card.cardFrameHeaderClassName} style={{padding: "0px", marginTop: "0px"}} >
            <div id={`name-${card.id}`} style={{alignSelf:"center"}} className="name name-type-size">
              <div>{card.name}</div>
            </div>
            <div id={`mana-${card.id}`} className="mana-symbols">
              {card.manaCostTokens.map((manaCostToken, i) => (
                <i key={card.name + "-manaToken-"+ i} className={MagicCard.getManaClassNameForTitle(manaCostToken) + " manaCost " + `manaCost-${card.id}`} id="mana-icon"></i>
              ))}
            </div>
          </div>
        </Tooltip>
      </div>
    )
  }
}