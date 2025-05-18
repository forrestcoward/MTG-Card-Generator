using MTG.CardGenerator.Exceptions;
using MTG.CardGenerator.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MTG.CardGenerator
{
    public class ReplicateApiClient
    {
        private readonly string replicateApiKey;
        private readonly HttpClient httpClient;

        public ReplicateApiClient(string apiKey)
        {
            replicateApiKey = apiKey;
            httpClient = new HttpClient();
        }

        /// <summary>
        /// Runs a prediction on Replicate and polls until completion.
        /// </summary>
        /// <param name="prompt">The prompt</param>
        /// <param name="model">Model name, e.g. "google/imagen-3-fast"</param>
        /// <param name="input">Input object for the model (will be serialized as JSON)</param>
        /// <returns>The output URL or value as a string</returns>
        public async Task<string> ExecuteRelicateAPI(string prompt, string model, object input)
        {
            var url = $"https://api.replicate.com/v1/models/{model}/predictions";
            var payload = new { input };
            var json = JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", replicateApiKey);

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new FailedToGenerateImageException($"Replicate API request failed: {response.StatusCode} - {errorContent}", prompt, model);
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var prediction = JsonSerializer.Deserialize<ReplicatePredictionResponse>(responseJson);

            if (prediction == null)
            {
                throw new Exception("Failed to deserialize Replicate API response");
            }

            if (prediction.Urls == null || string.IsNullOrEmpty(prediction.Urls.Get))
            {
                throw new Exception("Replicate API response missing 'urls.get' field.");
            }

            var pollUrl = prediction.Urls.Get;
            string outputUrl = null;
            string errorMsg = null;

            while (true)
            {
                if (prediction.Status == ReplicatePredictionStatus.Succeeded)
                {
                    // Output can be a string or array of strings
                    if (prediction.Output is string s)
                    {
                        outputUrl = s;
                    }
                    else if (prediction.Output is System.Text.Json.JsonElement elem)
                    {
                        if (elem.ValueKind == JsonValueKind.String)
                        {
                            outputUrl = elem.GetString();
                        }
                        else if (elem.ValueKind == JsonValueKind.Array && elem.GetArrayLength() > 0)
                        {
                            var first = elem[0];
                            if (first.ValueKind == JsonValueKind.String)
                                outputUrl = first.GetString();
                        }
                    }
                    break;
                }
                if (prediction.Status == ReplicatePredictionStatus.Failed || prediction.Status == ReplicatePredictionStatus.Canceled)
                {
                    errorMsg = prediction.Error;
                    break;
                }

                await Task.Delay(2000);

                using var pollRequest = new HttpRequestMessage(HttpMethod.Get, pollUrl);
                pollRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", replicateApiKey);
                var pollResponse = await httpClient.SendAsync(pollRequest);
                if (!pollResponse.IsSuccessStatusCode)
                {
                    var pollError = await pollResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Replicate API polling failed: {pollResponse.StatusCode} - {pollError}");
                }
                var pollJson = await pollResponse.Content.ReadAsStringAsync();
                prediction = JsonSerializer.Deserialize<ReplicatePredictionResponse>(pollJson);

                if (prediction == null)
                {
                    throw new Exception("Failed to deserialize Replicate API polling response.");
                }
            }

            if (prediction.Status == ReplicatePredictionStatus.Succeeded && !string.IsNullOrEmpty(outputUrl))
            {
                return outputUrl;
            }
            else
            {
                if (errorMsg.Contains("output was flagged as sensitive"))
                {
                    throw new FailedToGenerateImageException($"Failed to generate an image with model '{model}' because of an inappropriate prompt", prompt, model);
                }
                else
                {
                    throw new FailedToGenerateImageException($"Replicate '{model}' API failed with error '{errorMsg}'", prompt, model);
                }
            }
        }
    }
}