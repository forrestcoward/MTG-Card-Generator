# Magic: The Gathering Card Generator (Utilizing OpenAI)

This is a project to generate realistic Magic: The Gathering cards using OpenAI's text and image generation capabilties. Generated cards are rendered beautifully using CSS.

Users can enter a prompt describing the type of Magic card they would like to generate, and the response will be a picture of a card. Here is an example card generation:

<p align="center">
  <img src="cards\gpt-generator.png" width="480px" height="700px">
</p>

# Local Development Guide

## Components

There are two components to this project:
1. A C# back end consisting of a single Azure function. The backend generates cards based on a prompt and an image based on the card text.
2. A Typescript and React front end. The front end allows the user to enter a prompt and then renders the generated card on the page.

## Build & Run the Backend

1. Open `MTG.CardGenerator\MTG.CardGenerator.sln` in Visual Studio 2022 and build the solution. 

2. Add a `local.settings.json` inside the `MTG.CardGenerator`:

```
{
  "IsEncrypted": false,
  "Host": {
    "CORS": "http://localhost:8080",
    "CORSCredentials": true
  },
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "OPEN_API_KEY": "<YOUR OPEN AI KEY GOES HERE>"
  }
}
```
You will need a valid OpenAI key for running locally. This file is in the `.gitignore` so you do not accidently commit your key.

3. Start the `MTG.CardGenerator` project to launch the Azure function emulator over local host.

## Build & Run the Frontend

From the root of the repository, install depenencies and start the local web server:
```
npm install
npm run dev
```

Navigate to `http://localhost:8080/` in your browser.

# Links

* [Max Woolf's blog on using ChatGPT to generate MTG cards](https://minimaxir.com/2023/03/new-chatgpt-overlord/)
* [Make a Magic: The Gathering card in CSS](https://codeburst.io/make-a-magic-the-gathering-card-in-css-5e4e06a5e604)
* [Magic: the Gathering font project](https://github.com/andrewgioia/mana)