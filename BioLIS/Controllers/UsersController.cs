using BioLIS.Filters;
using BioLIS.Helpers;
using BioLIS.Models;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace BioLIS.Controllers
{
    [AuthorizeUsers]
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

        [AuthorizeUsers(Policy = "AdminOnly")]
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

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Inactive()
        {
            var users = await this.authRepo.GetInactiveUsersAsync();
            return View(users);
        }

        [AuthorizeUsers(Policy = "AdminOnly")]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers(Policy = "AdminOnly")]
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

            TempData["ErrorMessage"] = result.Message;
            await LoadCreateViewDataAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Reactivate(int id)
        {
            var result = await this.authRepo.ReactivateUserAsync(id);

            if (result.Success)
            {
                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Usuario reactivado";
                TempData["SwalMessage"] = result.Message;
            }
            else
            {
                TempData["SwalType"] = "error";
                TempData["SwalTitle"] = "No se pudo reactivar";
                TempData["SwalMessage"] = result.Message;
            }

            return RedirectToAction("Inactive");
        }

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

        [AuthorizeUsers(Policy = "AdminOnly")]
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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await this.authRepo.DeleteUserAsync(id);

            if (result.Success)
            {
                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Usuario desactivado";
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

        [AuthorizeUsers]
        public async Task<IActionResult> ChangePassword()
        {
            var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await this.authRepo.GetUserByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            return View(user);
        }

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

            var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var currentUser = await this.authRepo.GetUserByIdAsync(userId);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            string photoFilename = currentUser.PhotoFilename;
            if (photoFile != null)
            {
                photoFilename = photoFile.FileName;
                string path = this.pathHelper.MapPath(photoFilename, Folders.Users);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await photoFile.CopyToAsync(stream);
                }
            }

            // BUSCAR EL ROL EXACTO PARA ACTUALIZAR (Ya que ahora la tabla Users tiene RoleID)
            var roleEntity = await this.catalogRepo.Context.Roles.FindAsync(currentUser.RoleID);
            string currentRoleName = roleEntity?.RoleName ?? "Laboratorio";

            var result = await this.authRepo.UpdateUserAsync(
                userId,
                username,
                email,
                photoFilename,
                null,
                currentRoleName,
                currentUser.DoctorID
            );

            if (result.Success)
            {
                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Perfil actualizado";
                TempData["SwalMessage"] = result.Message;
            }
            else
            {
                TempData["SwalType"] = "error";
                TempData["SwalTitle"] = "No se pudo actualizar";
                TempData["SwalMessage"] = result.Message;
            }

            return RedirectToAction("ChangePassword");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["SwalType"] = "warning";
                TempData["SwalTitle"] = "Contraseñas no coinciden";
                TempData["SwalMessage"] = "La nueva contraseña y su confirmación no coinciden";
                var userIdForError = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdForError) || !int.TryParse(userIdForError, out int currentUserId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var currentUserForError = await this.authRepo.GetUserByIdAsync(currentUserId);
                return View(currentUserForError);
            }

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
                return RedirectToAction("ChangePassword");
            }
            else
            {
                TempData["SwalType"] = "error";
                TempData["SwalTitle"] = "No se pudo cambiar";
                TempData["SwalMessage"] = result.Message;
                var currentUser = await this.authRepo.GetUserByIdAsync(userId);
                return View(currentUser);
            }
        }

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Stats()
        {
            var stats = await this.authRepo.CountUsersByRoleAsync();
            return View(stats);
        }
    }
}