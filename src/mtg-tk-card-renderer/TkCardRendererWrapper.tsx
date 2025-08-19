import React, { useEffect, useRef } from 'react';
import './style.css';
import { RenderCard } from './RenderCard';
import { MagicCard } from '../models/MagicCard';

interface TKCardCardRendererWrapperProps {
  card: MagicCard;
  scale?: number;
  renderMargin?: number;
  className?: string;
  showFlavorText?: boolean;
}

/**
 * React wrapper around the original createMTGCard function (from script.js).
 * It mounts the produced HTMLElement into a container div so the original
 * HTML/CSS/JS behavior is preserved while providing a React component API.
 */
export const TKCardCardRendererWrapper : React.FC<TKCardCardRendererWrapperProps> = ({ card, scale = 1, renderMargin = 1, className, showFlavorText = true }) => {
  const containerRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    // Clear previous content
    container.innerHTML = '';

    // Create a modified card object with flavor text visibility info
    const modifiedCard = MagicCard.clone(card, true);
    // Store the showFlavorText state in a temporary property that RenderCard can access
    (modifiedCard.cardFaces[0] as any).tempShowFlavorText = showFlavorText;

    const el = RenderCard(modifiedCard, scale, renderMargin);
    if (el instanceof HTMLElement) {
      // Ensure the top-level wrapper keeps any caller-provided className
      if (className) el.classList.add(className);
      container.appendChild(el);
    } else {
      const err = document.createElement('div');
      err.style.color = 'red';
      err.textContent = 'createMTGCard did not return an HTMLElement.';
      container.appendChild(err);
    }


    // No cleanup necessary; the DOM node is replaced on next render via innerHTML = ''.
  }, [card, scale, renderMargin, className, showFlavorText]);

  return <div ref={containerRef} />;
};

export default TKCardCardRendererWrapper;
