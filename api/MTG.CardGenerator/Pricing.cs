using Betalgo.Ranul.OpenAI.ObjectModels.ResponseModels;
using Castle.Core.Logging;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics.Contracts;

namespace MTG.CardGenerator
{
    public class Cost
    {
        [JsonProperty("totalCost")]
        public double TotalCost { get; set; }

        [JsonProperty("costToUserApiKeys")]
        public double CostToUserApiKeys { get; set; }

        [JsonProperty("costToWebsiteApiKeys")]
        public double CostToWebsiteApiKeys { get; set; }

        [JsonProperty("charges")]
        public List<Charge> Charges { get; set; } = new List<Charge>();

        public class Charge()
        {
            [JsonProperty("operationName")]
            public string OperationName { get; set; }
            [JsonProperty("model")]
            public string Model { get; set; }
            [JsonProperty("cost")]
            public double Cost { get; set; }
            [JsonProperty("wasUserCharged")]
            public bool WasUserCharged { get; set; }
        }

        public void AddChatCost(ChatCompletionCreateResponse result, bool wasUserCharged, string operationName, Microsoft.Extensions.Logging.ILogger logger)
        {
            if (result == null)
            {
                logger.LogError($"ChatCompletionCreateResponse is null. Cannot calculate cost for operation '{operationName}'.");
                return;
            }

            Contract.Assert(CostToUserApiKeys + CostToWebsiteApiKeys == TotalCost, "TotalCost should be the sum of User and Website costs.");
            var cost = Pricing.GetCost(result);
            TotalCost += cost;

            if (wasUserCharged)
            {
                CostToUserApiKeys += cost;
            }
            else
            {
                CostToWebsiteApiKeys += cost;
            }

            Charges.Add(new Charge()
            {
                Cost = cost,
                Model = result.Model,
                OperationName = operationName,
                WasUserCharged = wasUserCharged,
            });
        }

        public void AddImageCost(string imageSize, string imageModel, bool wasUserCharged, string operationName)
        {
            Contract.Assert(CostToUserApiKeys + CostToWebsiteApiKeys == TotalCost, "TotalCost should be the sum of User and Website costs.");
            var cost = Pricing.GetImageCost(imageSize, imageModel);
            TotalCost += cost;

            if (wasUserCharged)
            {
                CostToUserApiKeys += cost;
            }
            else
            {
                CostToWebsiteApiKeys += cost;
            }

            Charges.Add(new Charge()
            {
                Cost = cost,
                Model = imageModel,
                OperationName = operationName,
                WasUserCharged = wasUserCharged,
            });
        }

        // Merge two Cost objects into a new Cost object with combined charges and summed costs.
        public Cost Merge(Cost other)
        {
            return new Cost
            {
                TotalCost = TotalCost + other.TotalCost,
                CostToUserApiKeys = CostToUserApiKeys + other.CostToUserApiKeys,
                CostToWebsiteApiKeys = CostToWebsiteApiKeys + other.CostToWebsiteApiKeys,
                Charges = Charges.Concat(other.Charges).ToList()
            };
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
                throw new Exception($"Model '{result.Model}' not found in pricing list.");
            }

            return (result.Usage.PromptTokens * cost.InputPrice) + (result.Usage.CompletionTokens.Value * cost.OutputPrice);
        }

        public static double GetImageCost(string size, string model)
        {
            var s = size.ToString();
            if (s == "1024x1024" && model == Constants.ImageModels.Dalle2ModelName)
            {
                return .02;
            }

            if (s == "512x512" && model == Constants.ImageModels.Dalle2ModelName)
            {
                return .018;
            }

            if (s == "256x256" && model == Constants.ImageModels.Dalle2ModelName)
            {
                return .016;
            }

            if (s == "1024x1024" && model == Constants.ImageModels.Dalle3ModelName)
            {
                return .04;
            }

            if ((s == "1024x1792" || s == "1792x1024") && model == Constants.ImageModels.Dalle3ModelName)
            {
                return .08;
            }

            if (model == Constants.ImageModels.GoogleImagen3FastModelName)
            {
                return 0.025;
            }

            if (model == Constants.ImageModels.StableDiffusion35MediumModelName)
            {
                return 0.035;
            }

            if (model == Constants.ImageModels.BlackForestLabsFluxSchnell)
            {
                return 0.003;
            }

            throw new Exception($"Image model '{model}' and size '{size}' not found in pricing list.");
        }
    }
}
