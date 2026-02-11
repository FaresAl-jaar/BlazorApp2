using System.Text.Json;

namespace BlazorApp2.Services;

public interface IOcrConfigService
{
    Task<string> GetConfigJsonAsync();
    Task SaveConfigJsonAsync(string json);
    Task ResetToDefaultAsync();
    string GetConfigPath();
}

public class OcrConfigService : IOcrConfigService
{
    private readonly string _configPath;
    private readonly ILogger<OcrConfigService> _logger;
    
    // WICHTIG: Keine automatischen Klassen-Konvertierungen mehr, um Datenverlust zu vermeiden!
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public OcrConfigService(IConfiguration configuration, ILogger<OcrConfigService> logger)
    {
        _logger = logger;
        _configPath = Environment.GetEnvironmentVariable("OCR_CONFIG_PATH") ?? 
                      Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "ocr_config.json");
        
        var dir = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    public string GetConfigPath() => _configPath;

    public async Task<string> GetConfigJsonAsync()
    {
        if (!File.Exists(_configPath)) return await GetDefaultJson();
        return await File.ReadAllTextAsync(_configPath);
    }

    public async Task SaveConfigJsonAsync(string json)
    {
        try 
        { 
            // Nur validieren, ob es echtes JSON ist
            using var doc = JsonDocument.Parse(json); 
            await File.WriteAllTextAsync(_configPath, json);
        }
        catch (JsonException ex) 
        { 
            throw new ArgumentException($"Ungültiges JSON-Format: {ex.Message}"); 
        }
    }

    public async Task ResetToDefaultAsync()
    {
        var json = await GetDefaultJson();
        await File.WriteAllTextAsync(_configPath, json);
    }

    private Task<string> GetDefaultJson()
    {
        // Format passend zum Python-Skript processor.py
        var fullConfig = new
        {
            General = new
            {
                DateTimePattern = "(\\d{2}\\.\\d{2}\\.\\d{2}|\\d{4}/\\d{2}/\\d{2})\\s*,\\s*\\d{2}:\\d{2}",
                DateFormatList = new[] { "%d.%m.%y, %H:%M", "%Y/%m/%d, %H:%M", "%d.%m.%Y, %H:%M", "%Y-%m-%d %H:%M" },
                ZipCodePattern = "\\b\\d{4,5}\\b"
            },
            Vehicle = new
            {
                PlatePatterns = new[]
                {
                    "\\b([A-Z0-9]{3,10}\\s*\\([A-Z]{1,3}-[A-Z0-9]+\\s*\\d{0,5}[A-Z]?\\))",
                    "([A-Z]{1,3}-[A-Z0-9]+\\s*\\d{0,5}[A-Z]?)\\b"
                },
                Keywords = new[] { "Fahrzeug", "Pojazd" },
                TrailerKeywords = new[] { "Anhänger", "Przyczepa" }
            },
            Driver = new
            {
                Keywords = new[] { "Fahrer", "Kierowca" },
                IgnoreList = new[] { "PARK", "ALMSTRASSE", "FILIALE", "LEERGUT", "GESAMT", "ZUSAMMENFASSUNG", "FILIALLEITER" },
                NamePattern = "\\b([A-Za-zÄÖÜäöüß]{3,}\\s+[A-Za-zÄÖÜäöüß]{3,})"
            },
            Address = new
            {
                Keywords = new[] { "Adresse", "Adres", "Lieferanschrift" },
                MainNotePattern = "Haupt\\s+Lieferschein\\s*[\\r\\n]+([^\\r\\n]+\\d{4,5})"
            },
            Timestamps = new
            {
                Labels = new
                {
                    GeplanteLieferung = new[] { "Geplante\\s+Lieferung", "Planowana\\s+dostawa" },
                    GeplantAnkunft = new[] { "Geplant\\s+Ankunft" },
                    TatsAnkunft = new[] { "Tats\\.?\\s*Ankunft" },
                    BeginnLieferung = new[] { "Beginn\\s+Lieferung" },
                    EndeLieferung = new[] { "Ende\\s+Lieferung" },
                    Abfahrt = new[] { "Abfahrt" }
                },
                PunctualityPatterns = new[]
                {
                    "(Zu\\s*früh)\\s*[\\(\\[]?(\\d{2}:\\d{2})[\\) \\]]?",
                    "(Verspätet)\\s*[\\(\\[]?(\\d{2}:\\d{2})[\\) \\]]?",
                    "(Spät)\\s*[\\(\\[]?(\\d{2}:\\d{2})[\\) \\]]?",
                    "(Pünktlich)\\s*[\\(\\[]?(\\d{2}:\\d{2})[\\) \\]]?"
                }
            },
            Temperature = new
            {
                ChamberCodes = new[] { "FR", "TK" },
                RegexPattern = "\\b(FR|TK)\\s+(-?[0-9.,]+)\\s*\\u00b0?r?\\s*C"
            },
            Goods = new
            {
                TablePattern = "([0-9]{6,})\\s+([0-9]+)\\s+([0-9]+)\\s+([0-9]+)\\s+([0-9]+[.,][0-9]*)\\s+([0-9.,-]+)\\s+(?:.*?)?([0-9]{1,3}[.,][0-9]{3})\\s+([0-9.,]+)",
                TotalPattern = "(?is)Gesamt\\s+([0-9]+)\\s+([0-9]+)\\s+([0-9]+)\\s+([0-9.,]+)\\s+([0-9.,-]+)\\s+([0-9.,]+)\\s+([0-9.,]+)"
            },
            Empties = new
            {
                Keywords = new[] { "Leergut", "Opakowania" },
                CollectionPattern = "Geplante\\s+Abholung",
                SummaryPattern = "(?is)Zusammenfassung\\s+(.*)"
            },
            Conclusion = new
            {
                SignatureKeywords = new[] { "Filialleiter", "Kierownik" },
                IgnoreSignatureContent = new[] { "filiale", "leergut", "gesamt", "zusammenfassung", "filialleiter", "helpdesk", "telefon", "datum", "unterschrift", "annahmebereitschaft", "kommentar", "haftungsausschluss", "geliefert", "an", "haupt", "lieferschein", "ware", "wurde", "auf", "wunsch", "des", "tür" }
            }
        };

        return Task.FromResult(JsonSerializer.Serialize(fullConfig, JsonOptions));
    }
}
