import React from "react";
import { CardDisplay, MagicCard } from "./Card";
import Tooltip from "antd/es/tooltip";
import { adjustTextHeightBasedOnChildrenClientOffsetHeight, adjustTextHeightBasedOnClientHeight } from "./Utility";

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

  componentDidMount(): void {
    this.adjustNameSize();
  }

  // Adjust name and mana cost size relative to the height of their container.
  private adjustNameSize(nameToContainerRatio:number = .9, manaToContainerRatio:number = .75) {
    const container : HTMLElement | null = document.getElementById(`title-container-${this.props.card.id}`)
    const nameContainer : HTMLElement | null = document.getElementById(`name-${this.props.card.id}`)
    const manaContainer : HTMLElement | null = document.getElementById(`mana-${this.props.card.id}`)

    if (!container || !nameContainer || !manaContainer) {
      return
    }

    adjustTextHeightBasedOnClientHeight(container, nameContainer, nameToContainerRatio)

    if (manaContainer.children.length != 0) {
      // Some malformed cards will have no mana value.
      adjustTextHeightBasedOnChildrenClientOffsetHeight(container, manaContainer, manaToContainerRatio, 4, false)
    }

    const calculateWidth = function() { return nameContainer.scrollWidth + manaContainer.scrollWidth }
    let prevWidth = 0;
    let fontSize = parseFloat(window.getComputedStyle(nameContainer, null).getPropertyValue('font-size'));
    let offset = 4; // Adjust so mana does not go off card.

    while (calculateWidth() > container.offsetWidth - offset && calculateWidth() != prevWidth) {
      prevWidth = calculateWidth()
      fontSize--;
      nameContainer.style.fontSize = fontSize + "px";

      if (fontSize <= 4) {
        break;
      }
    }
  }

  render() {
    const card = this.props.card;
    return (
      <div>
        <Tooltip trigger={["click", "hover", "contextMenu"]} placement="left" mouseEnterDelay={0} mouseLeaveDelay={0} overlayClassName="antd-no-style-tooltip" overlayInnerStyle={{width: `${this.props.cardWidth-20}px`, marginLeft: "20px"}}
        title={<CardDisplay card={this.state.tooltipCard} cardWidth={this.props.cardWidth-20} showCardMenu={false} allowImagePreview={false} allowEdits={false} allowImageUpdate={false} />}>
          <div id={`title-container-${card.id}`} className={card.cardFrameHeaderClassName} style={{padding: "0px", marginTop: "0px", height:"28px"}} >
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