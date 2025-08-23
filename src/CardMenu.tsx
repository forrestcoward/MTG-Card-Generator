import React from 'react';
import { Button, Dropdown, Tooltip } from 'antd';
import { 
  CaretDownFilled, 
  CloudDownloadOutlined, 
  CopyOutlined, 
  EditOutlined,
  EyeInvisibleOutlined,
  EyeOutlined,
  FileImageOutlined,
  InfoCircleOutlined, 
  QuestionCircleOutlined, 
  SaveFilled,
  ZoomInOutlined, 
  ZoomOutOutlined 
} from '@ant-design/icons';
import { copyTextToClipboard, getBaseUrl, isMobileDevice } from './Utility';
import { MagicCard } from './models/MagicCard';

export interface CardMenuProps {
  card: MagicCard;
  menuId?: string;
  // Menu display options.
  showCardMenu: boolean;
  showSizeAdjustmentButtons?: boolean;
  cardMenuIconFontSize?: string;
  // Size adjustment.
  increaseSizeAllowed: boolean;
  decreaseSizeAllowed: boolean;
  onIncreaseSize: (amount: number) => void;
  onDecreaseSize: (amount: number) => void;
  sizeAdjustmentIncrement?: number;
  // Download handlers.
  onDownloadCard: () => void;
  onDownloadArt: () => void;
  // Copy link options
  copyLinkUrl?: string; // If provided, uses this URL, otherwise constructs from card data.
  // Edit functionality (optional - for CardRenderer).
  editMode?: boolean;
  allowEdits?: boolean;
  onToggleEditMode?: () => void;
  // Image editor functionality (optional - for CardRenderer).
  allowImageUpdate?: boolean;
  onToggleImageEditor?: () => void;
  // Flavor text toggle functionality.
  flavorTextVisible?: boolean;
  onToggleFlavorText?: () => void;
}

