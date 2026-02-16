using BlazorApp2.Models.DTOs;
using BlazorApp2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlazorApp2.Controllers;

[ApiController]
[Route("api/[controller]")]
[IgnoreAntiforgeryToken]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Empfängt eine PDF-Datei direkt als multipart/form-data (für Postman)
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PdfUploadResponse>> UploadPdf(
        [FromForm] IFormFile file,
        [FromForm] string? externalId = null,
        [FromForm] string? sourceSystem = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new PdfUploadResponse
            {
                Success = false,
                Message = "Keine Datei hochgeladen."
            });
        }

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new PdfUploadResponse
            {
                Success = false,
                Message = "Nur PDF-Dateien sind erlaubt."
            });
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();

        var request = new PdfUploadRequest
        {
            ExternalId = externalId ?? Guid.NewGuid().ToString(),
            FileName = file.FileName,
            Base64Content = Convert.ToBase64String(fileBytes),
            SourceSystem = sourceSystem ?? "Postman"
        };

        var result = await _documentService.ReceiveDocumentAsync(request);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Empfängt mehrere PDF-Dateien als multipart/form-data (Batch-Upload)
    /// </summary>
    [HttpPost("upload-batch")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BatchUploadResponse>> UploadMultiplePdfs(
        [FromForm] List<IFormFile> files,
        [FromForm] string? sourceSystem = null)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new BatchUploadResponse
            {
                Success = false,
                Message = "Keine Dateien hochgeladen.",
                TotalFiles = 0,
                SuccessCount = 0,
                FailedCount = 0
            });
        }

        var results = new List<PdfUploadResponse>();
        var successCount = 0;
        var failedCount = 0;

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new PdfUploadResponse
                {
                    Success = false,
                    Message = $"'{file.FileName}': Nur PDF-Dateien sind erlaubt."
                });
                failedCount++;
                continue;
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var request = new PdfUploadRequest
                {
                    ExternalId = Guid.NewGuid().ToString(),
                    FileName = file.FileName,
                    Base64Content = Convert.ToBase64String(fileBytes),
                    SourceSystem = sourceSystem ?? "BatchUpload"
                };

                var result = await _documentService.ReceiveDocumentAsync(request);
                results.Add(result);

                if (result.Success)
                    successCount++;
                else
                    failedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Verarbeiten von {FileName}", file.FileName);
                results.Add(new PdfUploadResponse
                {
                    Success = false,
                    Message = $"'{file.FileName}': {ex.Message}"
                });
                failedCount++;
            }
        }

        return Ok(new BatchUploadResponse
        {
            Success = failedCount == 0,
            Message = $"{successCount} von {files.Count} Dateien erfolgreich verarbeitet.",
            TotalFiles = files.Count,
            SuccessCount = successCount,
            FailedCount = failedCount,
            Results = results
        });
    }

    /// <summary>
    /// Empfängt ein PDF-Dokument von der Lobster API (Base64)
    /// </summary>
    [HttpPost("receive")]
    public async Task<ActionResult<PdfUploadResponse>> ReceiveDocument([FromBody] PdfUploadRequest request)
    {
        if (string.IsNullOrEmpty(request.ExternalId))
        {
            return BadRequest(new PdfUploadResponse
            {
                Success = false,
                Message = "ExternalId ist erforderlich."
            });
        }

        if (string.IsNullOrEmpty(request.Base64Content))
        {
            return BadRequest(new PdfUploadResponse
            {
                Success = false,
                Message = "PDF-Inhalt ist erforderlich."
            });
        }

        var result = await _documentService.ReceiveDocumentAsync(request);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Liefert die Liste aller Dokumente
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<DocumentListItemDto>>> GetDocuments([FromQuery] string? status = null)
    {
        BlazorApp2.Models.DocumentStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BlazorApp2.Models.DocumentStatus>(status, true, out var parsed))
        {
            statusFilter = parsed;
        }

        var documents = await _documentService.GetDocumentsAsync(statusFilter);
        return Ok(documents);
    }

    /// <summary>
    /// Liefert den PDF-Inhalt eines Dokuments
    /// </summary>
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetPdfContent(int id)
    {
        var content = await _documentService.GetPdfContentAsync(id);
        if (content == null)
        {
            return NotFound();
        }

        return File(content, "application/pdf");
    }

    /// <summary>
    /// Liefert die extrahierten Daten eines Dokuments
    /// </summary>
    [HttpGet("{id}/extracted-data")]
    public async Task<IActionResult> GetExtractedData(int id)
    {
        var data = await _documentService.GetExtractedDataAsync(id);
        if (data == null)
        {
            return NotFound();
        }

        return Ok(new ExtractedDataDto
        {
            Id = data.Id,
            PdfDocumentId = data.PdfDocumentId,
            JsonContent = data.JsonContent,
            ExtractedAt = data.ExtractedAt,
            IsValidated = data.IsValidated,
            Version = data.Version
        });
    }

    /// <summary>
    /// Speichert bearbeitete JSON-Daten
    /// </summary>
    [HttpPut("{id}/extracted-data")]
    public async Task<IActionResult> SaveExtractedData(int id, [FromBody] SubmitDataRequest request)
    {
        var userId = User.Identity?.Name;
        var success = await _documentService.SaveExtractedDataAsync(id, request.JsonContent, userId);

        if (success)
        {
            return Ok(new { Message = "Daten erfolgreich gespeichert." });
        }

        return BadRequest(new { Message = "Fehler beim Speichern." });
    }

    /// <summary>
    /// Sendet die Daten an die externe API
    /// </summary>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult<SubmitDataResponse>> SubmitToExternalApi(int id)
    {
        var result = await _documentService.SubmitToExternalApiAsync(id);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Löscht ein einzelnes Dokument (nur Admin)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var success = await _documentService.DeleteDocumentAsync(id);

        if (success)
        {
            return Ok(new { Message = $"Dokument {id} wurde gelöscht." });
        }

        return NotFound(new { Message = "Dokument nicht gefunden." });
    }

    /// <summary>
    /// Löscht alle Dokumente (nur Admin)
    /// </summary>
    [HttpDelete("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAllDocuments()
    {
        var count = await _documentService.DeleteAllDocumentsAsync();
        return Ok(new { Message = $"{count} Dokumente wurden gelöscht." });
    }

    /// <summary>
    /// Loescht alle extrahierten JSON-Daten (nur Admin)
    /// </summary>
    [HttpDelete("extracted-data/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAllExtractedData()
    {
        var count = await _documentService.DeleteAllExtractedDataAsync();
        return Ok(new { Message = $"{count} JSON-Datensaetze wurden geloescht." });
    }

    /// <summary>
    /// Prüft die API-Verbindung
    /// </summary>
    [HttpGet("health/api")]
    public async Task<IActionResult> CheckApiConnection()
    {
        var isConnected = await _documentService.CheckApiConnectionAsync();
        return Ok(new 
        { 
            Connected = isConnected, 
            Status = isConnected ? "Verbunden" : "Nicht erreichbar",
            Timestamp = DateTime.UtcNow
        });
    }
}
