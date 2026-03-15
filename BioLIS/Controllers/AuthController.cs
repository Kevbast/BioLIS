
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using BioLIS.Repositories;
using System.Security.Claims;

namespace BioLIS.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthRepository authRepo;

        public AuthController(AuthRepository authRepo)
        {
            this.authRepo = authRepo;
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
                ViewData["MENSAJE"] = "Usuario y contraseña son obligatorios";
                return View();
            }

            // Validar credenciales (mantiene Salt+SHA512)
            var userValidation = await this.authRepo.ValidateCredentialsAsync(username, password);

            if (userValidation == null)
            {
                ViewData["MENSAJE"] = "Usuario o contraseña incorrectos";
                return View();
            }

            // Obtener usuario completo
            var user = await this.authRepo.GetUserByIdAsync(userValidation.UserID);

            if (user == null)
            {
                ViewData["MENSAJE"] = "Error al cargar datos del usuario";
                return View();
            }

            // ========================================
            // CREAR CLAIMS
            // ========================================
            ClaimsIdentity identity = new ClaimsIdentity(
                CookieAuthenticationDefaults.AuthenticationScheme,
                ClaimTypes.Name, ClaimTypes.Role);

            // Claims estándar
            identity.AddClaim(new Claim(ClaimTypes.Name, username));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Role, user.Role));

            // Claims personalizados
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

            // Si es doctor, agregar DoctorID
            if (user.DoctorID.HasValue)
            {
                identity.AddClaim(new Claim("DoctorID", user.DoctorID.Value.ToString()));
            }

            // Claim especial para Admin
            if (user.Role == "Admin")
            {
                identity.AddClaim(new Claim("Admin", "Administrador del sistema"));
            }

            ClaimsPrincipal userPrincipal = new ClaimsPrincipal(identity);

            // Autenticar
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                userPrincipal);

            // ========================================
            // REDIRIGIR USANDO RETURN URL SEGURA
            // ========================================
            string? returnUrl = TempData["returnUrl"]?.ToString();
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
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