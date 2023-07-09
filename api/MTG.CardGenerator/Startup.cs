using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using MTG.CardGenerator;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[assembly: FunctionsStartup(typeof(Startup))]
namespace MTG.CardGenerator
{
    /// <summary>
    /// Adds start up configuration for all functions.
    /// This is used to add JWT bearer token validation to enable fine grained API authorization using DarkLoop.Azure.Functions.Authorize package (https://github.com/dark-loop/functions-authorize).
    /// This class was sampled after this: https://github.com/dark-loop/functions-authorize/blob/master/sample/Darkloop.Azure.Functions.Authorize.SampleFunctions.V4/Startup.cs.
    /// </summary>
    internal class Startup : FunctionsStartup
    {
        public IConfigurationRoot Configuration { get; private set; }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddFunctionsAuthorization();

            // Set ValidateBearerToken to false to disable bearer token validation.
            // Useful for local development.
            var validateBearerToken = Configuration[Constants.ValidateBearerToken] == null || bool.Parse(Configuration[Constants.ValidateBearerToken]);
            if (!validateBearerToken)
            {
                builder.Services.AddAuthorizationCore(options =>
                {
                    options.AddPolicy(Constants.APIAuthorizationScope, policy => 
                    {
                       // Always pass. Required because a policy must have a least one requirement.
                       policy.RequireAssertion(_ => true).Build();
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
            builder.Services
                .AddFunctionsAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.IncludeErrorDetails = true;

                    var validAuthority = Configuration[Constants.ValidJWTAuthority] ?? throw new ArgumentException($"Missing configuration value for '{Constants.ValidJWTAuthority}'.");
                    var validAudience = Configuration[Constants.ValidJWTAudience] ?? throw new ArgumentException($"Missing configuration value for '{Constants.ValidJWTAudience}'.");
                    var validIssuers = Configuration[Constants.ValidJWTIssuer] ?? throw new ArgumentException($"Missing configuration value for '{Constants.ValidJWTIssuer}'.");

                    options.Authority = validAuthority;
                    options.Audience = validAudience;
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidIssuers = new List<string> { validIssuers },
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = async x =>
                        {
                            // Inspect to view reason for failure.
                            var reason = x.Exception.Message;
                            // Send back anunauthorized response.
                            var body = "Unauthorized request";
                            var response = x.Response;
                            response.ContentType = "text/plain";
                            response.ContentLength = body.Length;
                            response.StatusCode = 401;
                            await response.WriteAsync(body);
                            await response.Body.FlushAsync();
                        },
                        OnMessageReceived = x =>
                        {
                            // x.Request.Headers.TryGetValue("Authorization", out var bearerToken);
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = x =>
                        {
                            return Task.CompletedTask;
                        },
                    };
                }, true);

            builder.Services.AddAuthorizationCore(options =>
            {
                options.AddPolicy(Constants.APIAuthorizationScope, policy =>
                {
                    policy.RequireClaim("http://schemas.microsoft.com/identity/claims/scope", Constants.APIAuthorizationScope);
                });
            });
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            builder.ConfigurationBuilder.AddUserSecrets<Startup>();
            Configuration = builder.ConfigurationBuilder.Build();
            base.ConfigureAppConfiguration(builder);
        }
    }
}