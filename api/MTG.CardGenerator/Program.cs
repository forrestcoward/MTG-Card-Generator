using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace MTG.CardGenerator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWebApplication(builder =>
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

                    // Add special token handling to intercept the Open AI Card Generator GPT access code.
                    // This is a special case where we want to allow access without a valid JWT token.
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (context.Request.Headers.TryGetValue("Authorization", out var authValues))
                            {
                                var authHeader = authValues.FirstOrDefault() ?? string.Empty;
                                var token = Extensions.GetBearerTokenFromHeader(authHeader);

                                if (token != null && Extensions.IsCardGeneratorGPTAccessToken(token))
                                {
                                    // Create a ClaimsPrincipal for the special access code user
                                    var identity = new ClaimsIdentity(JwtBearerDefaults.AuthenticationScheme);
                                    identity.AddClaim(new Claim("sub", Extensions.GetCardGeneratorGPTUserSubject()));
                                    identity.AddClaim(new Claim("name", Extensions.GetCardGeneratorGPTUsername()));
                                    identity.AddClaim(new Claim("http://schemas.microsoft.com/identity/claims/scope", Constants.APIAuthorizationScope));
                                    context.Principal = new ClaimsPrincipal(identity);
                                    context.Success();
                                    return Task.CompletedTask;
                                }
                            }
                            return Task.CompletedTask;
                        }
                    };
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
