import React, { useEffect, useRef } from 'react';
import { createRoot } from 'react-dom/client';
import { Image } from 'antd';
import { IconsHelper, setsHelper } from './Helpers';
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
}

/* =======================================================
 * Utility Functions
 * =======================================================
 */

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
 * Text Resizing Functions
 * =======================================================
 */

/**
 * Resizes oracle text to fit within its container.
 */
function resizeOracleText(oracle: HTMLElement, cardEl: HTMLElement, needsSpace: boolean): void {
  // Start with a reasonable font size
  let fontSize = 1.0;
  oracle.style.fontSize = `${fontSize}em`;

  // Get container dimensions
  const boxHeight = oracle.clientHeight;
  const boxWidth = oracle.clientWidth;

  // Reduce font size until content fits
  while ((oracle.scrollHeight > boxHeight || oracle.scrollWidth > boxWidth) && fontSize > 0.5) {
    fontSize -= 0.05;
    oracle.style.fontSize = `${fontSize}em`;
  }

  // If we need space for P/T box, reduce slightly more
  if (needsSpace && oracle.scrollHeight > boxHeight * 0.9) {
    fontSize *= 0.95;
    oracle.style.fontSize = `${fontSize}em`;
  }
}

/**
 * Creates and sets up observers for text resizing.
 * This version ensures proper initial sizing and subsequent updates.
 */
function setupTextResizing(cardEl: HTMLElement, props: CardProps): void {
  // Create a function to handle initial setup once the DOM is ready
  const setupElements = (): void => {
    // Resize card name
    const nameEl = cardEl.querySelector('.card-name') as HTMLElement;
    if (nameEl) {
      const nameResizeObserver = new ResizeObserver(() => {
        resizeTextHorizontally(nameEl);
      });
      nameResizeObserver.observe(nameEl);
      // Initial resize
      resizeTextHorizontally(nameEl);

      // Also observe for content changes
      const nameObserver = new MutationObserver(() => resizeTextHorizontally(nameEl));
      nameObserver.observe(nameEl, { childList: true, subtree: true, characterData: true });
    }

    // Resize type line
    const typeLineEl = cardEl.querySelector('.type-line') as HTMLElement;
    if (typeLineEl) {
      const typeLineResizeObserver = new ResizeObserver(() => {
        resizeTextHorizontally(typeLineEl);
      });
      typeLineResizeObserver.observe(typeLineEl);
      // Initial resize
      resizeTextHorizontally(typeLineEl);

      // Also observe for content changes
      const typeLineObserver = new MutationObserver(() => resizeTextHorizontally(typeLineEl));
      typeLineObserver.observe(typeLineEl, { childList: true, subtree: true, characterData: true });
    }

    // Handle oracle text
    if (!props.is_planeswalker && !props.is_saga) {
      const oracle = cardEl.querySelector('.oracle') as HTMLElement;
      if (oracle) {
        // Check if card needs bottom space
        const needsSpace = props.card_face.power !== undefined &&
            props.card_face.toughness !== undefined;

        const oracleResizeObserver = new ResizeObserver(() => {
          resizeOracleText(oracle, cardEl, needsSpace);
        });
        oracleResizeObserver.observe(oracle);
        // Initial resize
        resizeOracleText(oracle, cardEl, needsSpace);

        // Also observe for content changes
        const oracleObserver = new MutationObserver(() =>
            resizeOracleText(oracle, cardEl, needsSpace)
        );
        oracleObserver.observe(oracle, {
          childList: true,
          subtree: true,
          characterData: true
        });
      }
    } else if (props.is_saga) {
      // For saga cards, handle saga oracle text resizing
      const sagaReminderEl = cardEl.querySelector('.saga-reminder') as HTMLElement;
      if (sagaReminderEl) {
        const reminderResizeObserver = new ResizeObserver(() => {
          let fontSize = 7.3;
          sagaReminderEl.style.fontSize = `${fontSize}pt`;
          while (sagaReminderEl.scrollWidth > sagaReminderEl.clientWidth && fontSize > 5) {
            fontSize -= 0.2;
            sagaReminderEl.style.fontSize = `${fontSize}pt`;
          }
        });
        reminderResizeObserver.observe(sagaReminderEl);
        // Initial resize
        let fontSize = 7.3;
        sagaReminderEl.style.fontSize = `${fontSize}pt`;
        setTimeout(() => {
          while (sagaReminderEl.scrollWidth > sagaReminderEl.clientWidth && fontSize > 5) {
            fontSize -= 0.2;
            sagaReminderEl.style.fontSize = `${fontSize}pt`;
          }
        }, 0);
      }

      const sagaStepsEl = cardEl.querySelector('.saga-steps') as HTMLElement;
      if (sagaStepsEl) {
        const stepsResizeObserver = new ResizeObserver(() => {
          let fontSize = 6.48;
          sagaStepsEl.style.fontSize = `${fontSize}pt`;
          while (sagaStepsEl.scrollHeight > sagaStepsEl.clientHeight && fontSize > 4.5) {
            fontSize -= 0.2;
            sagaStepsEl.style.fontSize = `${fontSize}pt`;
          }
        });
        stepsResizeObserver.observe(sagaStepsEl);
        // Initial resize
        let fontSize = 6.48;
        sagaStepsEl.style.fontSize = `${fontSize}pt`;
        setTimeout(() => {
          while (sagaStepsEl.scrollHeight > sagaStepsEl.clientHeight && fontSize > 4.5) {
            fontSize -= 0.2;
            sagaStepsEl.style.fontSize = `${fontSize}pt`;
          }
        }, 0);
      }
    }
  };

  // Ensure elements are in DOM before setting up observers
  if (document.readyState === 'complete') {
    setupElements();
  } else {
    // Wait for DOM to be ready
    window.addEventListener('load', setupElements);
  }
}

