import { BasicCard, CardGenerationRecord, CardRating, MagicCard } from "./Card";
import { AuthenticationResult, IPublicClientApplication, InteractionRequiredAuthError, PublicClientApplication } from "@azure/msal-browser";
import { User } from "./User";

export async function RetrieveMsalToken(msal: IPublicClientApplication, scopes: string[]): Promise<AuthenticationResult | undefined> {
  await msal.initialize();
  const accounts = msal.getAllAccounts();
  const account = accounts.length > 0 ? accounts[0] : null;
  
  if (!account) {
    return undefined;
  }

  const accessTokenRequest = {
    scopes: scopes,
    account: account,
  };

  try {
    // Try silent token acquisition first
    return await msal.acquireTokenSilent(accessTokenRequest);
  } catch (error) {
    console.log("Silent token acquisition failed", error);
    if (error instanceof InteractionRequiredAuthError) {
      // Fallback to interaction when silent call fails
      try {
        return await msal.acquireTokenPopup(accessTokenRequest);
      } catch (popupError) {
        console.error("Error during popup authentication:", popupError);
        return undefined;
      }
    }
    console.error("Error retrieving MSAL token:", error);
    return undefined;
  }
}

const productionApiUrl = 'https://mtgcardgenerator.azurewebsites.net/api';
const developmentApiUrl  = 'https://mtgcardgenerator-dev.azurewebsites.net/api';
const developmentHostName = 'ambitious-meadow-0e2e9ce0f-development.eastus2.3.azurestaticapps.net';
const localApiUrl = 'http://localhost:7071/api';

function getApiUrl(apiName:string): string {
  var baseUrl = productionApiUrl;

  if (location.hostname === developmentHostName) {
    baseUrl = developmentApiUrl
  }

  if (location.hostname === "localhost") {
    baseUrl = localApiUrl;
  }

  return `${baseUrl}/${apiName}`;
}

// Call an API that returns a CardGenerationRecord.
async function GetCardAPICall(apiName:string, msal:IPublicClientApplication, params?:Record<string, string>): Promise<CardGenerationRecord> {
  let url = getApiUrl(apiName);
  var token = await RetrieveMsalToken(msal, ["https://mtgcardgenerator.onmicrosoft.com/api/generate.mtg.card"])

  let record:CardGenerationRecord
  await HttpGet(url, token, params)
    .then(data => {
      record = JSON.parse(data);
    })
    .catch(error => {
      console.error(`There was an error calling '${apiName}': `, error);
      throw error
    })

    return record!;
}

// Call an API that retruns a CardGenerationRecord[].
async function GetCardsAPICall(apiName:string, msal:IPublicClientApplication): Promise<CardGenerationRecord[]> {
  let url = getApiUrl(apiName);
  var token = await RetrieveMsalToken(msal, ["https://mtgcardgenerator.onmicrosoft.com/api/generate.mtg.card"])

  const params: Record<string, string> = { };
  let cardGenerationRecords:CardGenerationRecord[] = []
  await HttpGet(url, token, params)
    .then(data => {
      cardGenerationRecords = JSON.parse(data);
    })
    .catch(error => {
      console.error(`There was an error calling '${apiName}': `, error);
      throw error
    });

    return cardGenerationRecords;
}

export async function GetCardToRate(msal:IPublicClientApplication): Promise<CardGenerationRecord> {
  return GetCardAPICall('GetCardToRate', msal);
}

export async function GetCard(msal:IPublicClientApplication, cardId:string): Promise<CardGenerationRecord> {
  return GetCardAPICall('GetMagicCard', msal, { "cardId": cardId });
}

export async function RateCard(cardId:string, rating:number, msal:IPublicClientApplication): Promise<CardRating|undefined> {
  let url = getApiUrl('RateCard');
  var token = await RetrieveMsalToken(msal, ["https://mtgcardgenerator.onmicrosoft.com/api/generate.mtg.card"])

  const params: Record<string, string> = { "cardId": cardId, "rating": rating.toString()};
  let cardRating:CardRating | undefined = undefined
  await HttpGet(url, token, params)
    .then(data => {
      cardRating = JSON.parse(data);
    })
    .catch(error => {
      console.error('There was an error rating a card:', error);
      throw error
    });
    
    return cardRating;
}

