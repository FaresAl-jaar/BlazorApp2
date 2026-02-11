using BlazorApp2.Data;
using BlazorApp2.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp2.Services;

/// <summary>
/// Background service that automatically processes newly uploaded PDFs.
/// Triggers external OCR processing (Python Plumber) via HTTP call.
/// </summary>
public class AutoProcessingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoProcessingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public AutoProcessingService(
        IServiceProvider serviceProvider,
        ILogger<AutoProcessingService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutoProcessingService gestartet.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingDocumentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler im AutoProcessingService");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingDocumentsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var errorLogService = scope.ServiceProvider.GetRequiredService<IErrorLogService>();

        // Find documents that need processing
        var pendingDocuments = await context.PdfDocuments
            .Where(d => d.Status == DocumentStatus.Received)
            .Take(5) // Process max 5 at a time
            .ToListAsync(stoppingToken);

        if (!pendingDocuments.Any())
        {
            return;
        }

        _logger.LogInformation("Verarbeite {Count} Dokumente automatisch.", pendingDocuments.Count);

        foreach (var document in pendingDocuments)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                document.Status = DocumentStatus.Processing;
                await context.SaveChangesAsync(stoppingToken);

                // Call external OCR service (Python Plumber)
                var success = await CallOcrServiceAsync(document, stoppingToken);

                if (success)
                {
                    document.Status = DocumentStatus.Extracted;
                    document.ProcessedAt = DateTime.UtcNow;
                    _logger.LogInformation("Dokument {Id} erfolgreich verarbeitet.", document.Id);
                }
                else
                {
                    document.Status = DocumentStatus.PendingReview;
                    document.ProcessingError = "OCR-Service nicht erreichbar oder fehlgeschlagen.";
                    _logger.LogWarning("Dokument {Id} konnte nicht verarbeitet werden.", document.Id);
                }
            }
            catch (Exception ex)
            {
                document.Status = DocumentStatus.Error;
                document.ProcessingError = ex.Message;
                
                await errorLogService.LogErrorAsync(
                    "AutoProcessingService",
                    $"Fehler bei Dokument {document.Id}: {ex.Message}",
                    ex.StackTrace,
                    documentId: document.Id
                );
            }

            await context.SaveChangesAsync(stoppingToken);
        }
    }

    private async Task<bool> CallOcrServiceAsync(PdfDocument document, CancellationToken stoppingToken)
    {
        var ocrServiceUrl = _configuration["OcrService:Url"];
        
        if (string.IsNullOrEmpty(ocrServiceUrl))
        {
            // No OCR service configured - mark as pending review for manual processing
            _logger.LogWarning("OcrService:Url nicht konfiguriert. Dokument wird zur manuellen Prüfung markiert.");
            return false;
        }

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(document.FileContent), "file", document.FileName);
            content.Add(new StringContent(document.Id.ToString()), "document_id");

            var response = await httpClient.PostAsync($"{ocrServiceUrl}/process", content, stoppingToken);

            if (response.IsSuccessStatusCode)
            {
                var jsonResult = await response.Content.ReadAsStringAsync(stoppingToken);
                
                // Save extracted data
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var extractedData = new ExtractedData
                {
                    PdfDocumentId = document.Id,
                    JsonContent = jsonResult,
                    ExtractedAt = DateTime.UtcNow
                };
                
                context.ExtractedData.Add(extractedData);
                await context.SaveChangesAsync(stoppingToken);
                
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR-Service Aufruf fehlgeschlagen für Dokument {Id}", document.Id);
            return false;
        }
    }
}
