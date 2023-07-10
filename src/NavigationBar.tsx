import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react';
import { loginRequest } from './AuthConfig';
import React from 'react';
import "./nav-bar.css";

// @ts-ignore
import siteIcon from '../card-backgrounds/site-icon.png'
import { EventType } from '@azure/msal-browser';

export const NavigationBar = () => {
    const { instance, inProgress } = useMsal();
    let activeAccount;

    if (instance) {
        activeAccount = instance.getActiveAccount();
    }

    const handleLoginPopup = () => {
        instance
            .loginPopup({
                ...loginRequest
            })
            .catch((error) => console.log(error));
    };

    const handleLogoutPopup = () => {
        instance.logoutPopup();
    };

    // Do not know if this helps.
    instance.addEventCallback((event : any) => {
      if ((event.eventType === EventType.LOGIN_SUCCESS ||
           event.eventType === EventType.ACQUIRE_TOKEN_SUCCESS || 
           event.eventType === EventType.SSO_SILENT_SUCCESS) && event.payload.account) {
            instance.setActiveAccount(event.payload.account);
      }
  });

    // <img width={30} height={30} src={siteIcon}></img>
    return (
      <>
        <div className="navbar">
          <ul className="navbar-items">
            <li className="navbar-item navbar-brand">
              <div>
                MTG Card Generator
              </div>
            </li>
            <AuthenticatedTemplate>
              <li className="navbar-item">
                <div>
                  Welcome, {activeAccount && activeAccount.name ? activeAccount.name : 'Unknown'}!
                </div>
              </li>
              <li className="navbar-item">
                <div>
                  <a href="/">Generate</a>
                </div>
              </li>
              <li className="navbar-item">
                <div>
                  <a href="/MyCards">My Cards</a>
                </div>
              </li>
              <li className="navbar-item">
                <div>
                  <button className="loginButton" onClick={handleLogoutPopup}>
                    Logout
                  </button>
                </div>
              </li>
            </AuthenticatedTemplate>
            <UnauthenticatedTemplate>
              <li className="navbar-item">
                <div>
                  <button className="loginButton" onClick={handleLoginPopup}>
                          Login
                  </button>
                </div>
              </li>
            </UnauthenticatedTemplate>
          </ul>
        </div>
      </>
    )
};