using BlazorApp2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Web;

namespace BlazorApp2.Controllers;

[Route("[controller]")]
public class AuthController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    private IActionResult RedirectWithError(string errorMessage)
    {
        // URL-encode the error message to handle German Umlaute
        var encodedError = HttpUtility.UrlEncode(errorMessage);
        return Redirect($"/login?error={encodedError}");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password, [FromForm] bool rememberMe = false)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return RedirectWithError("Bitte Benutzername und Passwort eingeben");
        }

        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
        {
            return RedirectWithError("Ungueltige Anmeldedaten");
        }

        // Check if user is approved
        if (!user.IsApproved)
        {
            return RedirectWithError("Konto wartet auf Genehmigung");
        }

        var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, false);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("Benutzer {Username} hat sich angemeldet.", username);
            return Redirect("/");
        }

        if (result.IsLockedOut)
        {
            return RedirectWithError("Konto ist gesperrt");
        }

        return RedirectWithError("Falsches Passwort");
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/login");
    }
}
