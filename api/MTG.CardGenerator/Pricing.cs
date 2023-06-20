using OpenAI_API.Chat;
using OpenAI_API.Images;
using System.Collections.Generic;

namespace MTG.CardGenerator
{
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
        };

        public static double GetCost(ChatResult result)
        {
            var cost = ModelCosts.Find(x => x.Model == result.Model.ModelID);

            if (cost == null)
            {
                return 0;
            }

            return result.Usage.PromptTokens * cost.InputPrice + result.Usage.CompletionTokens * cost.OutputPrice;
        }

        public static double GetImageCost(ImageSize size)
        {
            if (size.ToString() == "1024x1024")
            {
                return .02;
            }

            if (size.ToString() == "512x512")
            {
                return .018;
            }

            if (size.ToString() == "256x256")
            {
                return .016;
            }

            return 0;
        }
    }
}
