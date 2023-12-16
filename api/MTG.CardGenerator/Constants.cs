namespace MTG.CardGenerator
{
    public class Constants
    {
        // Setting names.
        public const string ValidJWTAuthority = "ValidJWTAuthority";
        public const string ValidJWTAudience = "ValidJWTAudience";
        public const string ValidJWTIssuer = "ValidJWTIssuer";
        public const string ValidateBearerToken = "ValidateBearerToken";
        public const string OpenAIApiKey = "OPENAI_API_KEY";
        public const string OpenAIApiKeyLoggedIn = "OpenAIAPIKeyLoggedIn";
        public const string AzureFunctionsEnvironment = "AZURE_FUNCTIONS_ENVIRONMENT";
        public const string CosmosDBEndpointUrl = "CosmosDBEndpointUrl";
        public const string CosmosDBAccessKey = "CosmosDBAccessKey";
        public const string CosmosDBDatabaseId = "CosmosDBDatabaseId";
        public const string BlobStorageName = "BlobStorageName";
        public const string BlobStorageEndpoint = "BlobStorageEndpoint";
        public const string BlobStorageContainerName = "BlobStorageContainerName";
        public const string BlobStorageAccessKey = "BlobStorageAccessKey";
        public const string DefaultUserSubject = "DefaultUserSubject";
        public const string CognitiveSearchEndpoint = "CognitiveSearchEndpoint";
        public const string CognitiveSearchConnectionKey = "CognitiveSearchConnectionKey";
        public const string CognitiveSearchCardSearchIndexName = "CognitiveSearchCardSearchIndexName";
        // Constants.
        public const string APIAuthorizationScope = "generate.mtg.card";
        public const string CosmosDBCardsCollectionName = "generated-cards";
        public const string CosmosDBUsersCollectionName = "users";
        // Model constants.
        public const string Dalle3ModelName = "dall-e-3";
        public const string Dalle2ModelName = "dall-e-2";
    }
}
