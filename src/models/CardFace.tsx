export interface CardFace {
  name: string
  manaCost: string
  typeLine: string
  type: string
  oracleText: string
  flavorText?: string
  power?: number
  toughness?: number
  pt?: string
  loyalty?: number

  colors?: string[];
  color_identity?: string[];
  color_indicator?: string[];

  artVariant?: string;
  layout?: string;
  mdfc_hint?: string;
  imageUrl: string
  temporaryImageUrl: string

  illustration_scale?: number;
  illustration_position?: {
    x: number;
    y: number;
  };
}
