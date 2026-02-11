using BlazorApp2.Components;
using BlazorApp2.Data;
using BlazorApp2.Hubs;
using BlazorApp2.Models;
using BlazorApp2.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=pdfmanager.db"));

// SignalR for real-time updates
builder.Services.AddSignalR();

// Identity with Cookie Authentication
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/Auth/logout";
    options.AccessDeniedPath = "/login";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    
    // Important: Handle redirects for Blazor
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// Services
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IErrorLogService, ErrorLogService>();
builder.Services.AddSingleton<IOcrConfigService, OcrConfigService>();
builder.Services.AddSingleton<IPdfPlumberService, PdfPlumberService>();
builder.Services.AddHttpClient<ILobsterApiService, LobsterApiService>();

// Background Service for automatic PDF processing
builder.Services.AddHostedService<AutoProcessingService>();

// Controllers for API endpoints (including Auth)
builder.Services.AddControllers();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();



var app = builder.Build();

// Initialize Database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
    
    // Create roles
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }
    if (!await roleManager.RoleExistsAsync("User"))
    {
        await roleManager.CreateAsync(new IdentityRole("User"));
    }
    
    // Create default admin user if not exists (Main Admin - cannot be deleted)
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    if (await userManager.FindByNameAsync("admin") == null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = "admin",
            Email = "admin@local.de",
            FullName = "Haupt-Administrator",
            EmailConfirmed = true,
            IsMainAdmin = true,
            IsApproved = true // Main admin is auto-approved
        };
        await userManager.CreateAsync(adminUser, "Admin123!");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapHub<DocumentHub>("/hubs/documents");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
