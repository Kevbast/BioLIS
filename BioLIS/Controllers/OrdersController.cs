using BioLab.Models;
using BioLIS.Filters;
using BioLIS.Repositories;
using BioLIS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BioLIS.Controllers
{
    [AuthorizeUsers] // Base: requiere autenticación
    public class OrdersController : Controller
    {
        private readonly OrderRepository orderRepo;
        private readonly CatalogRepository catalogRepo;
        private readonly HelperRepository helperRepo;
        private readonly PdfReportService pdfReportService;

        public OrdersController(OrderRepository orderRepo, CatalogRepository catalogRepo,
                               HelperRepository helperRepo, PdfReportService pdfReportService)
        {
            this.orderRepo = orderRepo;
            this.catalogRepo = catalogRepo;
            this.helperRepo = helperRepo;
            this.pdfReportService = pdfReportService;
        }

        // ========================================
        // INDEX: Ver órdenes según rol
        // ========================================
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            List<Order> orders;

            // Si es Doctor, solo ver SUS órdenes
            if (role == "Doctor")
            {
                var doctorIdClaim = HttpContext.User.FindFirstValue("DoctorID");
                if (int.TryParse(doctorIdClaim, out int doctorId))
                {
                    orders = await orderRepo.GetOrdersByDoctorAsync(doctorId);
                }
                else
                {
                    orders = new List<Order>();
                }
            }
            else
            {
                // Admin y Laboratorio ven todas
                orders = await orderRepo.GetAllOrdersAsync();
            }

            return View(orders);
        }

        // ========================================
        // CREATE: Todos pueden crear órdenes
        // ========================================
        [AuthorizeUsers(Policy = "AllRoles")]
        public async Task<IActionResult> Create()
        {
            var patients = await catalogRepo.GetPatientsAsync();
            ViewData["Patients"] = patients.Select(p => new SelectListItem
            {
                Value = p.PatientID.ToString(),
                Text = $"{p.FirstName} {p.LastName} - {p.PatientID}"
            }).ToList();

            var doctors = await catalogRepo.GetDoctorsAsync();
            ViewData["Doctors"] = doctors.Select(d => new SelectListItem
            {
                Value = d.DoctorID.ToString(),
                Text = $"{d.FullName} ({d.LicenseNumber ?? "Sin licencia"})"
            }).ToList();

            var labTests = await catalogRepo.GetLabTestsAsync();
            ViewData["LabTests"] = labTests;

            // Si es Doctor, pre-seleccionar su DoctorID
            var role = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            if (role == "Doctor")
            {
                var doctorIdClaim = HttpContext.User.FindFirstValue("DoctorID");
                ViewData["PreselectedDoctorId"] = doctorIdClaim;
            }

            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers(Policy = "AllRoles")]
        public async Task<IActionResult> Create(int patientId, int doctorId, List<int> selectedTests)
        {
            // Validar que sea su propio doctorId si es Doctor
            var role = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            if (role == "Doctor")
            {
                var doctorIdClaim = HttpContext.User.FindFirstValue("DoctorID");
                if (int.TryParse(doctorIdClaim, out int userDoctorId))
                {
                    if (doctorId != userDoctorId)
                    {
                        TempData["ErrorMessage"] = "Solo puedes crear órdenes para ti mismo.";
                        return RedirectToAction("Create");
                    }
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

            var order = await orderRepo.CreateOrderAsync(patientId, doctorId);

            var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? userId = userIdClaim != null ? int.Parse(userIdClaim) : null;

            foreach (var testId in selectedTests)
            {
                await orderRepo.AddTestResultAsync(order.OrderID, testId, null, null, userId);
            }

            TempData["SwalType"] = "success";
            TempData["SwalTitle"] = "Orden creada";
            TempData["SwalMessage"] = $"Orden {order.OrderNumber} creada exitosamente con {selectedTests.Count} exámenes.";
            return RedirectToAction("Details", new { orderId = order.OrderID });
        }

        // ========================================
        // DETAILS: Ver detalles (con control de acceso para Doctor)
        // ========================================
        public async Task<IActionResult> Details(int orderId)
        {
            var order = await orderRepo.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            // Verificar permisos: Doctor solo puede ver sus órdenes
            var role = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            if (role == "Doctor")
            {
                var doctorIdClaim = HttpContext.User.FindFirstValue("DoctorID");
                if (int.TryParse(doctorIdClaim, out int doctorId))
                {
                    if (order.DoctorID != doctorId)
                    {
                        return RedirectToAction("ErrorAcceso", "Auth");
                    }
                }
            }

            var results = await orderRepo.GetResultsByOrderAsync(orderId);
            ViewData["Results"] = results;

            return View(order);
        }

        // ========================================
        // DELETE: Solo Admin
        // ========================================
        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int orderId)
        {
            var order = await orderRepo.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int orderId)
        {
            var result = await orderRepo.DeleteOrderAsync(orderId);

            if (result.Success)
            {
                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Orden eliminada";
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

        // ========================================
        // ENTER RESULTS: Solo Admin y Laboratorio
        // ========================================
        [AuthorizeUsers(Policy = "AdminOrLab")]
        public async Task<IActionResult> EnterResults(int orderId)
        {
            var order = await orderRepo.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            var results = await orderRepo.GetResultsByOrderAsync(orderId);
            ViewData["Results"] = results;

            var summary = await orderRepo.GetOrderSummaryAsync(orderId);
            ViewData["Summary"] = summary;

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers(Policy = "AdminOrLab")]
        public async Task<IActionResult> EnterResults(int orderId, Dictionary<int, string> resultValues, Dictionary<int, string> notes)
        {
            var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            int updatedCount = 0;
            int errorCount = 0;

            foreach (var kvp in resultValues)
            {
                int resultId = kvp.Key;
                string valueStr = kvp.Value;

                if (string.IsNullOrWhiteSpace(valueStr))
                    continue;

                if (decimal.TryParse(valueStr, out decimal resultValue))
                {
                    string note = notes.ContainsKey(resultId) ? notes[resultId] : null;

                    var testResult = await catalogRepo.Context.TestResults
                        .Include(tr => tr.Order)
                        .FirstOrDefaultAsync(tr => tr.ResultID == resultId);

                    if (testResult != null)
                    {
                        var validation = await helperRepo.ValidateResultAsync(
                            testResult.TestID,
                            testResult.Order.PatientID,
                            resultValue
                        );

                        string alertLevel = validation.Status;

                        bool success = await orderRepo.UpdateTestResultWithAuditAsync(
                            resultId, resultValue, alertLevel, userId, note
                        );

                        if (success)
                            updatedCount++;
                        else
                            errorCount++;
                    }
                }
                else
                {
                    errorCount++;
                }
            }

            // Automatización: Cambiar el estado a "EnProceso" si se actualizaron resultados y estaba en "Pendiente"
            if (updatedCount > 0)
            {
                var currentOrder = await orderRepo.GetOrderByIdAsync(orderId);
                if (currentOrder != null && currentOrder.Status == "Pendiente")
                {
                    await orderRepo.ChangeOrderStatusAsync(orderId, "EnProceso");
                }

                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Resultados actualizados";
                TempData["SwalMessage"] = $"{updatedCount} resultado(s) actualizado(s) exitosamente.";
            }

            if (errorCount > 0)
            {
                TempData["SwalType"] = "warning";
                TempData["SwalTitle"] = "Resultados con incidencias";
                TempData["SwalMessage"] = $"{errorCount} resultado(s) no pudieron ser actualizados.";
            }

            return RedirectToAction("Details", new { orderId });
        }

        // ========================================
        // CHANGE STATUS: Marcar como Completada o Aprobada
        // ========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int orderId, string status)
        {
            var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var role = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            var order = await orderRepo.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Orden no encontrada.";
                return RedirectToAction("Index");
            }

            // Reglas de estado por rol
            if (status == "Aprobada")
            {
                // Solo Doctor puede aprobar
                if (role != "Doctor")
                {
                    TempData["ErrorMessage"] = "Solo el médico puede aprobar la orden.";
                    return RedirectToAction("Details", new { orderId });
                }

                // El doctor solo puede aprobar sus propias órdenes
                var doctorIdClaim = HttpContext.User.FindFirstValue("DoctorID");
                if (!int.TryParse(doctorIdClaim, out int doctorId) || order.DoctorID != doctorId)
                {
                    TempData["ErrorMessage"] = "No tienes permisos para aprobar esta orden.";
                    return RedirectToAction("Details", new { orderId });
                }
            }

            if (status == "Completada" && role != "Admin" && role != "Laboratorio")
            {
                TempData["ErrorMessage"] = "Solo Admin o Laboratorio pueden marcar una orden como completada.";
                return RedirectToAction("Details", new { orderId });
            }

            var statusResult = await orderRepo.ChangeOrderStatusAsync(orderId, status, userId);

            if (statusResult.Success)
            {
                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Estado actualizado";
                TempData["SwalMessage"] = statusResult.Message;
            }
            else
            {
                TempData["SwalType"] = "error";
                TempData["SwalTitle"] = "Error de actualización";
                TempData["SwalMessage"] = statusResult.Message;
            }

            return RedirectToAction("Details", new { orderId });
        }

        // ========================================
        // PRINT: Todos pueden imprimir (con control de acceso)
        // ========================================
        public async Task<IActionResult> Print(int orderId)
        {
            var order = await orderRepo.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            // BLOQUEO DE SEGURIDAD: Solo se imprime si está Aprobada
            if (order.Status != "Aprobada")
            {
                TempData["ErrorMessage"] = "Acceso denegado: La orden debe estar 'Aprobada' por el médico para poder imprimirse.";
                return RedirectToAction("Details", new { orderId = orderId });
            }

            // Verificar permisos: Los doctores solo pueden imprimir sus órdenes
            var role = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            if (role == "Doctor")
            {
                var doctorIdClaim = HttpContext.User.FindFirstValue("DoctorID");
                if (int.TryParse(doctorIdClaim, out int doctorId))
                {
                    if (order.DoctorID != doctorId)
                    {
                        return RedirectToAction("ErrorAcceso", "Auth");
                    }
                }
            }

            var results = await orderRepo.GetResultsByOrderAsync(orderId);
            var pdfBytes = pdfReportService.GenerateResultsPdf(order, results);

            return File(pdfBytes, "application/pdf", $"Resultados_{order.OrderNumber}.pdf");
        }
    }
}