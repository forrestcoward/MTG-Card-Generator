using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MTG.CardGenerator
{
    public static class Extensions
    {
        private static readonly HttpClient client = new();

        public static JwtSecurityToken ReadJwtToken(this HttpRequest request, ILogger log = null)
        {
            try
            {
                var authHeader = request.Headers["Authorization"].ToString();
                var jwtToken = authHeader.Replace("Bearer ", string.Empty);
                var token = new JwtSecurityTokenHandler().ReadJwtToken(jwtToken);
                return token;
            }
            catch(Exception ex)
            {
                log?.LogError($"Error reading JWT token: {ex}");
                return null;
            }
        }
        
        public static string GetClaim(this HttpRequest req, string claimName, string defaultValue = "")
        {
            var jwtToken = req.ReadJwtToken();
            return jwtToken.GetClaim(claimName, defaultValue);
        }

        public static string GetClaim(this JwtSecurityToken jwtToken, string claimName, string defaultValue = "")
        {
            if (jwtToken == null)
            {
                return defaultValue;
            }

            var subClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == claimName);
            return subClaim == null ? defaultValue : subClaim.Value;
        }

        public static string GetSettingOrThrow(string settingName)
        {
            var setting = Environment.GetEnvironmentVariable(settingName);
            if (string.IsNullOrWhiteSpace(setting))
            {
                throw new ArgumentException($"Setting '{settingName}' is not set or empty.");
            }

            return setting;
        }

        public static string GetUserSubject(this HttpRequest req, ILogger log = null)
        {
            var validateBearerToken = Environment.GetEnvironmentVariable(Constants.ValidateBearerToken) == null || bool.Parse(Environment.GetEnvironmentVariable(Constants.ValidateBearerToken));

            JwtSecurityToken jwtToken = null;
            if (validateBearerToken)
            {
                jwtToken = req.ReadJwtToken(log);
            }

            if (jwtToken == null && validateBearerToken)
            {
                // This should never happen if the FunctionAuthorize attribute is present.
                return string.Empty;
            }

            var defaultUserSubject = Environment.GetEnvironmentVariable(Constants.DefaultUserSubject);
            if (string.IsNullOrWhiteSpace(defaultUserSubject))
            {
                defaultUserSubject = "Anonymous";
            }

            var userSubject = GetClaim(jwtToken, "sub", defaultValue: defaultUserSubject);
            if (userSubject == defaultUserSubject && validateBearerToken)
            {
                // This should never happen if the FunctionAuthorize attribute is present.
                return string.Empty;
            }

            return userSubject;
        }

        public static async Task<string> DownloadImageAsB64(string imageUrl)
        {
            var imageBytes = await client.GetByteArrayAsync(imageUrl);
            var base64Image = Convert.ToBase64String(imageBytes);
            return base64Image;
        }

        public static async Task<string> StoreImageInBlobAsync(string imageUrl, string blobStorageName, string blobStorageEndpoint, string blobStorageContainerName, string blobStorageAccessKey, ILogger log = null)
        {
            using var response = await client.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var blobName = $"{Guid.NewGuid()}.jpg";

            // Create a StorageSharedKeyCredential which we will use to build the BlobServiceClient.
            var sharedKeyCredential = new Azure.Storage.StorageSharedKeyCredential(blobStorageName, blobStorageAccessKey);

            // Create a BlobServiceClient that will authenticate through the StorageSharedKeyCredential.
            var blobServiceClient = new BlobServiceClient(new Uri(blobStorageEndpoint), sharedKeyCredential);

            // Get reference to the blobContainer and the blob to upload.
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(blobStorageContainerName);
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            await blobClient.UploadAsync(await response.Content.ReadAsStreamAsync());

            log?.LogInformation($"Image '{imageUrl}' uploaded to blob '{blobClient.Uri}'.");
            return blobClient.Uri.ToString();
        }

        public static Dictionary<string, string> GetNamedGroupsMatches(this Regex regex, string input)
        {
            var namedGroupMatches = new Dictionary<string, string>();

            var match = regex.Match(input);
            if (match.Success)
            {
                foreach (string groupName in regex.GetGroupNames())
                {
                    if (int.TryParse(groupName, out _)) // Skip non-named groups (represented by numbers)
                    {
                        continue;
                    }

                    namedGroupMatches[groupName] = match.Groups[groupName].Value;
                }
            }

            return namedGroupMatches;
        }

        public static IEnumerable<string> GetAllMatches(this Regex regex, string input)
        {
            var stringMatches = new List<string>();
            var matches = new List<Match>();
            matches.AddRange(regex.Matches(input));

            foreach (var match in matches)
            {
                if (match.Success)
                {
                    foreach (string groupName in regex.GetGroupNames())
                    {
                        if (int.TryParse(groupName, out _)) // Skip non-named groups (represented by numbers)
                        {
                            continue;
                        }

                        stringMatches.Add(match.Groups[groupName].Value);
                    }
                }
            }

            return stringMatches;
        }

        // Add '{' and '}' around legal mana character symbols. This is used to correct improperly formatted mana costs in card titles and oracle text.
        public static string BracketManaSymbols(this string input)
        {
            // TODO: Define this somewhere more globally. May need to add other symbols too, like 'X' and 'T'.
            var validManaSymbols = new[] { 'R', 'W', 'G', 'U', 'B', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            var result = new StringBuilder();
            foreach (char c in input)
            {
                if (!validManaSymbols.Contains(c))
                {
                    // Do not bracket non mana symbols, just add them back to the output.
                    result.Append(c);
                    continue;
                }

                result.Append('{');
                result.Append(c);
                result.Append('}');
            }
            return result.ToString();
        }

        public static bool IsNumericOrWhitespace(this string input)
        {
            return new Regex(@"^[0-9\s]*$").IsMatch(input);
        }

        public static bool IsBracketed(this string input)
        {
            if (input.Length % 3 != 0)
            {
                return false;
            }

            for (int i = 0; i < input.Length; i += 3)
            {
                if (input[i] != '{' || input[i + 2] != '}')
                {
                    return false;
                }
            }

            return true;
        }

        public static string FirstCharToLowerCase(this string str)
        {
            if (!string.IsNullOrEmpty(str) && char.IsUpper(str[0]))
                return str.Length == 1 ? char.ToLower(str[0]).ToString() : char.ToLower(str[0]) + str[1..];

            return str;
        }

        public static bool ContainsIgnoreCase(this string source, string toCheck)
        {
            return source?.IndexOf(toCheck, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string GetAsObfuscatedSecret(this string input, int x)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (x > input.Length / 2)
            {
                return string.Empty;
            }

            var firstX = input[..x];
            var lastX = input.Substring(input.Length - x, x);

            return $"{firstX}*********{lastX}";
        }
    }
}
