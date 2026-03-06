using BioLab.Models;
using BioLIS.Filters;
using BioLIS.Helpers;
using BioLIS.Models;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BioLIS.Controllers
{
    [AuthorizeRole("Admin")] // Solo admins pueden gestionar usuarios
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
            // Obtener usuarios completos desde la tabla Users
            var users = await this.authRepo.GetAllUsersAsync();
            
            // Obtener todos los doctores para poder mostrar sus nombres
            var allDoctors = await this.catalogRepo.GetDoctorsAsync();
            ViewBag.Doctors = allDoctors.ToDictionary(d => d.DoctorID, d => d.FullName);
            
            // Convertir UserValidation a User completo para incluir PhotoFilename
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
            // Obtener todos los usuarios existentes para filtrar doctores disponibles
            var existingUsers = await this.authRepo.GetAllUsersAsync();
            var assignedDoctorIds = existingUsers
                .Where(u => u.DoctorID.HasValue)
                .Select(u => u.DoctorID.Value)
                .ToList();

            // Obtener doctores que NO tienen usuario asignado
            var allDoctors = await this.catalogRepo.GetDoctorsAsync();
            var availableDoctors = allDoctors
                .Where(d => !assignedDoctorIds.Contains(d.DoctorID))
                .Select(d => new SelectListItem
                {
                    Value = d.DoctorID.ToString(),
                    Text = $"{d.FullName} ({d.LicenseNumber ?? "Sin licencia"})"
                })
                .ToList();

            ViewBag.AvailableDoctors = availableDoctors;
            ViewBag.Roles = UserRoles.GetAll();
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string username, string password, string email, 
                                                 string role, int? doctorId, IFormFile? photoFile)
        {
            // Validación: Solo usuarios con rol "Doctor" pueden tener DoctorID
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

            // Subir foto si se proporcionó
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
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Index");
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                await LoadCreateViewDataAsync();
                return View();
            }
        }

        // Método auxiliar para cargar datos del formulario Create
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

            ViewBag.AvailableDoctors = availableDoctors;
            ViewBag.Roles = UserRoles.GetAll();
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var user = await this.authRepo.GetUserByIdAsync(id);
            
            if (user == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToAction("Index");
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await this.authRepo.DeleteUserAsync(id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: Users/ChangePassword
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: Users/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "La nueva contraseña y su confirmación no coinciden";
                return View();
            }

            var userId = HttpContext.Session.GetInt32("UserID");
            
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var result = await this.authRepo.ChangePasswordAsync(userId.Value, currentPassword, newPassword);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["Error"] = result.Message;
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
