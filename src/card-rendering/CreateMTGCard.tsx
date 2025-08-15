import React, { useEffect, useRef } from 'react';
import './style.css';
import { createMTGCard } from './mtgCardRenderer';

declare global {
  interface Window {
    createMTGCard?: (card: any, scale?: number, renderMargin?: number) => HTMLElement;
  }
}

interface CreateMTGCardProps {
  card: any;
  scale?: number;
  renderMargin?: number;
  className?: string;
}

/**
 * React wrapper around the original createMTGCard function (from script.js).
 * It mounts the produced HTMLElement into a container div so the original
 * HTML/CSS/JS behavior is preserved while providing a React component API.
 */
export const CreateMTGCard: React.FC<CreateMTGCardProps> = ({ card, scale = 1, renderMargin = 1, className }) => {
  const containerRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    // Clear previous content
    container.innerHTML = '';

    try {
      const el = createMTGCard(card, scale, renderMargin);
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
    } catch (e) {
      const err = document.createElement('div');
      err.style.color = 'red';
      err.textContent = `Error rendering card: ${String(e)}`;
      container.appendChild(err);
    }

    // No cleanup necessary; the DOM node is replaced on next render via innerHTML = ''.
  }, [card, scale, renderMargin, className]);

  return <div ref={containerRef} />;
};

export default CreateMTGCard;
