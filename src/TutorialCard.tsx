import { BasicCard, MagicCard } from "./Card";
// @ts-ignore
import tutorialCardWizardImage from './card-backgrounds/wizard.png'

const _tutorialCard:BasicCard = {
  name: "The Magic: The Gathering Card Creator",
  manaCost: "{6}",
  typeLine: "Legendary Creature — Artificer God",
  type: "Artifact",
  rawOracleText: "Haste, Hexproof\n{T}: Enter a prompt above and hit \"Generate!\" to generate a unique Magic: The Gathering card into my spell book",
  text: "",
  modifiedOracleText: "",
  colorIdentity: "Colorless",
  pt: "6/6",
  power: 6,
  toughness: 6,
  flavorText: "\"I recall the huge design teams employed to devise even the simplest cards. Even the most intelligent of designers will never hope to match the execution and creativity of modern machines. I respect them only as much as they have paved the way, but we will not be looking backwards.\"\n - The Creator",
  rarity: "Mythic",
  imageUrl: tutorialCardWizardImage,
}

export const TutorialCard:MagicCard = new MagicCard(_tutorialCard);
