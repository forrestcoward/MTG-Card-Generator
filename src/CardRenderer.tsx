import React from "react";
import { UserContext } from './UserContext';
import { Image, Dropdown, Tooltip, Button } from 'antd';
import { BulbOutlined, CaretDownFilled, CaretRightOutlined, CloseOutlined, CloudDownloadOutlined, CopyOutlined, EditOutlined, 
  FileImageOutlined, InfoCircleOutlined, QuestionCircleOutlined, RightCircleOutlined, SaveFilled, ZoomInOutlined, ZoomOutOutlined } from '@ant-design/icons';
import { adjustTextHeightBasedOnChildrenClientOffsetHeight, adjustTextHeightBasedOnClientHeight, copyTextToClipboard, getChildrenClientOffsetHeight, isMobileDevice } from "./Utility";
import { saveAs } from 'file-saver';
import { CardMenu } from './CardMenu';

// @ts-ignore
import whitePaintBrush from './card-backgrounds/paintbrush-white.png'
import { toBlob } from 'html-to-image';
import { GenerateImageResponse, UploadImageToAzure } from "./CallAPI";
import { msalInstance } from "./Index";
import { MagicCard } from "./models/MagicCard";
import { ImageEditor } from "./ImageEditor";

const manaTokens : string[] = ["{G}", "{W}", "{U}", "{B}", "{R}", "{0}", "{1}", "{2}", "{3}", 
"{4}", "{5}", "{6}", "{7}", "{8}", "{9}", "{10}", "{C}", "{T}", "{X}"]

export const manaTokenToCssCharacter : Record<string, string> = {
  "{G}": "g",
  "{W}": "w",
  "{U}": "u",
  "{B}": "b",
  "{R}": "r",
  "{0}": "0",
  "{1}": "1",
  "{2}": "2",
  "{3}": "3",
  "{4}": "4",
  "{5}": "5",
  "{6}": "6",
  "{7}": "7",
  "{8}": "8",
  "{9}": "9",
  "{10}": "10",
  "{C}": "c",
  "{T}": "tap",
  "{X}": "x",
}

interface CardRendererProps {
  // The underlying card.
  card: MagicCard;
  // The initial card width.
  cardWidth: number;
  // Display the card menu (resize, download etc.)
  showCardMenu: boolean;
  // Allow user to regenerate card image with custom prompts.
  allowImageUpdate: boolean;
  // Allow the user to click on the card image to show a zoomed preview.
  allowImagePreview: boolean;
  // Allow the user to update the card text through clicking the holofoil or using the menu.
  allowEdits: boolean;
}

interface CardRendererState {
  card: MagicCard;
  editMode: boolean;
  oracleTextUpdate: string;
  nameUpdate: string;
  typeUpdate : string;
  manaCostUpdate: string;
  powerAndToughnessUpdate?: string;

  useTemporaryImage: boolean;
  showCardMenu: boolean;
  showSizeAdjustmentButtons: boolean;
  increaseSizeAllowed: boolean;
  decreaseSizeAllowed: boolean;
  cardWidth: number;

  // Image editor state.
  showImageEditor: boolean;
  availableImageUrls: GenerateImageResponse[];
}

