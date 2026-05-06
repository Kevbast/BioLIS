using BioLIS.Filters;
using BioLIS.Models.Common;
using BioLIS.Models.Entities;
using BioLIS.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using UserRoles = BioLIS.Models.Common.UserRoles;

namespace BioLIS.Controllers
{
    [AuthorizeUsers]
    public class UsersController : Controller
    {
        private readonly ApiService api;
        public UsersController(ApiService api) => this.api = api;

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Index()
        {
            var users   = await this.api.GetAllUsersAsync() ?? new();
            var doctors = await this.api.GetDoctorsAsync()  ?? new();
            ViewData["Doctors"] = doctors.ToDictionary(d => d.DoctorID, d => d.FullName);
            return View(users);
        }

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Inactive()
            => View(await this.api.GetInactiveUsersAsync() ?? new());

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Create()
        {
            await LoadCreateViewDataAsync();
            return View();
        }

        [HttpPost][ValidateAntiForgeryToken][AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(string username, string password, string email,
            string role, int? doctorId, IFormFile? photoFile)
        {
            if (role == UserRoles.Doctor && !doctorId.HasValue)
            {
                TempData["ErrorMessage"] = "Debe seleccionar un médico para el rol Doctor.";
                await LoadCreateViewDataAsync(); return View();
            }
            if (role != UserRoles.Doctor && doctorId.HasValue)
            {
                TempData["ErrorMessage"] = "Solo los usuarios con rol Doctor pueden estar vinculados a un médico.";
                await LoadCreateViewDataAsync(); return View();
            }

            string photoFilename = photoFile?.FileName ?? "default-user.png";
            var result = await this.api.CreateUserAsync(username, password, role, email, photoFilename, doctorId, photoFile);
            if (result.Success)
            {
                TempData["SwalType"] = "success"; TempData["SwalTitle"] = "Usuario creado";
                TempData["SwalMessage"] = "Usuario creado exitosamente.";
                return RedirectToAction("Index");
            }
            TempData["ErrorMessage"] = result.Body;
            await LoadCreateViewDataAsync();
            return View();
        }

        [HttpPost][ValidateAntiForgeryToken][AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Reactivate(int id)
        {
            var result = await this.api.ReactivateUserAsync(id);
            TempData["SwalType"]    = result.Success ? "success" : "error";
            TempData["SwalTitle"]   = result.Success ? "Usuario reactivado" : "No se pudo reactivar";
            TempData["SwalMessage"] = result.Body;
            return RedirectToAction("Inactive");
        }

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await this.api.GetUserByIdAsync(id);
            if (user == null) { TempData["ErrorMessage"] = "Usuario no encontrado."; return RedirectToAction("Index"); }
            return View(user);
        }

        [HttpPost, ActionName("Delete")][ValidateAntiForgeryToken][AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await this.api.DeleteUserAsync(id);
            TempData["SwalType"]    = result.Success ? "success" : "error";
            TempData["SwalTitle"]   = result.Success ? "Usuario desactivado" : "No se pudo eliminar";
            TempData["SwalMessage"] = result.Body;
            return RedirectToAction("Index");
        }

        //[AuthorizeUsers]
        //public async Task<IActionResult> ChangePassword()
        //{
        //    var claimId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    if (!int.TryParse(claimId, out int userId)) return RedirectToAction("Login", "Auth");
        //    var user = await this.api.GetUserByIdAsync(userId);
        //    if (user == null) return RedirectToAction("Login", "Auth");
        //    return View(user);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers]
        public async Task<IActionResult> UpdateProfile(string username, string email, IFormFile? photoFile)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                TempData["SwalType"] = "warning";
                TempData["SwalTitle"] = "Datos incompletos";
                TempData["SwalMessage"] = "El nombre de usuario es obligatorio.";
                return RedirectToAction("ChangePassword");
            }

            // 1. Obtenemos el perfil actual antes de hacer nada para saber qué foto tenía
            var currentUser = await this.api.GetProfileAsync();

            // 2. Si el usuario NO subió un archivo nuevo, le pasamos el nombre de la foto vieja
            // para que la API entienda que debe "conservar" la misma foto.
            string? photoFilename = photoFile == null || photoFile.Length == 0
                                    ? currentUser?.PhotoFilename
                                    : photoFile.FileName;

            // 3. Enviamos la petición a la API
            var result = await this.api.UpdateMyProfileAsync(username, email, photoFilename, photoFile);

