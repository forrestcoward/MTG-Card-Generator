import React, { useState } from 'react';
import { Button, Image } from 'antd';
import { CloseOutlined, CaretRightOutlined, BulbOutlined } from '@ant-design/icons';
import { GenerateImageResponse, GenerateNewCardImage, GetImagePrompt } from './CallAPI';
import { msalInstance } from './Index';
import { LoadingSpinner } from './LoadingSpinner';
import { UserContext } from './UserContext';
import { MagicCard } from './models/MagicCard';
import { isMobileDevice } from './Utility';
import './mtg-card.css';

interface ImageEditorProps {
  card: MagicCard;
  isVisible: boolean;
  onClose: () => void;
  onImageUpdate: (imageUrls: GenerateImageResponse) => void;
  width?: number;
  availableImageUrls?: GenerateImageResponse[];
  onAvailableImageUrlsUpdate?: (urls: GenerateImageResponse[]) => void;
}

export const ImageEditor: React.FC<ImageEditorProps> = ({
  card,
  isVisible,
  onClose,
  onImageUpdate,
  width,
  availableImageUrls = [],
  onAvailableImageUrlsUpdate
}) => {
  const userContext = React.useContext(UserContext);
  const [imageEditorPrompt, setImageEditorPrompt] = useState("");
  const [imageEditorError, setImageEditorError] = useState("");
  const [isImageLoading, setIsImageLoading] = useState(false);

  const imageEditorWidth = width || (isMobileDevice() ? 300 : 400);
  const cardIsTutorialCard = card.databaseId === "-1";

  const handleImagePromptUpdate = (event: React.ChangeEvent<HTMLTextAreaElement>) => {
    setImageEditorPrompt(event.target.value);
  };

  const getImagePrompt = () => {
    setIsImageLoading(true);
    setImageEditorError("");
    GetImagePrompt(card, msalInstance).then(prompt => {
      setIsImageLoading(false);
      setImageEditorError("");
      setImageEditorPrompt(prompt);
    }).catch((error: Error) => {
      let msg = error.message;
      setIsImageLoading(false);
      setImageEditorError(msg);
    });
  };

  const generateNewImage = () => {
    setIsImageLoading(true);
    setImageEditorError("");

    var openAIApiKey = userContext?.openAIAPIKey;
    if (!openAIApiKey) {
      openAIApiKey = "";
    }

    GenerateNewCardImage(imageEditorPrompt, card.databaseId, openAIApiKey, msalInstance).then(imageUrls => {
      const newAvailableImageUrls = [...availableImageUrls, imageUrls];
      onImageUpdate(imageUrls);
      if (onAvailableImageUrlsUpdate) {
        onAvailableImageUrlsUpdate(newAvailableImageUrls);
      }
      setIsImageLoading(false);
      setImageEditorError("");
      if (userContext?.refreshUser) userContext.refreshUser();
    }).catch((error: Error) => {
      let msg = error.message;
      if (msg.includes("This request has been blocked by our content filters") ||
        msg.includes("Your request was rejected as a result of our safety system")) {
        msg = "Your prompt was deemed inappropriate and blocked by automated safety systems."
      }
      setIsImageLoading(false);
      setImageEditorError(msg);
    });
  };

  if (!isVisible || cardIsTutorialCard) {
    return null;
  }

  return (
    <div 
      id={`card-image-flow-${card.id}`} 
      className="card-image-update-container" 
      style={{
        width: `${imageEditorWidth}px`, 
        display: "flex", 
        alignItems: "center", 
        flexDirection: "column"
      }}
    >
      <table style={{width: "100%"}}>
        <tbody>
          <tr>
            <td style={{display: "flex", position: "relative"}}>
              {/* Invisible spacer to maintain center alignment. */}
              <div style={{visibility: 'hidden', flex: 1}} />
              {/* Centered content. */}
              <p style={{
                color: "white", 
                margin: 0, 
                position: 'absolute', 
                top: '50%', 
                transform: 'translateY(-50%)', 
                width: '100%', 
                textAlign: 'center'
              }}>
                Image Prompt
              </p>
              {/* Right-aligned button. */}
              <div style={{flex: 1, display: 'flex', justifyContent: 'flex-end', margin: 8}}>
                <Button 
                  size="small" 
                  type="default" 
                  icon={<CloseOutlined />} 
                  onClick={onClose} 
                />
              </div>
            </td>
          </tr>
        </tbody>
      </table>
      {/* Image prompt text area. */}
      <table style={{marginLeft: 10, marginRight: 10, width: "-webkit-fill-available"}}>
        <tbody>
          <tr>
            <td>
              <textarea 
                disabled={isImageLoading} 
                maxLength={1500} 
                onChange={handleImagePromptUpdate} 
                value={imageEditorPrompt} 
                style={{height: 120, width: "100%", resize: "none"}} 
              />
            </td>
            <td>
            </td>
          </tr>
        </tbody>
      </table>
      {/* Generate buttons and loading icon. */}
      <table>
        <tbody>
          <tr>
            <td>
              <Button 
                className={isImageLoading ? "custom-disabled-btn" : ""} 
                size="middle"
                type="primary" 
                disabled={isImageLoading} 
                style={{ marginTop: 3}} 
                icon={<CaretRightOutlined />}
                onClick={generateNewImage}
              >
                Generate Image
              </Button>
              <Button 
                className={isImageLoading ? "custom-disabled-btn" : ""} 
                size="middle" 
                type="primary" 
                disabled={isImageLoading} 
                style={{marginLeft: 10, marginTop: 10}} 
                icon={<BulbOutlined />}
                onClick={getImagePrompt}
              >
                Generate Prompt
              </Button>
            </td>
          </tr>
          {isImageLoading && (
            <tr style={{display: "grid", justifyContent: "center", marginTop: 5}}>
              <td>
                <LoadingSpinner isLoading={isImageLoading} />
              </td>
            </tr>
          )}
        </tbody>
      </table>  
      {imageEditorError && (
        <div style={{marginLeft: 10}}>
          <h3 style={{ color: 'red' }}>{imageEditorError}</h3>
        </div>
      )}
      {/* Card image gallery. */}
      <div>
        <div style={{margin: 10, display: "flex", flexWrap: "wrap", justifyContent: "center"}}>
          {availableImageUrls.map((imageUrl, index) => (
            <div key={index} style={{margin: 3}} className="image-hover-animation">
              <Image 
                style={{border: "1px solid #cbcaca"}} 
                height={90} 
                width={90} 
                preview={false} 
                src={imageUrl.imageUrl ?? imageUrl.temporaryUrl} 
                onClick={() => onImageUpdate(imageUrl)} 
              />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
