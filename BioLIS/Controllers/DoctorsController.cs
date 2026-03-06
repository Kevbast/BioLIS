using BioLab.Models;
using BioLIS.Filters;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BioLIS.Controllers
{
    [AuthorizeSession]
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
        [AuthorizeRole("Admin")] // Solo Admin puede crear doctores
        public IActionResult Create()
        {
            return View();
        }

        // POST: Doctors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole("Admin")] // Solo Admin puede crear doctores
        public async Task<IActionResult> Create(Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                await this.repo.CreateDoctorAsync(
                    doctor.FullName,
                    doctor.LicenseNumber,
                    doctor.Email
                );

                TempData["SuccessMessage"] = "Doctor registrado exitosamente.";
                return RedirectToAction("Index");
            }

            return View(doctor);
        }

        // GET: Doctors/Update/5
        [AuthorizeRole("Admin")] // Solo Admin puede editar doctores
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
        [AuthorizeRole("Admin")] // Solo Admin puede editar doctores
        public async Task<IActionResult> Update(Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                bool success = await this.repo.UpdateDoctorAsync(doctor);

                if (success)
                {
                    TempData["SuccessMessage"] = "Doctor actualizado exitosamente.";
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
        [AuthorizeRole("Admin")] // Solo Admin puede eliminar doctores
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
        [AuthorizeRole("Admin")] // Solo Admin puede eliminar doctores
        public async Task<IActionResult> DeleteConfirmed(int doctorId)
        {
            var result = await this.repo.DeleteDoctorAsync(doctorId);

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
    }
}
