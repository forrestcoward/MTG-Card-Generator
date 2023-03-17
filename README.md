# OpenAI-MTG-Card-Generator

An experimental project to generate realistic Magic: The Gathering cards using OpenAI. Generated cards are rendered using only CSS.

<img src="img\QuickfireChallenge.png" width="600px" height="875px">
<img src="img\GaeasTendrils.png"  width="600px" height="875px">

## Build & Run

First, create a file named `config.json` at the root of the repository with this structure:

```
{
  "OpenAIApiKey": "<OpenAI API key goes here>"
}
```
This file is in `.gitignore` so you do not actually add and commit your secret key.

Next, install dependencies and run:

```
npm install
npm run dev
```

## Links

* [Max Woolf's blog on using ChatGPT to generate MTG cards](https://minimaxir.com/2023/03/new-chatgpt-overlord/)
* [Make a Magic: The Gathering card in CSS](https://codeburst.io/make-a-magic-the-gathering-card-in-css-5e4e06a5e604)
* [Magic: the Gathering font project](https://github.com/andrewgioia/mana)