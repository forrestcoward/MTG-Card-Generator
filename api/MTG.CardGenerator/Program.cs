using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace MTG.CardGenerator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(builder =>
                {
                    // Add middleware for exception handling to not wrap all functions in try-catch blocks.
                    builder.UseFunctionsAuthorization();
                    builder.UseMiddleware<ExceptionHandlingMiddleware>();
                })
                .ConfigureServices(services =>
                {
                    ConfigureServices(services);
                })
                .Build();

            host.Run();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Add functions authorization.
            services.AddFunctionsAuthentication(JwtFunctionsBearerDefaults.AuthenticationScheme);

            // Set ValidateBearerToken to false to disable bearer token validation.
            // Useful for local development.
            var validateBearerToken = Environment.GetEnvironmentVariable(Constants.ValidateBearerToken) == null ||
                bool.Parse(Environment.GetEnvironmentVariable(Constants.ValidateBearerToken));

            if (!validateBearerToken)
            {
                services.AddFunctionsAuthorization(options =>
                {
                    options.AddPolicy(Constants.APIAuthorizationScope, policy =>
                    {
                        // Always pass. Required because a policy must have at least one requirement.
                        policy.RequireAssertion(_ => true);
                    });
                });

                return;
            }

            var isLocalDevelopment = Environment.GetEnvironmentVariable(Constants.AzureFunctionsEnvironment) == "Development";
            if (isLocalDevelopment)
            {
                // Show more detailed error messages for bearer token validation failures.
                IdentityModelEventSource.ShowPII = true;
            }

            // Add JWT bearer token validation middleware before every function gets called.
            services
                .AddFunctionsAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtFunctionsBearer(options =>
                {
                    options.IncludeErrorDetails = true;

                    var validAuthority = Environment.GetEnvironmentVariable(Constants.ValidJWTAuthority) ??
                        throw new ArgumentException($"Missing configuration value for '{Constants.ValidJWTAuthority}'.");
                    var validAudience = Environment.GetEnvironmentVariable(Constants.ValidJWTAudience) ??
                        throw new ArgumentException($"Missing configuration value for '{Constants.ValidJWTAudience}'.");
                    var validIssuers = Environment.GetEnvironmentVariable(Constants.ValidJWTIssuer) ??
                        throw new ArgumentException($"Missing configuration value for '{Constants.ValidJWTIssuer}'.");

                    options.Authority = validAuthority;
                    options.Audience = validAudience;

                    // Configure manual metadata endpoint retrieval to avoid dependency issues.
                    var httpClient = new HttpClient();
                    var metadataEndpoint = $"{validAuthority}/.well-known/openid-configuration";

                    // Configure manual metadata refresh for better control.
                    var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                        metadataEndpoint,
                        new OpenIdConnectConfigurationRetriever(),
                        new HttpDocumentRetriever(httpClient));

                    options.ConfigurationManager = configManager;

                    // Set required parameters
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuers = [validIssuers],
                        ValidateAudience = true,
                        ValidAudience = validAudience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(5)
                    };

                    // For local development, allow skipping certificate validation.
                    if (isLocalDevelopment)
                    {
                        options.BackchannelHttpHandler = new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                        };
                    }
                });

            services.AddFunctionsAuthorization(options =>
            {
                options.AddPolicy(Constants.APIAuthorizationScope, policy =>
                {
                    policy.RequireClaim("http://schemas.microsoft.com/identity/claims/scope", Constants.APIAuthorizationScope);
                });
            });
        }
    }
}
