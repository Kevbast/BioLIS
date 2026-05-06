using BioLIS.Filters;
using BioLIS.Models;
using BioLIS.Models.DTOs.Portal;
using BioLIS.Models.Entities;
using BioLIS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using System.Security.Claims;

namespace BioLIS.Controllers
{
    [AuthorizeUsers]
    public class OrdersController : Controller
    {
        private readonly ApiService api;
        private readonly PdfReportService pdfService;

        public OrdersController(ApiService api, PdfReportService pdfService)
        {
            this.api        = api;
            this.pdfService = pdfService;
        }

        public async Task<IActionResult> Index()
        {
            var role   = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            List<Order> orders;

            if (role == BioLIS.Models.Common.UserRoles.Doctor)
            {
                var doctorIdClaim = HttpContext.User.FindFirstValue("DoctorID");
                orders = int.TryParse(doctorIdClaim, out int did)
                    ? await this.api.GetOrdersByDoctorAsync(did) ?? new()
                    : new();
            }
            else
            {
                orders = await this.api.GetAllOrdersAsync() ?? new();
            }
            return View(orders);
        }

        [AuthorizeUsers(Policy = "AllRoles")]
        public async Task<IActionResult> Create()
        {
            var patientsTask = this.api.GetPatientsAsync();
            var doctorsTask = this.api.GetDoctorsAsync();
            var labTestsTask = this.api.GetLabTestsAsync();

            await Task.WhenAll(patientsTask, doctorsTask, labTestsTask);

            var patients = patientsTask.Result ?? new();
            var doctors = doctorsTask.Result ?? new();
            var labTests = labTestsTask.Result ?? new();

            ViewData["Patients"] = patients.Select(p => new SelectListItem
            {
                Value = p.PatientID.ToString(),
                Text  = $"{p.FirstName} {p.LastName} - {p.PatientID}"
            }).ToList();

            ViewData["Doctors"] = doctors.Select(d => new SelectListItem
            {
                Value = d.DoctorID.ToString(),
                Text  = $"{d.FullName} ({d.LicenseNumber ?? "Sin licencia"})"
            }).ToList();

            ViewData["LabTests"] = labTests;

            if (HttpContext.User.FindFirstValue(ClaimTypes.Role) == BioLIS.Models.Common.UserRoles.Doctor)
                ViewData["PreselectedDoctorId"] = HttpContext.User.FindFirstValue("DoctorID");

            return View();
        }

        [HttpPost][ValidateAntiForgeryToken][AuthorizeUsers(Policy = "AllRoles")]
        public async Task<IActionResult> Create(int patientId, int doctorId, List<int> selectedTests)
        {
            var role = HttpContext.User.FindFirstValue(ClaimTypes.Role);

            if (role == BioLIS.Models.Common.UserRoles.Doctor)
            {
                var claimDoctorId = HttpContext.User.FindFirstValue("DoctorID");
                if (int.TryParse(claimDoctorId, out int myDoctorId) && doctorId != myDoctorId)
                {
                    TempData["ErrorMessage"] = "Solo puedes crear órdenes para ti mismo.";
                    return RedirectToAction("Create");
                }
            }

            if (patientId <= 0 || doctorId <= 0)
            {
                TempData["ErrorMessage"] = "Debe seleccionar un paciente y un doctor.";
                return RedirectToAction("Create");
            }
            if (selectedTests == null || !selectedTests.Any())
            {
                TempData["ErrorMessage"] = "Debe seleccionar al menos un examen.";
                return RedirectToAction("Create");
            }

            var orderResponse = await this.api.CreateOrderAsync(patientId, doctorId);
            if (orderResponse == null)
            {
                TempData["ErrorMessage"] = "Error al crear la orden.";
                return RedirectToAction("Create");
            }

            foreach (var testId in selectedTests)
                await this.api.AddTestResultAsync(orderResponse.OrderId, testId);

            TempData["SwalType"]    = "success";
            TempData["SwalTitle"]   = "Orden creada";
            TempData["SwalMessage"] = $"Orden {orderResponse.OrderNumber} creada con {selectedTests.Count} exámenes.";
            return RedirectToAction("Details", new { orderId = orderResponse.OrderId });
        }

