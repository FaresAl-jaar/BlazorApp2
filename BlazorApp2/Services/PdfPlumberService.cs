using System.Diagnostics;
using System.Text.Json;

namespace BlazorApp2.Services;

public interface IPdfPlumberService
{
    Task<PodReportResult> ExtractDataFromPdfAsync(byte[] pdfContent, string fileName);
}

public class PdfPlumberService : IPdfPlumberService
{
    private readonly ILogger<PdfPlumberService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _pythonPath;
    private readonly string _scriptPath;
    private readonly string _tempDir;
    private readonly string _configPath;

    public PdfPlumberService(IConfiguration configuration, ILogger<PdfPlumberService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Configurable paths
        _pythonPath = configuration["PdfPlumber:PythonPath"] ?? "python";
        var configScriptPath = configuration["PdfPlumber:ScriptPath"] ?? "Python/processor.py";
        
        // Handle relative paths - resolve from app base directory
        if (!Path.IsPathRooted(configScriptPath))
        {
            _scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configScriptPath);
        }
        else
        {
            _scriptPath = configScriptPath;
        }
        
        _tempDir = configuration["PdfPlumber:TempDir"] ?? Path.GetTempPath();
        
        // OCR Config path - same logic as OcrConfigService
        _configPath = Environment.GetEnvironmentVariable("OCR_CONFIG_PATH") ?? 
                      Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "ocr_config.json");
        
        _logger.LogInformation("PdfPlumber configured: Python={Python}, Script={Script}, Config={Config}", _pythonPath, _scriptPath, _configPath);
    }

    public async Task<PodReportResult> ExtractDataFromPdfAsync(byte[] pdfContent, string fileName)
    {
        var result = new PodReportResult();
        string? tempPdfPath = null;
        
        try
        {
            // Save PDF to temp file
            tempPdfPath = Path.Combine(_tempDir, $"temp_{Guid.NewGuid():N}.pdf");
            await File.WriteAllBytesAsync(tempPdfPath, pdfContent);
            
            _logger.LogInformation("Processing PDF: {FileName}, TempPath: {TempPath}", fileName, tempPdfPath);




            // Check if script exists
            if (!File.Exists(_scriptPath))
            {
                _logger.LogWarning("Python script not found at {Path}", _scriptPath);
                result.Error = $"Python script not found at {_scriptPath}";
                result.RawJson = CreateDefaultJson(fileName);
                return result;
            }

            // Run Python script - pass original filename as second argument
            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = $"\"{_scriptPath}\" \"{tempPdfPath}\" \"{fileName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };
            
            // Pass the OCR config path and ensure UTF-8 encoding via environment variables
            startInfo.EnvironmentVariables["OCR_CONFIG_PATH"] = _configPath;
            startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            startInfo.EnvironmentVariables["PYTHONUTF8"] = "1";

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            // Wait with timeout
            var completed = process.WaitForExit(60000); // 60 seconds timeout
            
            if (!completed)
            {
                process.Kill();
                result.Error = "Python script timeout after 60 seconds";
                result.RawJson = CreateDefaultJson(fileName);
                return result;
            }

            var output = await outputTask;
            var error = await errorTask;

            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.LogWarning("Python stderr: {Error}", error);
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                result.Error = "Python script returned no output";
                result.RawJson = CreateDefaultJson(fileName);
                return result;
            }

            // Validate JSON
            try
            {
                using var doc = JsonDocument.Parse(output);
                result.RawJson = output;
                result.Success = true;
                
                // Extract key fields for logging
                if (doc.RootElement.TryGetProperty("Fahrer", out var fahrer))
                {
                    result.Fahrer = fahrer.GetString();
                }
                if (doc.RootElement.TryGetProperty("Fahrzeug", out var fahrzeug))
                {
                    result.Fahrzeug = fahrzeug.GetString();
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON from Python script");
                result.Error = $"Invalid JSON: {ex.Message}";
                result.RawJson = CreateDefaultJson(fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PDF with PdfPlumber");
            result.Error = ex.Message;
            result.RawJson = CreateDefaultJson(fileName);
        }
        finally
        {
            // Cleanup temp file
            if (tempPdfPath != null && File.Exists(tempPdfPath))
            {
                try { File.Delete(tempPdfPath); }
                catch { /* ignore */ }
            }
        }

        return result;
    }

    private string CreateDefaultJson(string fileName)
    {
        var defaultData = new
        {
            FileName = fileName,
            ProcessedAt = DateTime.UtcNow.ToString("o"),
            Error = "PDF extraction not available - using default template",
            Mandant = "",
            Depot = "",
            Filiale = "",
            Tour = "",
            Fahrzeug = "",
            Anhaenger = "",
            Fahrer = "",
            Adresse = "",
            GeplanteLieferung = "",
            StoppInfos = new
            {
                GeplantAnkunft = "",
                TatsAnkunft = "",
                BeginnLieferung = "",
                EndeLieferung = "",
                Lieferzeit = "",
                Standzeit = "",
                LeistungPuenktlichkeit = "",
                Abfahrt = ""
            },
            Temperaturen = Array.Empty<object>(),
            Waren = Array.Empty<object>(),
            WarenGesamt = new
            {
                AnzArtikel = 0,
                MengeBestellt = 0,
                MengeGeliefert = 0,
                MengeErhalten = "",
                GesamtGewicht = "",
                GesPreis = ""
            },
            LeergutSummeSeite1 = new
            {
                Anlieferung = 0,
                Zurueck = 0,
                Differenz = 0,
                Bestaetigung = ""
            },
            LeergutDetails = Array.Empty<object>(),
            LeergutZusammenfassung = new
            {
                Geplant = 0,
                Anlieferung = 0,
                Abholung = 0,
                Differenz = 0
            },
            Abschluss = new
            {
                AnnahmeStatus = "",
                Kommentar = "",
                FahrerSignatur = "",
                Zeitstempel = ""
            }
        };

        return JsonSerializer.Serialize(defaultData, new JsonSerializerOptions { WriteIndented = true });
    }
}

public class PodReportResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string RawJson { get; set; } = "{}";
    public string? Fahrer { get; set; }
    public string? Fahrzeug { get; set; }
}
