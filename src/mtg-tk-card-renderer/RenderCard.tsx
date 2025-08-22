import React, { useEffect, useRef } from 'react';
import { createRoot } from 'react-dom/client';
import { Image } from 'antd';
import { IconsHelper, baseAssetUrl } from '../IconHelper';
import { CardFace } from '../models/CardFace';
import { MagicCard } from '../models/MagicCard';

interface PlaneswalkerAbility {
  html: string;
  cost: number | null;
}

interface SagaStep {
  step: string;
  html: string;
}

interface ClassStep {
  class: string;
  html: string;
}

interface ArchiveFrameColors {
  primary: string;
  lighter: string;
  darker: string;
  left: { primary: string; lighter: string; darker: string };
  right: { primary: string; lighter: string; darker: string };
}

interface CardProps {
  card_face: CardFace;
  back_face?: CardFace;
  is_land: boolean;
  is_legendary: boolean;
  is_planeswalker: boolean;
  has_legendary_crown: boolean;
  is_adventure: boolean;
  is_saga: boolean;
  is_class: boolean;
  is_vehicle: boolean;
  is_dualfaced: boolean;
  is_mdfc: boolean;
  is_transform: boolean;
  is_levelup: boolean;
  is_large_planeswalker?: boolean;
  mana_cost: string[];
  adventure_mana_cost: string[];
  mdfc_back_mana_cost: string[];
  mdfc_hint_text: string;
  extended_art: boolean;
  oracle_lines: string[];
  adventure_oracle_lines: string[];
  saga_reminder: string;
  class_reminder: string;
  saga_steps: SagaStep[];
  class_steps: ClassStep[];
  planeswalker_abilities: PlaneswalkerAbility[] | null;
  copyright: string;
  colors: string;
  boxes_colors: string;
  background: string;
  frame: string;
  boxes: string;
  mid_boxes: string;
  legendary_crown: string;
  pt_box: string;
  saga_text_box: string;
  mdfc_icon: string;
  mdfc_hint: string;
  mdfc_hint_color: string;
  transform_icon: string;
  pt_box_color: string;
  top_line_color: string;
  mid_line_color: string;
  color_indicator: string;
  illustration: string;
  illustration_scale: number;
  illustration_position: {
    x: string;
    y: string;
  };
  set_icon_uri: string;
  archive_frame_colors: ArchiveFrameColors;
  has_image: boolean;
}

/**
 * Replaces mana symbols in the oracle text with <img> tags
 * and wraps text in parentheses with a span.
 */
function parseOracle(str: string): string {
  const manaRegex = /{([^}]+)}/g;
  str = str.replace(manaRegex, (match: string, group: string) => {
    const key = `{${group}}`;
    const iconUri = IconsHelper.getManaIcon(key);
    return iconUri ? `<img src="${iconUri}" class="card-ms">` : match;
  });
  // Wrap text in parentheses with a special class.
  str = str.replace(/\([^)]+\)/g, (match: string) => `<span class="oracle-reminder">${match}</span>`);
  return str;
}

/**
 * Helper function that returns true if the given string contains
 * any of the search terms (case insensitive).
 */
function contains(str: string | undefined, search: string | string[]): boolean {
  if (!str || !search) return false;
  if (Array.isArray(search))
    return search.some(s => str.toLowerCase().includes(s.toLowerCase()));
  return str.toLowerCase().includes(search.toLowerCase());
}

/* =======================================================
 * Card Computation Functions
 * Computes and returns an object with properties derived from the card JSON.
 * =======================================================
 */
