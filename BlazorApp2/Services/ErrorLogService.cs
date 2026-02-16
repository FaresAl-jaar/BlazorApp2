using BlazorApp2.Data;
using BlazorApp2.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp2.Services;

public interface IErrorLogService
{
    Task LogErrorAsync(string source, string message, string? stackTrace = null, string? requestPath = null, string? userId = null, int? documentId = null, string? documentName = null, string? externalId = null);
    Task LogWarningAsync(string source, string message, int? documentId = null, string? documentName = null);
    Task LogInfoAsync(string source, string message, int? documentId = null, string? documentName = null);
    Task<List<ErrorLog>> GetRecentErrorsAsync(int count = 50);
    Task<List<ErrorLog>> GetErrorsByDateRangeAsync(DateTime from, DateTime to);
    Task<int> ClearOldLogsAsync(int daysToKeep = 30);
    Task<bool> DeleteLogAsync(int id);
    Task ClearAllLogsAsync();
}

public class ErrorLogService : IErrorLogService
{
    private readonly ApplicationDbContext _context;

    public ErrorLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogErrorAsync(string source, string message, string? stackTrace = null, string? requestPath = null, string? userId = null, int? documentId = null, string? documentName = null, string? externalId = null)
    {
        // Build detailed message
        var detailedMessage = message;
        if (documentId.HasValue || !string.IsNullOrEmpty(documentName) || !string.IsNullOrEmpty(externalId))
        {
            var docInfo = new List<string>();
            if (documentId.HasValue) docInfo.Add($"ID: {documentId}");
            if (!string.IsNullOrEmpty(documentName)) docInfo.Add($"Datei: {documentName}");
            if (!string.IsNullOrEmpty(externalId)) docInfo.Add($"ExternalId: {externalId}");
            detailedMessage = $"[{string.Join(", ", docInfo)}] {message}";
        }
        
        var log = new ErrorLog
        {
            Level = "Error",
            Source = source,
            Message = detailedMessage,
            StackTrace = stackTrace,
            RequestPath = requestPath,
            UserId = userId,
            DocumentId = documentId
        };

        _context.ErrorLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogWarningAsync(string source, string message, int? documentId = null, string? documentName = null)
    {
        var detailedMessage = message;
        if (documentId.HasValue || !string.IsNullOrEmpty(documentName))
        {
            var docInfo = new List<string>();
            if (documentId.HasValue) docInfo.Add($"ID: {documentId}");
            if (!string.IsNullOrEmpty(documentName)) docInfo.Add($"Datei: {documentName}");
            detailedMessage = $"[{string.Join(", ", docInfo)}] {message}";
        }
        
        var log = new ErrorLog
        {
            Level = "Warning",
            Source = source,
            Message = detailedMessage,
            DocumentId = documentId
        };

        _context.ErrorLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogInfoAsync(string source, string message, int? documentId = null, string? documentName = null)
    {
        var detailedMessage = message;
        if (documentId.HasValue || !string.IsNullOrEmpty(documentName))
        {
            var docInfo = new List<string>();
            if (documentId.HasValue) docInfo.Add($"ID: {documentId}");
            if (!string.IsNullOrEmpty(documentName)) docInfo.Add($"Datei: {documentName}");
            detailedMessage = $"[{string.Join(", ", docInfo)}] {message}";
        }
        
        var log = new ErrorLog
        {
            Level = "Info",
            Source = source,
            Message = detailedMessage,
            DocumentId = documentId
        };

        _context.ErrorLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ErrorLog>> GetRecentErrorsAsync(int count = 50)
    {
        return await _context.ErrorLogs
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<ErrorLog>> GetErrorsByDateRangeAsync(DateTime from, DateTime to)
    {
        return await _context.ErrorLogs
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();
    }

    public async Task<int> ClearOldLogsAsync(int daysToKeep = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
        var oldLogs = await _context.ErrorLogs
            .Where(e => e.Timestamp < cutoff)
            .ToListAsync();

        _context.ErrorLogs.RemoveRange(oldLogs);
        await _context.SaveChangesAsync();

        return oldLogs.Count;
    }

    public async Task<bool> DeleteLogAsync(int id)
    {
        var log = await _context.ErrorLogs.FindAsync(id);
        if (log == null) return false;

        _context.ErrorLogs.Remove(log);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task ClearAllLogsAsync()
    {
        _context.ErrorLogs.RemoveRange(_context.ErrorLogs);
        await _context.SaveChangesAsync();
    }
}
