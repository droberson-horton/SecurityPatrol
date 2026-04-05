using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SecurityPatrol.Web.Models;
using SecurityPatrol.Web.ViewModels;
using System.Security.Claims;

namespace SecurityPatrol.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToRoleHome();

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        // Ensure the user has the role claim set
        var existingClaims = await _userManager.GetClaimsAsync(user);
        var roleClaim = existingClaims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        if (roleClaim == null)
        {
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, user.Role.ToString()));
        }
        else if (roleClaim.Value != user.Role.ToString())
        {
            await _userManager.RemoveClaimAsync(user, roleClaim);
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, user.Role.ToString()));
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in.", model.Email);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return user.Role switch
            {
                UserRole.Administrator => RedirectToAction("Index", "Admin"),
                UserRole.SecurityOfficer => RedirectToAction("Officer", "Dashboard"),
                UserRole.Auditor => RedirectToAction("Index", "Audit"),
                _ => RedirectToAction("Officer", "Dashboard")
            };
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} account locked out.", model.Email);
            ModelState.AddModelError(string.Empty, "Your account has been locked out. Please try again later.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Login", "Account");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToRoleHome()
    {
        if (User.HasClaim(ClaimTypes.Role, UserRole.Administrator.ToString()))
            return RedirectToAction("Index", "Admin");
        if (User.HasClaim(ClaimTypes.Role, UserRole.Auditor.ToString()))
            return RedirectToAction("Index", "Audit");
        return RedirectToAction("Officer", "Dashboard");
    }
}
