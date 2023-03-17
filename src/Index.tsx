import React from 'react';
import { createRoot } from 'react-dom/client';
import { MTGCardGenerator } from './App';
const rootNode = document.getElementById('app');
if (rootNode) {
  createRoot(rootNode)
    .render(<MTGCardGenerator />);
}