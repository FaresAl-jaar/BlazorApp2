namespace BlazorApp2.Models;

public class PdfDocument
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = [];
    public long FileSize { get; set; }
    public string? FileHash { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Received;
    public string? ProcessingError { get; set; }
    public string? SourceSystem { get; set; }
    
    // Claiming - only the user who claimed can edit
    public string? ClaimedByUserId { get; set; }
    public string? ClaimedByUserName { get; set; }
    public DateTime? ClaimedAt { get; set; }

    public ICollection<ExtractedData> ExtractedDataEntries { get; set; } = new List<ExtractedData>();
}

public enum DocumentStatus
{
    Received,
    Processing,
    Extracted,
    PendingReview,
    Reviewed,
    Submitted,
    Error
}
