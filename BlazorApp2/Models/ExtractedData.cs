namespace BlazorApp2.Models;

public class ExtractedData
{
    public int Id { get; set; }
    public int PdfDocumentId { get; set; }
    public string JsonContent { get; set; } = "{}";
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsValidated { get; set; }
    public string? ValidationNotes { get; set; }
    public int Version { get; set; } = 1;

    public PdfDocument PdfDocument { get; set; } = null!;
}