export function CardRenderer(props: CardRendererProps) {
  const userContext = React.useContext(UserContext);
  const [state, setState] = React.useState<CardRendererState>({
    card: props.card,
    editMode: false,
    nameUpdate: props.card.face.name,
    oracleTextUpdate: props.card.face.oracleText,
    typeUpdate: props.card.face.typeLine,
    manaCostUpdate: props.card.face.manaCost,
    powerAndToughnessUpdate: props.card.face.pt,
    useTemporaryImage: true,
    showCardMenu: props.showCardMenu,
    increaseSizeAllowed: true,
    decreaseSizeAllowed: true,
    cardWidth: props.cardWidth,
    showSizeAdjustmentButtons: !isMobileDevice() || Math.max(document.documentElement.clientWidth || 0, window.innerWidth || 0) > 900,
    showImageEditor: false,
    availableImageUrls: props.card.face.alternativeImageUrls?.map(url => ({ imageUrl: url, temporaryUrl: url })) || [],
  });

  React.useEffect(() => {
    adjustFontSize();
  }, [state.card.face.oracleText, state.card.face.typeLine, state.card.face.name, state.editMode]);

  function handleCardNameUpdate(event: React.ChangeEvent<HTMLInputElement>) {
    setState(prev => ({ ...prev, nameUpdate: event.target.value }));
  }

  function handleCardTypeUpdate(event: React.ChangeEvent<HTMLInputElement>) {
    setState(prev => ({ ...prev, typeUpdate: event.target.value }));
  }

  function handleCardOracleTextUpdate(event: React.ChangeEvent<HTMLTextAreaElement>) {
    setState(prev => ({ ...prev, oracleTextUpdate: event.target.value }));
  }

  function handleCardManaCostUpdate(event: React.ChangeEvent<HTMLInputElement>) {
    setState(prev => ({ ...prev, manaCostUpdate: event.target.value.trim() }));
  }

  function handleCardPowerAndToughnessUpdate(event: React.ChangeEvent<HTMLInputElement>) {
    setState(prev => ({ ...prev, powerAndToughnessUpdate: event.target.value.trim() }));
  }

  function updateEditMode() {
    if (!props.allowEdits) {
      return;
    }

    var updatedCard = MagicCard.clone(state.card, true);
    updatedCard.face.name = state.nameUpdate;
    updatedCard.face.oracleText = state.oracleTextUpdate;
    updatedCard.face.typeLine = state.typeUpdate;
    updatedCard.face.manaCost = state.manaCostUpdate;
    updatedCard.face.pt = state.powerAndToughnessUpdate;
    setState(prev => ({ ...prev, editMode: !prev.editMode, card: updatedCard }));
  }

  function handleCardDownload() {
    setState(prev => ({ ...prev, useTemporaryImage: false }));
    toImage().then(blob => {
      if (blob != null) {
        saveAs(blob, `${state.card.face.name}.png`);
      }
    }).catch(error => {
      console.error("Error downloading card as image: " + error.message);
    }).finally(() => {
      setState(prev => ({ ...prev, useTemporaryImage: true }));
    });
  }

  function handleArtDownload() {
    var img = state.card.face.temporaryImageUrl ? state.card.face.temporaryImageUrl : state.card.face.imageUrl;
    window.open(img, '_blank');
  }

  function increaseCardSize(increase: number) {
    var newWidth = state.cardWidth + increase;
    adjustCardSize(newWidth);
    updateSizeToggles(newWidth);
  }

  function decreaseCardSize(decrease: number) {
    var newWidth = state.cardWidth - decrease;
    adjustCardSize(newWidth);
    updateSizeToggles(newWidth);
  }

  function updateImageUrl(imageUrls: GenerateImageResponse) {
    var updatedCard = MagicCard.clone(state.card, true);
    updatedCard.face.imageUrl = imageUrls.imageUrl;
    updatedCard.face.temporaryImageUrl = imageUrls.temporaryUrl;
    setState(prev => ({ ...prev, card: updatedCard }));
  }

  function toggleShowImageEditor() {
    setState(prev => ({ ...prev, showImageEditor: !prev.showImageEditor }));
  }

  function updateSizeToggles(newWidth: number) {
    if (newWidth <= 330) {
      setState(prev => ({ ...prev, increaseSizeAllowed: true, decreaseSizeAllowed: false, cardWidth: newWidth }));
    }

    if (newWidth >= 330) {
      setState(prev => ({ ...prev, increaseSizeAllowed: true, decreaseSizeAllowed: true, cardWidth: newWidth }));
    }
  }

  function cardDivClassName() {
    return `card-background card-background-${manaCssClassPostfix()}`
  }

  function cardFrameTypeLineClassName()  {
    return `frame-type-line frame-${manaCssClassPostfix()} frame-type-line-${manaCssClassPostfix()}`
  }

  function cardFrameHeaderClassName() {
    return `frame-header frame-${manaCssClassPostfix()} frame-header-${manaCssClassPostfix()}`
  }

  function cardFrameTextBoxClassName() {
    return `frame-text-box frame-text-box-${state.card.id} frame-text-box-${manaCssClassPostfix()} frame-margin`
  }

  function cardFrameContentClassName() {
    return `frame-text-box-inner frame-text-box-inner-${state.card.id}`
  }
  
  function cardFrameArtClassName() {
    return `frame-art frame-art-${manaCssClassPostfix()} frame-margin`
  }

  function rarityDisplay() {
    var token = ""
    switch (state.card.rarity.toLocaleLowerCase()) {
      case "common": token = "C"; break;
      case "uncommon": token = "U"; break;
      case "rare": token = "R"; break;
      case "mythic": token = "M"; break;
      case "mythic rare": token = "M"; break;
    }
    return token;
  }

  function cardType(): CardType {
    var type = state.card.face.type.toLocaleLowerCase();
    if (type.includes("legendary creature") || type.includes("creature")) return CardType.Creature
    if (type == "instant") return CardType.Instant;
    if (type == "sorcery") return CardType.Sorcery;
    if (type.includes("artifact")) return CardType.Artifact;
    if (type.includes("enchantment")) return CardType.Enchantment;
    if (type.includes("land")) return CardType.Land;
    return CardType.Unknown;
  }

  function textDisplay() {
    return state.card.face.oracleText.split('\n').map(line => addManaHtmlToCardTextLine(line));
  }

  // Gets the CSS postfix to create class names that are differentiated by color identity.
  function manaCssClassPostfix() {
    return state.card.colorIdentity;
  }

  // Transforms text representations of mana symbols from OpenAI into HTML that can render those mana symbols using the mana project (https://github.com/andrewgioia/mana).
  function addManaHtmlToCardTextLine(line: string) {
    manaTokens.forEach(token => {
      line = line.replaceAll(token, `<i class=\"${getManaClassName(token)} card-text-mana-token-${state.card.id}\"></i>`)
    })

    return line
  }

  // Gets the matching mana project (https://github.com/andrewgioia/mana) CSS token for <i></i> based on the mana token string.
  function getManaClassName(manaToken: string) {
    return `ms ms-${manaTokenToCssCharacter[manaToken]} ms-padding`
  }

  function getManaClassNameForTitle(manaToken: string) {
    return `ms ms-${manaTokenToCssCharacter[manaToken]} ms-padding-title`
  }

  function toImage(): Promise<Blob | null> {
    var node = document.getElementById(`card-${state.card.id}`)
    if (!node) {
      return Promise.reject(new Error('Card node not found in DOM tree to transform to image.'));
    }

    try {
      return toBlob(node, { 
        cacheBust: true,
        // Get rid of 'margin-top' and 'box-shadow' styles. Causes html-to-image to render incorrectly.
        style: { 
          marginTop: '0px',
          boxShadow: 'none',
      }, },)
    }
    catch (error) {
      return Promise.reject(error)
    }
  }

  async function uploadCardBlob(): Promise<string | undefined> {
    try {
      const blob = await toImage();
      if (blob) {
        const url = await UploadImageToAzure(msalInstance, blob, state.card.id.toString());
        return url
      } else {
        console.log('No image blob available to upload.');
      }
    } catch (error) {
      console.error('Error in image conversion or upload:', error);
      return undefined
    }
  }

  // Adjusts the font sizes on the card to fit within the card dimensions.
  function adjustFontSize() {
    adjustTypeLineSize()
    adjustNameSize()
    adjustFrameBottomSize();
    adjustCardTextSize();
  }

  function increaseSize(increase: number = 40) {
    const container : HTMLElement | null = document.getElementById(`card-${state.card.id}`)
    adjustCardSize(container!.clientWidth + increase)
  }

  function decreaseSize(decrease: number = 40) {
    const container : HTMLElement | null = document.getElementById(`card-${state.card.id}`)
    adjustCardSize(container!.clientWidth - decrease)
  }

  function adjustCardSize(width: number = 440) {
    const container : HTMLElement | null = document.getElementById(`card-${state.card.id}`)
    if (container == null) {
      return
    }

    const height = ((width * 3.6) / 2.5);
    container!.style.width  = `${width}px`;
    container!.style.height = `${height}px`;
    const i : HTMLElement | null = document.getElementById(`card-image-flow-${state.card.id}`)
    adjustFontSize();
  }

  // Adjust type and set icon size relative to the height of their container.
  function adjustTypeLineSize(ratio:number = .6) {
    const container : HTMLElement | null = document.getElementById(`type-container-${state.card.id}`)
    const nameContainer : HTMLElement | null = document.getElementById(`type-${state.card.id}`)
    const manaContainer : HTMLElement | null = document.getElementById(`set-${state.card.id}`)

    if (!container || !nameContainer || !manaContainer) {
      return
    }

    adjustTextHeightBasedOnClientHeight(container, nameContainer, ratio)
    adjustTextHeightBasedOnChildrenClientOffsetHeight(container, manaContainer, ratio, 4, false)

    const calculateWidth = function() { return nameContainer.scrollWidth + manaContainer.scrollWidth }
    let prevWidth = 0;
    let fontSize = parseFloat(window.getComputedStyle(nameContainer, null).getPropertyValue('font-size'));

    while (calculateWidth() > container.offsetWidth && calculateWidth() != prevWidth) {
      prevWidth = calculateWidth()
      fontSize--;
      nameContainer.style.fontSize = fontSize + "px";

      if (fontSize <= 4) {
        break;
      }
    }
  }

  // Adjust name and mana cost size relative to the height of their container.
  function adjustNameSize(nameToContainerRatio:number = .6, manaToContainerRatio:number = .75) {
    const container : HTMLElement | null = document.getElementById(`title-container-${state.card.id}`)
    const nameContainer : HTMLElement | null = document.getElementById(`name-${state.card.id}`)
    const manaContainer : HTMLElement | null = document.getElementById(`mana-${state.card.id}`)

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

  // Adjust the size of the bottom frame relative to the card background.
  function adjustFrameBottomSize(ratio:number = .045, minFontSize:number = 1) {
    const container : HTMLElement | null = document.getElementById(`card-background-${state.card.id}`)
    const nameContainer : HTMLElement | null = document.getElementById(`frame-bottom-${state.card.id}`)

    if (!container || !nameContainer) {
      return
    }

    adjustTextHeightBasedOnClientHeight(container, nameContainer, ratio, minFontSize)
  }

  function adjustCardTextSize(minFontSize:number = 5, maxFontSize:number = 30) {
    const container : HTMLElement | null = document.querySelector(`.frame-text-box-${state.card.id}`)
    const innerContainer : HTMLElement | null = document.querySelector(`.frame-text-box-inner-${state.card.id}`)
    const manaTokens = document.querySelectorAll(`.card-text-mana-token-${state.card.id}`)
    const ptContainer = document.getElementById(`pt-${state.card.id}`)
    const frameBottom = document.getElementById(`frame-bottom-${state.card.id}`)

    if (!container || !innerContainer || !frameBottom) {
      return
    }

    // Increment or decrement the size of the text and flavor text containers until they fit within the card.
    // We do not edit the font size of the power and toughness frame because it should be uniform.
    let textContainer = innerContainer.children[0] as HTMLElement
    let flavorTextContainer = innerContainer.children[2] as HTMLElement

    const heightOffset = 30
    let fontSize = parseInt(window.getComputedStyle(textContainer).fontSize)

    // Make the font size larger until it overflows the container.
    // Make larger first so we can shrink it one notch after.
    while (getChildrenClientOffsetHeight(innerContainer) < (container.clientHeight - heightOffset)) {
      fontSize += .5
      textContainer.style.fontSize = fontSize + 'px'
      flavorTextContainer.style.fontSize = fontSize + 'px'

      if (ptContainer != null) {
        ptContainer.style.fontSize = (fontSize + 4) + 'px'
      }

      if (fontSize > maxFontSize) {
        break
      }

      manaTokens.forEach(manaToken => {(manaToken as HTMLElement).style.fontSize = (fontSize-2) + 'px'})
    }

    // Make the font size smaller until it fits the container.
    while (getChildrenClientOffsetHeight(innerContainer) > (container.clientHeight - heightOffset)) {
      fontSize -= .5
      textContainer.style.fontSize = fontSize + 'px'
      flavorTextContainer.style.fontSize = fontSize + 'px'
      manaTokens.forEach(manaToken => {(manaToken as HTMLElement).style.fontSize = (fontSize-2) + 'px'})

      if (ptContainer != null) {
        ptContainer.style.fontSize = (fontSize + 5) + 'px'
      }

      if (fontSize <= minFontSize) {
        break
      }
    }
  }

  const card = state.card;
  const cardIsTutorialCard = card.databaseId == "-1";

  const oracleEditTextAreaRows = textDisplay().length * 3;
  const cardMenuIconFontSize = "40px";

  const height = ((props.cardWidth * 3.6) / 2.5);
  const imageEditorWidth = isMobileDevice() ? props.cardWidth : props.cardWidth - 100;
  const haveCardImage = card.face.imageUrl || card.face.temporaryImageUrl;

  let fullCard = (
    <div style={{display:"flex", flexWrap:"wrap"}}>
      <div>
        <div id={`card-${card.id}`} className="card-container" style={{width: `${props.cardWidth}px`, height: `${height}px`}}>
          <div id={`card-background-${card.id}`} className={cardDivClassName()}></div>
            <div className="card-frame">
              <div id={`title-container-${card.id}`} className={cardFrameHeaderClassName()}>
                <div id={`name-${card.id}`} style={{alignSelf:"center"}} className="name name-type-size">
                  {!state.editMode ?
                    <div>{card.face.name}</div> :
                    <input className="card-edit-name" type="text" value={state.nameUpdate} onChange={handleCardNameUpdate} />
                  }
                </div>
                {!state.editMode ?
                    <div id={`mana-${card.id}`} className="mana-symbols">
                      {card.manaCostTokens.map((manaCostToken, i) => (
                        <i key={card.id + "-manaToken-"+ i} className={getManaClassNameForTitle(manaCostToken) + " manaCost " + `manaCost-${card.id}`} id="mana-icon"></i>
                      ))}
                    </div> :
                    <div className="card-edit-manaCost-container">
                      <input className="card-edit-manaCost" type="text" value={state.manaCostUpdate} onChange={handleCardManaCostUpdate} />
                    </div>
                }
              </div>
              <div className={cardFrameArtClassName()}>
                {!haveCardImage ? 
                  <div style={{width:"100%", height:"100%", backgroundColor:"black", color:"white", position:"relative"}}>
                    <Button style={{position:"absolute", bottom:0, right:0, margin:5, display: (state.showImageEditor || !props.allowImageUpdate || !state.useTemporaryImage) ? 'none' : 'inline-block' }} 
                    type="primary" 
                    icon={<RightCircleOutlined />} 
                    size="small" 
                    onClick={() => {setState(prev => ({ ...prev, showImageEditor: true }))}}>
                      Create Image
                    </Button>
                  </div>
                : (state.useTemporaryImage && card.face.temporaryImageUrl) ?
                  <Image onLoad={() => adjustFontSize()} preview={props.allowImagePreview} loading="lazy" height={"100%"} width={"100%"} src={card.face.temporaryImageUrl ? card.face.temporaryImageUrl : card.face.imageUrl} /> :
                  <Image onLoad={() => adjustFontSize()} preview={props.allowImagePreview} loading="lazy" height={"100%"} width={"100%"} src={card.face.imageUrl} />
                }
              </div>
              <div id={`type-container-${card.id}`} className={cardFrameTypeLineClassName()}>
                {!state.editMode ?
                    <h1 id={`type-${card.id}`} className="type name-type-size">{card.face.typeLine}</h1> :
                    <input className="card-edit-type" type="text" value={state.typeUpdate} onChange={handleCardTypeUpdate} />
                  }
                <div id={`set-${card.id}`} className="mana-symbols">
                  <i className="ms ms-dfc-ignite" id="mana-icon"></i>
                </div>
              </div>
              <div className={cardFrameTextBoxClassName()}>
                <div className={cardFrameContentClassName()}>
                  <div className="description ftb-inner-margin">
                    {!state.editMode ?
                      <div>
                        {textDisplay().map((line, i) => (
                          <p key={card.id + "-text-" + i} dangerouslySetInnerHTML={{ __html: line }}></p>
                        ))}
                      </div> 
                      :
                      <textarea className="card-edit-text" value={state.oracleTextUpdate} rows={oracleEditTextAreaRows} onChange={handleCardOracleTextUpdate} />
                    }
                  </div>
                  <p className="description">
                  </p>
                  <p className="flavour-text">
                    {card.face.flavorText}
                  </p>
                  {card.face.pt &&
                    <div className="power-and-toughness-frame">
                      <div id={`pt-${card.id}`} className="power-and-toughness power-and-toughness-size">
                        {!state.editMode ?
                          <div>{card.face.pt}</div> :
                          <input className="card-edit-pt" type="text" value={state.powerAndToughnessUpdate} onChange={handleCardPowerAndToughnessUpdate} />
                        }
                      </div>
                    </div>
                  }
                  </div>
              </div>
              <div id={`frame-bottom-${card.id}`} className="frame-bottom-info inner-margin">
                <div className="fbi-left">
                  <p>{card.collectorNumber} {rarityDisplay()}</p>
                  <p>OpenAI &#x2022; <img className="paintbrush" src={whitePaintBrush} alt="paintbrush icon" />Custom Magic</p>
                </div>
                <div className="fbi-center" onClick={(e) => { updateEditMode()}}></div>
                <div className="fbi-right">
                    &#169; 2025 MTG Card Generator
                </div>
              </div>
            </div>
        </div>
        { state.showCardMenu &&
        <CardMenu
          card={state.card}
          showCardMenu={state.showCardMenu}
          showSizeAdjustmentButtons={state.showSizeAdjustmentButtons}
          cardMenuIconFontSize={cardMenuIconFontSize}
          increaseSizeAllowed={state.increaseSizeAllowed}
          decreaseSizeAllowed={state.decreaseSizeAllowed}
          onIncreaseSize={increaseCardSize}
          onDecreaseSize={decreaseCardSize}
          sizeAdjustmentIncrement={30}
          onDownloadCard={handleCardDownload}
          onDownloadArt={handleArtDownload}
          copyLinkUrl={card.cardUrl}
          editMode={state.editMode}
          allowEdits={props.allowEdits}
          onToggleEditMode={updateEditMode}
          allowImageUpdate={props.allowImageUpdate && !cardIsTutorialCard}
          onToggleImageEditor={toggleShowImageEditor}
        />
        }
      </div>
      <ImageEditor
        card={state.card}
        isVisible={props.allowImageUpdate && state.showImageEditor && !cardIsTutorialCard}
        onClose={() => setState(prev => ({ ...prev, showImageEditor: false }))}
        onImageUpdate={updateImageUrl}
        width={imageEditorWidth}
        availableImageUrls={state.availableImageUrls}
        onAvailableImageUrlsUpdate={(urls) => setState(prev => ({ ...prev, availableImageUrls: urls }))}
      />
    </div>
  )

  return fullCard;
}
