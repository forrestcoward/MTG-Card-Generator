import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react';
import { loginRequest } from './AuthConfig';
import React from 'react';
import "./nav-bar.css";
import { useUserAccount, UserContext } from './UserContext';
// @ts-ignore
import githubIcon from "./card-backgrounds/github-mark-white.png"
import { Dropdown, Space, Tooltip } from 'antd';
import { CaretDownFilled, DatabaseOutlined, FireOutlined, HomeOutlined, InstagramOutlined, LogoutOutlined, DollarCircleOutlined } from '@ant-design/icons';

export const NavigationBar = () => {
    const { instance } = useMsal();
    const userAccount = useUserAccount();
    const userContext = React.useContext(UserContext);
    const user = userContext?.user;

    // Helper function to format currency to nearest cent
    const formatCurrency = (amount: number): string => {
      return amount?.toFixed(2);
    };

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
    
    // Add spend info as a dropdown item if user is present
    const spendInfoDropdownItem = user ? {
      key: 'spend-info',
      label: (
        <Tooltip
          title="This is your remaining daily spend limit for generating cards. It resets every 24 hours. Using your own API key will not affect this limit."
          placement="left"
        >
          <span style={{ color: 'inherit' }}>
            <DollarCircleOutlined style={{ color: '#52c41a', marginRight: 4 }} />
            ${formatCurrency(user.remainingDailySpendLimit)} / ${formatCurrency(user.dailySpendLimit)}
          </span>
        </Tooltip>
      ),
      disabled: true,
      style: { cursor: 'default', color: 'inherit' }
    } : null;

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
      // Insert spend info below "Rate Cards"
      ...(spendInfoDropdownItem ? [spendInfoDropdownItem] : []),
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
                <a href="https://www.instagram.com/mtg.cardgenerator/" target="_blank" style={{paddingLeft:3}}>
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
                    <div className='navbar-item' style={{display: 'flex', alignItems: 'center', gap: 10}}>
                      <Space>
                        <div>
                          {user?.userName ? user.userName : (userAccount?.name ? userAccount.name : 'Unknown')}
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
