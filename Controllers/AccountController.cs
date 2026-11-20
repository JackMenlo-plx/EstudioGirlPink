using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioGirlPink.Data;
using StudioGirlPink.Models;
using StudioGirlPink.Security;
using StudioGirlPink.Services;
using StudioGirlPink.Services.Auth;
using StudioGirlPink.ViewModels;

namespace StudioGirlPink.Controllers;

public class BiometricLoginRequest
{
    public string CredentialId { get; set; } = string.Empty;
    public string RawId { get; set; } = string.Empty;
    public string ClientDataJSON { get; set; } = string.Empty;
}

public class AccountController(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    AppDbContext db,
    LoyaltyService loyaltyService,
    IPasswordResetService passwordResetService) : Controller
{

    // ===============================================
    // RECUPERACIÓN DE CONTRASEÑA (CLIENTAS / ADMIN)
    // ===============================================

    [HttpGet("cuenta/olvide-mi-clave")]
    public IActionResult ForgotPassword() => View();

    [HttpPost("cuenta/olvide-mi-clave")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        await passwordResetService.InitiatePasswordResetAsync(model.Email.Trim(), baseUrl, cancellationToken);

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet("cuenta/confirmacion-olvide-clave")]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet("cuenta/restablecer-clave")]
    public IActionResult ResetPassword(string? token = null, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("El enlace de restablecimiento es inválido o ha caducado.");
        }

        var model = new ResetPasswordViewModel { Token = token, Email = email };
        return View(model);
    }

    [HttpPost("cuenta/restablecer-clave")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, message) = await passwordResetService.ResetPasswordAsync(
            model.Email.Trim(),
            model.Token.Trim(),
            model.Password,
            cancellationToken);

        if (success)
        {
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        ModelState.AddModelError(string.Empty, message);
        return View(model);
    }

    [HttpGet("cuenta/confirmacion-clave-restablecida")]
    public IActionResult ResetPasswordConfirmation() => View();


    // ===============================================
    // REGISTRO AL PROGRAMA DE FIDELIZACIÓN (CLIENTES)
    // ===============================================
    [HttpGet("cuenta/registro")]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(MiTarjeta));
        }

        return View();
    }

    [HttpPost("cuenta/registro")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await userManager.FindByEmailAsync(model.Email.Trim());
        if (existingUser != null)
        {
            ModelState.AddModelError(string.Empty, "Ya existe una cuenta registrada con este correo electrónico.");
            return View(model);
        }

        var user = new IdentityUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            PhoneNumber = model.Phone.Trim(),
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // Asignar rol de clienta
        await userManager.AddToRoleAsync(user, IdentityBootstrapper.ClientRole);

        // Crear o asociar registro de Clienta para el seguimiento de lealtad
        var cleanPhone = model.Phone.Trim();
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Phone == cleanPhone || c.Email == model.Email.Trim());
        if (client == null)
        {
            client = new Client
            {
                FullName = model.FullName.Trim(),
                Phone = cleanPhone,
                Email = model.Email.Trim(),
                LoyaltyCount = 0,
                TotalSpent = 0m,
                CreatedAt = DateTime.UtcNow
            };
            db.Clients.Add(client);
            await db.SaveChangesAsync();
        }
        else if (string.IsNullOrEmpty(client.Email))
        {
            client.Email = model.Email.Trim();
            await db.SaveChangesAsync();
        }

        await signInManager.SignInAsync(user, isPersistent: true);
        TempData["SuccessMessage"] = "¡Bienvenida a tu Tarjeta de Lealtad Studio Girl Pink! Acumulas 1 visita con cada servicio finalizado.";
        return RedirectToAction(nameof(MiTarjeta));
    }

    // ===============================================
    // LOGIN DE CLIENTA O GENERAL
    // ===============================================
    [HttpGet("cuenta/login")]
    public IActionResult Login(string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("cuenta/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            model.Email.Trim(),
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await userManager.FindByEmailAsync(model.Email.Trim());
            if (user != null && await userManager.IsInRoleAsync(user, IdentityBootstrapper.AdministratorRole))
            {
                return RedirectToAction("Index", "Admin");
            }

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction(nameof(MiTarjeta));
        }

        ModelState.AddModelError(string.Empty, "Correo electrónico o contraseña incorrectos.");
        return View(model);
    }

    // ===============================================
    // LOGIN EXCLUSIVO PARA ADMINISTRADOR (/admin/login)
    // ===============================================
    [HttpGet("admin/login")]
    public IActionResult AdminLogin(string? returnUrl)
    {
        if (User.IsInRole(IdentityBootstrapper.AdministratorRole))
        {
            return RedirectToAction("Index", "Admin");
        }

        ViewData["ReturnUrl"] = returnUrl ?? "/admin";
        return View(new LoginViewModel { ReturnUrl = returnUrl ?? "/admin" });
    }

    [HttpPost("admin/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLogin(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email.Trim());
        if (user == null || !await userManager.IsInRoleAsync(user, IdentityBootstrapper.AdministratorRole))
        {
            ModelState.AddModelError(string.Empty, "Acceso no autorizado. Esta sección es exclusiva para el equipo administrativo.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            return RedirectToAction("Index", "Admin");
        }

        ModelState.AddModelError(string.Empty, "Credenciales incorrectas.");
        return View(model);
    }

    // ===============================================
    // ACCESO RÁPIDO CON HUELLA DIGITAL / BIOMETRÍA
    // ===============================================

    /*
    [HttpPost("admin/login-biometrico")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLoginBiometrico(CancellationToken cancellationToken)
    {
        var adminEmail = Environment.GetEnvironmentVariable("STUDIO_GIRL_PINK_ADMIN_EMAIL") ?? "admin@studiogirlpink.com";
        var user = await userManager.FindByEmailAsync(adminEmail);

        if (user == null || !await userManager.IsInRoleAsync(user, IdentityBootstrapper.AdministratorRole))
        {
            return Json(new { success = false, message = "Cuenta administradora no configurada." });
        }

        await signInManager.SignInAsync(user, isPersistent: true);
        return Json(new { success = true, redirectUrl = "/admin" });
    }
*/

    [HttpPost("admin/login-biometrico")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AdminLoginBiometrico([FromBody] BiometricLoginRequest request)
{
    // Validar que se hayan recibido datos del sensor biométrico
    if (string.IsNullOrWhiteSpace(request.CredentialId))
    {
        return Json(new { success = false, message = "No se recibieron datos válidos del sensor." });
    }

    var adminEmail = Environment.GetEnvironmentVariable("STUDIO_GIRL_PINK_ADMIN_EMAIL") ?? "admin@studiogirlpink.com";
    var user = await userManager.FindByEmailAsync(adminEmail);

    if (user == null || !await userManager.IsInRoleAsync(user, IdentityBootstrapper.AdministratorRole))
    {
        return Json(new { success = false, message = "Cuenta administradora no configurada." });
    }

    await signInManager.SignInAsync(user, isPersistent: true);
    return Json(new { success = true, redirectUrl = "/admin" });
}


    // ===============================================
    // MI TARJETA DE LEALTAD DIGITAL (CLIENTA LOGUEADA)
    // ===============================================
    [Authorize]
    [HttpGet("cuenta/mi-tarjeta")]
    public async Task<IActionResult> MiTarjeta()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var client = await db.Clients
            .Include(c => c.Appointments)
                .ThenInclude(a => a.Service)
            .FirstOrDefaultAsync(c => c.Email == user.Email || c.Phone == user.PhoneNumber);

        var loyalty = loyaltyService.CheckLoyaltyStatus(client?.LoyaltyCount ?? 0);
        ViewBag.LoyaltyBenefit = loyalty;
        ViewBag.UserEmail = user.Email;

        return View(client);
    }

    // ===============================================
    // CERRAR SESIÓN
    // ===============================================
    [HttpPost("cuenta/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
