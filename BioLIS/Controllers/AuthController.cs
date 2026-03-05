using BioLIS.Models;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;

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
            if (HttpContext.Session.GetInt32("UserID").HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Usuario y contraseña son obligatorios";
                return View();
            }

            // Validar credenciales usando AuthRepository
            var userValidation = await this.authRepo.ValidateCredentialsAsync(username, password);

            if (userValidation == null)
            {
                ViewBag.Error = "Usuario o contraseña incorrectos";
                return View();
            }

            // Obtener datos completos del usuario
            var user = await this.authRepo.GetUserByIdAsync(userValidation.UserID);

            if (user == null)
            {
                ViewBag.Error = "Error al cargar datos del usuario";
                return View();
            }

            // Guardar datos en sesión
            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);
            
            if (!string.IsNullOrEmpty(user.PhotoFilename))
            {
                HttpContext.Session.SetString("Photo", user.PhotoFilename);
            }

            // Mensaje de bienvenida
            TempData["Success"] = $"Bienvenido/a, {user.Username}!";

            // Redirigir según returnUrl o al Home
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: Auth/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Info"] = "Sesión cerrada correctamente";
            return RedirectToAction("Login");
        }

        // GET: Auth/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}