using DarkLoop.Azure.Functions.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using System.Net;

namespace MTG.CardGenerator
{
    public class GetMagicCardGenerationStatusFunction
    {
        [Function("GetMagicCardGenerationStatus")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, [DurableClient(TaskHub = "%DurableTaskHubName%")] DurableTaskClient client, FunctionContext context)
        {
            var log = context.GetLogger<GetMagicCardGenerationStatusFunction>();

            var instanceId = req.GetQueryParam("instanceId");
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "instanceId parameter is required");
            }

            var instance = await client.GetInstanceAsync(instanceId);
            if (instance == null)
            {
                return await req.CreateErrorResponse(HttpStatusCode.NotFound, $"No orchestration instance found with ID '{instanceId}'");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            object statusResponse = new
            {
                id = instance.InstanceId,
                runtimeStatus = instance.RuntimeStatus.ToString(),
            };

            if (instance.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
            {
                instance = await client.GetInstanceAsync(instanceId, getInputsAndOutputs: true);

                statusResponse = new
                {
                    id = instance.InstanceId,
                    runtimeStatus = instance.RuntimeStatus.ToString(),
                    customStatus = instance.SerializedCustomStatus,
                    output = instance.ReadOutputAs<string>(),
                };
            }
            await response.WriteAsJsonAsync(statusResponse);
            return response;
        }
    }
}