export async function GetUserInfo(msal:IPublicClientApplication): Promise<User> {
  let url = getApiUrl('GetUser');
  var token = await RetrieveMsalToken(msal, ["https://mtgcardgenerator.onmicrosoft.com/api/generate.mtg.card"])

  const params: Record<string, string> = { };
  let user:User | undefined = undefined
  await HttpGet(url, token, params)
    .then(data => {
      user = JSON.parse(data);
    })
    .catch(error => {
      console.error('There was an error getting user information:', error);
      throw error
    });
    
    return user!;
}

export interface GenerateImageResponse {
  imageUrl: string,
  temporaryUrl: string,
}

export async function GenerateNewCardImage(imagePrompt: string, cardId: string, openAIApiKey:string, msal:PublicClientApplication): Promise<GenerateImageResponse> {
  let url = getApiUrl('GenerateNewCardImage');
  var token = await RetrieveMsalToken(msal, ["https://mtgcardgenerator.onmicrosoft.com/api/generate.mtg.card"])

  const params: Record<string, string> = {
    prompt: imagePrompt,
    cardId: cardId,
    openAIApiKey: openAIApiKey
   };

  let response:GenerateImageResponse = 
  {
    "imageUrl": "",
    "temporaryUrl": ""
  }

  await HttpGet(url, token, params)
    .then(imageUrls => {
      response = JSON.parse(imageUrls);
    })
    .catch(error => {
      console.error('There was an error generating an image:', error);
      throw error
    });
    
    return response;
}

export async function GetImagePrompt(card: MagicCard, msal:PublicClientApplication): Promise<string> {
  let url = getApiUrl('GetImagePrompt');
  var token = await RetrieveMsalToken(msal, ["https://mtgcardgenerator.onmicrosoft.com/api/generate.mtg.card"])

  var body = JSON.stringify(card);
  const params: Record<string, string> = { };

  let prompt = '';
  await MakeHttpCall("POST", url, body, params, token)
    .then(response => {
      prompt = JSON.parse(response).suggestedPrompt;
    })
    .catch(error => {
      console.error('There was an error generating an image prompt:', error);
      throw error
    });
    
    return prompt;
}


export async function UploadImageToAzure(msal:PublicClientApplication, blob:Blob, cardId:string): Promise<string> {
  var token = await RetrieveMsalToken(msal, ["https://mtgcardgenerator.onmicrosoft.com/api/generate.mtg.card"])
  let url = getApiUrl('UploadCardImage')
  var result = await HttpPost(url, blob, token, { "cardId": cardId })
  return JSON.parse(result).url
}

export async function GetTopCards(msal: IPublicClientApplication): Promise<CardGenerationRecord[]> {
  return GetCardsAPICall('GetTopCards', msal);
}

export async function GetUserMagicCards(msal: IPublicClientApplication): Promise<CardGenerationRecord[]> {
  return GetCardsAPICall('GetMagicCards', msal);
}

export interface CardGenerationCosts {
  userRemainingDailySpendLimit: number;
  cost: number;
  userIncurredCost: number;
  serverIncurredCost: number;
}

export interface CardGenerationResult {
  cards: MagicCard[];
  costs: CardGenerationCosts;
}

export async function GenerateMagicCardRequest(
  userPrompt: string,
  model: string,
  imageModel: string,
  includeExplanation: boolean,
  generateImagePrompt: boolean,
  numCards: number,
  extraCreative: boolean,
  openAIApiKey: string,
  msal: IPublicClientApplication
): Promise<CardGenerationResult> {
  let url = getApiUrl('GenerateMagicCard');
  var token = await RetrieveMsalToken(msal, ["https://mtgcardgenerator.onmicrosoft.com/api/generate.mtg.card"]);

  const params: Record<string, string> = {
    userPrompt: userPrompt,
    model: model,
    imageModel: imageModel,
    numCards: numCards.toString(),
    includeExplanation: includeExplanation.toString(),
    generateImagePrompt: generateImagePrompt.toString(),
    extraCreative: extraCreative.toString(),
    source: "MTGCardGeneratorWebsite"
  };

  if (openAIApiKey) {
    params.openAIApiKey = openAIApiKey;
  }

  try {
    // First request to start the orchestration
    const initialResponse = await HttpGet(url, token, params);
    let responseData = JSON.parse(initialResponse);

    // Check if we got an ID for querying the status
    if (responseData.id) {
      // Use the ID to poll the status until completion
      return await pollDurableFunctionStatus(responseData.id, token);
    } else {
      throw new Error("Unexpected response format from card generation API");
    }
  } catch (error) {
    console.error('There was an error generating card JSON:', error);
    throw error;
  }
}

