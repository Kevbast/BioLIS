using BioLIS.Filters;
using BioLIS.Models.Entities;
using BioLIS.Services;
using Microsoft.AspNetCore.Mvc;

namespace BioLIS.Controllers
{
    [AuthorizeUsers(Policy = "AdminOnly")]
    public class RecoveryController : Controller
    {
        private readonly ApiService api;

        public RecoveryController(ApiService api)
        {
            this.api = api;
        }

        public async Task<IActionResult> Index(string tab = "usuarios")
        {
            string activeTab = (tab ?? "usuarios").Trim().ToLowerInvariant();
            ViewBag.ActiveTab = activeTab;

            switch (activeTab)
            {
                case "pacientes":
                    ViewData["Items"] = await this.api.GetInactivePatientsAsync() ?? new List<Patient>();
                    break;
                case "medicos":
                    ViewData["Items"] = await this.api.GetInactiveDoctorsAsync() ?? new List<Doctor>();
                    break;
                case "examenes":
                    ViewData["Items"] = await this.api.GetInactiveLabTestsAsync() ?? new List<LabTest>();
                    break;
                case "muestras":
                    ViewData["Items"] = await this.api.GetInactiveSampleTypesAsync() ?? new List<SampleType>();
                    break;
                case "rangos":
                    ViewData["Items"] = await this.api.GetInactiveReferenceRangesAsync() ?? new List<ReferenceRange>();
                    break;
                default:
                    ViewData["Items"] = await this.api.GetInactiveUsersAsync() ?? new List<User>();
                    break;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateUser(int id)
        {
            var result = await this.api.ReactivateUserAsync(id);
            TempData["SwalType"] = result.Success ? "success" : "error";
            TempData["SwalTitle"] = result.Success ? "Usuario Reactivado" : "Error";
            TempData["SwalMessage"] = result.Body;
            return RedirectToAction("Index", new { tab = "usuarios" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivatePatient(int id)
        {
            var result = await this.api.ReactivatePatientAsync(id);
            TempData["SwalType"] = result.Success ? "success" : "error";
            TempData["SwalTitle"] = result.Success ? "Paciente Reactivado" : "Error";
            TempData["SwalMessage"] = result.Body;
            return RedirectToAction("Index", new { tab = "pacientes" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateDoctor(int id)
        {
            var result = await this.api.ReactivateDoctorAsync(id);
            TempData["SwalType"] = result.Success ? "success" : "error";
            TempData["SwalTitle"] = result.Success ? "Médico Reactivado" : "Error";
            TempData["SwalMessage"] = result.Body;
            return RedirectToAction("Index", new { tab = "medicos" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateLabTest(int id)
        {
            var result = await this.api.ReactivateLabTestAsync(id);
            TempData["SwalType"] = result.Success ? "success" : "error";
            TempData["SwalTitle"] = result.Success ? "Examen Reactivado" : "Error";
            TempData["SwalMessage"] = result.Body;
            return RedirectToAction("Index", new { tab = "examenes" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateSampleType(int id)
        {
            var result = await this.api.ReactivateSampleTypeAsync(id);
            TempData["SwalType"] = result.Success ? "success" : "error";
            TempData["SwalTitle"] = result.Success ? "Tipo de muestra Reactivado" : "Error";
            TempData["SwalMessage"] = result.Body;
            return RedirectToAction("Index", new { tab = "muestras" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateReferenceRange(int id)
        {
            var result = await this.api.ReactivateReferenceRangeAsync(id);
            TempData["SwalType"] = result.Success ? "success" : "error";
            TempData["SwalTitle"] = result.Success ? "Rango Reactivado" : "Error";
            TempData["SwalMessage"] = result.Body;
            return RedirectToAction("Index", new { tab = "rangos" });
        }
    }
}
