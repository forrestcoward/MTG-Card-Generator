import React from "react";
import { CardDisplay, MagicCard } from "./Card";
import Tooltip from "antd/es/tooltip";

interface CardPreviewProps {
  card: MagicCard;
  cardWidth: number;
}

interface CardPreviewState {
  tooltipCard: MagicCard
}

export class CardPreview extends React.Component<CardPreviewProps, CardPreviewState> {
  constructor(props: CardPreviewProps) {
    super(props);
    this.state = {
      tooltipCard: MagicCard.clone(props.card, false)
    };
  }

  render() {
    const card = this.props.card;
    return (
      <div>
        <Tooltip trigger={["click", "hover", "contextMenu"]} placement="left" mouseEnterDelay={0} mouseLeaveDelay={0} overlayClassName="antd-no-style-tooltip" overlayInnerStyle={{width: `${this.props.cardWidth-20}px`, marginLeft: "20px"}}
        title={<CardDisplay card={this.state.tooltipCard} cardWidth={this.props.cardWidth-20} showCardMenu={false} allowImagePreview={false} allowEdits={false} />}>
          <div id={`title-container-${card.id}`} className={card.cardFrameHeaderClassName} style={{padding: "0px", marginTop: "0px"}} >
            <div id={`name-${card.id}`} style={{alignSelf:"center"}} className="name name-type-size">
              <div>{card.name}</div>
            </div>
            <div id={`mana-${card.id}`} className="mana-symbols">
              {card.manaCostTokens.map((manaCostToken, i) => (
                <i key={card.id + "-manaToken-"+ i} className={MagicCard.getManaClassNameForTitle(manaCostToken) + " manaCost " + `manaCost-${card.id}`} id="mana-icon"></i>
              ))}
            </div>
          </div>
        </Tooltip>
      </div>
    )
  }
}