using BlazorApp2.Models.DTOs;

namespace BlazorApp2.Services;

public interface ILobsterApiService
{
    Task<SubmitDataResponse> SubmitDataAsync(string externalId, string jsonContent);
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
        
        // Add API Key header for Postman Mock Server
        var apiKey = _configuration["LobsterApi:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey) && apiKey != "YOUR_POSTMAN_API_KEY")
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", apiKey);
        }
    }

    public async Task<SubmitDataResponse> SubmitDataAsync(string externalId, string jsonContent)
    {
        try
        {
            var baseUrl = _configuration["LobsterApi:BaseUrl"];
            var endpoint = _configuration["LobsterApi:SubmitEndpoint"] ?? "/api/documents/upload";
            var fullUrl = $"{baseUrl}{endpoint}";

            _logger.LogInformation("Sende Daten an API: {Url}, ExternalId: {ExternalId}", fullUrl, externalId);

            var payload = new
            {
                externalId,
                data = jsonContent,
                submittedAt = DateTime.UtcNow
            };

            var response = await _httpClient.PostAsJsonAsync(fullUrl, payload);
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

            // Check for common Postman Mock errors
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
            
            // Try a simple GET
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _httpClient.GetAsync(checkUrl, cts.Token);
            
            _logger.LogInformation("API-Antwort: {StatusCode}", response.StatusCode);
            // Any response (even 404) means the server is reachable
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
