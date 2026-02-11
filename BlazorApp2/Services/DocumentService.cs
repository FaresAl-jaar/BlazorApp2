using BlazorApp2.Data;
using BlazorApp2.Hubs;
using BlazorApp2.Models;
using BlazorApp2.Models.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace BlazorApp2.Services;


public interface IDocumentService
{
    Task<PdfUploadResponse> ReceiveDocumentAsync(PdfUploadRequest request);
    Task<List<DocumentListItemDto>> GetDocumentsAsync(DocumentStatus? statusFilter = null);
    Task<PdfDocument?> GetDocumentByIdAsync(int id);
    Task<byte[]?> GetPdfContentAsync(int id);
    Task<ExtractedData?> GetExtractedDataAsync(int documentId);
    Task<bool> SaveExtractedDataAsync(int documentId, string jsonContent, string? userId);
    Task<bool> UpdateDocumentStatusAsync(int id, DocumentStatus status);
    Task<SubmitDataResponse> SubmitToExternalApiAsync(int documentId);
    Task<bool> DeleteDocumentAsync(int id);
    Task<int> DeleteAllDocumentsAsync();
    Task<bool> CheckApiConnectionAsync();
    Task<bool> ClaimDocumentAsync(int documentId, string userId, string userName);
    Task<bool> UnclaimDocumentAsync(int documentId, string userId);
    Task<bool> CanUserEditDocumentAsync(int documentId, string userId);
}


public class DocumentService : IDocumentService
{
private readonly ApplicationDbContext _context;
private readonly ILobsterApiService _lobsterApi;
private readonly ILogger<DocumentService> _logger;
private readonly IHubContext<DocumentHub> _hubContext;
private readonly IErrorLogService _errorLogService;
private readonly IPdfPlumberService _pdfPlumber;
private readonly IConfiguration _configuration;

public DocumentService(
    ApplicationDbContext context,
    ILobsterApiService lobsterApi,
    ILogger<DocumentService> logger,
    IHubContext<DocumentHub> hubContext,
    IErrorLogService errorLogService,
    IPdfPlumberService pdfPlumber,
    IConfiguration configuration)
{
    _context = context;
    _lobsterApi = lobsterApi;
    _logger = logger;
    _hubContext = hubContext;
    _errorLogService = errorLogService;
    _pdfPlumber = pdfPlumber;
    _configuration = configuration;
}

private static string ComputeFileHash(byte[] fileContent)
{
    using var sha256 = SHA256.Create();
    var hash = sha256.ComputeHash(fileContent);
    return Convert.ToBase64String(hash);
}


