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
                // userValidation contiene la columna Role como TEXTO gracias a la Vista SQL
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

                // AQUÍ ESTABA EL ERROR: Usamos userValidation.Role (que es string)
                identity.AddClaim(new Claim(ClaimTypes.Role, userValidation.Role));

                identity.AddClaim(new Claim("Username", user.Username));
                identity.AddClaim(new Claim("UserID", user.UserID.ToString()));
                identity.AddClaim(new Claim("Role", userValidation.Role));

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

                if (userValidation.Role == "Admin")
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
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error durante Login para el usuario {Username}", username);
                ViewData["LoginError"] = "Credenciales incorrectas";
                return View();
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        public IActionResult ErrorAcceso()
        {
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View("ErrorAcceso");
        }
    }
}