import React from "react";
import { Image, Menu, Dropdown, Tooltip, Button } from 'antd';
import { CaretDownFilled, CloudDownloadOutlined, EditOutlined, InfoCircleOutlined, QuestionCircleOutlined, SaveFilled, ZoomInOutlined, ZoomOutOutlined } from '@ant-design/icons';
import { adjustTextHeightBasedOnChildrenClientOffsetHeight, adjustTextHeightBasedOnClientHeight, getChildrenClientOffsetHeight, getRandomInt, isMobileDevice } from "./Utility";
import { saveAs } from 'file-saver';

// @ts-ignore
import whitePaintBrush from './card-backgrounds/paintbrush-white.png'
import { toBlob } from 'html-to-image';

export interface BasicCard {
  name: string,
  manaCost: string,
  typeLine: string,
  type: string,
  text: string,
  rawOracleText: string,
  modifiedOracleText: string,
  power: number,
  toughness: number,
  colorIdentity: string
  pt: string,
  flavorText: string,
  rarity: string,
  imageUrl: string,
  temporaryImageUrl: string,
  userPrompt: string,
  explanation: string,
  funnyExplanation: string
}

enum ColorIdentity {
  White = "w",
  Blue = "u",
  Black = "b",
  Green = "g",
  Red = "r",
  Colorless = "c",
  Azorius = "uw",
  Dimir = "ub",
  Rakdos = "br",
  Gruul = "rg",
  Selesnya = "wg",
  Orzhov = "wb",
  Izzet = "ur",
  Golgari = "bg",
  Boros = "rw",
  Simic = "ug",
  ThreePlusColored = "mc",
  Unknown = "unknown",
}

enum CardType {
  Creature,
  Instant,
  Sorcery,
  Enchantment,
  Artifact,
  Land,
  Unknown
}

const manaTokens : string[] = ["{G}", "{W}", "{U}", "{B}", "{R}", "{0}", "{1}", "{2}", "{3}", 
"{4}", "{5}", "{6}", "{7}", "{8}", "{9}", "{10}", "{C}", "{T}", "{X}"]