/**
 * Resizes text horizontally until it fits within its container.
 */
function resizeTextHorizontally(el: HTMLElement): void {
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
  }, 0);
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
  props.copyright =
      card.copyright || `™ & © ${new Date().getFullYear()} Wizards of the Coast`;

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
  props.background = `url(assets/img/${folder}/${props.is_vehicle ? "Vehicle" : props.boxes_colors}.webp)`;

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
  props.frame = `url(assets/img/${folder}/${frameColor}.webp)`;

  // Boxes and mid_boxes images.
  folder = props.is_planeswalker
      ? "planeswalker_boxes"
      : props.is_mdfc || props.is_transform
          ? currentFace === 0 ? "mdfc_boxes" : "mdfc_back_boxes"
          : props.extended_art && !props.is_class
              ? "extended_boxes"
              : "boxes";
  props.boxes = `url(assets/img/${folder}/${props.boxes_colors}.webp)`;
  props.mid_boxes = props.extended_art && !props.is_planeswalker && !props.is_class
      ? `url(assets/img/extended_boxes/${props.boxes_colors}.webp)`
      : props.boxes;

  // Legendary crown.
  const crownFolder = props.extended_art ? "extended_legendary_crowns" : "legendary_crowns";
  props.legendary_crown = `url(assets/img/${crownFolder}/${props.boxes_colors}.webp)`;

  // Power/Toughness box.
  const ptFolder = (props.is_mdfc || props.is_transform) && currentFace === 1 ? "transform_back_pt_boxes" : "pt_boxes";
  props.pt_box = `url(assets/img/${ptFolder}/${props.is_vehicle ? "Vehicle" : props.boxes_colors}.webp)`;

  // Saga text box.
  props.saga_text_box = `url(assets/img/saga_textboxes/${props.boxes_colors}.webp)`;

  // MDFC icon and hint.
  props.mdfc_icon = `url(assets/img/mdfc${currentFace === 0 ? "" : "_back"}_icons/${props.boxes_colors === "Land" ? props.colors : props.boxes_colors}.webp)`;
  let hintColors = props.colors;
  if (hintColors.length > 1 && hintColors.length < 5) hintColors = "Gold";
  props.mdfc_hint = `url(assets/img/mdfc${currentFace === 0 ? "" : "_back"}_hints/${hintColors}.webp)`;
  props.mdfc_hint_color = currentFace === 0 ? "white" : "black";

  // Transform icon.
  props.transform_icon = `url(assets/img/transform${currentFace === 0 ? "" : "_back"}_icons/${(card.frameEffects && card.frameEffects[0]) ? card.frameEffects[0] : "sunmoondfc"}.webp)`;

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
    props.color_indicator = `assets/img/color_indicators/${sorted}.webp`;
  } else {
    props.color_indicator = "";
  }

  // Illustration handling – choose art crop if available.
  // const artCard = props.is_adventure ? card : props.card_face;
  const artCard = props.card_face;

  const artUrl = props.card_face.imageUrl;
  /*
  let artUrl = "";
  if (artCard.image_uris && artCard.image_uris.art_crop && artCard.image_uris.art_crop.trim() !== "") {
    artUrl = artCard.image_uris.art_crop;
  } else {
    artUrl = "assets/img/placeholder-art.webp";
  }
  */

  props.illustration = `url("${artUrl}")`;
  props.illustration_scale = artCard.illustration_scale || 1;
  if (artCard.illustration_position) {
    props.illustration_position = {
      x: artCard.illustration_position.x + "mm",
      y: artCard.illustration_position.y + "mm"
    };
  } else {
    props.illustration_position = { x: "0mm", y: "0mm" };
  }

  // Set icon URI (using the Sets helper defined in helpers.ts).

  // props.set_icon_uri = setsHelper.getSetIconUri(card);
  props.set_icon_uri = setsHelper.getRandomSetIcon(card);

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
 * Sets CSS custom properties on the card element based on computed props.
 */
