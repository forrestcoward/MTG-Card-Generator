import React from 'react';
import { MagicCard } from './Card';

interface SharingButtonProps {
  card: MagicCard;
}

export class SharingButton extends React.Component<SharingButtonProps> {
  constructor(props: SharingButtonProps) {
    super(props);
  }

  handleShareClick = async () => {
    try {
      const url = await this.props.card.uploadCardBlob()
      if (url) {
        this.openMessengerShareDialog(url);
      } else {
        console.error('No image blob available.');
      }
    } catch (error) {
      console.error('Error in handling share:', error);
    }
  };

  openFacebookShareDialog = (imageUrl: string) => {
    const facebookShareUrl = `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(imageUrl)}`;
    window.open(facebookShareUrl, '_blank', 'width=600,height=400');
  };

  openMessengerShareDialog = (imageUrl: string) => {
    const messengerShareUrl = `https://www.facebook.com/dialog/send?link=${this.props.card.getCardUrl()}&app_id=1505672836947005&redirect_uri=https://www.mtgcardgenerator.com`;
    window.open(messengerShareUrl, '_blank', 'width=600,height=400');
  };

  render() {
    return (
      <button onClick={this.handleShareClick}>
        Share on Facebook
      </button>
    );
  }
}