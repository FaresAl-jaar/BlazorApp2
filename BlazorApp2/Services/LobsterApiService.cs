using BlazorApp2.Models.DTOs;

namespace BlazorApp2.Services;

public interface ILobsterApiService
{
    Task<SubmitDataResponse> SubmitDataAsync(string externalId, string jsonContent, string fileName);
    Task<bool> CheckConnectionAsync();
}

public class LobsterApiService : ILobsterApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LobsterApiService> _logger;

    public LobsterApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LobsterApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        
        var apiKey = _configuration["LobsterApi:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey) && apiKey != "MY_POSTMAN_API_KEY")
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", apiKey);
        }
    }

    public async Task<SubmitDataResponse> SubmitDataAsync(string externalId, string jsonContent, string fileName)
    {
        try
        {
            var baseUrl = _configuration["LobsterApi:BaseUrl"]?.TrimEnd('/');
            var endpoint = _configuration["LobsterApi:SubmitEndpoint"]?.TrimStart('/');
            var fullUrl = $"{baseUrl}/{endpoint}";

            // Create multipart content
            using var content = new MultipartFormDataContent();
            
            // Send as a form field named "file" (matches Lobster "Multipart FileKey: file")
            // using text/plain to ensure it's treated as a string content
            var fileContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            
            // We use the original filename but change extension to .json for the export
            var exportFileName = Path.ChangeExtension(fileName, ".json");
            content.Add(fileContent, "file", exportFileName);

            var response = await _httpClient.PostAsync(fullUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Daten erfolgreich an API gesendet: {ExternalId}", externalId);
                return new SubmitDataResponse
                {
                    Success = true,
                    Message = "Daten erfolgreich uebermittelt.",
                    SubmittedAt = DateTime.UtcNow
                };
            }

            // Detailed error message
            var errorMessage = $"API-Fehler {(int)response.StatusCode} ({response.StatusCode}): {responseContent}";
            _logger.LogWarning("Lobster API Fehler: {Error}", errorMessage);

            
            if (responseContent.Contains("mockRequestNotFoundError"))
            {
                errorMessage = "Mock API: Kein Example Response definiert. Bitte in Postman ein Example fuer diesen Request erstellen.";
            }
            else if (responseContent.Contains("invalidCredentialsError"))
            {
                errorMessage = "Mock API: Ungueltiger API-Key. Bitte x-api-key in appsettings.json pruefen.";
            }

            return new SubmitDataResponse
            {
                Success = false,
                Message = errorMessage
            };
        }
        catch (HttpRequestException ex)
        {
            var errorMsg = $"HTTP-Verbindungsfehler: {ex.Message}";
            _logger.LogError(ex, "HTTP-Fehler bei der Kommunikation mit Lobster API");
            return new SubmitDataResponse
            {
                Success = false,
                Message = errorMsg
            };
        }
        catch (Exception ex)
        {
            var errorMsg = $"Verbindungsfehler: {ex.Message}";
            _logger.LogError(ex, "Fehler bei der Kommunikation mit Lobster API");
            return new SubmitDataResponse
            {
                Success = false,
                Message = errorMsg
            };
        }
    }

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            var checkUrl = _configuration["LobsterApi:BaseUrl"] ?? "";
            
            _logger.LogInformation("Pruefe Verbindung zu: {Url}", checkUrl);
            
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.GetAsync(checkUrl, cts.Token);
            
            _logger.LogInformation("API-Antwort: {StatusCode}", response.StatusCode);
            
            return true;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("API-Verbindung: Timeout nach 10 Sekunden");
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("API-Verbindung fehlgeschlagen: {Error}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("API-Verbindungsfehler: {Error}", ex.Message);
            return false;
        }
    }
}
