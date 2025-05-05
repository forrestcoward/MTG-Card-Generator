using DarkLoop.Azure.Functions.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace MTG.CardGenerator
{
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
