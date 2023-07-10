import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react';
import { loginRequest } from './AuthConfig';
import React from 'react';
import "./nav-bar.css";

// @ts-ignore
import siteIcon from '../card-backgrounds/site-icon.png'

export const NavigationBar = () => {
    const { instance, inProgress } = useMsal();
    let activeAccount;

    if (instance) {
        activeAccount = instance.getActiveAccount();
    }

    const handleLoginPopup = () => {
        instance
            .loginPopup({
                ...loginRequest,
                redirectUri: 'https://ambitious-meadow-0e2e9ce0f-development.eastus2.3.azurestaticapps.net/',
            })
            .catch((error) => console.log(error));
    };

    const handleLoginRedirect = () => {
        instance.loginRedirect(loginRequest).catch((error) => console.log(error));
    };

    const handleLogoutRedirect = () => {
        instance.logoutRedirect();
    };

    const handleLogoutPopup = () => {
        instance.logoutPopup({
            mainWindowRedirectUri: '/', // Redirects the top level app after logout.
        });
    };

    //const handleProfileEdit = () => {
    //    if (inProgress === InteractionStatus.None) {
    //        instance.acquireTokenRedirect(b2cPolicies.authorities.editProfile);
    //    }
    //};

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