function setCardStyles(cardEl: HTMLElement, props: CardProps, scale: number, renderMargin: number): void {
  cardEl.style.setProperty("--bg-image", props.background);
  cardEl.style.setProperty("--frame-image", props.frame);
  cardEl.style.setProperty("--boxes-image", props.boxes);
  cardEl.style.setProperty("--mid-boxes-image", props.mid_boxes);
  cardEl.style.setProperty("--top-line-color", props.top_line_color);
  cardEl.style.setProperty("--mid-line-color", props.mid_line_color);
  cardEl.style.setProperty("--pt-box-image", props.pt_box);
  cardEl.style.setProperty("--pt-box-color", props.pt_box_color);
  cardEl.style.setProperty("--saga-text-box-image", props.saga_text_box);
  cardEl.style.setProperty("--mdfc-icon-image", props.mdfc_icon);
  cardEl.style.setProperty("--mdfc-hint-image", props.mdfc_hint);
  cardEl.style.setProperty("--mdfc-hint-color", props.mdfc_hint_color);
  cardEl.style.setProperty("--transform-icon-image", props.transform_icon);
  cardEl.style.setProperty("--renderMargin", renderMargin.toString());
  cardEl.style.setProperty("--scale", scale.toString());
  cardEl.style.setProperty("--frame-colors-left", props.archive_frame_colors.left.primary);
  cardEl.style.setProperty("--frame-colors-right", props.archive_frame_colors.right.primary);
  cardEl.style.setProperty("--illustration-image", props.illustration);
  cardEl.style.setProperty("--illustration-position-x", props.illustration_position.x);
  cardEl.style.setProperty("--illustration-position-y", props.illustration_position.y);
  cardEl.style.setProperty("--illustration-scale", props.illustration_scale.toString());
}

/**
 * Returns inner HTML for a planeswalker card.
 */