function computeCardProps(card: MagicCard, currentFace: number = 0): CardProps {
  const props = {} as CardProps;

  // Determine primary face (for DFC cards) and back face.
  props.card_face = card.cardFaces[0]
  props.back_face = card.cardFaces.length > 1 ? card.cardFaces[1] : undefined

  // Determine type booleans.
  props.is_land = !!(props.card_face.typeLine &&
      (props.card_face.typeLine.startsWith("Land") ||
          props.card_face.typeLine.startsWith("Terrain")));
  props.is_legendary = !!((card.frameEffects && card.frameEffects.includes("legendary")) ||
      (props.card_face.typeLine &&
          (props.card_face.typeLine.startsWith("Legendary") ||
              props.card_face.typeLine.includes("légendaire"))));
  props.is_planeswalker = !!(props.card_face.typeLine &&
      props.card_face.typeLine.toLowerCase().includes("planeswalker"));
  props.has_legendary_crown = props.is_legendary &&
      !props.is_planeswalker &&
      !(card.frameEffects && card.frameEffects.includes("compasslanddfc"));
  props.is_adventure = card.layout === "adventure";
  props.is_saga = !!(props.card_face.layout === "saga" ||
      (props.card_face.typeLine && props.card_face.typeLine.includes("Saga")));
  props.is_class = !!(props.card_face.layout === "class" ||
      (props.card_face.typeLine && props.card_face.typeLine.includes("Class")));
  props.is_vehicle = !!(props.card_face.typeLine &&
      (props.card_face.typeLine.includes("Vehicle") ||
          props.card_face.typeLine.includes("Véhicule")));
  props.is_dualfaced = !!(card.cardFaces && !props.is_adventure);
  props.is_mdfc = card.layout === "modal_dfc";
  props.is_transform = card.layout === "transform";
  props.is_levelup = !!(props.card_face.oracleText &&
      (props.card_face.oracleText.includes("Level up") ||
          props.card_face.oracleText.includes("Montée de niveau")));

  // Parse mana cost using our IconsHelper module.
  props.mana_cost = props.card_face.manaCost
      ? IconsHelper.parseManaCost(props.card_face.manaCost)
      : [];

  // Adventure or modal back mana cost.
  props.adventure_mana_cost = [];
  if (card.cardFaces && card.cardFaces[1] && card.cardFaces[1].manaCost) {
    props.adventure_mana_cost = IconsHelper.parseManaCost(card.cardFaces[1].manaCost);
  }
  props.mdfc_back_mana_cost = [];
  if (
      card.cardFaces &&
      card.cardFaces[(currentFace + 1) % 2] &&
      card.cardFaces[(currentFace + 1) % 2].manaCost
  ) {
    props.mdfc_back_mana_cost = IconsHelper.parseManaCost(card.cardFaces[(currentFace + 1) % 2].manaCost!);
  }

  // MDFC hint text.
  if (props.card_face.mdfc_hint)
    props.mdfc_hint_text = props.card_face.mdfc_hint;
  else if (props.back_face?.typeLine)
    props.mdfc_hint_text = props.back_face.typeLine.substr(props.back_face.typeLine.indexOf("—") + 1).trim();
  else
    props.mdfc_hint_text = "";

  // Extended art detection.
  props.extended_art = ["extended", "full", "full-footer"].includes(props.card_face.artVariant || "");

  // Oracle text and flavor text.
  props.oracle_lines = props.card_face.oracleText
      ? props.card_face.oracleText.split("\n").map(parseOracle)
      : [];
  props.adventure_oracle_lines = (card.cardFaces && card.cardFaces[1] && card.cardFaces[1].oracleText)
      ? card.cardFaces[1].oracleText.split("\n").map(parseOracle)
      : [];
  props.saga_reminder = props.card_face.oracleText
      ? parseOracle(props.card_face.oracleText.split("\n")[0])
      : "";
  props.class_reminder = props.saga_reminder;
  if (props.card_face.oracleText) {
    const steps = props.card_face.oracleText.split("\n").filter(s => s.match(/^([IVX]+) — /));
    props.saga_steps = steps.map(s => {
      const m = s.match(/([IVX]+) — (.+)/);
      if (m) {
        const step = m[1].trim();
        return { step: step, html: parseOracle(m[2]) };
      }
      return { step: "", html: "" };
    });
  } else {
    props.saga_steps = [];
  }
  props.class_steps = props.card_face.oracleText
      ? props.card_face.oracleText.split("\n").slice(1).map(line => ({
        class: "normal",
        html: parseOracle(line)
      }))
      : [];

  // Planeswalker abilities.
  if (props.is_planeswalker && props.card_face.oracleText) {
    props.planeswalker_abilities = props.card_face.oracleText.split("\n").map(line => {
      const ability: PlaneswalkerAbility = { html: parseOracle(line), cost: null };
      const m = line.match(/^[+-−]?(\d+):/);
      if (m) {
        if (line[0] === "0") ability.cost = 0;
        else if (line[0] === "+") ability.cost = parseInt(m[1]);
        else ability.cost = -parseInt(m[1]);
        ability.html = parseOracle(line.substr(m[0].length + 1));
      }
      return ability;
    });
  } else {
    props.planeswalker_abilities = null;
  }

  if (props.is_planeswalker) {
    // For planeswalkers, add a "large" class if there are more than 3 abilities.
    if (props.planeswalker_abilities && props.planeswalker_abilities.length > 3) {
      props.is_large_planeswalker = true;
    } else {
      props.is_large_planeswalker = false;
    }
  }

  // Copyright.
  props.copyright = card.copyright || `™ & © ${new Date().getFullYear()} MTG Card Generator`;

  // Compute colors.
  function computeColors(face: CardFace): string {
    // Determine base colors
    let colors: string[];
    if (face.colors && face.colors.length > 0) colors = face.colors;
    else if (face.color_identity) colors = face.color_identity;
    else if (face.manaCost) colors = Array.from(face.manaCost).filter(c => "WUBRG".includes(c));
    else colors = [];

    // Fall back to card color identity if no colors found
    if (colors.length === 0) {
        // If saga card, use color Land, since Artifact does not exist
        if (props.is_saga) colors = ["Land"];
        else colors = ["Artifact"];
    }

    // Remove duplicates and get unique colors
    const uniqueColors = Array.from(new Set(colors));

    // Sort colors based on WUBRG order
    const sorted_colors = uniqueColors.sort((l, r) => "WUBRG".indexOf(l) - "WUBRG".indexOf(r));

    // Convert to string
    let colorString = sorted_colors.join("");

    // Special cases
    if (contains(face.typeLine, ["Artifact", "Artefact", "Artéfact"])) {
      return "Artifact";
    }

    if (colorString.length === 0) {
      return "Colourless";
    }

    if (colorString.length > 2) {
      return "Gold";
    }

    // For exactly 2 colors, ensure WUBRG order
    if (colorString.length === 2) {
      const firstIndex = "WUBRG".indexOf(colorString[0]);
      const secondIndex = "WUBRG".indexOf(colorString[1]);
      if (firstIndex > secondIndex) {
        // Swap the characters if they're in the wrong order
        colorString = colorString[1] + colorString[0];
      }
    }

    return colorString;
  }
  props.colors = computeColors(props.card_face);

  // Determine boxes colors.
  if (props.is_vehicle) props.boxes_colors = "Artifact";
  else if (props.is_land)
    props.boxes_colors = props.colors.length > 2 && props.colors.length < 5 ? "Gold" : "Land";
  else
    props.boxes_colors = props.colors.length > 1 && props.colors.length < 5 ? "Gold" : props.colors;

  // Choose background – select folder based on type.
  let folder: string;
  if (props.is_planeswalker) {
    folder = props.is_large_planeswalker ? "planeswalker_large_bg" : "planeswalker_bg";
  }
  else if (props.is_saga) folder = "saga_bg";
  else if (props.is_class) folder = "saga_bg";
  else if (card.frameEffects && card.frameEffects.includes("compasslanddfc") && currentFace === 1)
    folder = "ixalan_bg";
  else folder = "bg";
  props.background = `url(${baseAssetUrl}img/${folder}/${props.is_vehicle ? "Vehicle" : props.boxes_colors}.webp)`;

  // Choose frame image folder.
  if (props.is_adventure) folder = "adventure_frames";
  else if (props.is_saga) folder = "saga_frames";
  else if (props.is_class) folder = "saga_frames";
  else if (props.is_planeswalker) folder = props.is_large_planeswalker ? "planeswalker_large_frames" : "planeswalker_frames";
  else if (props.is_mdfc) folder = "mdfc_frames";
  else if (props.is_transform) folder = currentFace === 0 ? "transform_frames" : "transform_back_frames";
  else folder = "frames";
  if (props.extended_art && !props.is_saga && !props.is_class) folder = "extended_" + folder;
  const frameColor = (props.colors === "Artifact" && props.card_face.colors && props.card_face.colors.length === 1)
      ? props.card_face.colors[0]
      : props.colors;
  props.frame = `url(${baseAssetUrl}img/${folder}/${frameColor}.webp)`;

  // Boxes and mid_boxes images.
  folder = props.is_planeswalker
      ? "planeswalker_boxes"
      : props.is_mdfc || props.is_transform
          ? currentFace === 0 ? "mdfc_boxes" : "mdfc_back_boxes"
          : props.extended_art && !props.is_class
              ? "extended_boxes"
              : "boxes";
  props.boxes = `url(${baseAssetUrl}img/${folder}/${props.boxes_colors}.webp)`;
  props.mid_boxes = props.extended_art && !props.is_planeswalker && !props.is_class
      ? `url(${baseAssetUrl}img/extended_boxes/${props.boxes_colors}.webp)`
      : props.boxes;

  // Legendary crown.
  const crownFolder = props.extended_art ? "extended_legendary_crowns" : "legendary_crowns";
  props.legendary_crown = `url(${baseAssetUrl}img/${crownFolder}/${props.boxes_colors}.webp)`;

  // Power/Toughness box.
  const ptFolder = (props.is_mdfc || props.is_transform) && currentFace === 1 ? "transform_back_pt_boxes" : "pt_boxes";
  props.pt_box = `url(${baseAssetUrl}img/${ptFolder}/${props.is_vehicle ? "Vehicle" : props.boxes_colors}.webp)`;

  // Saga text box.
  props.saga_text_box = `url(${baseAssetUrl}img/saga_textboxes/${props.boxes_colors}.webp)`;

  // MDFC icon and hint.
  props.mdfc_icon = `url(${baseAssetUrl}img/mdfc${currentFace === 0 ? "" : "_back"}_icons/${props.boxes_colors === "Land" ? props.colors : props.boxes_colors}.webp)`;
  let hintColors = props.colors;
  if (hintColors.length > 1 && hintColors.length < 5) hintColors = "Gold";
  props.mdfc_hint = `url(${baseAssetUrl}img/mdfc${currentFace === 0 ? "" : "_back"}_hints/${hintColors}.webp)`;
  props.mdfc_hint_color = currentFace === 0 ? "white" : "black";

  // Transform icon.
  props.transform_icon = `url(${baseAssetUrl}img/transform${currentFace === 0 ? "" : "_back"}_icons/${(card.frameEffects && card.frameEffects[0]) ? card.frameEffects[0] : "sunmoondfc"}.webp)`;

  // Determine power/toughness box color and top/mid line colors.
  props.pt_box_color = props.is_vehicle || (props.is_transform && currentFace === 1) ? "white" : "black";
  if (props.is_mdfc)
    props.top_line_color = currentFace === 0 ? "black" : "white";
  else if (props.is_transform && currentFace === 1 && !props.is_planeswalker)
    props.top_line_color = "white";
  else
    props.top_line_color = "black";
  if ((props.extended_art && !props.is_class && !props.is_planeswalker && (!props.is_transform || currentFace === 0)) ||
      ((props.is_transform || props.is_mdfc) && currentFace === 1 && !props.is_planeswalker))
    props.mid_line_color = "white";
  else
    props.mid_line_color = "black";

  // Color indicator (if available).
  if (props.card_face.color_indicator) {
    const sorted = Array.from(props.card_face.color_indicator)
        .sort((a, b) => "WUBRG".indexOf(a) - "WUBRG".indexOf(b))
        .join("");
    props.color_indicator = `${baseAssetUrl}img/color_indicators/${sorted}.webp`;
  } else {
    props.color_indicator = "";
  }

  // Illustration handling – choose art crop if available.
  // const artCard = props.is_adventure ? card : props.card_face;
  const artCard = props.card_face;

  const artUrl = props.card_face.imageUrl;
  const hasImage = !!(artUrl && artUrl.trim() !== "");
  /*
  let artUrl = "";
  if (artCard.image_uris && artCard.image_uris.art_crop && artCard.image_uris.art_crop.trim() !== "") {
    artUrl = artCard.image_uris.art_crop;
  } else {
    artUrl = `${baseAssetUrl}img/placeholder-art.webp`;
  }
  */

  props.illustration = hasImage ? `url("${artUrl}")` : 'none';
  props.illustration_scale = artCard.illustration_scale || 1;
  if (artCard.illustration_position) {
    props.illustration_position = {
      x: artCard.illustration_position.x + "mm",
      y: artCard.illustration_position.y + "mm"
    };
  } else {
    props.illustration_position = { x: "0mm", y: "0mm" };
  }

  // Set icon URI (using the pre-selected setIconUrl from the card).
  props.set_icon_uri = card.setIconUrl || "";

  // Store whether the card has an image
  props.has_image = hasImage;

  // Archive frame colors (simplified).
  const archiveColor = (props.card_face.color_identity && props.card_face.color_identity[0]) ? props.card_face.color_identity[0] : "Gold";
  props.archive_frame_colors = {
    primary: archiveColor,
    lighter: archiveColor,
    darker: archiveColor,
    left: { primary: archiveColor, lighter: archiveColor, darker: archiveColor },
    right: { primary: archiveColor, lighter: archiveColor, darker: archiveColor }
  };

  return props;
}

