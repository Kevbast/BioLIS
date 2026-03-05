using BioLab.Models;
using BioLIS.Filters;
using BioLIS.Models;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BioLIS.Controllers
{
    [AuthorizeRole("Admin")] // Solo admins pueden gestionar usuarios
    public class UsersController : Controller
    {
        private readonly AuthRepository authRepo;
        private readonly CatalogRepository catalogRepo;

        public UsersController(AuthRepository authRepo, CatalogRepository catalogRepo)
        {
            this.authRepo = authRepo;
            this.catalogRepo = catalogRepo;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            // Obtener usuarios completos desde la tabla Users
            var users = await this.authRepo.GetAllUsersAsync();
            
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
            // Cargar lista de doctores para el dropdown
            ViewBag.Doctors = await this.catalogRepo.GetDoctorsAsync();
            ViewBag.Roles = UserRoles.GetAll();
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string username, string password, string email, 
                                                 string role, int? doctorId, IFormFile? photoFile)
        {
            string photoFilename = "default-user.png";

            // Subir foto si se proporcionó
            if (photoFile != null)
            {
                photoFilename = photoFile.FileName;
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "users", photoFilename);
                
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
                TempData["Success"] = result.Message;
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Error"] = result.Message;
                ViewBag.Doctors = await this.catalogRepo.GetDoctorsAsync();
                ViewBag.Roles = UserRoles.GetAll();
                return View();
            }
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
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
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
