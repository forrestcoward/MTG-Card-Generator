using DarkLoop.Azure.Functions.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Net;

namespace MTG.CardGenerator
{
    public class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
    {
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                var logger = context.GetLogger<ExceptionHandlingMiddleware>();
                logger.LogError(ex, "An unhandled exception occurred.");

                // Try to access HttpRequestData from FunctionContext.Features
                var httpRequestData = await context.GetHttpRequestDataAsync();

                if (httpRequestData != null)
                {
                    var response = httpRequestData.CreateResponse(HttpStatusCode.InternalServerError);
                    await response.WriteStringAsync("An unexpected error occurred. Please try again later.");
                    context.GetInvocationResult().Value = response;
                }
            }
        }
    }
}
