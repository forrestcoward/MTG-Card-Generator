import React, { useState } from 'react';

interface ImageGenerationModalProps {
  showModal: boolean;
  onClose: () => void;
  onAccept: (imageUrl: string) => void;
}

const ImageGenerationModal: React.FC<ImageGenerationModalProps> = ({ showModal, onClose, onAccept }) => {
  const [imageGenerationPrompt, setImageGenerationPrompt] = useState<string>('');
  const [loadingImage, setLoadingImage] = useState<boolean>(false);
  const [generatedImage, setGeneratedImage] = useState<string | null>(null);

  const generateImage = () => {
    setLoadingImage(true);
    // Simulate an API call with setTimeout
    setTimeout(() => {
      // This is where you'd call the actual API to generate the image
      // For demonstration, let's assume the API returns a URL to the generated image
      const imageUrl: string = "path/to/generated/image.jpg";
      setGeneratedImage(imageUrl);
      setLoadingImage(false);
    }, 2000); // Simulate API call delay
  };

  const handleAccept = () => {
    if (generatedImage) {
      onAccept(generatedImage);
      handleClose(); // Close modal after accepting the image
    }
  };

  const handleClose = () => {
    setImageGenerationPrompt(''); // Reset prompt
    setGeneratedImage(null); // Reset generated image
    onClose(); // Invoke parent's onClose handler
  };

  if (!showModal) {
    return null;
  }

  return (
    <div className="image-generation-modal">
      <input
        type="text"
        value={imageGenerationPrompt}
        onChange={(e) => setImageGenerationPrompt(e.target.value)}
        placeholder="Enter prompt or use the suggested one"
      />
      <button onClick={generateImage} disabled={loadingImage}>
        OK
      </button>
      {loadingImage && <div>Loading...</div>}
      {generatedImage && (
        <>
          <img src={generatedImage} alt="Generated" style={{ width: '100%', height: 'auto' }} />
          <button onClick={handleAccept}>Accept</button>
          <button onClick={generateImage}>Regenerate</button>
        </>
      )}
      <button onClick={handleClose}>Close</button>
    </div>
  );
};

export default ImageGenerationModal;