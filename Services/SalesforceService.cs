using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CvManagementSystem.ViewModels;

namespace CvManagementSystem.Services;

public class SalesforceService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    private readonly JsonSerializerOptions _jsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true
        };

    // Salesforce rejects empty strings for typed fields (e.g. Email),
    // so null fields are omitted from record payloads entirely.
    private static readonly JsonSerializerOptions RecordJsonOptions =
        new()
        {
            DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public SalesforceService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }


    // =========================================================
    // CONFIGURATION
    // =========================================================

    private string BaseUrl
    {
        get
        {
            var value =
                _configuration["Salesforce:BaseUrl"];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    "Salesforce:BaseUrl is not configured.");
            }

            return value.TrimEnd('/');
        }
    }


    private string ClientId
    {
        get
        {
            var value =
                _configuration["Salesforce:ClientId"];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    "Salesforce:ClientId is not configured.");
            }

            return value;
        }
    }


    private string ClientSecret
    {
        get
        {
            var value =
                _configuration["Salesforce:ClientSecret"];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    "Salesforce:ClientSecret is not configured.");
            }

            return value;
        }
    }


    private string ApiVersion
    {
        get
        {
            var value =
                _configuration["Salesforce:ApiVersion"];

            return string.IsNullOrWhiteSpace(value)
                ? "v60.0"
                : value.Trim();
        }
    }


    private string CallbackUrl
    {
        get
        {
            var value =
                _configuration["Salesforce:CallbackUrl"];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    "Salesforce:CallbackUrl is not configured.");
            }

            return value;
        }
    }


    // The redirect_uri sent to Salesforce must match the Connected App
    // callback exactly; callers pass one built from the current request,
    // and Salesforce:CallbackUrl is only a fallback.
    private string ResolveRedirectUri(
        string? redirectUri)
    {
        return string.IsNullOrWhiteSpace(redirectUri)
            ? CallbackUrl
            : redirectUri.Trim();
    }


    // =========================================================
    // OAUTH STATE
    // =========================================================

    public string GenerateState()
    {
        return CreateRandomUrlSafeString(32);
    }


    // =========================================================
    // PKCE
    // =========================================================

    public string GenerateCodeVerifier()
    {
        return CreateRandomUrlSafeString(64);
    }


    public string GenerateCodeChallenge(
        string codeVerifier)
    {
        if (string.IsNullOrWhiteSpace(codeVerifier))
        {
            throw new ArgumentException(
                "Code verifier cannot be empty.",
                nameof(codeVerifier));
        }

        var hash =
            SHA256.HashData(
                Encoding.ASCII.GetBytes(
                    codeVerifier));

        return Base64UrlEncode(hash);
    }


    // =========================================================
    // AUTHORIZATION URL
    // =========================================================

    public string BuildAuthorizationUrl(
        string state,
        string codeChallenge,
        string? redirectUri = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException(
                "OAuth state cannot be empty.",
                nameof(state));
        }

        if (string.IsNullOrWhiteSpace(codeChallenge))
        {
            throw new ArgumentException(
                "PKCE code challenge cannot be empty.",
                nameof(codeChallenge));
        }

        var queryParameters =
            new Dictionary<string, string>
            {
                ["response_type"] =
                    "code",

                ["client_id"] =
                    ClientId,

                ["redirect_uri"] =
                    ResolveRedirectUri(redirectUri),

                ["scope"] =
                    "api refresh_token offline_access",

                ["state"] =
                    state,

                ["code_challenge"] =
                    codeChallenge,

                ["code_challenge_method"] =
                    "S256"
            };

        var queryString =
            string.Join(
                "&",
                queryParameters.Select(
                    x =>
                        $"{Uri.EscapeDataString(x.Key)}=" +
                        $"{Uri.EscapeDataString(x.Value)}"));

        return
            $"{BaseUrl}/services/oauth2/authorize?{queryString}";
    }


    // Backward-compatible alias.
    public string BuildAuthorizeUrl(
        string state,
        string codeChallenge)
    {
        return BuildAuthorizationUrl(
            state,
            codeChallenge);
    }


    // =========================================================
    // TOKEN EXCHANGE
    // =========================================================

    public async Task<SalesforceTokenResponse>
        ExchangeCodeAsync(
            string code,
            string codeVerifier)
    {
        return await ExchangeCodeAsync(
            code,
            codeVerifier,
            CancellationToken.None);
    }


    public async Task<SalesforceTokenResponse>
        ExchangeCodeAsync(
            string code,
            string codeVerifier,
            CancellationToken cancellationToken,
            string? redirectUri = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolvedRedirectUri =
            ResolveRedirectUri(redirectUri);

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Salesforce authorization code is empty.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(codeVerifier))
        {
            throw new ArgumentException(
                "Salesforce PKCE code verifier is empty.",
                nameof(codeVerifier));
        }

        var tokenUrl =
            $"{BaseUrl}/services/oauth2/token";

        var formData =
            new Dictionary<string, string>
            {
                ["grant_type"] =
                    "authorization_code",

                ["code"] =
                    code,

                ["client_id"] =
                    ClientId,

                ["client_secret"] =
                    ClientSecret,

                ["redirect_uri"] =
                    resolvedRedirectUri,

                ["code_verifier"] =
                    codeVerifier
            };

        Console.WriteLine();
        Console.WriteLine(
            "=================================================");

        Console.WriteLine(
            "SALESFORCE OAUTH TOKEN EXCHANGE");

        Console.WriteLine(
            "=================================================");

        Console.WriteLine(
            $"POST: {tokenUrl}");

        Console.WriteLine(
            $"Client ID present: {!string.IsNullOrWhiteSpace(ClientId)}");

        Console.WriteLine(
            $"Client Secret present: {!string.IsNullOrWhiteSpace(ClientSecret)}");

        Console.WriteLine(
            $"Callback URL: {resolvedRedirectUri}");

        Console.WriteLine(
            $"Authorization Code present: {!string.IsNullOrWhiteSpace(code)}");

        Console.WriteLine(
            $"Code Verifier present: {!string.IsNullOrWhiteSpace(codeVerifier)}");

        Console.WriteLine(
            "=================================================");

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                tokenUrl);

        request.Content =
            new FormUrlEncodedContent(
                formData);

        var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        var responseText =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        Console.WriteLine();
        Console.WriteLine(
            "=================================================");

        Console.WriteLine(
            "SALESFORCE TOKEN RESPONSE");

        Console.WriteLine(
            "=================================================");

        Console.WriteLine(
            $"HTTP Status: {(int)response.StatusCode} {response.StatusCode}");

        Console.WriteLine(
            $"Response: {RedactSensitiveValues(responseText)}");

        Console.WriteLine(
            "=================================================");

        Console.WriteLine();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                "Salesforce OAuth token request failed. " +
                $"HTTP {(int)response.StatusCode} " +
                $"{response.StatusCode}. " +
                $"Salesforce response: " +
                $"{RedactSensitiveValues(responseText)}");
        }

        SalesforceTokenResponse token;

        try
        {
            using var document =
                JsonDocument.Parse(responseText);

            var root =
                document.RootElement;

            var accessToken =
                GetJsonString(
                    root,
                    "access_token");

            var instanceUrl =
                GetJsonString(
                    root,
                    "instance_url");

            if (string.IsNullOrWhiteSpace(
                    accessToken))
            {
                throw new InvalidOperationException(
                    "Salesforce token response does not contain " +
                    "an access_token. " +
                    $"Response: {RedactSensitiveValues(responseText)}");
            }

            if (string.IsNullOrWhiteSpace(
                    instanceUrl))
            {
                throw new InvalidOperationException(
                    "Salesforce token response does not contain " +
                    "an instance_url. " +
                    $"Response: {RedactSensitiveValues(responseText)}");
            }

            token =
                new SalesforceTokenResponse
                {
                    AccessToken =
                        accessToken,

                    InstanceUrl =
                        instanceUrl,

                    Id =
                        GetJsonString(
                            root,
                            "id"),

                    TokenType =
                        GetJsonString(
                            root,
                            "token_type"),

                    IssuedAt =
                        GetJsonString(
                            root,
                            "issued_at"),

                    Signature =
                        GetJsonString(
                            root,
                            "signature"),

                    Scope =
                        GetJsonString(
                            root,
                            "scope")
                };
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "Salesforce returned invalid JSON from " +
                "the OAuth token endpoint. " +
                $"Response: {RedactSensitiveValues(responseText)}",
                exception);
        }

        Console.WriteLine(
            $"Salesforce OAuth token received successfully.");

        Console.WriteLine(
            $"Salesforce instance URL: {token.InstanceUrl}");

        return token;
    }


    // =========================================================
    // CREATE ACCOUNT + CONTACT
    // =========================================================

    // Both records go in one Composite API call with allOrNone, so either
    // the Account and its linked Contact are both created, or neither is
    // (no orphaned Account when the Contact is rejected).
    public async Task<SalesforceIntegrationResultViewModel>
        CreateAccountAndContactAsync(
            SalesforceTokenResponse token,
            SalesforceCreateAccountViewModel model,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(
                token.AccessToken))
        {
            throw new InvalidOperationException(
                "Salesforce access token is empty.");
        }

        if (string.IsNullOrWhiteSpace(
                token.InstanceUrl))
        {
            throw new InvalidOperationException(
                "Salesforce instance URL is empty.");
        }

        var accountName =
            Limit(
                string.IsNullOrWhiteSpace(model.AccountName)
                    ? BuildDefaultAccountName(model)
                    : model.AccountName,
                255)!;

        var sobjectsPath =
            $"/services/data/{ApiVersion}/sobjects";

        // Field limits follow the standard Salesforce field sizes; longer
        // values make Salesforce reject the record with STRING_TOO_LONG.
        var payload =
            new
            {
                allOrNone = true,

                compositeRequest = new object[]
                {
                    new
                    {
                        method = "POST",
                        url = $"{sobjectsPath}/Account",
                        referenceId = "newAccount",
                        body = new
                        {
                            Name =
                                accountName,

                            Phone =
                                Limit(model.Phone, 40),

                            Description =
                                Limit(BuildAccountDescription(model), 32000)
                        }
                    },
                    new
                    {
                        method = "POST",
                        url = $"{sobjectsPath}/Contact",
                        referenceId = "newContact",
                        body = new
                        {
                            FirstName =
                                Limit(model.FirstName, 40),

                            LastName =
                                Limit(model.LastName, 80) ?? "User",

                            Email =
                                CleanEmail(Limit(model.Email, 80)),

                            Phone =
                                Limit(model.Phone, 40),

                            Title =
                                Limit(model.JobTitle, 128),

                            Department =
                                Limit(model.Department, 80),

                            Description =
                                Limit(BuildContactDescription(model), 32000),

                            AccountId =
                                "@{newAccount.id}"
                        }
                    }
                }
            };

        var url =
            $"{token.InstanceUrl.TrimEnd('/')}" +
            $"/services/data/{ApiVersion}/composite";

        Console.WriteLine(
            "=================================================");

        Console.WriteLine(
            "SALESFORCE ACCOUNT + CONTACT CREATE (COMPOSITE)");

        Console.WriteLine(
            $"POST: {url}");

        Console.WriteLine(
            "=================================================");

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                url);

        AddBearerToken(
            request,
            token.AccessToken);

        // Serialize with the runtime types so the nested anonymous
        // bodies are written in full, with null fields omitted.
        request.Content =
            new StringContent(
                JsonSerializer.Serialize<object>(
                    payload,
                    RecordJsonOptions),
                Encoding.UTF8,
                "application/json");

        var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        var responseText =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        Console.WriteLine(
            $"Composite HTTP Status: " +
            $"{(int)response.StatusCode} " +
            $"{response.StatusCode}");

        Console.WriteLine(
            $"Composite response: {responseText}");

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                "Salesforce rejected the request " +
                $"(HTTP {(int)response.StatusCode}). " +
                DescribeErrors(responseText));
        }

        var (accountId, contactId) =
            ParseCompositeResponse(responseText);

        return new SalesforceIntegrationResultViewModel
        {
            AccountId =
                accountId,

            ContactId =
                contactId,

            AccountUrl =
                BuildLightningUrl(
                    token.InstanceUrl,
                    accountId),

            ContactUrl =
                BuildLightningUrl(
                    token.InstanceUrl,
                    contactId),

            DisplayName =
                accountName
        };
    }


    private static (string AccountId, string ContactId)
        ParseCompositeResponse(
            string responseText)
    {
        var ids =
            new Dictionary<string, string>();

        var failures =
            new List<string>();

        try
        {
            using var document =
                JsonDocument.Parse(responseText);

            foreach (var item in document.RootElement
                         .GetProperty("compositeResponse")
                         .EnumerateArray())
            {
                var referenceId =
                    GetJsonString(item, "referenceId");

                var objectName =
                    referenceId == "newAccount"
                        ? "Account"
                        : "Contact";

                var status =
                    item.TryGetProperty(
                        "httpStatusCode",
                        out var statusProperty)
                        ? statusProperty.GetInt32()
                        : 0;

                var body =
                    item.GetProperty("body");

                if (status is >= 200 and < 300 &&
                    body.ValueKind == JsonValueKind.Object)
                {
                    ids[referenceId] =
                        GetJsonString(body, "id");

                    continue;
                }

                var message =
                    DescribeErrorElement(body);

                // With allOrNone, the records that did not fail are
                // reported as PROCESSING_HALTED; only the cause matters.
                if (!message.Contains("PROCESSING_HALTED"))
                {
                    failures.Add($"{objectName}: {message}");
                }
            }
        }
        catch (Exception exception)
            when (exception is JsonException
                      or KeyNotFoundException
                      or InvalidOperationException)
        {
            throw new InvalidOperationException(
                "Salesforce returned an unexpected response. " +
                $"Response: {responseText}",
                exception);
        }

        if (failures.Count > 0 ||
            !ids.TryGetValue("newAccount", out var accountId) ||
            !ids.TryGetValue("newContact", out var contactId) ||
            string.IsNullOrWhiteSpace(accountId) ||
            string.IsNullOrWhiteSpace(contactId))
        {
            throw new InvalidOperationException(
                "Salesforce did not create the records, " +
                "so nothing was saved. " +
                (failures.Count > 0
                    ? string.Join(" ", failures)
                    : $"Response: {responseText}"));
        }

        return (accountId, contactId);
    }


    private static string
        DescribeErrors(
            string responseText)
    {
        try
        {
            using var document =
                JsonDocument.Parse(responseText);

            return DescribeErrorElement(
                document.RootElement);
        }
        catch (JsonException)
        {
            return responseText;
        }
    }


    // Salesforce errors are arrays of { errorCode, message, fields }.
    private static string
        DescribeErrorElement(
            JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return element.ToString();
        }

        return string.Join(
            " ",
            element.EnumerateArray()
                .Select(error =>
                {
                    var code =
                        GetJsonString(error, "errorCode");

                    var message =
                        GetJsonString(error, "message");

                    var fields =
                        error.TryGetProperty(
                            "fields",
                            out var fieldsProperty) &&
                        fieldsProperty.ValueKind == JsonValueKind.Array &&
                        fieldsProperty.GetArrayLength() > 0
                            ? $" (fields: {string.Join(", ", fieldsProperty.EnumerateArray())})"
                            : string.Empty;

                    return $"{code}: {message}{fields}";
                }));
    }


    private static string?
        Limit(
            string? value,
            int maxLength)
    {
        var cleaned =
            CleanNullable(value);

        return cleaned == null || cleaned.Length <= maxLength
            ? cleaned
            : cleaned[..maxLength];
    }

    // =========================================================
    // DESCRIPTION
    // =========================================================

    private static string
        BuildAccountDescription(
            SalesforceCreateAccountViewModel model)
    {
        var parts =
            new List<string>();

        if (!string.IsNullOrWhiteSpace(
                model.Location))
        {
            parts.Add(
                $"Location: {model.Location.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(
                model.PersonalPhotoUrl))
        {
            parts.Add(
                $"Personal Photo URL: " +
                $"{model.PersonalPhotoUrl.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(
                model.AdditionalNotes))
        {
            parts.Add(
                $"Additional Notes: " +
                $"{model.AdditionalNotes.Trim()}");
        }

        return string.Join(
            Environment.NewLine,
            parts);
    }


    private static string
        BuildContactDescription(
            SalesforceCreateAccountViewModel model)
    {
        var parts =
            new List<string>();

        if (!string.IsNullOrWhiteSpace(
                model.Location))
        {
            parts.Add(
                $"Location: {model.Location.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(
                model.PersonalPhotoUrl))
        {
            parts.Add(
                $"Personal Photo URL: " +
                $"{model.PersonalPhotoUrl.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(
                model.AdditionalNotes))
        {
            parts.Add(
                $"Additional Notes: " +
                $"{model.AdditionalNotes.Trim()}");
        }

        return string.Join(
            Environment.NewLine,
            parts);
    }


    private static string
        BuildDefaultAccountName(
            SalesforceCreateAccountViewModel model)
    {
        var name =
            $"{model.FirstName} {model.LastName}"
                .Trim();

        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        if (!string.IsNullOrWhiteSpace(
                model.Email))
        {
            return model.Email.Trim();
        }

        return "CV Management User";
    }


    // =========================================================
    // HTTP HELPERS
    // =========================================================

    private static void
        AddBearerToken(
            HttpRequestMessage request,
            string accessToken)
    {
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Headers.TryAddWithoutValidation(
                "Sforce-Duplicate-Rule-Header",
                "allowSave=true;includeRecordDetails=false;runAsCurrentUser=true");
    }


    private static string?
        CleanNullable(
            string? value)
    {
        return string.IsNullOrWhiteSpace(
            value)
            ? null
            : value.Trim();
    }


    // Salesforce returns 400 INVALID_EMAIL_ADDRESS for malformed
    // emails, so anything that isn't a valid address is omitted.
    private static string?
        CleanEmail(
            string? value)
    {
        var email =
            CleanNullable(value);

        if (email == null)
        {
            return null;
        }

        return new System.ComponentModel.DataAnnotations
                .EmailAddressAttribute()
                .IsValid(email)
               && System.Net.Mail.MailAddress.TryCreate(
                   email,
                   out var address)
               && address.Host.Contains('.')
            ? email
            : null;
    }


    private static string
        BuildLightningUrl(
            string instanceUrl,
            string recordId)
    {
        return
            $"{instanceUrl.TrimEnd('/')}" +
            $"/lightning/r/{recordId}/view";
    }


    private static string
        GetJsonString(
            JsonElement root,
            string propertyName)
    {
        if (!root.TryGetProperty(
                propertyName,
                out var property))
        {
            return string.Empty;
        }

        return property.ValueKind ==
               JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }


    // =========================================================
    // RANDOM / PKCE HELPERS
    // =========================================================

    private static string
        CreateRandomUrlSafeString(
            int byteCount)
    {
        var bytes =
            RandomNumberGenerator.GetBytes(
                byteCount);

        return Base64UrlEncode(
            bytes);
    }


    private static string
        Base64UrlEncode(
            byte[] bytes)
    {
        return Convert
            .ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }


    // =========================================================
    // SAFE LOGGING
    // =========================================================

    private static string
        RedactSensitiveValues(
            string responseText)
    {
        if (string.IsNullOrWhiteSpace(
                responseText))
        {
            return string.Empty;
        }

        try
        {
            using var document =
                JsonDocument.Parse(
                    responseText);

            using var stream =
                new MemoryStream();

            using (
                var writer =
                    new Utf8JsonWriter(
                        stream,
                        new JsonWriterOptions
                        {
                            Indented = false
                        }))
            {
                writer.WriteStartObject();

                foreach (
                    var property
                    in document.RootElement.EnumerateObject())
                {
                    if (
                        string.Equals(
                            property.Name,
                            "access_token",
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        string.Equals(
                            property.Name,
                            "refresh_token",
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        string.Equals(
                            property.Name,
                            "client_secret",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        writer.WriteString(
                            property.Name,
                            "[REDACTED]");

                        continue;
                    }

                    property.WriteTo(
                        writer);
                }

                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(
                stream.ToArray());
        }
        catch
        {
            return responseText;
        }
    }


    // =========================================================
    // RESPONSE MODELS
    // =========================================================

    public class SalesforceTokenResponse
    {
        public string AccessToken { get; set; } =
            string.Empty;

        public string InstanceUrl { get; set; } =
            string.Empty;

        public string Id { get; set; } =
            string.Empty;

        public string TokenType { get; set; } =
            string.Empty;

        public string IssuedAt { get; set; } =
            string.Empty;

        public string Signature { get; set; } =
            string.Empty;

        public string Scope { get; set; } =
            string.Empty;
    }


    public class SalesforceCreateRecordResponse
    {
        public string Id { get; set; } =
            string.Empty;

        public bool Success { get; set; }
    }
}