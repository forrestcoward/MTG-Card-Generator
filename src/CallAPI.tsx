import { Configuration, CreateImageRequestSizeEnum, OpenAIApi } from "openai";
import { BasicCard, MagicCard } from "./Card";
import { AuthenticationResult, PublicClientApplication } from "@azure/msal-browser";

async function MakeOpenAIChatComletionRequest(apiKey:string, userPrompt:string, systemPrompt:string, temperature:number = 1, model:string = "gpt-3.5-turbo") : Promise<string> {
  const configuration = new Configuration({
    apiKey: apiKey,
  });
  const openai = new OpenAIApi(configuration);

  const response = await openai.createChatCompletion({
    model: model,
    messages: [{role: "user", content: userPrompt},
               {role: "system", content: systemPrompt}],
    temperature: temperature
  });

  if (response.data && response.data.choices && response.data.choices[0] && response.data.choices[0].message) {
      return response.data.choices[0].message.content;
  } else {
      console.log(response)
      throw "Error: Invalid response from OpenAI.crateChatComletion. See console.log for more details."
  }
}

async function MakeOpenAIImageCreateRequest(apiKey:string, imagePrompt:string) : Promise<string> {
  const configuration = new Configuration({
    apiKey: apiKey,
  });
  const openai = new OpenAIApi(configuration);

  let response = await openai.createImage({
    prompt: imagePrompt,
    n: 1,
    size: CreateImageRequestSizeEnum._256x256,
  })

  if (response.data && response.data.data && response.data.data[0] && response.data.data[0].url) {
      return response.data.data[0].url
  } else {
      console.log(response)
      throw "Error: Invalid response from OpenAI.createImage. See console.log for more details."
  }
}

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
    console.error("Error retrieving MSAL token:" + error)
    return undefined;
  });
}

export async function GetUserMagicCards(msal: PublicClientApplication): Promise<MagicCard[]> {
  let url = 'https://mtgcardgenerator-development.azurewebsites.net/api/GetMagicCards';

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

export async function GenerateMagicCardRequest(userPrompt: string, model: string, includeExplanation: boolean, openAIApiKey: string, msal: PublicClientApplication): Promise<MagicCard[]> {
  let url = 'https://mtgcardgenerator-development.azurewebsites.net/api/GenerateMagicCard';

  if (location.hostname === "localhost") {
    url = 'http://localhost:7071/api/GenerateMagicCard';
  }

  var token = await RetrieveMsalToken(msal, ["https://mtgcardgenerator.onmicrosoft.com/api/generate.mtg.card"])

  const params: Record<string, string> = {
    userPrompt: userPrompt,
    model: model,
    includeExplanation: includeExplanation.toString(),
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
      if (data == "Backend call failure") {
        data = "The backend timed out after 45 seconds while generating your card. This is a known issue that will be fixed soon. Sorry! :( Try using GPT-3 or turning off the 'Explain Yourself' setting for faster generation."
      }

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
