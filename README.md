# Magic: The Gathering Card Generator

## Try the website: [https://www.mtgcardgenerator.com](https://www.mtgcardgenerator.com/)

## Try the OpenAI GPT plugin: [View Plugin](https://chatgpt.com/g/g-gyWs4hFFS-magic-the-gathering-card-generator)

## Follow us on [instagram](https://www.instagram.com/mtg.cardgenerator/)!


The Magic: The Gathering Card Generator is a project that uses AI to generate realistic Magic: The Gathering cards. Generated cards are rendered beautifully using just CSS.

Users can enter a prompt describing the type of Magic card they would like to generate, and the response will be a be a rendered image of a realistic card that matches the prompt. Here is an [example card generation](https://www.mtgcardgenerator.com/Card?id=732874ce-468e-4823-8605-2cceea9626c5):

<p align="center">
  <img src="cards\Cutlery-Cohort.png" width=420>
</p> 

# Features

* Users supply an open ended prompt describing the Magic card they would like to generate. Users may:
    * Choose the language model.
    * Choose the image model or supply their own image prompt.
    * Select a number of other fun options such as asking the AI explain itself or asking for extra creative cards.
    * Resize, download, or share generated card links.

* Generated cards are rendered beautifully using just CSS and look like real Magic cards. Realistic card background and symbols are used, and card text is scaled appropriately to fill the content.

* Users may login to a daily free spent limit for generating and images (this limit resets each day).
  * User's card generation history stored for viewing later.

* A rating page is available to score and view top cards.

# API

A public API is available to generate cards.

The API is located at https://mtgcardgenerator.azurewebsites.net/api/GenerateMagicCard.

### Parameters

| Name | Pass As | Type | Description |
|---|---|---|---|
| `userPrompt` | query param | string | The prompt to use to generate the card. |
| `model` | query param | string | The OpenAI language model to use. Allowed values: `gpt-41`, `gpt-40`, `gpt-o3` |
| `imageModel` | query param | string | The OpenAI model to use for generating the card image. <br/>Allowed values: `imagen-4-fast`, `imagen-3-fast`, `black-forest-labs-flux-schnell`, `stable-diffusion-3.5-medium`, `dall-e-3`,`dall-e-2`, `none` |
| `includeExplanation` | query param | bool | Whether to include the AI's reasoning in the card text. If `true`, `explanation` and `funnyExplanation` will be included in the response. |
| `extraCreative` | query param | bool | If `true`, uses a different prompt and higher temperature to generate more creative cards with more interesting usage of keywords and mechanics. |
| `generateImagePrompt` | query param | bool | If `true`, use an intelligently generated image prompt to produce more unique and interesting art. |
| `x-openai-api-key` | header | string | Your [OpenAI API key](https://platform.openai.com/api-keys). The API will **never** store your key. An API key must be provided when the user's daily spend runs out.<br><br> The Open AI API key is used to generate card text and when generating card images using a Dall-e image model. |
| `x-replicate-api-key` | header | string | Your [Replicate](https://replicate.com/) API key. The API will **never** store your key. An API key must be provided when the user's daily spend runs out.<br> <br> The Replicate API key is used to generate card image using non Dall-e models.|

### Sample Response

The response from https://mtgcardgenerator.azurewebsites.net/api/GenerateMagicCard will return an `id` you can use to track the request.

```json
{ 
  "id": "75f60a63dc874f2999bf0316f5c5a0d4" 
}
```

Poll the `GetMagicCardGenerationStatus` endpoint e.g. https://mtgcardgenerator.azurewebsites.net/api/GetMagicCardGenerationStatus?instanceId={id} to get the status and eventual outcome of the request. The `runtimeStatus` will be `Completed` when the card is ready.

```json
{
  {
    "id": "75f60a63dc874f2999bf0316f5c5a0d4",
    "runtimeStatus": "Completed",
    "customStatus": "success",
    "output": 
    {  
      "cards": [
        {
          "id": "969f10c2-66f4-40e4-be96-8dd0341fd0c4",
          "name": "Djurge, the Timebender",
          "manaCost": "{3}{U}{U}{U}",
          "type": "Creature",
          "typeLine": "Legendary Creature - Human Wizard",
          "oracleText": "When Djurge, the Timebender enters the battlefield, you may exile target nonland permanent. Its controller puts it into their library second from the top.\nAt the beginning of your upkeep, you may exile the top card of your library. If you do, you may cast it without paying its mana cost.",
          "flavorText": "Time is but a mere thread for Djurge, the weaver of history.",
          "colorIdentity": "Blue",
          "rarity": "Mythic Rare",
          "pt": "4/4",
          "explanation": null,
          "funnyExplanation": null,
          "userPrompt": "A card from the Dominaria plane.",
          "imageUrl": "<link>",
          "temporaryImageUrl": "<link>",
          "url":"https://www.mtgcardgenerator.com/Card?id=969f10c2-66f4-40e4-be96-8dd0341fd0c4"
        }
      ],
      "costs": {
        "userRemainingDailySpendLimit": 0.2767,
        "cost": 0.023136,
        "userIncurredCost": 0.003136,
        "serverIncurredCost": 0.02
      }
    }
  }
}
```

## Authentication

An authentication endpoint is provided for convenience for retrieving the access token associated with your account.

**Endpoint:** `POST https://mtgcardgenerator.azurewebsites.net/api/Authenticate`

**Request Body:**
```json
{
  "username": "your-email@example.com",
  "password": "your-password"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6...",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

Include the access token in the `Authorization` header for authenticated requests to the API.
```
Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6...
```

## Daily Spend

Each user is given a daily free spend limit for generating cards and images on the website and through the API. This limit resets each day at midnight UTC. If a user exceeds their daily free spend, they can provide their own [OpenAI](https://platform.openai.com/api-keys) and/or [Replicate](https://replicate.com/explore) API keys in the request headers to continue generating card text and images. **The website and API will never store your API keys, but obviously, you can use at your own risk and check the source code yourself! :)**

A user who exceeds their daily free spend will receive a 429 response from the API. The *GetMagicCardGenerationStatus* API provides a breakdown of the costs incurred and the user's remaining daily free spend limit.

```json
"costs": {
  "userRemainingDailySpendLimit": 0.2767,
  "cost": 0.023136,
  "userIncurredCost": 0.003136,
  "serverIncurredCost": 0.02
}
```

Generating card images is the most expensive operation. If you are only generating card text and want to avoid the limit, just provide an OpenAI API key.

# Development Guide

This section gives an overview for how to contribute to this project. 

## Components

There are two components to this project: 
1. An Azure, C# back end which is an Azure function API built on top of other Azure resources (Cosmos DB, Blob storage).
    * The backend API generates cards based on a prompt and an image based on the card text. A simple, stupid, rules engine is implemented to fix common problems on generated cards.
    * [Azure B2C](https://learn.microsoft.com/en-us/azure/active-directory-b2c/overview) is used as the SSO provider for the login experience.
    * If the user is logged on, a history is stored for the user so generated cards can be viewed later.
2. A React, Typescript single-page application (SPA). The front end allows the user to login, enter a prompt and then renders the generated card on the page once the API response is received.

## Build & Run the Backend 

1. Open `api\MTG.CardGenerator.sln` in Visual Studio 2022 and build the solution. 

2. Add a `local.settings.json` inside the `MTG.CardGenerator` project:

```json
{
  "IsEncrypted": false,
  "Host": {
    "CORS": "http://localhost:8080",
    "CORSCredentials": true
  },
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "DurableTaskHubName": "mtgcardgeneratorlocalhub",
    // API keys.
    "OPENAI_API_KEY": "",
    "REPLICATE_API_KEY": "",
    // Bearer token validation settings.
    "ValidateBearerToken": false,
    "ValidJWTAuthority": "https://mtgcardgenerator.b2clogin.com/mtgcardgenerator.onmicrosoft.com/B2C_1_signup_signin",
    "ValidJWTAudience": "1cc495e4-bd18-4bed-9c18-857689172c50",
    "ValidJWTIssuer": "https://mtgcardgenerator.b2clogin.com/e856e196-1342-4533-b8a6-0e5a50bf6a67/v2.0/",
    // Database settings.
    "CosmosDBEndpointUrl": "",
    "CosmosDBAccessKey": "",
    "CosmosDBDatabaseId": "",
    // Blob storage settings.
    "BlobStorageName": "",
    "BlobStorageEndpoint": "",
    "BlobStorageContainerName": "card-images",
    "BlobStorageAccessKey": ""
  }
}
```

You will need to fill in the missing values which include supplying an [Open AI API key](https://platform.openai.com/api-keys), [Replicate API key](https://replicate.com/account/api-tokens), an Azure Cosmos DB, and an Azure Blob storage. Set `ValidateBearerToken` to false to skip any API authentication when running the Azure function emulator locally (per function authorization is implemented). Or, you may utilize the [Azure B2C instance]((https://learn.microsoft.com/en-us/azure/active-directory-b2c/overview)) that has been set up for this website with the values provided above to test the login experience.

3. Start the `MTG.CardGenerator` project to launch the Azure function emulator over local host.

## Build & Run the Frontend

From the root of the repository, install depenencies and start the local web server:
```
npm install
npm run dev
```

Navigate to `http://localhost:8080/` in your browser. VS Code launch configurations are provided to easily enable debugging.

## Deployment

This application is deployed entirely within a personal Azure subscription. Pushing to the `main` or `development` branch will trigger [GitHub actions](https://github.com/forrestcoward/MTG-Card-Generator/actions) which perform deployments into Azure updating the resources shown below:

| Branch | Azure Static Web App SPA  | Azure Function |
|---|---|---|
| `main` | https://www.mtgcardgenerator.com/ <br /> https://ambitious-meadow-0e2e9ce0f.3.azurestaticapps.net/ | https://mtgcardgenerator.azurewebsites.net |
| `development` | https://ambitious-meadow-0e2e9ce0f-development.eastus2.3.azurestaticapps.net/ | https://mtgcardgenerator-development.azurewebsites.net |

All other resources required to make the website work are already deployed and configured in Azure.

Deployments should take only a few minutes.

If you are serious about helping with development, we can discuss gaining limited access to the underlying Azure resources for better testing, but most development and testing has been designed to happen locally.

# Links

* [Max Woolf's blog on using ChatGPT to generate MTG cards](https://minimaxir.com/2023/03/new-chatgpt-overlord/)
* [Make a Magic: The Gathering card in CSS](https://codeburst.io/make-a-magic-the-gathering-card-in-css-5e4e06a5e604)
* [Magic: the Gathering font project](https://github.com/andrewgioia/mana)