/* =======================================================
 * Card Rendering Functions
 * =======================================================
 */

/**
 * React component props for rendering any type of MTG card component.
 */
interface CardComponentProps {
  cardProps: CardProps;
  card: MagicCard;
  showFlavorText?: boolean;
  onCreateImageClick?: () => void;
  isImageEditorOpen?: boolean;
}

/**
 * Custom hook for text resizing functionality.
 */
function useTextResizing(cardProps: CardProps) {
  const cardNameRef = useRef<HTMLSpanElement>(null);
  const typeLineRef = useRef<HTMLDivElement>(null);
  const oracleRef = useRef<HTMLDivElement>(null);

  const resizeTextHorizontally = (el: HTMLElement) => {
    if (!el) return;
    // Force a reflow to ensure we have correct dimensions
    void el.offsetHeight;

    const computedStyle = window.getComputedStyle(el);
    let fontSize = parseFloat(computedStyle.fontSize);
    el.style.fontSize = fontSize + "px";

    // Add a small delay to ensure styles are applied
    setTimeout(() => {
      while (el.scrollWidth > el.clientWidth && fontSize > 4) {
        fontSize -= 0.5;
        el.style.fontSize = fontSize + "px";
      }
    }, 10);
  };

  const resizeOracleText = (oracle: HTMLElement, needsSpace: boolean) => {
    if (!oracle) return;
    // Start with a reasonable font size.
    let fontSize = 1.0;
    oracle.style.fontSize = `${fontSize}em`;

    // Get container dimensions
    const boxHeight = oracle.clientHeight;
    const boxWidth = oracle.clientWidth;

    // Reduce font size until content fits.
    while ((oracle.scrollHeight > boxHeight || oracle.scrollWidth > boxWidth) && fontSize > 0.5) {
      fontSize -= 0.05;
      oracle.style.fontSize = `${fontSize}em`;
    }

    // If we need space for P/T box, reduce slightly more.
    if (needsSpace && oracle.scrollHeight > boxHeight * 0.9) {
      fontSize *= 0.95;
      oracle.style.fontSize = `${fontSize}em`;
    }
  };

  useEffect(() => {
    // Setup resize observers and initial resizing.
    const observers: (ResizeObserver | MutationObserver)[] = [];

    // Resize card name.
    if (cardNameRef.current) {
      const nameEl = cardNameRef.current;
      const nameResizeObserver = new ResizeObserver(() => {
        resizeTextHorizontally(nameEl);
      });
      nameResizeObserver.observe(nameEl);
      resizeTextHorizontally(nameEl);
      observers.push(nameResizeObserver);

      const nameObserver = new MutationObserver(() => resizeTextHorizontally(nameEl));
      nameObserver.observe(nameEl, { childList: true, subtree: true, characterData: true });
      observers.push(nameObserver);
    }

    // Resize type line.
    if (typeLineRef.current) {
      const typeLineEl = typeLineRef.current;
      const typeLineResizeObserver = new ResizeObserver(() => {
        resizeTextHorizontally(typeLineEl);
      });
      typeLineResizeObserver.observe(typeLineEl);
      resizeTextHorizontally(typeLineEl);
      observers.push(typeLineResizeObserver);

      const typeLineObserver = new MutationObserver(() => resizeTextHorizontally(typeLineEl));
      typeLineObserver.observe(typeLineEl, { childList: true, subtree: true, characterData: true });
      observers.push(typeLineObserver);
    }

    // Handle oracle text resizing.
    if (oracleRef.current && !cardProps.is_planeswalker && !cardProps.is_saga) {
      const oracle = oracleRef.current;
      const needsSpace = cardProps.card_face.power !== undefined &&
          cardProps.card_face.toughness !== undefined;

      const oracleResizeObserver = new ResizeObserver(() => {
        resizeOracleText(oracle, needsSpace);
      });
      oracleResizeObserver.observe(oracle);
      resizeOracleText(oracle, needsSpace);
      observers.push(oracleResizeObserver);

      const oracleObserver = new MutationObserver(() =>
          resizeOracleText(oracle, needsSpace)
      );
      oracleObserver.observe(oracle, {
        childList: true,
        subtree: true,
        characterData: true
      });
      observers.push(oracleObserver);
    }

    // Cleanup function.
    return () => {
      observers.forEach(observer => observer.disconnect());
    };
  }, [cardProps]);

  return { cardNameRef, typeLineRef, oracleRef };
}

