using BioLab.Models;
using BioLIS.Filters;
using BioLIS.Helpers;
using BioLIS.Models;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace BioLIS.Controllers
{
    [AuthorizeUsers(Policy = "AdminOnly")] // CAMBIADO de [AuthorizeRole("Admin")]
    public class UsersController : Controller
    {
        private readonly AuthRepository authRepo;
        private readonly CatalogRepository catalogRepo;
        private readonly HelperPathProvider pathHelper;

        public UsersController(AuthRepository authRepo, CatalogRepository catalogRepo, HelperPathProvider pathHelper)
        {
            this.authRepo = authRepo;
            this.catalogRepo = catalogRepo;
            this.pathHelper = pathHelper;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await this.authRepo.GetAllUsersAsync();
            var allDoctors = await this.catalogRepo.GetDoctorsAsync();
            ViewData["Doctors"] = allDoctors.ToDictionary(d => d.DoctorID, d => d.FullName);

            var usersWithDetails = new List<User>();
            foreach (var userValidation in users)
            {
                var user = await this.authRepo.GetUserByIdAsync(userValidation.UserID);
                if (user != null)
                {
                    usersWithDetails.Add(user);
                }
            }

            return View(usersWithDetails);
        }

        // GET: Users/Create
        public async Task<IActionResult> Create()
        {
            var existingUsers = await this.authRepo.GetAllUsersAsync();
            var assignedDoctorIds = existingUsers
                .Where(u => u.DoctorID.HasValue)
                .Select(u => u.DoctorID.Value)
                .ToList();

            var allDoctors = await this.catalogRepo.GetDoctorsAsync();
            var availableDoctors = allDoctors
                .Where(d => !assignedDoctorIds.Contains(d.DoctorID))
                .Select(d => new SelectListItem
                {
                    Value = d.DoctorID.ToString(),
                    Text = $"{d.FullName} ({d.LicenseNumber ?? "Sin licencia"})"
                })
                .ToList();

            ViewData["AvailableDoctors"] = availableDoctors;
            ViewData["Roles"] = UserRoles.GetAll();
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string username, string password, string email,
                                                 string role, int? doctorId, IFormFile? photoFile)
        {
            if (role == "Doctor" && !doctorId.HasValue)
            {
                TempData["ErrorMessage"] = "Debe seleccionar un médico para el rol Doctor.";
                await LoadCreateViewDataAsync();
                return View();
            }

            if (role != "Doctor" && doctorId.HasValue)
            {
                TempData["ErrorMessage"] = "Solo los usuarios con rol Doctor pueden estar vinculados a un médico.";
                await LoadCreateViewDataAsync();
                return View();
            }

            string photoFilename = "default-user.png";

            if (photoFile != null)
            {
                photoFilename = photoFile.FileName;
                string path = this.pathHelper.MapPath(photoFilename, Folders.Users);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await photoFile.CopyToAsync(stream);
                }
            }

            var result = await this.authRepo.CreateUserAsync(
                username, password, role, email, photoFilename, doctorId
            );

            if (result.Success)
            {
                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Usuario creado";
                TempData["SwalMessage"] = result.Message;
                return RedirectToAction("Index");
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                await LoadCreateViewDataAsync();
                return View();
            }
        }

        // Método auxiliar
        private async Task LoadCreateViewDataAsync()
        {
            var existingUsers = await this.authRepo.GetAllUsersAsync();
            var assignedDoctorIds = existingUsers
                .Where(u => u.DoctorID.HasValue)
                .Select(u => u.DoctorID.Value)
                .ToList();

            var allDoctors = await this.catalogRepo.GetDoctorsAsync();
            var availableDoctors = allDoctors
                .Where(d => !assignedDoctorIds.Contains(d.DoctorID))
                .Select(d => new SelectListItem
                {
                    Value = d.DoctorID.ToString(),
                    Text = $"{d.FullName} ({d.LicenseNumber ?? "Sin licencia"})"
                })
                .ToList();

            ViewData["AvailableDoctors"] = availableDoctors;
            ViewData["Roles"] = UserRoles.GetAll();
        }

        // GET: Users/Delete
        public async Task<IActionResult> Delete(int id)
        {
            var user = await this.authRepo.GetUserByIdAsync(id);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado";
                return RedirectToAction("Index");
            }

            return View(user);
        }

        // POST: Users/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await this.authRepo.DeleteUserAsync(id);

            if (result.Success)
            {
                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Usuario eliminado";
                TempData["SwalMessage"] = result.Message;
            }
            else
            {
                TempData["SwalType"] = "error";
                TempData["SwalTitle"] = "No se pudo eliminar";
                TempData["SwalMessage"] = result.Message;
            }

            return RedirectToAction("Index");
        }

        // ========================================
        // CHANGE PASSWORD - CUALQUIER USUARIO AUTENTICADO
        // ========================================
        [AuthorizeUsers] // Sobreescribe la policy de clase - permite a cualquier autenticado
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers] // Sobreescribe la policy de clase
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "La nueva contraseña y su confirmación no coinciden";
                return View();
            }

            // CAMBIADO: Usar Claims en lugar de Session
            var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var result = await this.authRepo.ChangePasswordAsync(userId, currentPassword, newPassword);

            if (result.Success)
            {
                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Contraseña actualizada";
                TempData["SwalMessage"] = result.Message;
                return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["SwalType"] = "error";
                TempData["SwalTitle"] = "No se pudo cambiar";
                TempData["SwalMessage"] = result.Message;
                return View();
            }
        }

        // GET: Users/Stats
        public async Task<IActionResult> Stats()
        {
            var stats = await this.authRepo.CountUsersByRoleAsync();
            return View(stats);
        }
    }
}