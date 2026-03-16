using BioLab.Models;
using BioLIS.Filters;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;

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

        // GET: Doctors
        public async Task<IActionResult> Index()
        {
            List<Doctor> doctors = await this.repo.GetDoctorsAsync();
            return View(doctors);
        }

        // GET: Doctors/Create
        [AuthorizeUsers(Policy = "AdminOnly")] // Solo Admin puede crear doctores
        public IActionResult Create()
        {
            return View();
        }

        // POST: Doctors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers(Policy = "AdminOnly")] // Solo Admin puede crear doctores
        public async Task<IActionResult> Create(Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                await this.repo.CreateDoctorAsync(
                    doctor.FullName,
                    doctor.LicenseNumber,
                    doctor.Email
                );

                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Médico registrado";
                TempData["SwalMessage"] = "El médico se registró exitosamente.";
                return RedirectToAction("Index");
            }

            return View(doctor);
        }

        // GET: Doctors/Update/5
        [AuthorizeUsers(Policy = "AdminOnly")] // Solo Admin puede editar doctores
        public async Task<IActionResult> Update(int doctorId)
        {
            Doctor doctor = await this.repo.GetDoctorByIdAsync(doctorId);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // POST: Doctors/Update/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers(Policy = "AdminOnly")] // Solo Admin puede editar doctores
        public async Task<IActionResult> Update(Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                bool success = await this.repo.UpdateDoctorAsync(doctor);

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

        // GET: Doctors/Delete/5
        [AuthorizeUsers(Policy = "AdminOnly")] // Solo Admin puede eliminar doctores
        public async Task<IActionResult> Delete(int doctorId)
        {
            Doctor doctor = await this.repo.GetDoctorByIdAsync(doctorId);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // POST: Doctors/DeleteConfirmed/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers(Policy = "AdminOnly")] // Solo Admin puede eliminar doctores
        public async Task<IActionResult> DeleteConfirmed(int doctorId)
        {
            var result = await this.repo.DeleteDoctorAsync(doctorId);

            if (result.Success)
            {
                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Médico eliminado";
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
    }
}