function buildPlaneswalkerCardHTML(props: CardProps, card: MagicCard): string {
  return `
    <div class="inner-background"></div>
    <div class="illustration behind-textbox"></div>
    <div class="planeswalker-oracle-bg"></div>
    <div class="inner-frame"></div>
    <div class="legendary-crown" style="display: none;"></div>
    <div class="top-line">
      <span class="card-name">${props.card_face.name}</span>
      <div class="mana-cost">
        ${props.mana_cost.map(uri => `<img src="${uri}" class="card-ms">`).join("")}
      </div>
    </div>
    <div class="mid-line">
      ${props.card_face.color_indicator ? `<img class="color-indicator" src="${props.color_indicator}">` : ""}
      <div class="type-line">${props.card_face.typeLine}</div>
      ${props.set_icon_uri ? `<div class="set-icon-container"><img class="set-icon" src="${props.set_icon_uri}"></div>` : ""}
    </div>
    <div class="oracle planeswalker-oracle" style="font-size: 6.48pt;">
      ${props.planeswalker_abilities ? props.planeswalker_abilities.map(ability => {
    if (ability.cost !== null) {
      return `<div class="planeswalker-ability planeswalker-ability-with-cost">
                    <div class="planeswalker-ability-cost ${ability.cost > 0 ? "planeswalker-ability-cost-plus" : "planeswalker-ability-cost-minus"}">
                      ${ability.cost > 0 ? "+" + ability.cost : ability.cost}
                    </div>
                    <div class="planeswalker-ability-text">${ability.html}</div>
                  </div>`;
    } else {
      return `<div class="planeswalker-ability">
                    <div class="planeswalker-ability-text">${ability.html}</div>
                  </div>`;
    }
  }).join("") : ""}
    </div>
    <div class="loyalty">${props.card_face.loyalty}</div>
    <div class="footer">
      <div class="footer-left">
        <div class="collector-number">${card.collectorNumber}</div>
        <div>
          ${card.set ? `<span class="set">${card.set.toUpperCase()}</span>` : ""}
          ${card.set && card.lang ? "&nbsp;•&nbsp;" : ""}
          ${card.lang ? `<span class="language">${card.lang.toUpperCase()}</span>` : ""}
          ${card.artist ? '<span class="artist-icon"> a </span>' : ""}
          ${card.artist ? `<span class="artist-name">${card.artist}</span>` : ""}
        </div>
      </div>
      <div class="footer-right">
        <div>&nbsp;</div>
        <div class="copyright">${card.copyright || `™ & © ${new Date().getFullYear()} Wizards of the Coast`}</div>
      </div>
    </div>
  `;
}

/**
 * Returns inner HTML for a saga card.
 */
function buildSagaCardHTML(props: CardProps, card: MagicCard): string {
  // Extract saga steps based on oracle text format "I — Text", "II — Text", etc.
  const sagaSteps: { number: string; text: string }[] = [];
  if (props.card_face.oracleText) {
    const lines = props.card_face.oracleText.split('\n');
    for (const line of lines) {
      const match = line.match(/^([IVX]+) — (.+)$/);
      if (match) {
        const stepNumber = match[1].trim();
        const stepText = parseOracle(match[2].trim());
        sagaSteps.push({ number: stepNumber, text: stepText });
      }
    }
  }

  return `
    <div class="inner-background"></div>
    <div class="illustration"></div>
    <div class="inner-frame"></div>
    <div class="legendary-crown" style="${props.has_legendary_crown ? '' : 'display: none;'}"></div>
    <div class="top-line">
      <span class="card-name">${props.card_face.name}</span>
      <div class="mana-cost">
        ${props.mana_cost.map(uri => `<img src="${uri}" class="card-ms">`).join("")}
      </div>
    </div>
    <div class="oracle saga-oracle">
      <div class="saga-reminder"><i>
(As this Saga enters and after your draw step, add a lore counter. Sacrifice after III.)</i></div>
      <div class="saga-frame"></div>
      <div class="saga-steps">
        ${sagaSteps.map(step => `
          <div class="saga-step">
            <div class="saga-step-number">
              <img src="assets/img/saga/${step.number}.webp">
            </div>
            <div>${step.text}</div>
          </div>
        `).join('')}
      </div>
    </div>
    <div class="mid-line">
      ${props.card_face.color_indicator ? `<img class="color-indicator" src="${props.color_indicator}">` : ""}
      <div class="type-line">${props.card_face.typeLine}</div>
      ${props.set_icon_uri ? `<div class="set-icon-container"><img class="set-icon" src="${props.set_icon_uri}"></div>` : ""}
    </div>
    <div class="footer">
      <div class="footer-left">
        <div class="collector-number">${card.collectorNumber}</div>
        <div>
          ${card.set ? `<span class="set">${card.set.toUpperCase()}</span>` : ""}
          ${card.set && card.lang ? "&nbsp;•&nbsp;" : ""}
          ${card.lang ? `<span class="language">${card.lang.toUpperCase()}</span>` : ""}
          ${card.artist ? '<span class="artist-icon"> a </span>' : ""}
          ${card.artist ? `<span class="artist-name">${card.artist}</span>` : ""}
        </div>
      </div>
      <div class="footer-right">
        <div>&nbsp;</div>
        <div class="copyright">${card.copyright || `™ & © ${new Date().getFullYear()} Wizards of the Coast`}</div>
      </div>
    </div>
  `;
}

/**
 * React component for rendering a default (non-planeswalker) card.
 */
