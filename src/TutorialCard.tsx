import { BasicCard, MagicCard } from "./Card";
// @ts-ignore
import tutorialCardWizardImage from './card-backgrounds/wizard.png'

const _tutorialCard:BasicCard = {
  name: "The M.T.G. Card Creator",
  manaCost: "{6}",
  typeLine: "Legendary Creature — Wizard",
  type: "Artifact",
  oracleText: "{T}: Enter a prompt above and hit \"Generate!\" to generate a unique Magic: The Gathering card into my spell book.",
  text: "",
  colorIdentity: "Colorless",
  pt: "6/6",
  power: 6,
  toughness: 6,
  flavorText: "\"Even the most intelligent of designers will never hope to match the execution and creativity of modern machines. I respect them only as much as they have paved the way, but we will not be looking backwards.\"\n - The Creator",
  rarity: "Mythic",
  imageUrl: tutorialCardWizardImage,
  temporaryImageUrl: '',
  userPrompt: "Enter a prompt above to start generating!",
  explanation: "",
  funnyExplanation: "",
  id: "-1",
  alternativeImageUrls: [],
}

export const TutorialCard:MagicCard = new MagicCard(_tutorialCard);