function PlaneswalkerCard({ cardProps, card, onCreateImageClick, isImageEditorOpen = false }: CardComponentProps): React.ReactElement {
  const { cardNameRef, typeLineRef } = useTextResizing(cardProps);

  // Extract the URL from the CSS url() string
  const getImageUrl = (cssUrl: string): string => {
    const match = cssUrl.match(/url\(["']?([^"']*)["']?\)/);
    return match ? match[1] : cssUrl;
  };

  return (
    <>
      <div className="inner-background"></div>
      <div className="illustration behind-textbox">
        {cardProps.illustration && cardProps.illustration !== 'none' && cardProps.illustration !== 'url("")' && (
          <Image preview={true} loading="lazy" height={"100%"} width={"100%"} src={getImageUrl(cardProps.illustration)} />
        )}
        {!cardProps.has_image && !isImageEditorOpen && (
          <div className="create-image-overlay">
            <button 
              className="create-image-button" 
              onClick={onCreateImageClick}
            >
              ▶ Create Image
            </button>
          </div>
        )}
      </div>
      <div className="planeswalker-oracle-bg"></div>
      <div className="inner-frame"></div>
      <div className="legendary-crown" style={{ display: 'none' }}></div>
      <div className="top-line">
        <span ref={cardNameRef} className="card-name">{cardProps.card_face.name}</span>
        <div className="mana-cost">
          {cardProps.mana_cost.map((uri: string, index: number) => (
            <img key={index} src={uri} className="card-ms" />
          ))}
        </div>
      </div>
      <div className="mid-line">
        {cardProps.card_face.color_indicator && (
          <img className="color-indicator" src={cardProps.color_indicator} />
        )}
        <div ref={typeLineRef} className="type-line">{cardProps.card_face.typeLine}</div>
        {cardProps.set_icon_uri && (
          <div className="set-icon-container">
            <img className="set-icon" src={cardProps.set_icon_uri} />
          </div>
        )}
      </div>
      <div className="oracle planeswalker-oracle" style={{ fontSize: '6pt' }}>
        {cardProps.planeswalker_abilities && cardProps.planeswalker_abilities.map((ability: PlaneswalkerAbility, index: number) => (
          <div key={index} className={ability.cost !== null ? "planeswalker-ability planeswalker-ability-with-cost" : "planeswalker-ability"}>
            {ability.cost !== null && (
              <div className={`planeswalker-ability-cost ${ability.cost > 0 ? "planeswalker-ability-cost-plus" : "planeswalker-ability-cost-minus"}`}>
                {ability.cost > 0 ? "+" + ability.cost : ability.cost}
              </div>
            )}
            <div className="planeswalker-ability-text" dangerouslySetInnerHTML={{ __html: ability.html }} />
          </div>
        ))}
      </div>
      <div className="loyalty">{cardProps.card_face.loyalty}</div>
      <div className="footer">
        <div className="footer-left">
          <div className="collector-number">
            {card.collectorNumber}
          </div>
          <div>
            {card.set && <span className="set">{card.set.toUpperCase()}</span>}
            {card.set && card.lang && <span>&nbsp;•&nbsp;</span>}
            {card.lang && <span className="language">{card.lang.toUpperCase()}</span>}
            {card.artist && <span className="artist-icon"> a </span>}
            {card.artist && <span className="artist-name">{card.artist}</span>}
          </div>
        </div>
        <div className="footer-right">
          <div>&nbsp;</div>
          <div className="copyright">
            {card.copyright || `™ & © ${new Date().getFullYear()} MTG Card Generator`}
          </div>
        </div>
      </div>
    </>
  );
}

