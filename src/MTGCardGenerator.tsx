import React, { useContext, useEffect, useState } from 'react';
import { UserContext } from './UserContext';
import { CardDisplay  } from './Card';
import { GenerateMagicCardRequest } from './CallAPI';
import { isMobileDevice, setCardContainerSize } from './Utility';
import { TutorialCard } from './TutorialCard';
import PopOutSettingsMenu from './PopOutSettingsMenu';
import { useMsal } from '@azure/msal-react';
import { SettingsButton } from './SettingsButton';

import './mana.min.css';
import './mtg-card.css';
import './app.css';
import { LoadingSpinner } from './LoadingSpinner';

export interface SettingGroup {
  name: string;
  type: string;
  description: string;
  settings: Setting[];
}

export interface Setting {
  name: string;
  id: string;
  value: boolean;
  description: string;
  mobileDescription?: string;
  group?: string;
}

const defaultPrompt:string = 'is from the Dominaria plane.'
const defaultLanguageModel:string= 'gpt-4-turbo-preview'
const defaultImageModel:string = 'dall-e-2'

const modelSettings = [
  { name: 'GPT 3.5', id: 'gpt-3.5', value: true, description: 'GPT 3.5 is a fast and capable general purpose model.', group: 'model-settings'},
  { name: 'GPT 4', id: 'gpt-4-turbo-preview', value: false, description: 'GPT 4 is the most intelligent model to date, but is slower and more expensive.', group: 'model-settings' },
]

const modelSettingsGroup : SettingGroup = {
  name: 'Language Model',
  description: 'Language model to use.',
  type: 'radio',
  settings: modelSettings,
}

const imageSettings = [
  { name: 'Dalle-2', id: 'dall-e-2', value: true, description: 'Dalle-2 is a fast, cheap and great for prototyping.', group: 'image-settings' },
  { name: 'Dalle-3', id: 'dall-e-3', value: false, description: 'Dalle-3 produces the highest quality images, but is slower and more expensive.', group: 'image-settings' },
  { name: 'No Image', id: 'none', value: false, description: 'Do not generate an image (the Image Editor tool can be used afterwards).', group: 'image-settings' },
]

const imageSettingsGroup : SettingGroup = {
  name: 'Image Model',
  description: 'Image model to use.',
  type: 'radio',
  settings: imageSettings
}

const cardGenerationSettings = [
  { name: 'Generative Image Prompt', id: 'setting-generate-image-prompt', value: true, description: 'Use an intelligently generated image prompt to produce more unique and interesting art.', mobileDescription: 'Use a dynamically generated image prompt.' },
  { name: 'Extra Creative', id: 'setting-extra-creative', value: false, description: 'Mix and match mechanics and keywords in more interesting ways.', mobileDescription: 'Utilize a unique blend of mechanics and keywords.' },
  { name: 'Explain Yourself', id: 'setting-provide-explanation', value: false, description: 'Provide an explanation why the card was generated. The AI can be even be quite funny!', mobileDescription: 'Provide an explanation for the card.' },
]

const cardGenerationSettingsGroup : SettingGroup = {
  name: 'Card Generation',
  description: 'Customize card generation.',
  type: 'switch',
  settings: cardGenerationSettings,
}

