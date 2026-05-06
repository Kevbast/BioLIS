using BioLIS.Filters;
using BioLIS.Models.Entities;
using BioLIS.Services;
using Microsoft.AspNetCore.Mvc;

namespace BioLIS.Controllers
{
    [AuthorizeUsers(Policy = "AllRoles")]
    public class DoctorsController : Controller
    {
        private readonly ApiService api;
        public DoctorsController(ApiService api) => this.api = api;

        public async Task<IActionResult> Index()
            => View(await this.api.GetDoctorsAsync() ?? new());

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Inactive()
            => View(await this.api.GetInactiveDoctorsAsync() ?? new());

        [AuthorizeUsers(Policy = "AdminOnly")]
        public IActionResult Create() => View();

        [HttpPost][ValidateAntiForgeryToken][AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(Doctor doctor)
        {
            if (!ModelState.IsValid) return View(doctor);
            var result = await this.api.CreateDoctorAsync(
                doctor.FullName, doctor.LicenseNumber, doctor.Email, doctor.PhoneNumber);
            if (result.Success)
            {
                TempData["SwalType"] = "success"; TempData["SwalTitle"] = "Médico registrado";
                TempData["SwalMessage"] = "El médico se registró exitosamente.";
                return RedirectToAction("Index");
            }
            TempData["SwalType"] = "error"; TempData["SwalTitle"] = "Error";
            TempData["SwalMessage"] = result.Body;
            return View(doctor);
        }

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(int doctorId)
        {
            var d = await this.api.GetDoctorByIdAsync(doctorId);
            if (d == null) return NotFound();
            return View(d);
        }

        [HttpPost][ValidateAntiForgeryToken][AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(Doctor doctor)
        {
            if (!ModelState.IsValid) return View(doctor);
            var result = await this.api.UpdateDoctorAsync(
                doctor.DoctorID, doctor.FullName, doctor.LicenseNumber, doctor.Email, doctor.PhoneNumber);
            if (result.Success)
            {
                TempData["SwalType"] = "success"; TempData["SwalTitle"] = "Médico actualizado";
                TempData["SwalMessage"] = "Datos del médico actualizados correctamente.";
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "Error al actualizar el doctor.");
            return View(doctor);
        }

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int doctorId)
        {
            var d = await this.api.GetDoctorByIdAsync(doctorId);
            if (d == null) return NotFound();
            return View(d);
        }

        [HttpPost, ActionName("Delete")][ValidateAntiForgeryToken][AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int doctorId)
        {
            var result = await this.api.DeleteDoctorAsync(doctorId);
            TempData["SwalType"]    = result.Success ? "success" : "error";
            TempData["SwalTitle"]   = result.Success ? "Médico desactivado" : "No se pudo eliminar";
            TempData["SwalMessage"] = result.Body;
            return RedirectToAction("Index");
        }

        [HttpPost][ValidateAntiForgeryToken][AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Reactivate(int doctorId)
        {
            var result = await this.api.ReactivateDoctorAsync(doctorId);
            TempData["SwalType"]    = result.Success ? "success" : "error";
            TempData["SwalTitle"]   = result.Success ? "Médico reactivado" : "No se pudo reactivar";
            TempData["SwalMessage"] = result.Body;
            return RedirectToAction("Inactive");
        }
    }
}