        public async Task<IActionResult> Details(int orderId)
        {
            var order = await this.api.GetOrderByIdAsync(orderId);
            if (order == null) return NotFound();

            var role = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            if (role == BioLIS.Models.Common.UserRoles.Doctor)
            {
                var claimDoctorId = HttpContext.User.FindFirstValue("DoctorID");
                if (int.TryParse(claimDoctorId, out int did) && order.DoctorID != did)
                    return RedirectToAction("ErrorAcceso", "Auth");
            }

            ViewData["Results"] = await this.api.GetResultsByOrderAsync(orderId) ?? new();
            return View(order);
        }

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int orderId)
        {
            var order = await this.api.GetOrderByIdAsync(orderId);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost, ActionName("Delete")][ValidateAntiForgeryToken][AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int orderId)
        {
            var result = await this.api.DeleteOrderAsync(orderId);
            TempData["SwalType"]    = result.Success ? "success" : "error";
            TempData["SwalTitle"]   = result.Success ? "Orden eliminada" : "No se pudo eliminar";
            TempData["SwalMessage"] = result.Body;
            return RedirectToAction("Index");
        }

        [AuthorizeUsers(Policy = "AdminOrLab")]
        public async Task<IActionResult> EnterResults(int orderId)
        {
            var order = await this.api.GetOrderByIdAsync(orderId);
            if (order == null) return NotFound();
            ViewData["Results"] = await this.api.GetResultsByOrderAsync(orderId) ?? new();
            ViewData["Summary"] = await this.api.GetOrderSummaryAsync(orderId);
            return View(order);
        }

        [HttpPost][ValidateAntiForgeryToken][AuthorizeUsers(Policy = "AdminOrLab")]
        public async Task<IActionResult> EnterResults(int orderId,
            Dictionary<int, string> resultValues, Dictionary<int, string> notes)
        {
            var order = await this.api.GetOrderByIdAsync(orderId);
            if (order == null) return RedirectToAction("Index");

            int updatedCount = 0, errorCount = 0;

            // Carga los resultados una sola vez para tener los TestIDs
            var results = await this.api.GetResultsByOrderAsync(orderId) ?? new();

            foreach (var kvp in resultValues)
            {
                if (string.IsNullOrWhiteSpace(kvp.Value)) continue;
                if (!TryParseResultValue(kvp.Value, out decimal resultValue)) { errorCount++; continue; }

                string? note       = notes.TryGetValue(kvp.Key, out var n) ? n : null;
                var     testResult = results.FirstOrDefault(r => r.ResultID == kvp.Key);
                if (testResult == null) { errorCount++; continue; }

                var r = await this.api.UpdateTestResultAsync(kvp.Key, resultValue, null, note);
                if (r.Success) updatedCount++; else errorCount++;
            }

            if (updatedCount > 0)
            {
                var fresh = await this.api.GetOrderByIdAsync(orderId);
                if (fresh?.Status == "Pendiente")
                    await this.api.ChangeOrderStatusAsync(orderId, "EnProceso");

                TempData["SwalType"]    = "success";
                TempData["SwalTitle"]   = "Resultados actualizados";
                TempData["SwalMessage"] = $"{updatedCount} resultado(s) actualizado(s) exitosamente.";
            }
            if (errorCount > 0)
            {
                TempData["SwalType"]    = "warning";
                TempData["SwalTitle"]   = "Resultados con incidencias";
                TempData["SwalMessage"] = $"{errorCount} resultado(s) no pudieron ser actualizados.";
            }
            return RedirectToAction("Details", new { orderId });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int orderId, string status)
        {
            var role  = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            var order = await this.api.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Orden no encontrada.";
                return RedirectToAction("Index");
            }

            if (status == "Aprobada")
            {
                if (role != BioLIS.Models.Common.UserRoles.Doctor)
                {
                    TempData["ErrorMessage"] = "Solo el médico puede aprobar la orden.";
                    return RedirectToAction("Details", new { orderId });
                }
                var claimDoctorId = HttpContext.User.FindFirstValue("DoctorID");
                if (!int.TryParse(claimDoctorId, out int did) || order.DoctorID != did)
                {
                    TempData["ErrorMessage"] = "No tienes permisos para aprobar esta orden.";
                    return RedirectToAction("Details", new { orderId });
                }
            }

