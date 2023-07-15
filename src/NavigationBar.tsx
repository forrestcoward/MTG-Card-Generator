import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react';
import { loginRequest } from './AuthConfig';
import React from 'react';
import "./nav-bar.css";

// @ts-ignore
import siteIcon from '../card-backgrounds/site-icon.png'
import { Dropdown, Space } from 'antd';
import { CaretDownFilled, DatabaseOutlined, DownOutlined, HomeOutlined, LogoutOutlined, SmileOutlined } from '@ant-design/icons';

export const NavigationBar = () => {
    const { instance, inProgress } = useMsal();
    let activeAccount;

    if (instance) {
        activeAccount = instance.getActiveAccount();
    }

    const handleLogin = () => {
      let mobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)
      if (mobile) {
        // MSAL not working well on mobile for some reason.
        handleLoginRedirect();
      } else {
        handleLoginPopup();
      }
    }

    const handleLoginPopup = () => {
        instance
            .loginPopup({
                ...loginRequest
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
        instance.logoutPopup();
    };

    // <img width={30} height={30} src={siteIcon}></img>

    const items = [
      {
        key: '1',
        label: (
          <a href="/">
            Home
          </a>
        ),
        icon: <HomeOutlined />
      },
      {
        key: '2',
        label: (
          <a href="/MyCards">
            My Cards
          </a>
        ),
        icon: <DatabaseOutlined />,
      },
      {
        key: '3',
        label: (
          <a onClick={handleLogoutPopup}>
            Logout
          </a>
        ),
        icon: <LogoutOutlined />,
        disabled: false,
        danger: true,
      }
    ];

    return (
      <>
        <div className="navbar">
          <ul className="navbar-items">
            <li className="navbar-item navbar-brand">
              <div>
                <a style={{textDecoration:"none"}} href="/">MTG Card Generator</a>
              </div>
            </li>
            <AuthenticatedTemplate>
              <li>
                <Dropdown menu={{ items }}>
                  <a onClick={(e) => e.preventDefault()}>
                    <div className='navbar-item'>
                      <Space>
                        <div>
                          {activeAccount && activeAccount.name ? activeAccount.name : 'Unknown'}
                        </div>
                        <CaretDownFilled />
                      </Space>
                    </div>
                  </a>
                </Dropdown>
              </li>
            </AuthenticatedTemplate>
            <UnauthenticatedTemplate>
              <li className="navbar-item">
                <div>
                  <button className="loginButton" onClick={handleLogin}>
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