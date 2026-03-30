using BioLIS.Models;
using BioLIS.Filters;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BioLIS.Controllers
{
    [AuthorizeUsers(Policy = "AllRoles")]
    public class DoctorsController : Controller
    {
        private CatalogRepository repo;

        public DoctorsController(CatalogRepository repo)
        {
            this.repo = repo;
        }

        public async Task<IActionResult> Index()
        {
            List<Doctor> doctors = await this.repo.GetDoctorsAsync();
            return View(doctors);
        }

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Inactive()
        {
            var doctors = await this.repo.GetInactiveDoctorsAsync();
            return View(doctors);
        }

        [AuthorizeUsers(Policy = "AdminOnly")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int? currentUserId = userIdClaim != null ? int.Parse(userIdClaim) : null;

                await this.repo.CreateDoctorAsync(
                    doctor.FullName,
                    doctor.LicenseNumber,
                    doctor.Email,
                    doctor.PhoneNumber, // NUEVO: Teléfono
                    currentUserId       // NUEVO: Auditoría
                );

                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Médico registrado";
                TempData["SwalMessage"] = "El médico se registró exitosamente.";
                return RedirectToAction("Index");
            }

            return View(doctor);
        }

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(int doctorId)
        {
            Doctor doctor = await this.repo.GetDoctorByIdAsync(doctorId);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int? currentUserId = userIdClaim != null ? int.Parse(userIdClaim) : null;

                bool success = await this.repo.UpdateDoctorAsync(doctor, currentUserId);

                if (success)
                {
                    TempData["SwalType"] = "success";
                    TempData["SwalTitle"] = "Médico actualizado";
                    TempData["SwalMessage"] = "Los datos del médico fueron actualizados correctamente.";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Error al actualizar el doctor.");
                }
            }

            return View(doctor);
        }

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int doctorId)
        {
            Doctor doctor = await this.repo.GetDoctorByIdAsync(doctorId);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int doctorId)
        {
            var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? currentUserId = userIdClaim != null ? int.Parse(userIdClaim) : null;

            var result = await this.repo.DeleteDoctorAsync(doctorId, currentUserId);

            if (result.Success)
            {
                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Médico desactivado";
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Reactivate(int doctorId)
        {
            var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? currentUserId = userIdClaim != null ? int.Parse(userIdClaim) : null;

            var result = await this.repo.ReactivateDoctorAsync(doctorId, currentUserId);

            if (result.Success)
            {
                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Médico reactivado";
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
    }
}