namespace BlazorApp2.Models;

public class ErrorLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = "Error";
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? RequestPath { get; set; }
    public string? UserId { get; set; }
    public int? DocumentId { get; set; }
}
