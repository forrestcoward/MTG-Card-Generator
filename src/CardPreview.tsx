import React, { useEffect } from "react";
import { CardDisplay, MagicCard } from "./Card";
import Tooltip from "antd/es/tooltip";
import { adjustTextHeightBasedOnChildrenClientOffsetHeight, adjustTextHeightBasedOnClientHeight } from "./Utility";

interface CardPreviewProps {
  card: MagicCard;
  cardWidth: number;
}

export function CardPreview(props: CardPreviewProps) {
  useEffect(() => {
    adjustNameSize();
  }, []);

  // Adjust name and mana cost size relative to the height of their container.
  function adjustNameSize(nameToContainerRatio:number = .9, manaToContainerRatio:number = .75) {
    const container : HTMLElement | null = document.getElementById(`title-container-${props.card.id}`)
    const nameContainer : HTMLElement | null = document.getElementById(`name-${props.card.id}`)
    const manaContainer : HTMLElement | null = document.getElementById(`mana-${props.card.id}`)

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

  const sameId = false;
  const tooltipCard = MagicCard.clone(props.card, sameId);
  return (
    <div>
      <Tooltip 
        trigger={["click", "hover", "contextMenu"]} 
        placement="left" 
        mouseEnterDelay={0} 
        mouseLeaveDelay={0} 
        overlayClassName="antd-no-style-tooltip" 
        overlayInnerStyle={{width: `${props.cardWidth-20}px`, marginLeft:20}} 
        title={<CardDisplay card={tooltipCard} cardWidth={props.cardWidth-20} showCardMenu={false} allowImagePreview={false} allowEdits={false} allowImageUpdate={false} />}>
        <div id={`title-container-${props.card.id}`} className={props.card.cardFrameHeaderClassName} style={{padding:0, marginTop:0, height:28}} >
          <div id={`name-${props.card.id}`} style={{alignSelf:"center"}} className="name name-type-size">
            <div>{props.card.name}</div>
          </div>
          <div id={`mana-${props.card.id}`} className="mana-symbols">
            {props.card.manaCostTokens.map((manaCostToken, i) => (
              <i key={props.card.id + "-manaToken-"+ i} className={MagicCard.getManaClassNameForTitle(manaCostToken) + " manaCost " + `manaCost-${props.card.id}`} id="mana-icon"></i>
            ))}
          </div>
        </div>
      </Tooltip>
    </div>
  )
}