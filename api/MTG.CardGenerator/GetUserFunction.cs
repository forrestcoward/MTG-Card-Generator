using DarkLoop.Azure.Functions.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.CosmosClients;
using MTG.CardGenerator.Models;
using Newtonsoft.Json;
using System.Net;

namespace MTG.CardGenerator
{
    public class GetUserFunction
    {
        internal class GetUserFunctionResponse
        {
            [JsonProperty("userName")]
            public string UserName { get; set; }
            [JsonProperty("numberOfCardsGenerated")]
            public long NumberOfCardsGenerated { get; set; }
            [JsonProperty("numberOfCardsRated")]
            public long NumberOfCardsRated { get; set; }
            [JsonProperty("numberOfFreeCardsGeneratedToday")]
            public long NumberOfFreeCardsGeneratedToday { get; set; }
            [JsonProperty("numberOfFreeCardsAllowedPerDay")]
            public long AllowedFreeCardGenerationsPerDay { get; set; }
        }

        // Gets the user information (or creates a new user if a record does not exist yet).
        [Function("GetUser")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, FunctionContext context)
        {
            var log = context.GetLogger<GetUserFunction>();

            var userSubject = req.GetUserSubject(log);
            if (string.IsNullOrWhiteSpace(userSubject))
            {
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            }

            var usersClient = new UsersClient(log);
            var user = await usersClient.GetUserRecord(userSubject);

            if (user == null)
            {
                var jwtToken = req.ReadJwtToken();
                var userName = Extensions.GetClaim(jwtToken, "name", defaultValue: "Anonymous");
                user = await usersClient.AddItemToContainerAsync(new User()
                {
                    UserName = userName,
                    UserSubject = userSubject,
                });

                log.LogInformation($"Created user record for user '{userSubject}' in database.");
            }

            var json = JsonConvert.SerializeObject(new GetUserFunctionResponse()
            {
                UserName = user.UserName,
                NumberOfCardsGenerated = user.NumberOfCardsGenerated,
                NumberOfCardsRated = user.NumberOfCardsRated,
                NumberOfFreeCardsGeneratedToday = user.NumberOfFreeCardsGeneratedToday,
                AllowedFreeCardGenerationsPerDay = user.AllowedFreeCardGenerationsPerDay != -1 ? user.AllowedFreeCardGenerationsPerDay : GenerateMagicCardFunction.AllowedFreeGenerationsPerDay,
            });

            return await req.CreateJsonResponse(HttpStatusCode.OK, json);
        }
    }
}
