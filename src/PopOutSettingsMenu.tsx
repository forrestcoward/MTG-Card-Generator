import React, { useContext } from 'react';
import styled from '@emotion/styled';
import { CSSTransition } from 'react-transition-group';
import { SettingGroup } from './MTGCardGenerator';
import { Radio, RadioChangeEvent, Switch } from 'antd';
import { CheckOutlined, CloseOutlined } from '@ant-design/icons';
import { UserContext } from './UserContext';

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

const SettingDescription = styled.p`
  margin-top: 10px;
  margin-left: 10px;
  margin-right: 10px;
`;

interface PopOutSettingsMenuProps {
  isOpen: boolean;
  onClose: () => void;
  settings: SettingGroup[];
  onModelSettingsChange: (setting: string, newValue: boolean) => void;
}

const PopOutSettingsMenu: React.FC<PopOutSettingsMenuProps> = ({ isOpen, onClose, settings, onModelSettingsChange}) => {
  
  const userContext = useContext(UserContext);
  if (!userContext) {
    throw new Error('UserContext not found');
  }
  const { openAIAPIKey, setOpenAIAPIKey } = userContext;

  const handleRadioChanged = (event: RadioChangeEvent, id: string) => {
    onModelSettingsChange(id, event.target.checked);
  };
  const handleSwitchChanged = (event: boolean, id: string) => {
    onModelSettingsChange(id, event);
  };
  const handleOpenAIKeyChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setOpenAIAPIKey(event.target.value);
  }
  return (
    <>
      <CSSTransition in={isOpen} timeout={300} classNames="pop-out-settings-menu" unmountOnExit>
        <MenuContainer>
          <div className="settingsMenuContainer">
            <h1>Settings</h1>
            <hr/>
            {settings.map((settingGroup) => (
              <React.Fragment key={`setting-group-${settingGroup.name}`}>
                <h2>{settingGroup.name}</h2>
                { settingGroup.type == "radio" &&
                <Radio.Group defaultValue={settingGroup.settings.find(setting => setting.value)?.name} buttonStyle="solid">
                  {settingGroup.settings.map((setting) => (
                    <Radio.Button key={`setting-${setting.id}`} value={setting.name} onChange={(event) => handleRadioChanged(event, setting.name)}>
                      {setting.name}
                    </Radio.Button>
                  ))}
                </Radio.Group>
                }
                { /* Show the setting description if chosen for radio options. */ }
                {settingGroup.type == "radio" && settingGroup.settings.map((setting) => (
                  <React.Fragment key={`setting-radio-description-${setting.id}`}>
                    {setting.value && <SettingDescription>{setting.description}</SettingDescription>}
                  </React.Fragment>
                ))}
                { settingGroup.type == "switch" &&
                  <div>
                    { settingGroup.settings.map((setting) => (
                      <div key={`setting-${setting.id}`} style={{marginBottom:20}}>
                        <Switch checked={setting.value} onChange={(event) => { handleSwitchChanged(event, setting.name) }} style={{marginLeft:5, marginRight:15, transform: "scale(1.2)"}} 
                        checkedChildren={<CheckOutlined />} unCheckedChildren={<CloseOutlined />} size='default' />
                        <b style={{fontSize:"large"}}>{setting.name}</b>
                        <SettingDescription>
                          {setting.description}
                        </SettingDescription>
                      </div>
                    ))}
                  </div>
                }
              </React.Fragment>
            ))}
            <h2>OpenAI API Key</h2>
            <div> 
              Your OpenAI API key. Create or view your keys at <a href="https://beta.openai.com/account/api-keys">OpenAI API Keys</a>. 
              <b>This website will never store your key. It is only sent to OpenAI to generate card text and images.</b>
            </div>
            <label>
            <input type="password" className="userOpenAIKeyInput" onChange={handleOpenAIKeyChange} value={openAIAPIKey} />
            </label>
          </div>
        </MenuContainer>
      </CSSTransition>
      {isOpen && <Overlay onClick={onClose} />}
    </>
  );
};

export default PopOutSettingsMenu;