function SagaCard({ cardProps, card, onCreateImageClick, isImageEditorOpen = false }: CardComponentProps): React.ReactElement {
  const { cardNameRef, typeLineRef } = useTextResizing(cardProps);

  // Extract the URL from the CSS url() string
  const getImageUrl = (cssUrl: string): string => {
    const match = cssUrl.match(/url\(["']?([^"']*)["']?\)/);
    return match ? match[1] : cssUrl;
  };

  // Extract saga steps based on oracle text format "I — Text", "II — Text", etc.
  const sagaSteps: { number: string; text: string }[] = [];
  if (cardProps.card_face.oracleText) {
    const lines = cardProps.card_face.oracleText.split('\n');
    for (const line of lines) {
      const match = line.match(/^([IVX]+)\s*—\s*(.+)$/);
      if (match) {
        sagaSteps.push({
          number: match[1],
          text: parseOracle(match[2])
        });
      }
    }
  }

  return (
    <>
      <div className="inner-background"></div>
      <div className="illustration">
        {cardProps.illustration && cardProps.illustration !== 'none' && cardProps.illustration !== 'url("")' && (
          <Image preview={true} loading="lazy" height={"100%"} width={"100%"} src={getImageUrl(cardProps.illustration)} />
        )}
        {!cardProps.has_image && !isImageEditorOpen && (
          <div className="create-image-overlay">
            <button 
              className="create-image-button" 
              onClick={onCreateImageClick}
            >
              ▶ Create Image
            </button>
          </div>
        )}
      </div>
      <div className="inner-frame"></div>
      <div className="legendary-crown" style={{ display: cardProps.has_legendary_crown ? 'block' : 'none' }}></div>
      <div className="top-line">
        <span ref={cardNameRef} className="card-name">{cardProps.card_face.name}</span>
        <div className="mana-cost">
          {cardProps.mana_cost.map((uri: string, index: number) => (
            <img key={index} src={uri} className="card-ms" />
          ))}
        </div>
      </div>
      <div className="oracle saga-oracle">
        <div className="saga-reminder">
          <i>(As this Saga enters and after your draw step, add a lore counter. Sacrifice after III.)</i>
        </div>
        <div className="saga-frame"></div>
        <div className="saga-steps">
          {sagaSteps.map((step, index) => (
            <div key={index} className="saga-step">
              <div className="saga-step-number">
                <img src={`${baseAssetUrl}img/saga/${step.number}.webp`} alt={step.number} />
              </div>
              <div dangerouslySetInnerHTML={{ __html: step.text }} />
            </div>
          ))}
        </div>
      </div>
      <div className="mid-line">
        {cardProps.card_face.color_indicator && (
          <img className="color-indicator" src={cardProps.color_indicator} />
        )}
        <div ref={typeLineRef} className="type-line">{cardProps.card_face.typeLine}</div>
        {cardProps.set_icon_uri && (
          <div className="set-icon-container">
            <img className="set-icon" src={cardProps.set_icon_uri} />
          </div>
        )}
      </div>
      <div className="footer">
        <div className="footer-left">
          <div className="collector-number">
            {card.collectorNumber}
          </div>
          <div>
            {card.set && <span className="set">{card.set.toUpperCase()}</span>}
            {card.set && card.lang && <span>&nbsp;•&nbsp;</span>}
            {card.lang && <span className="language">{card.lang.toUpperCase()}</span>}
            {card.artist && <span className="artist-icon"> a </span>}
            {card.artist && <span className="artist-name">{card.artist}</span>}
          </div>
        </div>
        <div className="footer-right">
          <div>&nbsp;</div>
          <div className="copyright">
            {card.copyright || `™ & © ${new Date().getFullYear()} MTG Card Generator`}
          </div>
        </div>
      </div>
    </>
  );
}

