import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';

import { Layout } from './App';
import 'virtual:uno.css';
import '@unocss/reset/tailwind.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Layout dataPromise={import('./data')} />
  </StrictMode>,
);
