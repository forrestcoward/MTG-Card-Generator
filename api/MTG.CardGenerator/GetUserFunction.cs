using DarkLoop.Azure.Functions.Authorize;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Spatial;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.CosmosClients;
using MTG.CardGenerator.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace MTG.CardGenerator
{
    public static class GetUserFunction
    {
        internal class GetuserFunctionResponse
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
        [FunctionName("GetUser")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                var userSubject = req.GetUserSubject(log);
                if (string.IsNullOrWhiteSpace(userSubject))
                {
                    return new UnauthorizedResult();
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

                var json = JsonConvert.SerializeObject(new GetuserFunctionResponse()
                {
                    UserName = user.UserName,
                    NumberOfCardsGenerated = user.NumberOfCardsGenerated,
                    NumberOfCardsRated = user.NumberOfCardsRated,
                    NumberOfFreeCardsGeneratedToday = user.NumberOfFreeCardsGeneratedToday,
                    AllowedFreeCardGenerationsPerDay = user.AllowedFreeCardGenerationsPerDay != -1 ? user.AllowedFreeCardGenerationsPerDay : GenerateMagicCardFunction.AllowedFreeGenerationsPerDay,
                });

                return new OkObjectResult(json);
            }
            catch (Exception ex)
            {
                log?.LogError(ex, ex.ToString());
                return new ContentResult
                {
                    StatusCode = 500,
                    Content = ex.Message,
                };
            }
        }
    }
}
