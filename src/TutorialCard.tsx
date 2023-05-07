import { BasicCard, MagicCard } from "./Card";
// @ts-ignore
import tutorialCardWizardImage from './card-backgrounds/wizard.png'

const _tutorialCard:BasicCard = {
  name: "The M.T.G. Card Creator",
  manaCost: "{6}",
  typeLine: "Legendary Creature — Wizard",
  type: "Artifact",
  rawOracleText: "Haste, Hexproof\n{T}: Enter a prompt above and hit \"Generate!\" to generate a unique Magic: The Gathering card into my spell book.\n{T}: Click my holofoil to edit this card.",
  text: "",
  modifiedOracleText: "",
  colorIdentity: "Colorless",
  pt: "6/6",
  power: 6,
  toughness: 6,
  flavorText: "\"Even the most intelligent of designers will never hope to match the execution and creativity of modern machines. I respect them only as much as they have paved the way, but we will not be looking backwards.\"\n - The Creator",
  rarity: "Mythic",
  imageUrl: tutorialCardWizardImage,
  explaination: "The M.T.G. Card Creator is the most powerful creature printed by an A.I. of all time.",
  funnyExplaination: "The researchers were not surprised even A.I. exhibits power creep."
}

var _tutorialMagicCard = new MagicCard(_tutorialCard);
_tutorialMagicCard.prompt = "Generate a card that generates other cards."
export const TutorialCard:MagicCard = _tutorialMagicCard;
