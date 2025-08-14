using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace MTG.CardGenerator
{
    public class AuthenticateFunction
    {
        public class AuthenticateRequest
        {
            [JsonProperty("username")]
            public string Username { get; set; }

            [JsonProperty("password")]
            public string Password { get; set; }
        }

        public class AuthenticateResponse
        {
            [JsonProperty("accessToken")]
            public string AccessToken { get; set; }

            [JsonProperty("idToken")]
            public string IdToken { get; set; }

            [JsonProperty("refreshToken")]
            public string RefreshToken { get; set; }

            [JsonProperty("expiresIn")]
            public int ExpiresIn { get; set; }

            [JsonProperty("tokenType")]
            public string TokenType { get; set; } = "Bearer";
        }

        public class AuthenticateErrorResponse
        {
            [JsonProperty("error")]
            public string Error { get; set; }

            [JsonProperty("errorDescription")]
            public string ErrorDescription { get; set; }
        }

        public class AuthenticationResult
        {
            public AuthenticateResponse Response { get; set; }
            public bool IsSuccess { get; set; }
            public bool IsSystemError { get; set; }
            public string ErrorMessage { get; set; }
            public HttpStatusCode? HttpStatusCode { get; set; }
        }

        [Function("Authenticate")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, FunctionContext context)
        {
            var log = context.GetLogger<AuthenticateFunction>();
            try
            {
                string requestBody;
                using (var streamReader = new StreamReader(req.Body))
                {
                    requestBody = await streamReader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "invalid_request", "Request body is required");
                }

                AuthenticateRequest authRequest;
                try
                {
                    authRequest = JsonConvert.DeserializeObject<AuthenticateRequest>(requestBody);
                }
                catch (JsonException ex)
                {
                    log.LogError(ex, "Failed to parse authentication request");
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "invalid_request", "Invalid JSON format");
                }

                if (string.IsNullOrWhiteSpace(authRequest?.Username) || string.IsNullOrWhiteSpace(authRequest?.Password))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "invalid_request", "Fields 'username' and 'password' are required in request body");
                }

                log.LogInformation($"Authentication attempt for username: {authRequest.Username}");

                // Authenticate with Azure AD B2C using Resource Owner Password Credentials (ROPC) flow.
                var authResult = await AuthenticateWithB2C(authRequest.Username, authRequest.Password, log);

                if (!authResult.IsSuccess)
                {
                    if (authResult.IsSystemError)
                    {
                        // System error - return 503 Service Unavailable.
                        log.LogError($"Authentication system error: {authResult.ErrorMessage}");
                        return await CreateErrorResponse(req, HttpStatusCode.ServiceUnavailable, "service_unavailable", "Authentication service is temporarily unavailable. Please try again later.");
                    }
                    else
                    {
                        // Authentication failed - return 401 Unauthorized.
                        return await CreateErrorResponse(req, authResult.HttpStatusCode ?? HttpStatusCode.Unauthorized, "invalid_grant", authResult.ErrorMessage ?? "Invalid username or password");
                    }
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                var responseJson = JsonConvert.SerializeObject(authResult.Response);
                await response.WriteStringAsync(responseJson);
                log.LogInformation($"Successful authentication for username: {authRequest.Username}");
                return response;
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"There was an unexpected error during authentication: {ex.Message}");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "server_error", "An internal error occurred");
            }
        }

        private static async Task<AuthenticationResult> AuthenticateWithB2C(string username, string password, ILogger log)
        {
            try
            {
                using var httpClient = new HttpClient();
                var clientId = "1cc495e4-bd18-4bed-9c18-857689172c50";
                var policy = "B2C_1_ROPC";
                var tokenEndpoint = $"https://mtgcardgenerator.b2clogin.com/mtgcardgenerator.onmicrosoft.com/{policy}/oauth2/v2.0/token";
                var scope = "https://mtgcardgenerator.onmicrosoft.com/api/generate.mtg.card openid profile";

                var tokenRequestBody = new List<KeyValuePair<string, string>>
                {
                    new("grant_type", "password"),
                    new("client_id", clientId),
                    new("scope", scope),
                    new("username", username),
                    new("password", password)
                };

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
                {
                    Content = new FormUrlEncodedContent(tokenRequestBody)
                };

                var tokenResponse = await httpClient.SendAsync(tokenRequest);
                var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();

                log.LogInformation($"B2C token response status: {tokenResponse.StatusCode}");

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    log.LogWarning($"B2C authentication failed: {tokenResponseContent}");
                    // Parse B2C error response to determine if it is a credential issue or system issue.
                    return ParseB2CError(tokenResponse.StatusCode, tokenResponseContent);
                }

                dynamic tokenData = JsonConvert.DeserializeObject(tokenResponseContent);
                var authResponse = new AuthenticateResponse
                {
                    AccessToken = tokenData?.access_token,
                    IdToken = tokenData?.id_token,
                    RefreshToken = tokenData?.refresh_token,
                    ExpiresIn = tokenData?.expires_in ?? 3600,
                    TokenType = "Bearer"
                };

                return new AuthenticationResult
                {
                    IsSuccess = true,
                    Response = authResponse
                };
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Unexpected error during B2C authentication: {ex.Message}");
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    IsSystemError = true,
                    ErrorMessage = "There was an internal error talking to the authentication service."
                };
            }
        }

        private static AuthenticationResult ParseB2CError(HttpStatusCode statusCode, string responseContent)
        {
            try
            {
                dynamic errorData = JsonConvert.DeserializeObject(responseContent);
                string error = errorData?.error;
                string errorDescription = errorData?.error_description;

                // Check for credential-related errors and clean the message.
                if (error == "invalid_grant" ||
                    errorDescription?.Contains("AADB2C90225") == true || // Invalid username/password
                    errorDescription?.Contains("AADB2C90055") == true || // Invalid credentials
                    errorDescription?.Contains("Invalid username or password") == true ||
                    errorDescription?.Contains("username or password provided") == true)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        IsSystemError = false,
                        ErrorMessage = "Invalid username or password",
                        HttpStatusCode = HttpStatusCode.Unauthorized
                    };
                }

                // Check for account status errors.
                if (errorDescription?.Contains("AADB2C90023") == true || // Account locked/disabled
                    errorDescription?.Contains("account is locked") == true ||
                    errorDescription?.Contains("account is disabled") == true)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        IsSystemError = false,
                        ErrorMessage = "Your account is temporarily locked or disabled. Please contact support.",
                        HttpStatusCode = HttpStatusCode.Unauthorized
                    };
                }

                // Check for system/service errors.
                if (statusCode == HttpStatusCode.InternalServerError ||
                    statusCode == HttpStatusCode.BadGateway ||
                    statusCode == HttpStatusCode.ServiceUnavailable ||
                    statusCode == HttpStatusCode.GatewayTimeout ||
                    error == "server_error" ||
                    errorDescription?.Contains("temporarily unavailable") == true)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        IsSystemError = true,
                        ErrorMessage = "Authentication service temporarily unavailable"
                    };
                }

                // Check for configuration/policy errors.
                if (errorDescription?.Contains("AADB2C90108") == true || // Policy configuration error
                    errorDescription?.Contains("AADB2C90205") == true) // Insufficient permissions
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        IsSystemError = true,
                        ErrorMessage = "Authentication service configuration error"
                    };
                }

                // Default to sanitized credential error for security.
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    IsSystemError = false,
                    ErrorMessage = "Invalid username or password",
                    HttpStatusCode = HttpStatusCode.Unauthorized
                };
            }
            catch
            {
                // If we cannot parse the error, assume it is a system issue.
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    IsSystemError = true,
                    ErrorMessage = "Authentication service error"
                };
            }
        }

        private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string error, string errorDescription)
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json");

            var errorResponse = new AuthenticateErrorResponse
            {
                Error = error,
                ErrorDescription = errorDescription
            };

            var errorJson = JsonConvert.SerializeObject(errorResponse);
            await response.WriteStringAsync(errorJson);
            return response;
        }
    }
}