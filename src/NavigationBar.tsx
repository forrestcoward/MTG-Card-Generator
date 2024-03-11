import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react';
import { loginRequest } from './AuthConfig';
import React from 'react';
import "./nav-bar.css";
import { useUserAccount } from './UserContext';
// @ts-ignore
import githubIcon from "./card-backgrounds/github-mark-white.png"
import { Dropdown, Space } from 'antd';
import { CaretDownFilled, DatabaseOutlined, FireOutlined, HomeOutlined, InstagramOutlined, LogoutOutlined } from '@ant-design/icons';

export const NavigationBar = () => {
    const { instance } = useMsal();
    const userAccount = useUserAccount();

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
          <a href="/RateCards">
            Rate Cards
          </a>
        ),
        icon: <FireOutlined />,
      },
      {
        key: '4',
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
              <div style={{display:'flex', padding:'none', textAlign:'center'}}>
                <a style={{textDecoration:"none", textAlign:'center'}} href="/">MTG Card Generator</a>
                <a href="https://www.instagram.com/mtg.cardgenerator/" target="_blank" style={{paddingLeft:6}}>
                  <InstagramOutlined />
                </a>
                <a href="https://github.com/forrestcoward/MTG-Card-Generator" target="_blank">
                  <img title="View source code on GitHub" src={githubIcon} width={15} height={15} style={{paddingLeft:7}}></img>
                </a>
              </div>
            </li>
            <AuthenticatedTemplate>
              <li>
                <Dropdown menu={{ items }}>
                  <a onClick={(e) => e.preventDefault()}>
                    <div className='navbar-item'>
                      <Space>
                        <div>
                          {userAccount?.name ? userAccount.name : 'Unknown'}
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