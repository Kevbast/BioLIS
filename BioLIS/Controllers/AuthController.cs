
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using BioLIS.Repositories;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace BioLIS.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthRepository authRepo;
        private readonly ILogger<AuthController> logger;

        public AuthController(AuthRepository authRepo, ILogger<AuthController> logger)
        {
            this.authRepo = authRepo;
            this.logger = logger;
        }

        // GET: Auth/Login
        public IActionResult Login()
        {
            // Si ya está autenticado, redirigir al home
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewData["LoginError"] = "Credenciales incorrectas";
                return View();
            }

            try
            {
                var userValidation = await this.authRepo.ValidateCredentialsAsync(username, password);

                if (userValidation == null)
                {
                    ViewData["LoginError"] = "Credenciales incorrectas";
                    return View();
                }

                var user = await this.authRepo.GetUserByIdAsync(userValidation.UserID);

                if (user == null)
                {
                    ViewData["LoginError"] = "Credenciales incorrectas";
                    return View();
                }

                ClaimsIdentity identity = new ClaimsIdentity(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    ClaimTypes.Name, ClaimTypes.Role);

                identity.AddClaim(new Claim(ClaimTypes.Name, username));
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()));
                identity.AddClaim(new Claim(ClaimTypes.Role, user.Role));

                identity.AddClaim(new Claim("Username", user.Username));
                identity.AddClaim(new Claim("UserID", user.UserID.ToString()));
                identity.AddClaim(new Claim("Role", user.Role));

                if (!string.IsNullOrEmpty(user.Email))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
                }

                if (!string.IsNullOrEmpty(user.PhotoFilename))
                {
                    identity.AddClaim(new Claim("Photo", user.PhotoFilename));
                }

                if (user.DoctorID.HasValue)
                {
                    identity.AddClaim(new Claim("DoctorID", user.DoctorID.Value.ToString()));
                }

                if (user.Role == "Admin")
                {
                    identity.AddClaim(new Claim("Admin", "Administrador del sistema"));
                }

                ClaimsPrincipal userPrincipal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    userPrincipal);

                string? returnUrl = TempData["returnUrl"]?.ToString();
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (CryptographicException ex)
            {
                this.logger.LogWarning(ex, "Error criptográfico durante Login para el usuario {Username}", username);
                ViewData["LoginError"] = "Credenciales incorrectas";
                return View();
            }
            catch (InvalidOperationException ex)
            {
                this.logger.LogWarning(ex, "Error de operación durante Login para el usuario {Username}", username);
                ViewData["LoginError"] = "Credenciales incorrectas";
                return View();
            }
            catch (DbUpdateException ex)
            {
                this.logger.LogError(ex, "Error de base de datos durante Login para el usuario {Username}", username);
                ViewData["LoginError"] = "Credenciales incorrectas";
                return View();
            }
        }

        // GET: Auth/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        // GET: Auth/ErrorAcceso
        public IActionResult ErrorAcceso()
        {
            return View();
        }

        // GET: Auth/AccessDenied (alias para compatibilidad)
        public IActionResult AccessDenied()
        {
            return View("ErrorAcceso");
        }
    }
}