export function MTGCardGenerator() {
  const [prompt, setPrompt] = useState('');
  const [response, setResponse] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [settings, setSettings] = useState([modelSettingsGroup, imageSettingsGroup, cardGenerationSettingsGroup]);
  const [cards, setCards] = useState([TutorialCard]);
  const [currentError, setCurrentError] = useState('');
  const [defaultCardWidth, setDefaultCardWidth] = useState(setCardContainerSize());
  const [showNewFeatureNotification] = useState(false);

  const userContext = useContext(UserContext);
  const { instance: msalInstance } = useMsal();

  useEffect(() => {
    const updateCardSize = () => {
      setDefaultCardWidth(setCardContainerSize());
    };
    window.addEventListener('resize', updateCardSize);
    return () => window.removeEventListener('resize', updateCardSize);
  }, []);

  function allSettings(): Setting[] {
    return settings.map(settingGroup => settingGroup.settings).flat();
  }

  function showCardExplanations(): boolean {
    return allSettings().find(setting => setting.id == 'setting-provide-explanation')?.value ?? true;
  }

  function extraCreative(): boolean {
    return allSettings().find(setting => setting.id == 'setting-extra-creative')?.value ?? true;
  }

  function highQualityImages(): boolean {
    return allSettings().find(setting => setting.id == 'setting-generate-image-prompt')?.value ?? false;
  }

  function getModelSetting(): string {
    let setting = allSettings().find(setting => setting.value == true && setting?.group == 'model-settings')
    return setting?.id ?? defaultLanguageModel
  }

  function getImageSetting(): string {
    let setting = allSettings().find(setting => setting.value == true && setting?.group == 'image-settings')
    return setting?.id ?? defaultImageModel
  }


  function handleSettingUpdate(updatedSetting: string, newValue: boolean) {
    // Flatten all settings.
    var newSettings = [...settings]
    var allSettings = newSettings.map(settingGroup => settingGroup.settings).flat();

    // Update the matching setting.
    let matchingSetting = allSettings.find(setting => setting.name == updatedSetting)
    if (matchingSetting && (matchingSetting.value != true || !matchingSetting.group)) {
      matchingSetting.value = newValue
    }

    // Set all other settings with the same group to false (if a group is specified).
    // For example, use this to allow only a single model setting.
    allSettings.forEach((setting) => {
      if (setting.name != updatedSetting && setting.group && setting.group == matchingSetting?.group) {
        setting.value = false
      }
    });

    setSettings(newSettings);
  };

  function handleSubmit() {
    setIsLoading(true)
    setCurrentError('')

    let model = getModelSetting()
    let imageModel = getImageSetting()

    const numCards = 1
    const openAIAPIKey = userContext?.openAIAPIKey
    GenerateMagicCardRequest(prompt, model, imageModel, showCardExplanations(), highQualityImages(), numCards, extraCreative(), openAIAPIKey!, msalInstance).then(generatedCards => {
      setResponse(JSON.stringify(generatedCards))
      setCards([...generatedCards, ...cards])
      setIsLoading(false)
    }).catch((error: Error) => {
      setCurrentError(error.message)
      setIsLoading(false)
    });
  }

  return (
    <div>
      <div className='outerContainer'>
        <div className='container' style={{position: 'relative'}}>
          { showNewFeatureNotification &&
          <div className='newFeatureNotification'>
            <b>
              <span style={{marginRight:5}}>
                New:
              </span>
            </b>
            <a className='pulsingLink' href='/RateCards'>
              { !isMobileDevice() ? "Rate Other User's Cards!" : 'Rate Cards!' } 
            </a>
          </div>
          }
          <p>Generate a Magic: The Gathering card...</p>
          <label>
            <input type='text' className='userInputPrompt' placeholder={defaultPrompt} onChange={(event) => {setPrompt(event.target.value)}} value={prompt} />
          </label>
          <p></p>
          <table>
            <tbody>
              <tr>
                <td>
                  <button className='generateButton' type='submit' onClick={() => handleSubmit()} disabled={isLoading}>Generate!</button>
                </td>
                <td>
                </td>
                <td>
                  <LoadingSpinner isLoading={isLoading} />
                </td>
                <td>
                  <SettingsButton onClick={() => setIsSettingsOpen(true)} />
                </td>
              </tr>
            </tbody>
          </table>
          <h2 style={{ color: 'red' }}>{currentError}</h2>
          <textarea value={response} readOnly={true} rows={30} cols={120} hidden={true} />
        </div>
        <div className='cardsContainer'>
        {
          cards.map(card => (
            <div className='cardContainer' key={`card-container-${card.id}`}>
              <CardDisplay 
                key={`card-display-${card.id}`} 
                card={card} 
                showCardMenu={true} 
                cardWidth={defaultCardWidth} 
                allowImagePreview={true} 
                allowEdits={true} 
                allowImageUpdate={true} />
            </div>
          ))
        }
        </div>
        <PopOutSettingsMenu 
          settings={settings}
          onModelSettingsChange={handleSettingUpdate}
          isOpen={isSettingsOpen} 
          onClose={() => setIsSettingsOpen(false)} />
      </div>
    </div>
  );
}