function DefaultCard({ cardProps, card, showFlavorText = true, onCreateImageClick, isImageEditorOpen = false }: CardComponentProps): React.ReactElement {
  const { cardNameRef, typeLineRef, oracleRef } = useTextResizing(cardProps);

  // Extract the URL from the CSS url() string
  const getImageUrl = (cssUrl: string): string => {
    const match = cssUrl.match(/url\(["']?([^"']*)["']?\)/);
    return match ? match[1] : cssUrl;
  };

  return (
    <>
      <div className="inner-background"></div>
      <div className="inner-frame"></div>
      <div className="top-line">
        <span ref={cardNameRef} className="card-name">{cardProps.card_face.name}</span>
        <div className="mana-cost">
          {cardProps.mana_cost.map((uri: string, index: number) => (
            <img key={index} src={uri} className="card-ms" />
          ))}
        </div>
      </div>
      <div className="mid-line">
        {cardProps.card_face.color_indicator && (
          <img className="color-indicator" src={cardProps.color_indicator} />
        )}
        <div ref={typeLineRef} className="type-line">{cardProps.card_face.typeLine}</div>
        {cardProps.set_icon_uri && (
          <div className="set-icon-container">
            <img className="set-icon" src={cardProps.set_icon_uri} />
          </div>
        )}
      </div>
      <div className='illustration'>
        {cardProps.illustration && cardProps.illustration !== 'none' && cardProps.illustration !== 'url("")' && (
          <Image preview={true} loading="lazy" height={"100%"} width={"100%"} src={getImageUrl(cardProps.illustration)} />
        )}
        {!cardProps.has_image && !isImageEditorOpen && (
          <div className="create-image-overlay">
            <button 
              className="create-image-button" 
              onClick={onCreateImageClick}
            >
              ▶ Create Image
            </button>
          </div>
        )}
      </div>
      <div ref={oracleRef} className="oracle normal-oracle">
        {cardProps.oracle_lines.map((line: string, index: number) => (
          <div key={index} className="oracle-inner" dangerouslySetInnerHTML={{ __html: line }} />
        ))}
        {showFlavorText && cardProps.card_face.flavorText && (
          <div className="oracle-flavor">
            <hr />
            {cardProps.card_face.flavorText}
          </div>
        )}
      </div>
      {cardProps.card_face.power !== undefined && cardProps.card_face.toughness !== undefined && (
        <div className="pt-box">
          <span>{cardProps.card_face.power}</span>/<span>{cardProps.card_face.toughness}</span>
        </div>
      )}
      <div className="footer">
        <div className="footer-left">
          <div className="collector-number">
            {card.collectorNumber}
          </div>
          <div>
            {card.set && <span className="set">{card.set.toUpperCase()}</span>}
            {card.set && card.lang && <span>&nbsp;•&nbsp;</span>}
            {card.lang && <span className="language">{card.lang.toUpperCase()}</span>}
            {card.artist && <span className="artist-icon"> a </span>}
            {card.artist && <span className="artist-name">{card.artist}</span>}
          </div>
        </div>
        <div className="footer-right">
          <div>&nbsp;</div>
          <div className="copyright">
            {card.copyright || `™ & © ${new Date().getFullYear()} MTG Card Generator`}
          </div>
        </div>
      </div>
    </>
  );
}

