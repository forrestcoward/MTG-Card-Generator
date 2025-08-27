import React, { useContext, useState } from 'react';
import styled from '@emotion/styled';
import { CSSTransition } from 'react-transition-group';
import { SettingGroup } from './MTGCardGenerator';
import { Radio, RadioChangeEvent, Switch } from 'antd';
import { CheckOutlined, CloseOutlined, DownOutlined, RightOutlined } from '@ant-design/icons';
import { UserContext } from './UserContext';
import { isMobileDevice } from './Utility';

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

const ModelInfo = styled.div`
  margin-left: 10px;
  margin-right: 10px;
  margin-bottom: 15px;
`;

const PricingInfo = styled.span`
  font-weight: 600;
  margin-left: 5px;
  color: #555;
`;

const DetailsToggle = styled.div`
  cursor: pointer;
  margin-left: 10px;
  margin-right: 10px;
  margin-top: 5px;
  color: #1890ff;
  font-weight: 500;
  display: flex;
  align-items: center;
`;

const DetailsContent = styled.div`
  margin-left: 20px;
  margin-right: 10px;
  margin-top: 5px;
  padding: 8px 12px;
  background-color: #f5f5f5;
  border-radius: 4px;
  border-left: 3px solid #1890ff;
`;

const DetailItem = styled.div`
  margin-bottom: 8px;
`;

interface PopOutSettingsMenuProps {
  isOpen: boolean;
  onClose: () => void;
  settings: SettingGroup[];
  onModelSettingsChange: (setting: string, newValue: boolean) => void;
}

interface DetailsSectionProps {
  setting: any;
}

// Component for the collapsible details section
const DetailsSection: React.FC<DetailsSectionProps> = ({ setting }) => {
  const [isExpanded, setIsExpanded] = useState(false);
  
  // Only render the toggle if there's something to show
  if (!setting.url && !setting.pricing && !setting.details) {
    return null;
  }
  
  return (
    <>
      <DetailsToggle onClick={() => setIsExpanded(!isExpanded)}>
        {isExpanded ? <DownOutlined style={{ marginRight: '5px' }} /> : <RightOutlined style={{ marginRight: '5px' }} />}
        {isExpanded ? 'Hide Details' : 'Details'}
      </DetailsToggle>
      
      {isExpanded && (
        <DetailsContent>
          {setting.details && (
            <DetailItem>{setting.details}</DetailItem>
          )}
          {setting.url && (
            <DetailItem>
              <a href={setting.url} target="_blank" rel="noopener noreferrer">Learn more about {setting.name}</a>
            </DetailItem>
          )}
          {setting.pricing && (
            <DetailItem>
              Cost: <PricingInfo>{setting.pricing}</PricingInfo>
            </DetailItem>
          )}
        </DetailsContent>
      )}
    </>
  );
};

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

  const isMobile = isMobileDevice();
  const isMobileDisplay = isMobile ? "none" : "inline"
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
                {settingGroup.type == "radio" && !isMobile && settingGroup.settings.map((setting) => (
                  <React.Fragment key={`setting-radio-description-${setting.id}`}>
                    {setting.value && (
                      <>
                        <SettingDescription>{setting.description}</SettingDescription>
                        <DetailsSection setting={setting} />
                      </>
                    )}
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
                          {isMobile ? setting.mobileDescription : setting.description}
                        </SettingDescription>
                        <DetailsSection setting={setting} />
                      </div>
                    ))}
                  </div>
                }
              </React.Fragment>
            ))}
            <h2>OpenAI API Key</h2>
            <div> 
              Your OpenAI API key. Create or view your keys at <a href="https://platform.openai.com/api-keys">OpenAI API Keys</a>. 
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
