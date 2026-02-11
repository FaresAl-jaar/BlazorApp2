using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BlazorApp2.Models;

namespace BlazorApp2.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<PdfDocument> PdfDocuments { get; set; } = null!;
    public DbSet<ExtractedData> ExtractedData { get; set; } = null!;
    public DbSet<ErrorLog> ErrorLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PdfDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.HasIndex(e => e.FileHash);
            entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>();
        });

        builder.Entity<ExtractedData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.PdfDocument)
                  .WithMany(p => p.ExtractedDataEntries)
                  .HasForeignKey(e => e.PdfDocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ErrorLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Level);
        });
    }
}