interface MTGCardProps {
  card: MagicCard;
  scale?: number;
  renderMargin?: number;
  className?: string;
  showFlavorText?: boolean;
  onCreateImageClick?: () => void;
  isImageEditorOpen?: boolean;
}

function MTGCard({ 
  card, 
  scale = 1, 
  renderMargin = 1, 
  className, 
  showFlavorText = true, 
  onCreateImageClick,
  isImageEditorOpen = false
}: MTGCardProps): React.ReactElement {
  const currentFace = 0;
  const props = computeCardProps(card, currentFace);

  const cardClasses = ["mtg-card"];
  if (className) cardClasses.push(className);
  if (props.is_planeswalker) {
    cardClasses.push("planeswalker");
    if (props.is_large_planeswalker) {
      cardClasses.push("planeswalker-large");
    }
  }
  if (props.is_saga) cardClasses.push("saga");
  if (props.is_legendary) cardClasses.push("legendary");

  const cardStyle: React.CSSProperties = {
    '--bg-image': props.background,
    '--frame-image': props.frame,
    '--boxes-image': props.boxes,
    '--mid-boxes-image': props.mid_boxes,
    '--top-line-color': props.top_line_color,
    '--mid-line-color': props.mid_line_color,
    '--pt-box-image': props.pt_box,
    '--pt-box-color': props.pt_box_color,
    '--saga-text-box-image': props.saga_text_box,
    '--mdfc-icon-image': props.mdfc_icon,
    '--mdfc-hint-image': props.mdfc_hint,
    '--mdfc-hint-color': props.mdfc_hint_color,
    '--transform-icon-image': props.transform_icon,
    '--renderMargin': renderMargin.toString(),
    '--scale': scale.toString(),
    '--frame-colors-left': props.archive_frame_colors.left.primary,
    '--frame-colors-right': props.archive_frame_colors.right.primary,
    '--illustration-image': props.illustration,
    '--illustration-position-x': props.illustration_position.x,
    '--illustration-position-y': props.illustration_position.y,
    '--illustration-scale': props.illustration_scale.toString(),
  } as React.CSSProperties;

  return (
    <div className={cardClasses.join(' ')} style={cardStyle}>
      {props.is_planeswalker ? (
        <PlaneswalkerCard 
          cardProps={props} 
          card={card} 
          onCreateImageClick={onCreateImageClick} 
          isImageEditorOpen={isImageEditorOpen} 
        />
      ) : props.is_saga ? (
        <SagaCard 
          cardProps={props} 
          card={card} 
          onCreateImageClick={onCreateImageClick} 
          isImageEditorOpen={isImageEditorOpen} 
        />
      ) : (
        <DefaultCard 
          cardProps={props} 
          card={card} 
          showFlavorText={showFlavorText}
          onCreateImageClick={onCreateImageClick} 
          isImageEditorOpen={isImageEditorOpen} 
        />
      )}
    </div>
  );
}

function RenderCard(card: MagicCard, scale: number = 1, renderMargin: number = 1, onCreateImageClick?: () => void, isImageEditorOpen: boolean = false, showFlavorText: boolean = true): HTMLElement {
  const container = document.createElement('div');
  const root = createRoot(container);
  
  root.render(React.createElement(MTGCard, { 
    card, 
    scale, 
    renderMargin, 
    showFlavorText,
    onCreateImageClick, 
    isImageEditorOpen 
  }));
  
  return container.firstElementChild as HTMLElement;
}

export { RenderCard, MTGCard, DefaultCard, PlaneswalkerCard, SagaCard };
export type { CardProps, MTGCardProps, CardComponentProps };
export default MTGCard;
