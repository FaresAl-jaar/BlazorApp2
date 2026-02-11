using Microsoft.AspNetCore.Identity;

namespace BlazorApp2.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsMainAdmin { get; set; } = false; // Only main admin can grant admin rights
    public bool IsApproved { get; set; } = false; // Must be approved by admin to login
}
