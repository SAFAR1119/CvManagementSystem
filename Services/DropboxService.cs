using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CvManagementSystem.Services;

public class DropboxService : IDropboxService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public DropboxService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task UploadJsonAsync(string fileName, string json)
    {
        var accessToken = await GetAccessTokenAsync();

        var path = "/" + fileName.TrimStart('/');

        var apiArg = new
        {
            path,
            mode = "add",
            autorename = true,
            mute = false
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://content.dropboxapi.com/2/files/upload");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        request.Headers.TryAddWithoutValidation(
            "Dropbox-API-Arg",
            JsonSerializer.Serialize(apiArg));

        var jsonBytes = Encoding.UTF8.GetBytes(json);

        var content = new ByteArrayContent(jsonBytes);

        content.Headers.ContentType =
            new MediaTypeHeaderValue("application/octet-stream");

        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Dropbox upload failed: {(int)response.StatusCode} " +
                $"{response.StatusCode}. {responseBody}");
        }
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var appKey = _configuration["Dropbox:AppKey"];
        var appSecret = _configuration["Dropbox:AppSecret"];
        var refreshToken = _configuration["Dropbox:RefreshToken"];

        if (string.IsNullOrWhiteSpace(appKey) ||
            string.IsNullOrWhiteSpace(appSecret) ||
            string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new InvalidOperationException(
                "Dropbox OAuth configuration is incomplete.");
        }

        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = appKey,
            ["client_secret"] = appSecret
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.dropboxapi.com/oauth2/token");

        request.Content = new FormUrlEncodedContent(body);

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Dropbox token refresh failed: " +
                $"{(int)response.StatusCode} {response.StatusCode}. " +
                $"{responseBody}");
        }

        var tokenResponse =
            JsonSerializer.Deserialize<DropboxTokenResponse>(
                responseBody);

        if (tokenResponse == null ||
            string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException(
                "Dropbox did not return a valid access token.");
        }

        return tokenResponse.AccessToken;
    }

    private sealed class DropboxTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
}