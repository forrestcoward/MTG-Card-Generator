import React from 'react';
import styled from '@emotion/styled';
import { CSSTransition } from 'react-transition-group';
import { Setting } from './App';

const MenuContainer = styled.div`
  position: fixed;
  top: 0;
  right: 0;
  height: 100%;
  background-color: white;
  z-index: 1001;
  width: 350px;
  transition: transform 0.3s ease-in-out;
  overflow-y: hidden;
  border-left: 3px solid black;
`;

const Overlay = styled.div`
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  z-index: 1000;
`;

interface PopOutSettingsMenuProps {
  isOpen: boolean;
  onClose: () => void;
  modelSettings: Setting[];
  onModelSettingsChange: (setting: string, newValue: boolean) => void;
}

const PopOutSettingsMenu: React.FC<PopOutSettingsMenuProps> = ({ 
  isOpen,
  onClose,
  modelSettings,
  onModelSettingsChange,
}) => {
  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>, id: string) => {
    onModelSettingsChange(id, event.target.checked);
  };
  return (
    <>
      <CSSTransition in={isOpen} timeout={300} classNames="pop-out-settings-menu" unmountOnExit>
        <MenuContainer>
          <div className="settingsMenuContainer">
            <h1>Settings</h1>
            <hr/>
            <h2>Model</h2>
            <div>
              Which langauge model to use when generating Magic cards.
            </div>
            <div>
              <table>
                <tbody>
                  {modelSettings.map((setting) => (
                    <React.Fragment key={`setting-${setting.id}`}>
                      <tr>
                        <td>
                          <h3>{setting.name}</h3>
                        </td>
                        <td  className="settingSlider">
                          <label className="switch">
                            <input
                              type="checkbox"
                              id={`${setting.name}`}
                              checked={setting.value}
                              onChange={(event) => handleInputChange(event, setting.name)}
                            />
                            <span className="slider round"></span>
                          </label>
                        </td>
                      </tr>
                      <tr>
                        <td>
                          <div className="settingsDescription">
                            {setting.description}
                          </div>
                        </td>
                      </tr>
                    </React.Fragment>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </MenuContainer>
      </CSSTransition>
      {isOpen && <Overlay onClick={onClose} />}
    </>
  );
};

export default PopOutSettingsMenu;