    /// <summary>
    /// Creates default POD JSON template matching the Python processor format
    /// </summary>
    private static string CreateDefaultPodJson(string fileName)
    {
        // Parse filename for metadata: PODReport_M03_714019_101270380_6X503_20260115.pdf
        var parts = Path.GetFileNameWithoutExtension(fileName).Split('_');
        
        var pod = new
        {
            FileName = fileName,
            ProcessedAt = DateTime.UtcNow.ToString("o"),
            Mandant = parts.Length > 1 ? parts[1] : "",
            Depot = "",
            Filiale = parts.Length > 2 ? parts[2] : "",
            Tour = parts.Length > 4 ? parts[4] : "",
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

        return System.Text.Json.JsonSerializer.Serialize(pod, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }

    public async Task<PdfUploadResponse> ReceiveDocumentAsync(PdfUploadRequest request)
    {
        try
        {
            var fileContent = Convert.FromBase64String(request.Base64Content);
            var fileHash = ComputeFileHash(fileContent);

            // Check for duplicate by ExternalId
            var existingByExternalId = await _context.PdfDocuments
                .FirstOrDefaultAsync(d => d.ExternalId == request.ExternalId);


            if (existingByExternalId != null)
            {
                var errorMsg = $"Duplikat: ExternalId existiert bereits";
                await _errorLogService.LogWarningAsync("DocumentService.ReceiveDocument", errorMsg,
                    documentId: existingByExternalId.Id,
                    documentName: request.FileName);
                
                return new PdfUploadResponse
                {
                    Success = false,
                    Message = $"Dokument mit ExternalId '{request.ExternalId}' existiert bereits (ID: {existingByExternalId.Id}).",
                    ExternalId = request.ExternalId,
                    DocumentId = existingByExternalId.Id
                };
            }

            // Check for duplicate by file hash (same PDF content)
            var existingByHash = await _context.PdfDocuments
                .FirstOrDefaultAsync(d => d.FileHash == fileHash);

            if (existingByHash != null)
            {
                var errorMsg = $"Duplikat: Identische PDF-Datei existiert bereits";
                await _errorLogService.LogWarningAsync("DocumentService.ReceiveDocument", errorMsg,
                    documentId: existingByHash.Id,
                    documentName: existingByHash.FileName);
                
                return new PdfUploadResponse
                {
                    Success = false,
                    Message = $"Identische PDF-Datei existiert bereits (ID: {existingByHash.Id}, Name: {existingByHash.FileName}).",
                    ExternalId = existingByHash.ExternalId,
                    DocumentId = existingByHash.Id
                };
            }

            var document = new PdfDocument
            {
                ExternalId = request.ExternalId,
                FileName = request.FileName,
                FileContent = fileContent,
                FileSize = fileContent.Length,
                FileHash = fileHash,
                SourceSystem = request.SourceSystem,
                Status = DocumentStatus.Received
            };

            _context.PdfDocuments.Add(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Dokument empfangen: {FileName} (ID: {Id})", document.FileName, document.Id);

            // Try PdfPlumber extraction first, fallback to default template
            string jsonContent;
            var pdfPlumberEnabled = _configuration.GetValue<bool>("PdfPlumber:Enabled", false);
            
            if (pdfPlumberEnabled)
            {
                _logger.LogInformation("Starte PdfPlumber-Extraktion fuer {FileName}", document.FileName);
                var extractionResult = await _pdfPlumber.ExtractDataFromPdfAsync(fileContent, document.FileName);
                
                if (extractionResult.Success)
                {
                    jsonContent = extractionResult.RawJson;
                    _logger.LogInformation("PdfPlumber-Extraktion erfolgreich: {FileName}", document.FileName);
                }
                else
                {
                    _logger.LogWarning("PdfPlumber fehlgeschlagen: {Error}, verwende Default-Template", extractionResult.Error);
                    jsonContent = CreateDefaultPodJson(document.FileName);
                }
            }
            else
            {
                jsonContent = CreateDefaultPodJson(document.FileName);
            }
            
            var extractedData = new ExtractedData
            {
                PdfDocumentId = document.Id,
                JsonContent = jsonContent,
                ExtractedAt = DateTime.UtcNow,
                IsValidated = false,
                Version = 1
            };
            _context.ExtractedData.Add(extractedData);
            document.Status = DocumentStatus.PendingReview;
            await _context.SaveChangesAsync();

            // Notify all clients via SignalR
            await _hubContext.Clients.All.SendAsync("DocumentReceived", document.Id, document.FileName);


            return new PdfUploadResponse
            {
                Success = true,
                DocumentId = document.Id,
                ExternalId = document.ExternalId,
                Message = "Dokument erfolgreich empfangen."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Empfangen des Dokuments {FileName}", request.FileName);
            await _errorLogService.LogErrorAsync(
                "DocumentService.ReceiveDocument",
                $"Fehler beim Empfangen: {ex.Message}",
                ex.StackTrace,
                documentName: request.FileName,
                externalId: request.ExternalId
            );
            return new PdfUploadResponse
            {
                Success = false,
                Message = $"Fehler: {ex.Message}"
            };
        }
    }




    public async Task<List<DocumentListItemDto>> GetDocumentsAsync(DocumentStatus? statusFilter = null)
    {
        var query = _context.PdfDocuments.AsQueryable();

        if (statusFilter.HasValue)
        {
            query = query.Where(d => d.Status == statusFilter.Value);
        }

        return await query
            .OrderByDescending(d => d.ReceivedAt)
            .Select(d => new DocumentListItemDto
            {
                Id = d.Id,
                ExternalId = d.ExternalId,
                FileName = d.FileName,
                ReceivedAt = d.ReceivedAt,
                Status = d.Status,
                HasExtractedData = d.ExtractedDataEntries.Any(),
                ClaimedByUserId = d.ClaimedByUserId,
                ClaimedByUserName = d.ClaimedByUserName,
                ClaimedAt = d.ClaimedAt
            })
            .ToListAsync();
    }

    public async Task<PdfDocument?> GetDocumentByIdAsync(int id)
    {
        return await _context.PdfDocuments
            .Include(d => d.ExtractedDataEntries)
            .FirstOrDefaultAsync(d => d.Id == id);
    }


    public async Task<byte[]?> GetPdfContentAsync(int id)
    {
        var document = await _context.PdfDocuments.FindAsync(id);
        return document?.FileContent;
    }

    public async Task<ExtractedData?> GetExtractedDataAsync(int documentId)
    {
        return await _context.ExtractedData
            .Where(e => e.PdfDocumentId == documentId)
            .OrderByDescending(e => e.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> SaveExtractedDataAsync(int documentId, string jsonContent, string? userId)
    {
        var document = await _context.PdfDocuments.FindAsync(documentId);
        if (document == null) return false;

        var existingData = await GetExtractedDataAsync(documentId);
        var newVersion = (existingData?.Version ?? 0) + 1;

        var extractedData = new ExtractedData
        {
            PdfDocumentId = documentId,
            JsonContent = jsonContent,
            ModifiedBy = userId,
            LastModifiedAt = DateTime.UtcNow,
            Version = newVersion
        };

        _context.ExtractedData.Add(extractedData);
        
        document.Status = DocumentStatus.Reviewed;
        document.ProcessedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        
        // Notify clients about status change
        await _hubContext.Clients.All.SendAsync("DocumentStatusChanged", documentId, document.Status.ToString());
        
        return true;
    }

    public async Task<bool> UpdateDocumentStatusAsync(int id, DocumentStatus status)
    {
        var document = await _context.PdfDocuments.FindAsync(id);
        if (document == null) return false;

        document.Status = status;
        await _context.SaveChangesAsync();
        
        // Notify clients about status change
        await _hubContext.Clients.All.SendAsync("DocumentStatusChanged", id, status.ToString());
        
        return true;
    }

    public async Task<SubmitDataResponse> SubmitToExternalApiAsync(int documentId)
    {
        var extractedData = await GetExtractedDataAsync(documentId);
        if (extractedData == null)
        {
            return new SubmitDataResponse
            {
                Success = false,
                Message = "Keine extrahierten Daten gefunden."
            };
        }

        var document = await _context.PdfDocuments.FindAsync(documentId);
        if (document == null)
        {
            await _errorLogService.LogWarningAsync("DocumentService.SubmitToExternalApi", 
                $"Dokument mit ID {documentId} nicht gefunden.",
                documentId: documentId);
            return new SubmitDataResponse
            {
                Success = false,
                Message = "Dokument nicht gefunden."
            };
        }

        var result = await _lobsterApi.SubmitDataAsync(document.ExternalId, extractedData.JsonContent);

        if (result.Success)
        {
            document.Status = DocumentStatus.Submitted;
            await _context.SaveChangesAsync();
            
            // Notify clients about status change
            await _hubContext.Clients.All.SendAsync("DocumentStatusChanged", documentId, DocumentStatus.Submitted.ToString());
            
            // Only log to console, NOT to error log (success is not an error!)
            _logger.LogInformation("Dokument {Id} ({FileName}) erfolgreich an API uebermittelt", documentId, document.FileName);
        }
        else
        {
            await _errorLogService.LogErrorAsync("DocumentService.SubmitToExternalApi", 
                $"API-Uebermittlung fehlgeschlagen: {result.Message}",
                documentId: documentId,
                documentName: document.FileName,
                externalId: document.ExternalId);
        }

        return result;
    }



    public async Task<bool> DeleteDocumentAsync(int id)
    {
        var document = await _context.PdfDocuments
            .Include(d => d.ExtractedDataEntries)
            .FirstOrDefaultAsync(d => d.Id == id);
        
        if (document == null) return false;

        var fileName = document.FileName;
        var externalId = document.ExternalId;
        
        _context.PdfDocuments.Remove(document);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Dokument geloescht: {FileName} (ID: {Id})", fileName, id);
        
        await _errorLogService.LogInfoAsync("DocumentService.DeleteDocument", 
            $"Dokument geloescht",
            documentId: id,
            documentName: fileName);
        
        // Notify clients about deletion
        await _hubContext.Clients.All.SendAsync("DocumentDeleted", id);

        
        return true;
    }

    public async Task<int> DeleteAllDocumentsAsync()
    {
        var count = await _context.PdfDocuments.CountAsync();
        _context.ExtractedData.RemoveRange(_context.ExtractedData);
        _context.PdfDocuments.RemoveRange(_context.PdfDocuments);
        await _context.SaveChangesAsync();
        
        _logger.LogWarning("Alle {Count} Dokumente wurden gelöscht", count);
        return count;
    }

    public async Task<bool> CheckApiConnectionAsync()
    {
        return await _lobsterApi.CheckConnectionAsync();
    }

    public async Task<bool> ClaimDocumentAsync(int documentId, string userId, string userName)
    {
        var document = await _context.PdfDocuments.FindAsync(documentId);
        if (document == null) return false;
        
        // Check if already claimed by someone else
        if (!string.IsNullOrEmpty(document.ClaimedByUserId) && document.ClaimedByUserId != userId)
        {
            return false;
        }
        
        document.ClaimedByUserId = userId;
        document.ClaimedByUserName = userName;
        document.ClaimedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Dokument {Id} wurde von {UserName} übernommen", documentId, userName);
        await _hubContext.Clients.All.SendAsync("DocumentStatusChanged", documentId, "Claimed");
        
        return true;
    }

    public async Task<bool> UnclaimDocumentAsync(int documentId, string userId)
    {
        var document = await _context.PdfDocuments.FindAsync(documentId);
        if (document == null) return false;
        
        // Only the claimer or admin can unclaim
        if (document.ClaimedByUserId != userId)
        {
            return false;
        }
        
        var previousUser = document.ClaimedByUserName;
        document.ClaimedByUserId = null;
        document.ClaimedByUserName = null;
        document.ClaimedAt = null;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Dokument {Id} wurde von {UserName} freigegeben", documentId, previousUser);
        await _hubContext.Clients.All.SendAsync("DocumentStatusChanged", documentId, "Unclaimed");
        
        return true;
    }

    public async Task<bool> CanUserEditDocumentAsync(int documentId, string userId)
    {
        var document = await _context.PdfDocuments.FindAsync(documentId);
        if (document == null) return false;
        
        // Document is not claimed - anyone can edit
        if (string.IsNullOrEmpty(document.ClaimedByUserId))
        {
            return true;
        }
        
        // Only the claimer can edit
        return document.ClaimedByUserId == userId;
    }
}