            if (status == "Completada" && role != BioLIS.Models.Common.UserRoles.Admin && role != BioLIS.Models.Common.UserRoles.Laboratorio)
            {
                TempData["ErrorMessage"] = "Solo Admin o Laboratorio pueden marcar una orden como completada.";
                return RedirectToAction("Details", new { orderId });
            }

            // La API gestiona automáticamente notificaciones, ShareToken e IntegrationEvent
            var result = await this.api.ChangeOrderStatusAsync(orderId, status);
            TempData["SwalType"]    = result.Success ? "success" : "error";
            TempData["SwalTitle"]   = result.Success ? "Estado actualizado" : "Error de actualización";
            TempData["SwalMessage"] = result.Body;
            return RedirectToAction("Details", new { orderId });
        }

        public async Task<IActionResult> Print(int orderId)
        {
            var order = await this.api.GetOrderByIdAsync(orderId);
            if (order == null) return NotFound();

            if (order.Status != "Aprobada")
            {
                TempData["ErrorMessage"] = "La orden debe estar 'Aprobada' para imprimir.";
                return RedirectToAction("Details", new { orderId });
            }

            var role = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            if (role == BioLIS.Models.Common.UserRoles.Doctor)
            {
                var claimDoctorId = HttpContext.User.FindFirstValue("DoctorID");
                if (int.TryParse(claimDoctorId, out int did) && order.DoctorID != did)
                    return RedirectToAction("ErrorAcceso", "Auth");
            }

            // Construimos los DTOs a partir de los datos que ya tenemos del MVC
            var results = await this.api.GetResultsByOrderAsync(orderId) ?? new();

            // Calculamos la edad (igual que hace la API)
            int age = DateTime.Today.Year - order.Patient.BirthDate.Year;
            if (order.Patient.BirthDate.Date > DateTime.Today.AddYears(-age)) age--;

            var orderDto = new PortalOrderDto
            {
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                Status = order.Status,
                PatientName = $"{order.Patient.FirstName} {order.Patient.LastName}",
                PatientAge = age,
                PatientGender = order.Patient.Gender,
                DoctorName = order.Doctor?.FullName ?? "N/D",
                DoctorLicense = order.Doctor?.LicenseNumber,
                ApproverName = order.ApprovedByUser?.Username
            };

            var resultsDto = results.Select(r =>
            {
                // Buscamos el rango de referencia aplicable
                var rr = r.LabTest.ReferenceRanges?
                    .Where(x => (x.Gender == order.Patient.Gender || x.Gender == "A") && age >= x.MinAgeYear && age <= x.MaxAgeYear)
                    .OrderBy(x => x.Gender == order.Patient.Gender ? 0 : 1)
                    .ThenBy(x => x.MaxAgeYear - x.MinAgeYear)
                    .FirstOrDefault();

                return new PortalResultDto
                {
                    TestName = r.LabTest.TestName,
                    ResultValue = r.ResultValue,
                    Units = r.LabTest.Units,
                    ReferenceRangeText = rr != null ? $"{rr.MinVal:0.##}-{rr.MaxVal:0.##}" : "Sin rango",
                    AlertLevel = r.AlertLevel,
                    Notes = r.Notes,
                    EnteredByName = r.EnteredByUser?.Username,
                    EnteredDate = r.EnteredDate,
                    ModifiedByName = r.ModifiedByUser?.Username,
                    ModifiedDate = r.ModifiedDate
                };
            }).ToList();

            // Ahora sí le pasamos los DTOs al PDF
            var pdfBytes = this.pdfService.GenerateResultsPdf(orderDto, resultsDto);
            return File(pdfBytes, "application/pdf", $"Resultados_{order.OrderNumber}.pdf");
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static bool TryParseResultValue(string input, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;
            var raw = input.Trim().Replace(" ", string.Empty);
            string norm;
            if (raw.Contains(',') && raw.Contains('.'))
            {
                bool commaIsDecimal = raw.LastIndexOf(',') > raw.LastIndexOf('.');
                norm = commaIsDecimal
                    ? raw.Replace(".", string.Empty).Replace(',', '.')
                    : raw.Replace(",", string.Empty);
            }
            else if (raw.Contains(','))
                norm = raw.Replace(',', '.');
            else
                norm = raw;
            return decimal.TryParse(norm, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }
    }
}
