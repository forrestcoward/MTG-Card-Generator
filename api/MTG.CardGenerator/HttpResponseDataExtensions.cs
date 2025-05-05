using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace MTG.CardGenerator
{
    public static class HttpResponseDataExtensions
    {
        public static async Task<HttpResponseData> CreateJsonResponse<T>(this HttpRequestData req, HttpStatusCode statusCode, T responseObject)
        {
            var response = req.CreateResponse(statusCode);
            string jsonString = JsonConvert.SerializeObject(responseObject);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(jsonString);
            return response;
        }

        public static async Task<HttpResponseData> CreateJsonResponse(this HttpRequestData req, HttpStatusCode statusCode, string jsonContent)
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(jsonContent, Encoding.UTF8);
            return response;
        }

        public static async Task<HttpResponseData> CreateErrorResponse(this HttpRequestData req, HttpStatusCode statusCode, string errorMessage)
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            await response.WriteStringAsync(errorMessage);
            return response;
        }

        public static async Task<HttpResponseData> CreateOkResponse(this HttpRequestData req, string content)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            await response.WriteStringAsync(content);
            return response;
        }
    }
}
