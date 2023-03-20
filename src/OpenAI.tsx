import { Configuration, CreateImageRequestSizeEnum, OpenAIApi } from "openai";

export async function MakeOpenAIChatComletionRequest(apiKey:string, userPrompt:string, systemPrompt:string, model:string = "gpt-3.5-turbo") : Promise<string> {
    const configuration = new Configuration({
      apiKey: apiKey,
    });
    const openai = new OpenAIApi(configuration);

    const response = await openai.createChatCompletion({
      model: model,
      messages: [{role: "user", content: userPrompt},
                 {role:"system", content: systemPrompt}],
    });

    if (response.data && response.data.choices && response.data.choices[0] && response.data.choices[0].message) {
        return response.data.choices[0].message.content;
    } else {
        console.log(response)
        throw "Error: Invalid response from OpenAI.crateChatComletion. See console.log for more details."
    }
  }

  export async function MakeOpenAIImageCreateRequest(apiKey:string, imagePrompt:string) : Promise<string> {
    const configuration = new Configuration({
      apiKey: apiKey,
    });
    const openai = new OpenAIApi(configuration);

    let response = await openai.createImage({
      prompt: imagePrompt,
      n: 1,
      size: CreateImageRequestSizeEnum._256x256,
    });

    if (response.data && response.data.data && response.data.data[0] && response.data.data[0].url) {
        return response.data.data[0].url
    } else {
        console.log(response)
        throw "Error: Invalid response from OpenAI.createImage. See console.log for more details."
    }
  }