            if (result.Success)
            {
                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Perfil actualizado";
                TempData["SwalMessage"] = "Tus datos se han guardado correctamente.";
                await RefreshUserClaimsAsync();
            }
            else
            {
                TempData["SwalType"] = "error";
                TempData["SwalTitle"] = "No se pudo actualizar";

                // Limpiamos un poco el mensaje JSON feo que manda la API para que quede más amigable
                if (result.Body.Contains("Error interno"))
                    TempData["SwalMessage"] = "Hubo un problema al procesar los datos (Posible formato de imagen no válido).";
                else
                    TempData["SwalMessage"] = result.Body;
            }

            return RedirectToAction("ChangePassword");
        }

        [AuthorizeUsers]
        public async Task<IActionResult> ChangePassword()
        {
            // CAMBIO: Usamos GetProfileAsync() en lugar de GetUserByIdAsync()
            // Esto permite que cada usuario (sin importar su rol) pueda ver sus propios datos.
            var user = await this.api.GetProfileAsync();

            if (user == null) return RedirectToAction("Login", "Auth");
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["SwalType"] = "warning"; TempData["SwalTitle"] = "Contraseñas no coinciden";
                TempData["SwalMessage"] = "La nueva contraseña y su confirmación no coinciden.";
                return RedirectToAction("ChangePassword");
            }
            var claimId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claimId, out int userId)) return RedirectToAction("Login", "Auth");

            var result = await this.api.ChangePasswordAsync(userId, currentPassword, newPassword);
            TempData["SwalType"] = result.Success ? "success" : "error";
            TempData["SwalTitle"] = result.Success ? "Contraseña actualizada" : "No se pudo cambiar";
            TempData["SwalMessage"] = result.Body;

            if (result.Success) return RedirectToAction("ChangePassword");

            // CAMBIO AQUÍ TAMBIÉN: Si falla el cambio de contraseña y hay que recargar la vista,
            // debemos usar GetProfileAsync() para que no expulse a los doctores/laboratorio.
            var user = await this.api.GetProfileAsync();
            return View(user);
        }

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Stats()
            => View(await this.api.GetUserStatsByRoleAsync() ?? new());

        private async Task LoadCreateViewDataAsync()
        {
            var users   = await this.api.GetAllUsersAsync() ?? new();
            var doctors = await this.api.GetDoctorsAsync()  ?? new();

            var assigned = users.Where(u => u.DoctorID.HasValue).Select(u => u.DoctorID!.Value).ToList();

            ViewData["AvailableDoctors"] = doctors
                .Where(d => !assigned.Contains(d.DoctorID))
                .Select(d => new SelectListItem
                {
                    Value = d.DoctorID.ToString(),
                    Text  = $"{d.FullName} ({d.LicenseNumber ?? "Sin licencia"})"
                }).ToList();

            ViewData["Roles"] = UserRoles.GetAll();
        }

        private async Task RefreshUserClaimsAsync()
        {
            var updatedUser = await this.api.GetProfileAsync();
            if (updatedUser == null)
                return;

            var token = HttpContext.User.FindFirstValue("TOKEN");
            if (string.IsNullOrWhiteSpace(token))
                return;

            ClaimsIdentity identity = new ClaimsIdentity(
                CookieAuthenticationDefaults.AuthenticationScheme,
                ClaimTypes.Name, ClaimTypes.Role);

            identity.AddClaim(new Claim(ClaimTypes.Name, updatedUser.Username));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, updatedUser.UserID.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Role, updatedUser.Role?.RoleName ?? string.Empty));
            identity.AddClaim(new Claim("TOKEN", token));
            identity.AddClaim(new Claim("UserID", updatedUser.UserID.ToString()));
            identity.AddClaim(new Claim("Role", updatedUser.Role?.RoleName ?? string.Empty));

            if (!string.IsNullOrEmpty(updatedUser.Email))
                identity.AddClaim(new Claim(ClaimTypes.Email, updatedUser.Email));
            if (!string.IsNullOrEmpty(updatedUser.PhotoFilename))
                identity.AddClaim(new Claim("Photo", updatedUser.PhotoFilename));
            if (updatedUser.DoctorID.HasValue)
                identity.AddClaim(new Claim("DoctorID", updatedUser.DoctorID.Value.ToString()));

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });
        }
    }
}
