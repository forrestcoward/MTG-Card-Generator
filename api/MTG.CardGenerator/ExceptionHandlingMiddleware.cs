using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using System.Net;

namespace MTG.CardGenerator
{
    public class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
    {
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            // Check if the function has an HTTP trigger.
            var isHttpTrigger = context.FunctionDefinition.InputBindings
                .Any(binding => binding.Value.Type.Equals("httpTrigger", StringComparison.OrdinalIgnoreCase));

            if (!isHttpTrigger)
            {
                // Skip middleware for non-HTTP triggered functions (e.g., activity functions).
                await next(context);
                return;
            }

            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                var logger = context.GetLogger<ExceptionHandlingMiddleware>();
                logger.LogError(ex, $"An unhandled exception occurred in {context.FunctionDefinition.Name}");

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
