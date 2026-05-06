using BioLIS.Filters;
using BioLIS.Models.Entities;
using BioLIS.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BioLIS.Controllers
{
    [AuthorizeUsers]
    public class PatientsController : Controller
    {
        private readonly ApiService api;
        public PatientsController(ApiService api) => this.api = api;

        public async Task<IActionResult> Index()
            => View(await this.api.GetPatientsAsync() ?? new());

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Inactive()
            => View(await this.api.GetInactivePatientsAsync() ?? new());

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Patient patient, IFormFile? fichero)
        {
            var result  = await this.api.CreatePatientAsync(
                patient.FirstName, patient.LastName, patient.Gender, patient.BirthDate,
                patient.Email, null, patient.PhoneNumber, fichero);

            if (result.Success)
            {
                TempData["SwalType"]    = "success";
                TempData["SwalTitle"]   = "Paciente registrado";
                TempData["SwalMessage"] = $"Se registró correctamente a {patient.FirstName} {patient.LastName}.";
                return RedirectToAction("Index");
            }
            TempData["SwalType"] = "error"; TempData["SwalTitle"] = "Error";
            TempData["SwalMessage"] = result.Body;
            return View(patient);
        }

        public async Task<IActionResult> Update(int patientId)
        {
            var p = await this.api.GetPatientByIdAsync(patientId);
            if (p == null) return RedirectToAction("Index");
            return View(p);
        }

        [HttpPost]
        public async Task<IActionResult> Update(Patient patient, IFormFile? fichero)
        {
            var result  = await this.api.UpdatePatientAsync(
                patient.PatientID, patient.FirstName, patient.LastName, patient.Gender,
                patient.BirthDate, patient.Email, patient.PhotoFilename, patient.PhoneNumber, fichero);

            if (result.Success)
            {
                TempData["SwalType"]    = "success";
                TempData["SwalTitle"]   = "Paciente actualizado";
                TempData["SwalMessage"] = $"Datos de {patient.FirstName} {patient.LastName} actualizados.";
                return RedirectToAction("Index");
            }
            ViewData["MENSAJE"] = "Error al actualizar al paciente.";
            return View(patient);
        }

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int patientId)
        {
            var result = await this.api.DeletePatientAsync(patientId);
            TempData["SwalType"]    = result.Success ? "success" : "error";
            TempData["SwalTitle"]   = result.Success ? "Paciente desactivado" : "No se pudo eliminar";
            TempData["SwalMessage"] = result.Success ? "El paciente fue desactivado correctamente." : result.Body;
            return RedirectToAction("Index");
        }

        [HttpPost][ValidateAntiForgeryToken][AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Reactivate(int patientId)
        {
            var result = await this.api.ReactivatePatientAsync(patientId);
            TempData["SwalType"]    = result.Success ? "success" : "error";
            TempData["SwalTitle"]   = result.Success ? "Paciente reactivado" : "No se pudo reactivar";
            TempData["SwalMessage"] = result.Body;
            return RedirectToAction("Inactive");
        }

        [AuthorizeUsers(Policy = "AllRoles")]
        public async Task<IActionResult> History(int patientId)
        {
            var patient = await this.api.GetPatientByIdAsync(patientId);
            if (patient == null) return NotFound();

            var history = await this.api.GetPatientHistoryAsync(patientId) ?? new();
            if (!history.Any())
            {
                var orders = await this.api.GetOrdersByPatientAsync(patientId) ?? new();
                foreach (var order in orders)
                {
                    var results = await this.api.GetResultsByOrderAsync(order.OrderID) ?? new();
                    foreach (var result in results)
                    {
                        result.Order ??= order;
                        history.Add(result);
                    }
                }
            }
            var data    = history
                .Where(h => h.Order != null && h.LabTest != null)
                .Select(h => new
                {
                    Date     = h.Order.OrderDate.ToString("dd/MM/yyyy"),
                    TestName = h.LabTest.TestName,
                    Value    = h.ResultValue,
                    Units    = h.LabTest.Units
                }).ToList();

            ViewData["AvailableTests"] = data.Select(h => h.TestName).Distinct().ToList();
            ViewData["HistoryJson"]    = JsonSerializer.Serialize(data);
            return View(patient);
        }
    }
}
