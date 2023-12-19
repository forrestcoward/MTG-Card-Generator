import { BasicCard, CardGenerationRecord, CardRating, MagicCard } from "./Card";
import { AuthenticationResult, InteractionRequiredAuthError, PublicClientApplication } from "@azure/msal-browser";

export async function RetrieveMsalToken(msal: PublicClientApplication, scopes: string[]): Promise<AuthenticationResult | undefined> {
  const account = msal.getAllAccounts()[0];
  if (!account) {
    return undefined;
  }

  const accessTokenRequest = {
    scopes: scopes,
    account: account,
  };

  await msal.initialize()
  return await msal.acquireTokenSilent(accessTokenRequest).then(async function (response) {
    return response;
  }).catch(async function (error) {
    if (error instanceof InteractionRequiredAuthError) {
      // Fallback to interaction when silent call fails.
      return msal.acquireTokenPopup(accessTokenRequest);
    }
    console.error("Error retrieving MSAL token: " + error)
    return undefined;
  });
}

const productionApiUrl = 'https://mtgcardgenerator.azurewebsites.net/api';
const developmentApiUrl  = 'https://mtgcardgenerator-development.azurewebsites.net/api';
const developmentHostName = 'ambitious-meadow-0e2e9ce0f-development.eastus2.3.azurestaticapps.net';
const localApiUrl = 'http://localhost:7071/api';

function getApiUrl(apiName : string): string {
  var baseUrl = productionApiUrl;

  if (location.hostname === developmentHostName) {
    baseUrl = developmentApiUrl
  }

  if (location.hostname === "localhost") {
    baseUrl = localApiUrl;
  }

  return `${baseUrl}/${apiName}`;
}

async function GetCardAPICall(apiName: string, msal: PublicClientApplication): Promise<CardGenerationRecord | undefined> {
  let url = getApiUrl(apiName);
  var token = await RetrieveMsalToken(msal, ["https://mtgcardgenerator.onmicrosoft.com/api/generate.mtg.card"])

  const params: Record<string, string> = { };
  let record:CardGenerationRecord | undefined = undefined
  await httpGet(url, token, params)
    .then(data => {
      record = JSON.parse(data);
    })
    .catch(error => {
      console.error(`There was an error calling '${apiName}': `, error);
      throw error
    })

    return record;
}

async function GetCardsAPICall(apiName: string, msal: PublicClientApplication): Promise<CardGenerationRecord[]> {
  let url = getApiUrl(apiName);
  var token = await RetrieveMsalToken(msal, ["https://mtgcardgenerator.onmicrosoft.com/api/generate.mtg.card"])

  const params: Record<string, string> = { };
  let cardGenerationRecords:CardGenerationRecord[] = []
  await httpGet(url, token, params)
    .then(data => {
      cardGenerationRecords = JSON.parse(data);
    })
    .catch(error => {
      console.error(`There was an error calling '${apiName}': `, error);
      throw error
    });

    return cardGenerationRecords;
}

export async function GetCardToRate(msal: PublicClientApplication): Promise<CardGenerationRecord | undefined> {
  return GetCardAPICall('GetCardToRate', msal);
}

export async function RateCard(cardId: string, rating: number, msal: PublicClientApplication): Promise<CardRating | undefined> {

  let url = getApiUrl('RateCard');
  var token = await RetrieveMsalToken(msal, ["https://mtgcardgenerator.onmicrosoft.com/api/generate.mtg.card"])

  const params: Record<string, string> = { "cardId": cardId, "rating": rating.toString()};
  let cardRating:CardRating | undefined = undefined
  await httpGet(url, token, params)
    .then(data => {
      cardRating = JSON.parse(data);
    })
    .catch(error => {
      console.error('There was an error rating a card:', error);
      throw error
    });
    
    return cardRating;
}

export async function GetTopCards(msal: PublicClientApplication): Promise<CardGenerationRecord[]> {
  return GetCardsAPICall('GetTopCards', msal);
}

export async function GetUserMagicCards(msal: PublicClientApplication): Promise<CardGenerationRecord[]> {
  return GetCardsAPICall('GetMagicCards', msal);
}

export async function GenerateMagicCardRequest(userPrompt: string, model: string, includeExplanation: boolean, highQualityImages: boolean, openAIApiKey: string, msal: PublicClientApplication): Promise<MagicCard[]> {
  let url = getApiUrl('GenerateMagicCard');
  var token = await RetrieveMsalToken(msal, ["https://mtgcardgenerator.onmicrosoft.com/api/generate.mtg.card"])

  const params: Record<string, string> = {
    userPrompt: userPrompt,
    model: model,
    includeExplanation: includeExplanation.toString(),
    highQualityImage: highQualityImages.toString(),
  };

  if (openAIApiKey) {
    params.openAIApiKey = openAIApiKey
  }

  let cards:BasicCard[] = []
  await httpGet(url, token, params)
    .then(data => {
      cards = JSON.parse(data).cards;
    })
    .catch(error => {
      console.error('There was an error generating card JSON:', error);
      throw error
    });

    return cards.map(card => new MagicCard(card));
}

export async function SearchMagicCardsRequest(query: string, msal: PublicClientApplication): Promise<MagicCard[]> {
  let url = getApiUrl('SearchMagicCards');
  var token = await RetrieveMsalToken(msal, ["https://mtgcardgenerator.onmicrosoft.com/api/generate.mtg.card"])

  const params: Record<string, string> = {
    query: query,
  };

  let cards:BasicCard[] = []
  await httpGet(url, token, params)
    .then(data => {
      cards = JSON.parse(data).cards;
    })
    .catch(error => {
      console.error('There was an error generating card JSON:', error);
      throw error
    });

    return cards.map(card => new MagicCard(card));
}

async function httpGet(url: string, msalResult: AuthenticationResult | undefined, params?: Record<string, string>): Promise<any> {
  const sanitizedParams: Record<string, string> = {};
  if (params) {
    Object.keys(params).forEach((key) => {
      sanitizedParams[key] = params[key];
    });
  }

  const queryParams = new URLSearchParams(sanitizedParams).toString();
  const fullUrl = queryParams ? `${url}?${queryParams}` : url;

  let headers : Record<string, string> = {
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
    const response = await fetch(fullUrl, {
      method: 'GET',
      headers: headers
    });

    let data = await response.text();

    if (!response.ok) {
      if (response.status == 401 || response.status == 403)  {
        throw new Error("You are not authorized to use this API. Please sign in and try again.")
      }

      throw new Error(data);
    }

    return data;
  } catch (error) {
    console.error('Error fetching data:', error);
    throw error;
  }
}
