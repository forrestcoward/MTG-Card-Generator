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
        public const string ReplicateApiKey = "REPLICATE_API_KEY";
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
        public const string WebsiteUrl = "WebsiteUrl";
        public const string OpenAICardGeneratorGPTAccessCode = "OpenAICardGeneratorGPTAccessCode";
        // Constants.
        public const string APIAuthorizationScope = "generate.mtg.card";
        public const string CosmosDBCardsCollectionName = "generated-cards";
        public const string CosmosDBUsersCollectionName = "users";

        public class ImageModels
        {
            public const string None = "none";
            public const string Dalle3ModelName = "dall-e-3";
            public const string Dalle2ModelName = "dall-e-2";
            public const string GoogleImagen3FastModelName = "imagen-3-fast";
            public const string GoogleImagen4FastModelName = "imagen-4-fast";
            public const string StableDiffusion35MediumModelName = "stable-diffusion-3.5-medium";
            public const string BlackForestLabsFluxSchnell = "black-forest-labs-flux-schnell";

            public static bool IsValidImageModel(string imageModel)
            {
                return
                    imageModel.Equals(None, StringComparison.OrdinalIgnoreCase) ||
                    imageModel.Equals(Dalle2ModelName, StringComparison.OrdinalIgnoreCase) ||
                    imageModel.Equals(Dalle3ModelName, StringComparison.OrdinalIgnoreCase) ||
                    imageModel.Equals(StableDiffusion35MediumModelName, StringComparison.OrdinalIgnoreCase) ||
                    imageModel.Equals(GoogleImagen3FastModelName, StringComparison.OrdinalIgnoreCase) ||
                    imageModel.Equals(GoogleImagen4FastModelName, StringComparison.OrdinalIgnoreCase) ||
                    imageModel.Equals(BlackForestLabsFluxSchnell, StringComparison.OrdinalIgnoreCase);
            }

            public static bool IsOpenAIImageModel(string imageModel)
            {
                return
                    imageModel.Equals(Constants.ImageModels.Dalle2ModelName, StringComparison.OrdinalIgnoreCase) ||
                    imageModel.Equals(Constants.ImageModels.Dalle3ModelName, StringComparison.OrdinalIgnoreCase);
            }

            public static bool IsReplicateImageModel(string imageModel)
            {
                return
                    imageModel.Equals(Constants.ImageModels.StableDiffusion35MediumModelName, StringComparison.OrdinalIgnoreCase) ||
                    imageModel.Equals(Constants.ImageModels.GoogleImagen3FastModelName, StringComparison.OrdinalIgnoreCase) ||
                    imageModel.Equals(Constants.ImageModels.GoogleImagen4FastModelName, StringComparison.OrdinalIgnoreCase) ||
                    imageModel.Equals(Constants.ImageModels.BlackForestLabsFluxSchnell, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