interface DefaultCardProps {
  cardProps: CardProps;
  card: MagicCard;
}

/**
 * Custom hook for text resizing functionality
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
    }, 0);
  };

  const resizeOracleText = (oracle: HTMLElement, needsSpace: boolean) => {
    if (!oracle) return;
    // Start with a reasonable font size
    let fontSize = 1.0;
    oracle.style.fontSize = `${fontSize}em`;

    // Get container dimensions
    const boxHeight = oracle.clientHeight;
    const boxWidth = oracle.clientWidth;

    // Reduce font size until content fits
    while ((oracle.scrollHeight > boxHeight || oracle.scrollWidth > boxWidth) && fontSize > 0.5) {
      fontSize -= 0.05;
      oracle.style.fontSize = `${fontSize}em`;
    }

    // If we need space for P/T box, reduce slightly more
    if (needsSpace && oracle.scrollHeight > boxHeight * 0.9) {
      fontSize *= 0.95;
      oracle.style.fontSize = `${fontSize}em`;
    }
  };

  useEffect(() => {
    // Setup resize observers and initial resizing
    const observers: (ResizeObserver | MutationObserver)[] = [];

    // Resize card name
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

    // Resize type line
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

    // Handle oracle text resizing
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

    // Cleanup function
    return () => {
      observers.forEach(observer => observer.disconnect());
    };
  }, [cardProps]);

  return { cardNameRef, typeLineRef, oracleRef };
}

function DefaultCard({ cardProps, card }: DefaultCardProps): React.ReactElement {
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
          {cardProps.mana_cost.map((uri, index) => (
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
        <Image preview={true} loading="lazy" height={"100%"} width={"100%"} src={getImageUrl(cardProps.illustration)} />
      </div>
      <div ref={oracleRef} className="oracle normal-oracle">
        {cardProps.oracle_lines.map((line, index) => (
          <div key={index} className="oracle-inner" dangerouslySetInnerHTML={{ __html: line }} />
        ))}
        {cardProps.card_face.flavorText && (
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
            {card.copyright || `™ & © ${new Date().getFullYear()} Wizards of the Coast`}
          </div>
        </div>
      </div>
    </>
  );
}

function RenderCard(card: MagicCard, scale: number = 1, renderMargin: number = 1): HTMLElement {
  const currentFace = 0;
  const props = computeCardProps(card, currentFace);
  const cardEl = document.createElement("div");
  cardEl.className = "mtg-card";

  // Add special classes for planeswalker, saga, and legendary cards.
  if (props.is_planeswalker) {
    cardEl.classList.add("planeswalker");
    if (props.is_large_planeswalker) {
      cardEl.classList.add("planeswalker-large");
    }
  }
  if (props.is_saga) {
    cardEl.classList.add("saga");
  }
  if (props.is_legendary) {
    cardEl.classList.add("legendary");
  }

  setCardStyles(cardEl, props, scale, renderMargin);

  // Build inner HTML based on card type
  if (props.is_planeswalker) {
    cardEl.innerHTML = buildPlaneswalkerCardHTML(props, card);
    // Set up text resizing for planeswalker cards
    setupTextResizing(cardEl, props);
  } else if (props.is_saga) {
    cardEl.innerHTML = buildSagaCardHTML(props, card);
    // Set up text resizing for saga cards
    setupTextResizing(cardEl, props);
  } else {
    // Use React component for default cards
    const reactContainer = document.createElement('div');
    cardEl.appendChild(reactContainer);
    const root = createRoot(reactContainer);
    root.render(React.createElement(DefaultCard, { cardProps: props, card: card }));
  }

  // Set illustration background
  /*
  const illustrationDiv = cardEl.querySelector(".illustration, .illustration.behind-textbox") as HTMLElement;
  if (illustrationDiv) {
    illustrationDiv.style.backgroundImage = props.illustration;
    illustrationDiv.style.backgroundSize = `calc(${props.illustration_scale} * 100%)`;
    illustrationDiv.style.backgroundPosition = `${props.illustration_position.x} ${props.illustration_position.y}`;
    illustrationDiv.style.backgroundRepeat = "no-repeat";
  }
  */

  return cardEl;
}

export { RenderCard, DefaultCard };
export type { CardProps, DefaultCardProps };
export default RenderCard;
