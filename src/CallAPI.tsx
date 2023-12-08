import { BasicCard, MagicCard } from "./Card";
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

  return await msal.acquireTokenSilent(accessTokenRequest).then(async function (response) {
    return response;
  }).catch(async function (error) {
    if (error instanceof InteractionRequiredAuthError) {
      // fallback to interaction when silent call fails
      return msal.acquireTokenPopup(accessTokenRequest);
    }
    console.error("Error retrieving MSAL token:" + error)
    return undefined;
  });
}

export async function GetUserMagicCards(msal: PublicClientApplication): Promise<MagicCard[]> {

  let url = 'https://mtgcardgenerator.azurewebsites.net/api/GetMagicCards';

  if (location.hostname === "ambitious-meadow-0e2e9ce0f-development.eastus2.3.azurestaticapps.net") {
    url = 'https://mtgcardgenerator-development.azurewebsites.net/api/GetMagicCards';
  }

  if (location.hostname === "localhost") {
    url = 'http://localhost:7071/api/GetMagicCards';
  }

  var token = await RetrieveMsalToken(msal, ["https://mtgcardgenerator.onmicrosoft.com/api/generate.mtg.card"])

  const params: Record<string, string> = { };

  let cards:BasicCard[] = []
  await httpGet(url, token, params)
    .then(data => {
      cards = JSON.parse(data).cards;
    })
    .catch(error => {
      console.error('There was an error getting magic cards for user:', error);
      throw error
    });

    return cards.map(card => new MagicCard(card));
}

export async function GenerateMagicCardRequest(userPrompt: string, model: string, includeExplanation: boolean, highQualityImages: boolean, openAIApiKey: string, msal: PublicClientApplication): Promise<MagicCard[]> {
  let url = 'https://mtgcardgenerator.azurewebsites.net/api/GenerateMagicCard';

  if (location.hostname === "ambitious-meadow-0e2e9ce0f-development.eastus2.3.azurestaticapps.net") {
    url = 'https://mtgcardgenerator-development.azurewebsites.net/api/GenerateMagicCard';
  }

  if (location.hostname === "localhost") {
    url = 'http://localhost:7071/api/GenerateMagicCard';
  }

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
  let url = 'https://mtgcardgenerator.azurewebsites.net/api/SearchMagicCards';

  if (location.hostname === "ambitious-meadow-0e2e9ce0f-development.eastus2.3.azurestaticapps.net") {
    url = 'https://mtgcardgenerator-development.azurewebsites.net/api/SearchMagicCards';
  }

  if (location.hostname === "localhost") {
    url = 'http://localhost:7071/api/SearchMagicCards';
  }

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