export const CardMenu: React.FC<CardMenuProps> = ({
  card,
  showCardMenu,
  showSizeAdjustmentButtons = !isMobileDevice() || Math.max(document.documentElement.clientWidth || 0, window.innerWidth || 0) > 900,
  cardMenuIconFontSize = "40px",
  increaseSizeAllowed,
  decreaseSizeAllowed,
  onIncreaseSize,
  onDecreaseSize,
  sizeAdjustmentIncrement = 30,
  onDownloadCard,
  onDownloadArt,
  copyLinkUrl,
  editMode,
  allowEdits = false,
  onToggleEditMode,
  allowImageUpdate = false,
  onToggleImageEditor,
  flavorTextVisible = true,
  onToggleFlavorText,
  menuId
}) => {
  const optionsMenuItemStyle: React.CSSProperties = { marginLeft: 5 };
  const optionsMenuIconStyle: React.CSSProperties = { fontSize: 30 };
  
  const cardIsTutorialCard = card.databaseId === "-1";

  // Build menu items.
  const menuItems = [
    {
      key: '1',
      label: (
        <Tooltip placement="left">
          <CloudDownloadOutlined style={optionsMenuIconStyle} />
          <span style={optionsMenuItemStyle}>Download Card</span>
        </Tooltip>
      ),
      onClick: onDownloadCard
    },
    {
      key: '2',
      label: (
        <Tooltip placement="left">
          <CloudDownloadOutlined style={optionsMenuIconStyle} />
          <span style={optionsMenuItemStyle}>Download Art</span>
        </Tooltip>
      ),
      onClick: onDownloadArt
    }
  ];

  // Add edit functionality if allowed.
  if (allowEdits && onToggleEditMode) {
    const editMenuItem = editMode ? 
      {
        key: '3',
        label: (
          <Tooltip title="Save the card text" placement='left'>
            <SaveFilled style={optionsMenuIconStyle} />
            <span style={optionsMenuItemStyle}>Save</span>
          </Tooltip>
        ),
        onClick: onToggleEditMode
      }
      : 
      {
        key: '3',
        label: (
          <Tooltip title="Modify the card text" placement='left'>
            <EditOutlined style={optionsMenuIconStyle} />
            <span style={optionsMenuItemStyle}>Edit Text</span>
          </Tooltip>
        ),
        onClick: onToggleEditMode
      };
    
    menuItems.push(editMenuItem);
  }

  // Add copy link if card has database ID.
  if (card.databaseId) {
    const copyLinkMenuItem = {
      key: '4',
      label: (
        <div>
          <CopyOutlined style={optionsMenuIconStyle} />
          <span style={optionsMenuItemStyle}>Copy Link</span>
        </div>
      ),
      onClick: async () => {
        const url = copyLinkUrl || `${getBaseUrl()}/Card?id=${card.databaseId}`;
        await copyTextToClipboard(url);
      }
    };
    
    menuItems.push(copyLinkMenuItem);
  }

  // Add image editor if allowed.
  if (allowImageUpdate && onToggleImageEditor && !cardIsTutorialCard) {
    const updateImageItem = {
      key: '5',
      label: (
        <Tooltip title="Regenerate the card image" placement='left'>
          <FileImageOutlined style={optionsMenuIconStyle} />
          <span style={optionsMenuItemStyle}>Image Editor</span>
        </Tooltip>
      ),
      onClick: onToggleImageEditor
    };
    
    menuItems.push(updateImageItem);
  }

  // Check if card is planeswalker or saga (these shouldn't show flavor text options)
  const isPlaneswalker = !!(card.cardFaces[0].typeLine &&
    card.cardFaces[0].typeLine.toLowerCase().includes("planeswalker"));
  const isSaga = !!(card.cardFaces[0].layout === "saga" ||
    (card.cardFaces[0].typeLine && card.cardFaces[0].typeLine.includes("Saga")));

  if (card.cardFaces[0].flavorText && onToggleFlavorText && !isPlaneswalker && !isSaga) {
    const flavorTextItem = {
      key: '6',
      label: (
        <Tooltip title={flavorTextVisible ? "Hide flavor text" : "Show flavor text"} placement='left'>
          {flavorTextVisible ? <EyeInvisibleOutlined style={optionsMenuIconStyle} /> : <EyeOutlined style={optionsMenuIconStyle} />}
          <span style={optionsMenuItemStyle}>{flavorTextVisible ? "Hide Flavor" : "Show Flavor"}</span>
        </Tooltip>
      ),
      onClick: onToggleFlavorText
    };
    
    menuItems.push(flavorTextItem);
  }

  if (!showCardMenu) {
    return null;
  }

  const menuStyle: React.CSSProperties = {
    display: 'flex',
    justifyContent: 'right'
  };

  return (
    <div id={menuId} style={menuStyle}>
      <Tooltip title={<div><b>Prompt: </b>{card.userPrompt}</div>} placement="left">
        <QuestionCircleOutlined style={{fontSize: cardMenuIconFontSize, marginLeft: 3, marginTop: 3}} />
      </Tooltip>
      
      {card.explanation && (
        <Tooltip 
          title={
            <div>
              <b>About This Card</b>
              <br/>
              <br/>
              {card.explanation}
              <br/>
              <br/>
              {card.funnyExplanation}
            </div>
          } 
          overlayInnerStyle={{width: 320, backgroundColor: 'rgba(0, 0, 0, 0.93)'}} 
          placement="left"
        >
          <InfoCircleOutlined style={{fontSize: cardMenuIconFontSize, marginLeft: 3, marginTop: 3}} />
        </Tooltip>
      )}
      
      {showSizeAdjustmentButtons && (
        <Button type="text" size="middle" shape="circle" disabled={!increaseSizeAllowed}>
          <ZoomInOutlined 
            onClick={() => onIncreaseSize(sizeAdjustmentIncrement)} 
            style={{fontSize: cardMenuIconFontSize, marginRight: -4}} 
          />
        </Button>
      )}
      
      {showSizeAdjustmentButtons && (
        <Button type="text" size="middle" shape="circle" disabled={!decreaseSizeAllowed}>
          <ZoomOutOutlined 
            onClick={() => onDecreaseSize(sizeAdjustmentIncrement)} 
            style={{fontSize: cardMenuIconFontSize, marginRight: -7}} 
          />
        </Button>
      )}
      
      <Dropdown menu={{items: menuItems}}>
        <a className="ant-dropdown-link" onClick={e => e.preventDefault()}>
          <CaretDownFilled style={{fontSize: cardMenuIconFontSize}} />
        </a>
      </Dropdown>
    </div>
  );
};
