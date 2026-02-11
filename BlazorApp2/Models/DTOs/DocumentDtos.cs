namespace BlazorApp2.Models.DTOs;

public class PdfUploadRequest
{
    public string ExternalId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Base64Content { get; set; } = string.Empty;
    public string? SourceSystem { get; set; }
}

public class PdfUploadResponse
{
    public bool Success { get; set; }
    public int? DocumentId { get; set; }
    public string? Message { get; set; }
    public string? ExternalId { get; set; }
}

public class ExtractedDataDto
{
    public int Id { get; set; }
    public int PdfDocumentId { get; set; }
    public string JsonContent { get; set; } = "{}";
    public DateTime ExtractedAt { get; set; }
    public bool IsValidated { get; set; }
    public int Version { get; set; }
}


public class DocumentListItemDto
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public DocumentStatus Status { get; set; }
    public bool HasExtractedData { get; set; }
    public string? ClaimedByUserId { get; set; }
    public string? ClaimedByUserName { get; set; }
    public DateTime? ClaimedAt { get; set; }
}

public class SubmitDataRequest
{
    public int DocumentId { get; set; }
    public string JsonContent { get; set; } = "{}";
}

public class SubmitDataResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public DateTime? SubmittedAt { get; set; }
}

public class BatchUploadResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<PdfUploadResponse> Results { get; set; } = new();
}
