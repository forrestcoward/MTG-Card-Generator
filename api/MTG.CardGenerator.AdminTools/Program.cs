using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.CosmosClients;
using Newtonsoft.Json;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.ClearProviders();
    builder.AddSimpleConsole(options =>
    {
        options.IncludeScopes = true;
        options.SingleLine = true;
        options.TimestampFormat = "hh:mm:ss ";
    });
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddConsole();
});

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("local.settings.prod.json", optional: false, reloadOnChange: true)
    .Build();


foreach (var variable in configuration.GetChildren())
{
    Environment.SetEnvironmentVariable(variable.Key, variable.Value);
}

var logger = loggerFactory.CreateLogger<Program>();
var usersClient = new UsersClient(logger);

var userSubject = "fd42ec51-676d-479a-bab4-b7e7b86887e8";
var user = await usersClient.GetUserRecord(userSubject);
Console.WriteLine(JsonConvert.SerializeObject(user, Formatting.Indented));

// await usersClient.UpdateAllowedFreeCardGenerations(userSubject, 50);
// user = await usersClient.GetUserRecord(userSubject);
// Console.WriteLine(JsonConvert.SerializeObject(user, Formatting.Indented));
