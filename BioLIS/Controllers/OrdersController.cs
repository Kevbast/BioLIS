using BioLIS.Models;
using BioLIS.Filters;
using BioLIS.Hubs;
using BioLIS.Repositories;
using BioLIS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Globalization;

namespace BioLIS.Controllers
{
    [AuthorizeUsers]
    public class OrdersController : Controller
    {
        private readonly OrderRepository orderRepo;
        private readonly CatalogRepository catalogRepo;
        private readonly HelperRepository helperRepo;
        private readonly PdfReportService pdfReportService;
        private readonly IHubContext<NotificationHub> hubContext;

        public OrdersController(OrderRepository orderRepo, CatalogRepository catalogRepo,
                               HelperRepository helperRepo, PdfReportService pdfReportService,
                               IHubContext<NotificationHub> hubContext)
        {
            this.orderRepo = orderRepo;
            this.catalogRepo = catalogRepo;
            this.helperRepo = helperRepo;
            this.pdfReportService = pdfReportService;
            this.hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            var role = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            List<Order> orders;

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
                orders = await orderRepo.GetAllOrdersAsync();
            }

            return View(orders);
        }

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

            var role = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            if (role == "Doctor")
            {
                var doctorIdClaim = HttpContext.User.FindFirstValue("DoctorID");
                ViewData["PreselectedDoctorId"] = doctorIdClaim;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers(Policy = "AllRoles")]
        public async Task<IActionResult> Create(int patientId, int doctorId, List<int> selectedTests)
        {
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

            var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? userId = userIdClaim != null ? int.Parse(userIdClaim) : null;

            var order = await orderRepo.CreateOrderAsync(patientId, doctorId, userId);

            foreach (var testId in selectedTests)
            {
                await orderRepo.AddTestResultAsync(order.OrderID, testId, null, null, userId);
            }

            TempData["SwalType"] = "success";
            TempData["SwalTitle"] = "Orden creada";
            TempData["SwalMessage"] = $"Orden {order.OrderNumber} creada exitosamente con {selectedTests.Count} exámenes.";
            return RedirectToAction("Details", new { orderId = order.OrderID });
        }

