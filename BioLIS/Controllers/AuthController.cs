using BioLIS.Services;
using BioLIS.Filters;
using BioLIS.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BioLIS.Models.Entities;

namespace BioLIS.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApiService api;

        public AuthController(ApiService api)
            => this.api = api;

        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated ?? false)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                // CAMBIO AQUÍ: LoginError en lugar de MENSAJE
                ViewData["LoginError"] = "Usuario o contraseña obligatorios.";
                return View();
            }

            // 1. Obtener token JWT de la API
            string? token = await this.api.LoginAsync(username, password);
            if (token == null)
            {
                // CAMBIO AQUÍ: LoginError en lugar de MENSAJE
                ViewData["LoginError"] = "Usuario o credenciales incorrectas.";
                return View();
            }

            // 2. Obtener perfil del usuario para construir los claims
            User? user = await this.api.GetProfileWithTokenAsync(token);
            if (user == null)
            {
                // CAMBIO AQUÍ: LoginError en lugar de MENSAJE
                ViewData["LoginError"] = "Error al obtener el perfil de usuario.";
                return View();
            }

            // 3. Crear la identidad de cookie
            ClaimsIdentity identity = new ClaimsIdentity(
                CookieAuthenticationDefaults.AuthenticationScheme,
                ClaimTypes.Name, ClaimTypes.Role);

            identity.AddClaim(new Claim(ClaimTypes.Name, user.Username));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Role, user.Role?.RoleName ?? ""));
            identity.AddClaim(new Claim("TOKEN", token));
            identity.AddClaim(new Claim("UserID", user.UserID.ToString()));
            identity.AddClaim(new Claim("Role", user.Role?.RoleName ?? ""));

            if (!string.IsNullOrEmpty(user.Email))
                identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
            if (!string.IsNullOrEmpty(user.PhotoFilename))
                identity.AddClaim(new Claim("Photo", user.PhotoFilename));
            if (user.DoctorID.HasValue)
                identity.AddClaim(new Claim("DoctorID", user.DoctorID.Value.ToString()));

            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        public IActionResult ErrorAcceso()  => View();
        public IActionResult AccessDenied() => View("ErrorAcceso");
    }
}