// New function to poll durable function status
async function pollDurableFunctionStatus(instanceId: string, token: AuthenticationResult | undefined): Promise<CardGenerationResult> {
  const maxAttempts = 120; // Maximum polling attempts (4 minutes with 3-second intervals)
  const pollIntervalMs = 4000; // Polling interval in milliseconds
  const statusUrl = getApiUrl(`GetMagicCardGenerationStatus?instanceId=${instanceId}`);

  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    try {
      const statusResponse = await MakeHttpCall("GET", statusUrl, undefined, {}, token);
      const status = JSON.parse(statusResponse);

      // Check if the orchestration has completed.
      if (status.runtimeStatus === "Completed") {
        if (status.customStatus === "error") {
          // Handle custom error status
          throw new Error(status.output || "An error occurred during card generation.");
        }
        // Parse the output from the orchestration.
        const output = JSON.parse(status.output);
        return {
          cards: output.cards.map((card: any) => new MagicCard(card)),
          costs: output.costs || { 
            userRemainingDailySpendLimit: 0, 
            cost: 0, 
            userIncurredCost: 0, 
            serverIncurredCost: 0 
          }
        };
      } else if (status.runtimeStatus === "Failed") {
        // Handle failure.
        let errorMessage;
        try {
          const parsedOutput = JSON.parse(status.output);
          errorMessage = parsedOutput.error || "Card generation failed";
        } catch {
          errorMessage = status.output || "Card generation failed";
        }
        throw new Error(errorMessage);
      } else if (status.runtimeStatus === "Terminated" || status.runtimeStatus === "Canceled") {
        throw new Error("Card generation was canceled or terminated");
      }

      // Wait before polling again.
      await new Promise(resolve => setTimeout(resolve, pollIntervalMs));
    } catch (error) {
      console.error('Error polling durable function status:', error);
      throw error;
    }
  }

  throw new Error("Timed out waiting for card generation to complete");
}

async function HttpPost(url: string, blob: Blob, msalResult: AuthenticationResult | undefined, params?: Record<string, string>): Promise<any> {
  const formData = new FormData();
  formData.append('file', blob);
  return MakeHttpCall("POST", url, formData, params, msalResult)
}

async function HttpGet(url: string, msalResult: AuthenticationResult | undefined, params?: Record<string, string>): Promise<any> {
  return MakeHttpCall("GET", url, undefined, params, msalResult)
}

async function MakeHttpCall(method:string, url:string, body?:any, params?:Record<string, string>, msalResult?:AuthenticationResult): Promise<any> {
  const encodedParams:Record<string,string> = {};
  if (params) {
    Object.keys(params).forEach((key) => {
      encodedParams[key] = encodeURIComponent(params[key]);
    });
  }

  const queryParams = new URLSearchParams(encodedParams).toString();
  const fullUrl = queryParams ? `${url}?${queryParams}` : url;

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  }

  if (msalResult) {
    var token = msalResult.accessToken;
    if (!token) {
      token = msalResult.idToken;
    }

    if (token) {
      headers['Authorization'] = 'Bearer ' + token
    }
  }

  try {
    const requestInit:RequestInit = {
      method: method,
      headers: headers
    }

    if (body) {
      requestInit.body = body;
    }

    const response = await fetch(fullUrl, requestInit);
    const data = await response.text();

    if (response.ok) {
      return data;
    }

    if (response.status === 401) {
      throw new Error("Please login to generate Magic: The Gathering cards! Making an account is free and just requires an email address.");
    }

    var error = data;
    try {
      const errorObj = JSON.parse(data);
      error = errorObj.error.message || errorObj.error || data
    } catch (e) { }
    throw new Error(error);
  } catch (error) {
    console.error('Error fetching data:', error);
    throw error;
  }
}
