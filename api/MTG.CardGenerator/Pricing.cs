using OpenAI.ObjectModels.ResponseModels;
using System.Collections.Generic;

namespace MTG.CardGenerator
{
    public class Cost
    {
        public double TotalCost { get; private set; } = 0;

        public void AddChatCost(ChatCompletionCreateResponse result)
        {
            TotalCost += Pricing.GetCost(result);
        }

        public void AddImageCost(string size, string model)
        {
            TotalCost += Pricing.GetImageCost(size, model);
        }
    }

    public static class Pricing
    {
        private class ModelCost
        {
            public string Model { get; set; }
            public double InputPrice { get; set; }
            public double OutputPrice { get; set; }
        }

        // From https://openai.com/pricing.
        // Currently no way to get this from an API.
        private static readonly List<ModelCost> ModelCosts = new()
        {
            // GPT 3.5
            new ModelCost() { Model = "gpt-3.5-turbo-0301", InputPrice = .0015 / 1000, OutputPrice = .002 / 1000 },
            new ModelCost() { Model = "gpt-3.5-turbo-0613", InputPrice = .0015 / 1000, OutputPrice = .002 / 1000 },
            // GPT 4
            new ModelCost() { Model = "gpt-4-0314", InputPrice = .03 / 1000, OutputPrice = .06 / 1000 },
            new ModelCost() { Model = "gpt-4-0613", InputPrice = .03 / 1000, OutputPrice = .06 / 1000 },
            new ModelCost() { Model = "gpt-4-1106-preview", InputPrice = .01 / 1000, OutputPrice = .03 / 1000 },
        };

        public static double GetCost(ChatCompletionCreateResponse result)
        {
            var cost = ModelCosts.Find(x => x.Model == result.Model);

            if (cost == null)
            {
                return 0;
            }

            return result.Usage.PromptTokens * cost.InputPrice + result.Usage.CompletionTokens.Value * cost.OutputPrice;
        }

        public static double GetImageCost(string size, string model)
        {
            var s = size.ToString();
            if (s == "1024x1024" && model == Constants.Dalle2ModelName)
            {
                return .02;
            }

            if (s == "512x512" && model == Constants.Dalle2ModelName)
            {
                return .018;
            }

            if (s == "256x256" && model == Constants.Dalle2ModelName)
            {
                return .016;
            }

            if (s == "1024x1024" && model == Constants.Dalle3ModelName)
            {
                return .04;
            }

            if ((s == "1024x1792" || s == "1792x1024") && model == Constants.Dalle3ModelName)
            {
                return .08;
            }

            return 0;
        }
    }
}