const manaTokenToCssCharacter : Record<string, string> = {
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

export interface Cardbattle {
  victories: number,
  defeats: number,
}

export interface CardRating {
  numberOfVotes: number,
  totalScore: number,
  averageScore: number,
}

export interface GenerationMetadata {
  model: string,
}

export interface User {
  username: string,
}

export interface CardGenerationRecords {
  cards: CardGenerationRecord[]
}

export interface CardGenerationRecord {
  id: string
  user: User
  magicCards: BasicCard[]
  cardBattle: Cardbattle
  rating: CardRating
  generationMetadata: GenerationMetadata
}

export class MagicCard {
  name: string
  manaCost: string
  typeLine: string
  type: string
  text: string
  rawOracleText: string
  modifiedOracleText: string
  power: number
  toughness: number
  _colorIdentity: string
  pt: string
  flavorText: string
  rarity: string
  imageUrl: string
  temporaryImageUrl: string
  setNumberDisplay: string
  id: number
  userPrompt: string
  explanation: string
  funnyExplanation: string

  constructor(card: BasicCard) {
    this.name = card.name
    this.manaCost = card.manaCost
    this.type = card.type
    this.typeLine = card.typeLine
    this.text = card.text
    this.rawOracleText = card.rawOracleText
    this.modifiedOracleText = card.modifiedOracleText
    this.power = 0
    this.toughness = 0
    this._colorIdentity = ""
    this.flavorText = card.flavorText
    this.pt = card.pt
    this.rarity = card.rarity
    this.imageUrl = card.imageUrl
    this.temporaryImageUrl = card.temporaryImageUrl
    this.setNumberDisplay = getRandomInt(0, 451) + "/" + 451
    this.id = getRandomInt(0, 1000000000)
    this.userPrompt = card.userPrompt
    this.explanation = card.explanation
    this.funnyExplanation = card.funnyExplanation
  }

  static clone(card: MagicCard, sameId: boolean): MagicCard {
    let newCard = new MagicCard(card)
    newCard.setNumberDisplay = card.setNumberDisplay
    if (sameId) {
      newCard.id = card.id
    } else {
      newCard.id = getRandomInt(0, 1000000000)
    }

    return newCard
  }

  get manaCostTokens(): string[] {
    return this.getManaCostTokens(this.manaCost);
  }

  private getManaCostTokens(tokensToParse: string): string[] {
    var r: RegExp = /\{(.*?)\}/g
    var match = tokensToParse.match(r)
    var manaCostTokens:string[] = []
    if (match) {
      match?.forEach(m => manaCostTokens.push(m))
    }
    return manaCostTokens;
  }

  get colorIdentity(): ColorIdentity {
    var white = false;
    var blue = false;
    var black = false;
    var red = false;
    var green = false;
  
    var manaCostTokens = this.manaCostTokens;
    if (!this.manaCostTokens) return ColorIdentity.Unknown
    if (manaCostTokens.includes("{W}")) white = true 
    if (manaCostTokens.includes("{U}")) blue = true
    if (manaCostTokens.includes("{B}")) black = true
    if (manaCostTokens.includes("{R}")) red = true
    if (manaCostTokens.includes("{G}")) green = true
  
    if ( white && !blue && !black && !red && !green) return ColorIdentity.White
    if (!white &&  blue && !black && !red && !green) return ColorIdentity.Blue
    if (!white && !blue &&  black && !red && !green) return ColorIdentity.Black
    if (!white && !blue && !black &&  red && !green) return ColorIdentity.Red
    if (!white && !blue && !black && !red &&  green) return ColorIdentity.Green
    if (!white && !blue && !black && !red && !green) return ColorIdentity.Colorless
  
    if ( white &&  blue && !black && !red && !green) return ColorIdentity.Azorius
    if (!white &&  blue &&  black && !red && !green) return ColorIdentity.Dimir
    if (!white && !blue &&  black &&  red && !green) return ColorIdentity.Rakdos
    if (!white && !blue && !black &&  red &&  green) return ColorIdentity.Gruul
    if ( white && !blue && !black && !red &&  green) return ColorIdentity.Selesnya
    if ( white && !blue &&  black && !red && !green) return ColorIdentity.Orzhov
    if (!white &&  blue && !black &&  red && !green) return ColorIdentity.Izzet
    if (!white && !blue &&  black && !red &&  green) return ColorIdentity.Golgari
    if ( white && !blue && !black &&  red && !green) return ColorIdentity.Boros
    if (!white &&  blue && !black && !red &&  green) return ColorIdentity.Simic
  
    return ColorIdentity.ThreePlusColored;
  }

  get cardDivClassName() {
    return `card-background card-background-${this.manaCssClassPostfix}`
  }

  get cardFrameTypeLineClassName()  {
    return `frame-type-line frame-${this.manaCssClassPostfix} frame-type-line-${this.manaCssClassPostfix}`
  }

  get cardFrameHeaderClassName() {
    return `frame-header frame-${this.manaCssClassPostfix} frame-header-${this.manaCssClassPostfix}`
  }

  get cardFrameTextBoxClassName() {
    return `frame-text-box frame-text-box-${this.id} frame-text-box-${this.manaCssClassPostfix} frame-margin`
  }

  get cardFrameContentClassName() {
    return `frame-text-box-inner frame-text-box-inner-${this.id}`
  }
  
  get cardFrameArtClassName() {
    return `frame-art frame-art-${this.manaCssClassPostfix} frame-margin`
  }

  get rarityDisplay() {
    var token = ""
    switch (this.rarity.toLocaleLowerCase()) {
      case "common": token = "C"; break;
      case "uncommon": token = "U"; break;
      case "rare": token = "R"; break;
      case "mythic": token = "M"; break;
      case "mythic rare": token = "M"; break;
    }
    return token;
  }

  get cardType(): CardType {
    var type = this.type.toLocaleLowerCase();
    if (type.includes("legendary creature") || type.includes("creature")) return CardType.Creature
    if (type == "instant") return CardType.Instant;
    if (type == "sorcery") return CardType.Sorcery;
    if (type.includes("artifact")) return CardType.Artifact;
    if (type.includes("enchantment")) return CardType.Enchantment;
    if (type.includes("land")) return CardType.Land;
    return CardType.Unknown;
  }

  get textDisplay() {
    return this.rawOracleText.split('\n').map(line => this.addManaHtmlToCardTextLine(line));
  }

  // Gets the CSS postfix to create class names that are differentiated by color identity.
  private get manaCssClassPostfix() {
    return this.colorIdentity;
  }

  // Transforms text representations of mana symbols from OpenAI into HTML that can render those mana symbols using the mana project (https://github.com/andrewgioia/mana).
  private addManaHtmlToCardTextLine(line: string) {
    manaTokens.forEach(token => {
      line = line.replaceAll(token, `<i class=\"${MagicCard.getManaClassName(token)} card-text-mana-token-${this.id}\"></i>`)
    })

    return line
  }

  // Gets the matching mana project (https://github.com/andrewgioia/mana) CSS token for <i></i> based on the mana token string.
  static getManaClassName(manaToken: string) {
    return `ms ms-${manaTokenToCssCharacter[manaToken]} ms-padding`
  }

  static getManaClassNameForTitle(manaToken: string) {
    return `ms ms-${manaTokenToCssCharacter[manaToken]} ms-padding-title`
  }

  toImage(): Promise<Blob | null> {
    var node = document.getElementById(`card-${this.id}`)
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

  getCardContainer() {
    return document.getElementById(`card-${this.id}`)
  }

  getWidth() {
    var node = document.getElementById(`card-${this.id}`)
    if (node == null) {
      return 0
    }
    return node!.clientWidth
  }

  // Adjusts the font sizes on the card to fit within the card dimensions.
  adjustFontSize() {
    this.adjustTypeLineSize()
    this.adjustNameSize()
    this.adjustFrameBottomSize();
    this.adjustCardTextSize();
  }

  increaseSize(increase: number = 40) {
    const container : HTMLElement | null = document.getElementById(`card-${this.id}`)
    this.adjustCardSize(container!.clientWidth + increase)
  }

  decreaseSize(decrease: number = 40) {
    const container : HTMLElement | null = document.getElementById(`card-${this.id}`)
    this.adjustCardSize(container!.clientWidth - decrease)
  }

  adjustCardSize(width: number = 440) {
    const container : HTMLElement | null = document.getElementById(`card-${this.id}`)
    if (container == null) {
      return
    }
    const height = ((width * 3.6) / 2.5);
    container!.style.width  = `${width}px`;
    container!.style.height = `${height}px`;
    this.adjustFontSize();
  }

  // Adjust type and set icon size relative to the height of their container.
  private adjustTypeLineSize(ratio:number = .6) {
    const container : HTMLElement | null = document.getElementById(`type-container-${this.id}`)
    const nameContainer : HTMLElement | null = document.getElementById(`type-${this.id}`)
    const manaContainer : HTMLElement | null = document.getElementById(`set-${this.id}`)

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
  private adjustNameSize(nameToContainerRatio:number = .6, manaToContainerRatio:number = .75) {
    const container : HTMLElement | null = document.getElementById(`title-container-${this.id}`)
    const nameContainer : HTMLElement | null = document.getElementById(`name-${this.id}`)
    const manaContainer : HTMLElement | null = document.getElementById(`mana-${this.id}`)

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

    while (calculateWidth() > container.offsetWidth && calculateWidth() != prevWidth) {
      prevWidth = calculateWidth()
      fontSize--;
      nameContainer.style.fontSize = fontSize + "px";

      if (fontSize <= 4) {
        break;
      }
    }
  }

  // Adjust the size of the bottom frame relative to the card background.
  private adjustFrameBottomSize(ratio:number = .045, minFontSize:number = 1) {
    const container : HTMLElement | null = document.getElementById(`card-background-${this.id}`)
    const nameContainer : HTMLElement | null = document.getElementById(`frame-bottom-${this.id}`)

    if (!container || !nameContainer) {
      return
    }

    adjustTextHeightBasedOnClientHeight(container, nameContainer, ratio, minFontSize)
  }

  private adjustCardTextSize(minFontSize:number = 5, maxFontSize:number = 30) {
    const container : HTMLElement | null = document.querySelector(`.frame-text-box-${this.id}`)
    const innerContainer : HTMLElement | null = document.querySelector(`.frame-text-box-inner-${this.id}`)
    const manaTokens = document.querySelectorAll(`.card-text-mana-token-${this.id}`)
    const ptContainer = document.getElementById(`pt-${this.id}`)
    const frameBottom = document.getElementById(`frame-bottom-${this.id}`)

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
}

interface CardDisplayProps {
  card: MagicCard;
  cardWidth: number;
  showCardMenu: boolean;
}

interface CardDisplayState {
  card: MagicCard;
  editMode: boolean;
  rawOracleTextUpdate: string;
  nameUpdate: string;
  typeUpdate : string;
  manaCostUpdate: string;
  powerAndToughnessUpdate: string;
  showTemporaryImage: boolean;
  showCardMenu: boolean;
  showSizeAdjustmentButtons: boolean;
  increaseSizeAllowed: boolean;
  decreaseSizeAllowed: boolean;
  cardWidth: number;
}

export class CardDisplay extends React.Component<CardDisplayProps, CardDisplayState> {
  constructor(props: CardDisplayProps) {
    super(props);
    const viewportWidth = Math.max(document.documentElement.clientWidth || 0, window.innerWidth || 0)
    this.state = {
      card: props.card,
      editMode: false,
      nameUpdate: props.card.name,
      rawOracleTextUpdate: props.card.rawOracleText,
      typeUpdate: props.card.typeLine,
      manaCostUpdate: props.card.manaCost,
      powerAndToughnessUpdate: props.card.pt,
      showTemporaryImage: true,
      showCardMenu: props.showCardMenu,
      increaseSizeAllowed: true,
      decreaseSizeAllowed: true,
      cardWidth: props.cardWidth,
      showSizeAdjustmentButtons: !isMobileDevice() || viewportWidth > 900,
    };

    this.handleCardNameUpdate = this.handleCardNameUpdate.bind(this);
    this.handleCardOracleTextUpdate = this.handleCardOracleTextUpdate.bind(this);
    this.handleCardTypeUpdate = this.handleCardTypeUpdate.bind(this);
    this.handleCardManaCostUpdate = this.handleCardManaCostUpdate.bind(this);
    this.handleCardPowerAndToughnessUpdate = this.handleCardPowerAndToughnessUpdate.bind(this);
  }

  componentDidUpdate(prevProps: Readonly<CardDisplayProps>, prevState: Readonly<CardDisplayState>, snapshot?: any): void {
    if (this.state.card.rawOracleText != prevState.card.rawOracleText || 
      this.state.card.typeLine != prevState.card.typeLine ||
      this.state.card.name != prevState.card.name ||
      this.state.editMode != prevState.editMode) {
        this.state.card.adjustFontSize()
    }
  }

  handleCardNameUpdate(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({ nameUpdate: event.target.value });
  }

  handleCardTypeUpdate(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({ typeUpdate: event.target.value });
  }

  handleCardOracleTextUpdate(event: React.ChangeEvent<HTMLTextAreaElement>) {
    this.setState({ rawOracleTextUpdate: event.target.value });
  }

  handleCardManaCostUpdate(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({ manaCostUpdate: event.target.value.trim() });
  }

  handleCardPowerAndToughnessUpdate(event: React.ChangeEvent<HTMLInputElement>) {
    this.setState({ powerAndToughnessUpdate: event.target.value.trim() });
  }

  updateEditMode() {
    var updatedCard = MagicCard.clone(this.state.card, true)
    updatedCard.name = this.state.nameUpdate
    updatedCard.rawOracleText = this.state.rawOracleTextUpdate
    updatedCard.typeLine = this.state.typeUpdate
    updatedCard.manaCost = this.state.manaCostUpdate
    updatedCard.pt = this.state.powerAndToughnessUpdate
    this.setState({ editMode: !this.state.editMode, card: updatedCard })
  }

  toggleShowTemporaryImage() {
    this.setState({ showTemporaryImage: !this.state.showTemporaryImage })
  }

  handleCardDownload() {
    // Bit of a hack but html-to-image cannot render the card if the image url is directly from dalle due to CORS.
    // Note that the dalle image is short lived and will not last ever, we just display it initially because it is higher resolution.
    // So when the user wants to download, we need to temporarily display our compressed version of the image.
    this.toggleShowTemporaryImage();
    this.state.card.toImage().then(blob => {
      if (blob != null) {
        saveAs(blob, `${this.state.card.name}.png`);
      }
    }).catch(error => {
      console.error("Error downloading card as image.")
    }).finally(() => {
      this.toggleShowTemporaryImage();
    })
  }

  handleArtDownload() {
    var img = this.state.card.temporaryImageUrl ? this.state.card.temporaryImageUrl : this.state.card.imageUrl
    window.open(img, '_blank');
  }

  increaseCardSize(increase: number) {
    var newWidth = this.state.cardWidth + increase
    this.state.card.adjustCardSize(newWidth)
    this.updateSizeToggles(newWidth)
  }

  decreaseCardSize(decrease: number) {
    var newWidth = this.state.cardWidth - decrease
    this.state.card.adjustCardSize(newWidth)
    this.updateSizeToggles(newWidth)
  }

  private updateSizeToggles(newWidth: number) {
    if (newWidth <= 330) {
      this.setState({ increaseSizeAllowed: true, decreaseSizeAllowed: false, cardWidth: newWidth })
    }

    if (newWidth >= 330) {
      this.setState({ increaseSizeAllowed: true, decreaseSizeAllowed: true, cardWidth: newWidth })
    }
  }

  render() {
    const card = this.state.card;
    const oracleEditTextAreaRows = card.textDisplay.length * 3;

    const cardMenuIconFontSize = "40px";
    const optionsMenuItemStyle : React.CSSProperties = {marginLeft: "5px"}
    const optionsMenuIconStyle : React.CSSProperties = {fontSize: "30px"}
    const menu = (
      <Menu>
        <Menu.Item  onClick={this.handleCardDownload.bind(this)}>
          <Tooltip placement="left">
              <CloudDownloadOutlined style={optionsMenuIconStyle} />
              <span style={optionsMenuItemStyle} >Download Card</span>
          </Tooltip>
        </Menu.Item>
        <Menu.Item onClick={this.handleArtDownload.bind(this)}>
          <Tooltip placement="left">
              <CloudDownloadOutlined style={optionsMenuIconStyle} />
              <span style={optionsMenuItemStyle} >Download Art</span>
          </Tooltip>
        </Menu.Item>
        {
        this.state.editMode ? 
          <Menu.Item onClick={this.updateEditMode.bind(this)}>
            <Tooltip title="Save the card text" placement='left'>
              <SaveFilled style={optionsMenuIconStyle} />
              <span style={optionsMenuItemStyle} >Save</span>
            </Tooltip>
          </Menu.Item>
        : 
          <Menu.Item onClick={this.updateEditMode.bind(this)}>
            <Tooltip title="Modify the card text" placement='left'>
              <EditOutlined style={optionsMenuIconStyle} />
              <span style={optionsMenuItemStyle} >Edit</span>
            </Tooltip>
          </Menu.Item>
        }
      </Menu>
    );

    const height = ((this.props.cardWidth * 3.6) / 2.5);

    let fullCard = (
      <div>
        <div id={`card-${card.id}`} className="card-container" style={{width: `${this.props.cardWidth}px`, height: `${height}px`}}>
          <div id={`card-background-${card.id}`} className={card.cardDivClassName}></div>
            <div className="card-frame">
              <div id={`title-container-${card.id}`} className={card.cardFrameHeaderClassName}>
                <div id={`name-${card.id}`} style={{alignSelf:"center"}} className="name name-type-size">
                  {!this.state.editMode ?
                    <div>{card.name}</div> :
                    <input className="card-edit-name" type="text" value={this.state.nameUpdate} onChange={this.handleCardNameUpdate} />
                  }
                </div>
                {!this.state.editMode ?
                    <div id={`mana-${card.id}`} className="mana-symbols">
                      {card.manaCostTokens.map((manaCostToken, i) => (
                        <i key={card.name + "-manaToken-"+ i} className={MagicCard.getManaClassNameForTitle(manaCostToken) + " manaCost " + `manaCost-${card.id}`} id="mana-icon"></i>
                      ))}
                    </div> :
                    <div className="card-edit-manaCost-container">
                      <input className="card-edit-manaCost" type="text" value={this.state.manaCostUpdate} onChange={this.handleCardManaCostUpdate} />
                    </div>
                }
              </div>
              <div className={card.cardFrameArtClassName}>
              {this.state.showTemporaryImage ?
                <Image onLoad={() => this.state.card.adjustFontSize()} loading="lazy" height={"100%"} width={"100%"} src={card.temporaryImageUrl ? card.temporaryImageUrl : card.imageUrl} /> :
                <Image onLoad={() => this.state.card.adjustFontSize()} loading="lazy" height={"100%"} width={"100%"} src={card.imageUrl} />
              }
              </div>
              <div id={`type-container-${card.id}`} className={card.cardFrameTypeLineClassName}>
                 {!this.state.editMode ?
                    <h1 id={`type-${card.id}`} className="type name-type-size">{card.typeLine}</h1> :
                    <input className="card-edit-type" type="text" value={this.state.typeUpdate} onChange={this.handleCardTypeUpdate} />
                  }
                <div id={`set-${card.id}`} className="mana-symbols">
                  <i className="ms ms-dfc-ignite" id="mana-icon"></i>
                </div>
              </div>
              <div className={card.cardFrameTextBoxClassName}>
                <div className={card.cardFrameContentClassName}>
                  <div className="description ftb-inner-margin">
                    {!this.state.editMode ?
                      <div>
                        {card.textDisplay.map((line, i) => (
                          <p key={card.name + "-text-" + i} dangerouslySetInnerHTML={{ __html: line }}></p>
                        ))}
                      </div> 
                      :
                      <textarea className="card-edit-text" value={this.state.rawOracleTextUpdate} rows={oracleEditTextAreaRows} onChange={this.handleCardOracleTextUpdate} />
                    }
                  </div>
                  <p className="description">
                  </p>
                  <p className="flavour-text">
                    {card.flavorText}
                  </p>
                  {card.pt &&
                    <div className="power-and-toughness-frame">
                      <div id={`pt-${card.id}`} className="power-and-toughness power-and-toughness-size">
                        {!this.state.editMode ?
                          <div>{card.pt}</div> :
                          <input className="card-edit-pt" type="text" value={this.state.powerAndToughnessUpdate} onChange={this.handleCardPowerAndToughnessUpdate} />
                        }
                      </div>
                    </div>
                  }
                  </div>
              </div>
              <div id={`frame-bottom-${card.id}`} className="frame-bottom-info inner-margin">
                <div className="fbi-left">
                  <p>{card.setNumberDisplay} {card.rarityDisplay}</p>
                  <p>OpenAI &#x2022; <img className="paintbrush" src={whitePaintBrush} alt="paintbrush icon" />Custom Magic</p>
                </div>
                <div className="fbi-center" onClick={(e) => { this.updateEditMode()}}></div>
                <div className="fbi-right">
                    &#x99; &amp; &#169; 2023 Chat GPT Turbo
                </div>
              </div>
            </div>
        </div>
        { this.state.showCardMenu &&
        <div className="card-menu">
          <Tooltip title={<div><b>Prompt: </b>{this.state.card.userPrompt}</div>} placement="left">
            <QuestionCircleOutlined style={{fontSize: cardMenuIconFontSize, marginLeft: "3px"}} />
          </Tooltip>
          { card.explanation ?
          <Tooltip title={
            <div>
              <b>About This Card</b>
              <br/>
              <br/>
              {card.explanation}
              <br/>
              <br/>
              {card.funnyExplanation}
            </div>
          } overlayInnerStyle={{width: '320px', backgroundColor:'rgba(0, 0, 0, 0.93)' }} placement="left" >
            <InfoCircleOutlined style={{fontSize: cardMenuIconFontSize, marginLeft: "3px"}} />
          </Tooltip>
          : null
          }
          { this.state.showSizeAdjustmentButtons &&
          <Button type="text" size="middle" shape="circle" disabled={!this.state.increaseSizeAllowed}>
              <ZoomInOutlined onClick={() => this.increaseCardSize(30)} style={{fontSize: cardMenuIconFontSize, marginRight: "-4px"}} />
          </Button>
          }
          { this.state.showSizeAdjustmentButtons &&
          <Button type="text" size="middle" shape="circle" disabled={!this.state.decreaseSizeAllowed}>
              <ZoomOutOutlined onClick={() => this.decreaseCardSize(30)} style={{fontSize: cardMenuIconFontSize, marginRight: "-7px"}} />
          </Button>
          }
          <Dropdown overlay={menu}>
            <a className="ant-dropdown-link" onClick={e => e.preventDefault()}>
              <CaretDownFilled style={{fontSize: cardMenuIconFontSize}} />
            </a>
          </Dropdown>
        </div>
        }
      </div>
    )

    return fullCard
  }
}
