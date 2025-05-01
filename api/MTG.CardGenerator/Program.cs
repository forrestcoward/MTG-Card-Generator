using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;

namespace MTG.CardGenerator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWebApplication(builder =>
                {
                    // builder.UseFunctionsAuthorization();
                })
                .ConfigureServices(services =>
                {
                    // ConfigureServices(services);
                })
                .Build();

            host.Run();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Add functions authorization.
            services.AddFunctionsAuthentication(JwtBearerDefaults.AuthenticationScheme);

            // Set ValidateBearerToken to false to disable bearer token validation.
            // Useful for local development.
            var validateBearerToken = Environment.GetEnvironmentVariable(Constants.ValidateBearerToken) == null || 
                bool.Parse(Environment.GetEnvironmentVariable(Constants.ValidateBearerToken));

            if (!validateBearerToken)
            {
                services.AddAuthorizationCore(options =>
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
                .AddJwtBearer(options =>
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
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidIssuers = [validIssuers],
                    };
                });

            services.AddAuthorizationCore(options =>
            {
                options.AddPolicy(Constants.APIAuthorizationScope, policy =>
                {
                    policy.RequireClaim("http://schemas.microsoft.com/identity/claims/scope", Constants.APIAuthorizationScope);
                });
            });
        }
    }
}
