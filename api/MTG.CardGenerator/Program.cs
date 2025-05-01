using DarkLoop.Azure.Functions.Authorization;
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
                .ConfigureFunctionsWebApplication(builder =>
                {
                    builder.UseFunctionsAuthorization();
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

    /// <summary>
    /// Default values used for Jwt Functions Bearer authentication.
    /// </summary>
    public class JwtFunctionsBearerDefaults
    {
        /// <summary>
        /// The default scheme used for Jwt Functions Bearer authentication.
        /// </summary>
        public const string AuthenticationScheme = "FunctionsBearer";
    }

    public static class FunctionsAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Adds the JWT "FunctionsBearer" scheme to the authentication configuration.
        /// </summary>
        /// <param name="builder">The current authentication builder.</param>
        /// <param name="configureOptions">An action configuring the JWT options for authentication. 
        /// <param name="authenticationScheme">The authentication scheme to use.</param>"
        /// <returns>A instance of the <see cref="FunctionsAuthenticationBuilder"/></returns>
        public static FunctionsAuthenticationBuilder AddJwtFunctionsBearer(
            this FunctionsAuthenticationBuilder builder, string authenticationScheme, Action<JwtBearerOptions> configureOptions)
        {
            if (authenticationScheme.Equals(JwtBearerDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Bearer scheme cannot be specified as it conflicts with Azure Functions built-in authentication", nameof(authenticationScheme));
            }

            builder.AddJwtBearer(authenticationScheme, configureOptions);

            return builder;
        }

        /// <summary>
        /// Adds the JWT "FunctionsBearer" scheme to the authentication configuration.
        /// </summary>
        /// <param name="builder">The current authentication builder.</param>
        /// <param name="configureOptions">An action configuring the JWT options for authentication. 
        /// <returns>A instance of the <see cref="FunctionsAuthenticationBuilder"/></returns>
        public static FunctionsAuthenticationBuilder AddJwtFunctionsBearer(
            this FunctionsAuthenticationBuilder builder, Action<JwtBearerOptions> configureOptions)
        {
            builder.AddJwtBearer(JwtFunctionsBearerDefaults.AuthenticationScheme, configureOptions);

            return builder;
        }

        /// <summary>
        /// Adds the JWT "FunctionsBearer" scheme to the authentication configuration.
        /// </summary>
        /// <param name="builder">The current authentication builder.</param>
        /// <returns>A instance of the <see cref="FunctionsAuthenticationBuilder"/></returns>
        public static FunctionsAuthenticationBuilder AddJwtFunctionsBearer(
            this FunctionsAuthenticationBuilder builder)
        {
            return builder.AddJwtFunctionsBearer(_ => { });
        }

        /// <summary>
        /// This is a no-op method to prevent conflicts with the built-in AddJwtBearer used by the functions host.
        /// </summary>
        /// <param name="builder">The current builder.</param>
        /// <param name="configureOptions">JWT options configuration.</param>
        [Obsolete("This method should not be called without specifying a name, as it would conflict with the framework's built-in setup. Use AddJwtFunctionsBearer instead, or specify a name other than 'Bearer' for scheme.", true)]
        public static FunctionsAuthenticationBuilder AddJwtBearer(this FunctionsAuthenticationBuilder builder, Action<JwtBearerOptions> configureOptions)
        {
            // This method should not be called without specifying a name, as it would conflict with the framework's built-in setup.
            return builder;
        }

        /// <summary>
        /// This is a no-op method to prevent conflicts with the built-in AddJwtBearer used by the functions host.
        /// </summary>
        /// <param name="builder">The current builder.</param>
        [Obsolete("This method should not be called without specifying a name, as it would conflict with the framework's built-in setup. Use AddJwtFunctionsBearer instead, or specify a name other than 'Bearer' for scheme.", true)]
        public static FunctionsAuthenticationBuilder AddJwtBearer(this FunctionsAuthenticationBuilder builder)
        {
            // This method should not be called without specifying a name, as it would conflict with the framework's built-in setup.
            return builder;
        }
    }
}