        public async Task<IActionResult> Details(int orderId)
        {
            var order = await orderRepo.GetOrderByIdAsync(orderId);
            if (order == null) return NotFound();

            var role = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            if (role == "Doctor")
            {
                var doctorIdClaim = HttpContext.User.FindFirstValue("DoctorID");
                if (int.TryParse(doctorIdClaim, out int doctorId))
                {
                    if (order.DoctorID != doctorId) return RedirectToAction("ErrorAcceso", "Auth");
                }
            }

            var results = await orderRepo.GetResultsByOrderAsync(orderId);
            ViewData["Results"] = results;

            return View(order);
        }

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int orderId)
        {
            var order = await orderRepo.GetOrderByIdAsync(orderId);
            if (order == null) return NotFound();

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

        [AuthorizeUsers(Policy = "AdminOrLab")]
        public async Task<IActionResult> EnterResults(int orderId)
        {
            var order = await orderRepo.GetOrderByIdAsync(orderId);
            if (order == null) return NotFound();

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
            if (!int.TryParse(userIdClaim, out int userId)) return RedirectToAction("Login", "Auth");

            int updatedCount = 0;
            int errorCount = 0;

            foreach (var kvp in resultValues)
            {
                int resultId = kvp.Key;
                string valueStr = kvp.Value;

                if (string.IsNullOrWhiteSpace(valueStr)) continue;

                if (TryParseResultValue(valueStr, out decimal resultValue))
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

                        bool success = await orderRepo.UpdateTestResultWithAuditAsync(
                            resultId, resultValue, validation.Status, userId, note
                        );

                        if (success) updatedCount++;
                        else errorCount++;
                    }
                }
                else
                {
                    errorCount++;
                }
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int orderId, string status)
        {
            var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId)) return RedirectToAction("Login", "Auth");

            var role = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            var order = await orderRepo.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Orden no encontrada.";
                return RedirectToAction("Index");
            }

            if (status == "Aprobada")
            {
                if (role != "Doctor")
                {
                    TempData["ErrorMessage"] = "Solo el médico puede aprobar la orden.";
                    return RedirectToAction("Details", new { orderId });
                }

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

            var previousStatus = order.Status;

            var statusResult = await orderRepo.ChangeOrderStatusAsync(orderId, status, userId);

            if (statusResult.Success)
            {
                if (status == "Completada" && previousStatus != "Completada")
                {
                    var doctorUsers = await catalogRepo.Context.Users
                        .Where(u => u.DoctorID == order.DoctorID && u.IsActive)
                        .ToListAsync();

                    foreach (var doctorUser in doctorUsers)
                    {
                        await catalogRepo.CreateNotificationAsync(
                            doctorUser.UserID,
                            "Resultados Listos",
                            $"La orden {order.OrderNumber} ha sido completada por el laboratorio y espera su aprobación.",
                            order.OrderID
                        );

                        var unreadCount = await catalogRepo.GetUnreadCountAsync(doctorUser.UserID);
                        await hubContext.Clients.User(doctorUser.UserID.ToString())
                            .SendAsync("NotificationUpdated", unreadCount);
                    }
                }

                if (status == "Aprobada" && previousStatus != "Aprobada")
                {
                    var labAndAdminUsers = await catalogRepo.Context.Users
                        .Include(u => u.Role)
                        .Where(u => u.IsActive && u.Role != null &&
                                    (u.Role.RoleName == "Laboratorio" || u.Role.RoleName == "Admin"))
                        .ToListAsync();

                    foreach (var targetUser in labAndAdminUsers)
                    {
                        await catalogRepo.CreateNotificationAsync(
                            targetUser.UserID,
                            "Orden Aprobada",
                            $"La orden {order.OrderNumber} fue aprobada por el médico y está lista para seguimiento/entrega.",
                            order.OrderID
                        );

                        var unreadCount = await catalogRepo.GetUnreadCountAsync(targetUser.UserID);
                        await hubContext.Clients.User(targetUser.UserID.ToString())
                            .SendAsync("NotificationUpdated", unreadCount);
                    }

                    // ==============================================================
                    // --- NUEVA LÓGICA PARA N8N Y PORTAL PACIENTE ---
                    // ==============================================================
                    var orderDetails = await orderRepo.GetOrderDetailsAsync(orderId);

                    if (orderDetails.Any() && !string.IsNullOrWhiteSpace(orderDetails.First().PatientPhone))
                    {
                        var firstDetail = orderDetails.First();

                        // 1. Generamos el Token usando TU tabla y modelo
                        var shareToken = await orderRepo.CreateOrderShareTokenAsync(order.OrderID);

                        // 2. Construimos la URL pública (usando el Guid generado)
                        var request = HttpContext.Request;
                        var baseUrl = $"{request.Scheme}://{request.Host}";
                        var portalUrl = $"{baseUrl}/Portal/Descargar/{shareToken.TokenID}";

                        // 3. Preparamos el Paquete para n8n
                        var payload = new
                        {
                            OrderId = order.OrderID,
                            OrderNumber = order.OrderNumber,
                            PatientName = firstDetail.PatientName,
                            PatientPhone = firstDetail.PatientPhone,
                            DoctorName = firstDetail.DoctorName,
                            PortalUrl = portalUrl,             // <-- URL lista
                            PortalPin = shareToken.PinCode,    // <-- PIN listo
                            Results = orderDetails.Select(r => new {
                                Test = r.TestName,
                                Value = r.ResultValue,
                                Units = r.Units,
                                Status = r.ValidationStatus
                            }).ToList()
                        };

                        string jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);

                        // Guardamos el evento en la cola para que n8n lo recoja
                        await orderRepo.CreateIntegrationEventAsync("OrderApproved", jsonPayload);
                    }
                    // ==============================================================
                }

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

        public async Task<IActionResult> Print(int orderId)
        {
            var order = await orderRepo.GetOrderByIdAsync(orderId);
            if (order == null) return NotFound();

            if (order.Status != "Aprobada")
            {
                TempData["ErrorMessage"] = "Acceso denegado: La orden debe estar 'Aprobada' por el médico para poder imprimirse.";
                return RedirectToAction("Details", new { orderId = orderId });
            }

            var role = HttpContext.User.FindFirstValue(ClaimTypes.Role);
            if (role == "Doctor")
            {
                var doctorIdClaim = HttpContext.User.FindFirstValue("DoctorID");
                if (int.TryParse(doctorIdClaim, out int doctorId))
                {
                    if (order.DoctorID != doctorId) return RedirectToAction("ErrorAcceso", "Auth");
                }
            }

            var results = await orderRepo.GetResultsByOrderAsync(orderId);
            var pdfBytes = pdfReportService.GenerateResultsPdf(order, results);

            return File(pdfBytes, "application/pdf", $"Resultados_{order.OrderNumber}.pdf");
        }

        private static bool TryParseResultValue(string input, out decimal value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            var raw = input.Trim().Replace(" ", string.Empty);
            string normalized;

            if (raw.Contains(',') && raw.Contains('.'))
            {
                bool commaIsDecimal = raw.LastIndexOf(',') > raw.LastIndexOf('.');
                normalized = commaIsDecimal
                    ? raw.Replace(".", string.Empty).Replace(',', '.')
                    : raw.Replace(",", string.Empty);
            }
            else if (raw.Contains(','))
            {
                normalized = raw.Replace(',', '.');
            }
            else
            {
                normalized = raw;
            }

            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }
    }
}