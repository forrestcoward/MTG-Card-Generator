using Betalgo.Ranul.OpenAI.ObjectModels.ResponseModels;
using Newtonsoft.Json;

namespace MTG.CardGenerator
{
    public class Cost
    {
        [JsonProperty("totalCost")]
        public double TotalCost { get; set; }

        public void AddChatCost(ChatCompletionCreateResponse result)
        {
            TotalCost += Pricing.GetCost(result);
        }

        public void AddImageCost(string imageSize, string imageModel)
        {
            TotalCost += Pricing.GetImageCost(imageSize, imageModel);
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
        private static readonly List<ModelCost> ModelCosts =
        [
            new ModelCost() { Model = "gpt-4o-mini", InputPrice = .15 / 1000000.0, OutputPrice = .6 / 1000000.0 },
            new ModelCost() { Model = "gpt-4o", InputPrice = 2.5 / 1000000.0, OutputPrice = 10 / 1000000.0 },
            new ModelCost() { Model = "o3-mini", InputPrice = 1.10 / 1000000.0, OutputPrice = 4.4 / 1000000.0 },
            new ModelCost() { Model = "o3", InputPrice = 10 / 1000000.0, OutputPrice = 40 / 1000000.0 },
            new ModelCost() { Model = "gpt-4.1-mini", InputPrice = .4 / 1000000.0, OutputPrice = 1.6 / 1000000.0 },
            new ModelCost() { Model = "gpt-4.1-nano", InputPrice = .1 / 1000000.0, OutputPrice = .4 / 1000000.0 },
            new ModelCost() { Model = "gpt-4.1", InputPrice = 2 / 1000000.0, OutputPrice = 8 / 1000000.0 },
        ];

        public static double GetCost(ChatCompletionCreateResponse result)
        {
            var cost = ModelCosts.Find(x => result.Model.StartsWith(x.Model, System.StringComparison.OrdinalIgnoreCase));

            if (cost == null)
            {
                return 0;
            }

            return (result.Usage.PromptTokens * cost.InputPrice) + (result.Usage.CompletionTokens.Value * cost.OutputPrice);
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
