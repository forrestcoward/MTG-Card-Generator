import React from 'react';
import { createRoot } from 'react-dom/client';
import { MTGCardGenerator } from './MTGCardGenerator';
import { EventType, PublicClientApplication } from '@azure/msal-browser';
import { msalConfig } from './AuthConfig';
import { MsalProvider } from '@azure/msal-react';
import { UserProvider } from './UserContext';
import { NavigationBar } from './NavigationBar';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { MyCards } from './MyCards';
import { CardRating } from './CardRating';
import { ViewCard } from './ViewCard';

// MSAL should be instantiated outside of the component tree to prevent it from being re-instantiated on re-renders.
// For more, visit: https://github.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-react/docs/getting-started.md
export const msalInstance = new PublicClientApplication(msalConfig);

// Default to using the first account if no account is active on page load.
if (!msalInstance.getActiveAccount() && msalInstance.getAllAccounts().length > 0) {
    // Account selection logic is app dependent. Adjust as needed for different use cases.
    msalInstance.setActiveAccount(msalInstance.getAllAccounts()[0]);
}

msalInstance.addEventCallback((event : any) => {
    if ((event.eventType === EventType.LOGIN_SUCCESS ||
         event.eventType === EventType.ACQUIRE_TOKEN_SUCCESS || 
         event.eventType === EventType.SSO_SILENT_SUCCESS) && event.payload.account) {
        msalInstance.setActiveAccount(event.payload.account);
    } else if (event.eventType === EventType.LOGIN_FAILURE) {
        console.error("MSAL login failure: " + event);
    }
});

const rootNode = document.getElementById('root');
if (rootNode) {
  createRoot(rootNode)
    .render(
        <BrowserRouter>
          <MsalProvider instance={msalInstance}>
            <UserProvider>
              <NavigationBar />
              <Routes>
                <Route path="/" element={<MTGCardGenerator />} />
                <Route path="MyCards" element={<MyCards />} />
                <Route path="RateCards" element={<CardRating />} />
                <Route path="Card" element={<ViewCard />} />
              </Routes>
            </UserProvider>
          </MsalProvider>
        </BrowserRouter>
  );
}