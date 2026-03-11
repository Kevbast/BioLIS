using BioLab.Models;
using BioLIS.Filters;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BioLIS.Controllers
{
    [AuthorizeSession] // Todos los usuarios autenticados pueden acceder
    public class OrdersController : Controller
    {
        private readonly OrderRepository orderRepo;
        private readonly CatalogRepository catalogRepo;
        private readonly HelperRepository helperRepo;

        public OrdersController(OrderRepository orderRepo, CatalogRepository catalogRepo, HelperRepository helperRepo)
        {
            this.orderRepo = orderRepo;
            this.catalogRepo = catalogRepo;
            this.helperRepo = helperRepo;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserID");

            List<Order> orders;

            // Si es Doctor, solo ver SUS órdenes
            if (role == "Doctor")
            {
                // Obtener el DoctorID del usuario logueado
                var user = await catalogRepo.Context.Users.FindAsync(userId);
                if (user?.DoctorID.HasValue == true)
                {
                    orders = await orderRepo.GetOrdersByDoctorAsync(user.DoctorID.Value);
                }
                else
                {
                    orders = new List<Order>(); // Sin órdenes si no tiene DoctorID
                }
            }
            else
            {
                // Admin y Laboratorio ven todas
                orders = await orderRepo.GetAllOrdersAsync();
            }

            return View(orders);
        }

        // GET: Orders/Create
        [AuthorizeRole("Admin", "Laboratorio")] // Solo Admin y Laboratorio pueden crear órdenes
        public async Task<IActionResult> Create()
        {
            // Obtener lista de pacientes
            var patients = await catalogRepo.GetPatientsAsync();
            ViewData["Patients"] = patients.Select(p => new SelectListItem
            {
                Value = p.PatientID.ToString(),
                Text = $"{p.FirstName} {p.LastName} - {p.PatientID}"
            }).ToList();

            // Obtener lista de doctores
            var doctors = await catalogRepo.GetDoctorsAsync();
            ViewData["Doctors"] = doctors.Select(d => new SelectListItem
            {
                Value = d.DoctorID.ToString(),
                Text = $"{d.FullName} ({d.LicenseNumber ?? "Sin licencia"})"
            }).ToList();

            // Obtener lista de exámenes disponibles
            var labTests = await catalogRepo.GetLabTestsAsync();
            ViewData["LabTests"] = labTests;

            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole("Admin", "Laboratorio")]
        public async Task<IActionResult> Create(int patientId, int doctorId, List<int> selectedTests)
        {
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

            // Crear la orden
            var order = await orderRepo.CreateOrderAsync(patientId, doctorId);

            // Agregar los exámenes seleccionados como resultados pendientes
            foreach (var testId in selectedTests)
            {
                await orderRepo.AddTestResultAsync(order.OrderID, testId);
            }

            TempData["SuccessMessage"] = $"Orden {order.OrderNumber} creada exitosamente con {selectedTests.Count} exámenes.";
            return RedirectToAction("Details", new { orderId = order.OrderID });
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int orderId)
        {
            var order = await orderRepo.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            // Verificar permisos: Doctor solo puede ver sus órdenes
            var role = HttpContext.Session.GetString("Role");
            if (role == "Doctor")
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                var user = await catalogRepo.Context.Users.FindAsync(userId);
                
                if (user?.DoctorID != order.DoctorID)
                {
                    return RedirectToAction("AccessDenied", "Auth");
                }
            }

            // Obtener resultados con detalles
            var results = await orderRepo.GetResultsByOrderAsync(orderId);
            ViewData["Results"] = results;

            return View(order);
        }

        // GET: Orders/Delete/5
        [AuthorizeRole("Admin")] // Solo Admin puede eliminar órdenes
        public async Task<IActionResult> Delete(int orderId)
        {
            var order = await orderRepo.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeRole("Admin")]
        public async Task<IActionResult> DeleteConfirmed(int orderId)
        {
            var result = await orderRepo.DeleteOrderAsync(orderId);

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

        // GET: Orders/EnterResults/5
        [AuthorizeRole("Admin", "Laboratorio")] // Solo Admin y Laboratorio pueden ingresar resultados
        public async Task<IActionResult> EnterResults(int orderId)
        {
            var order = await orderRepo.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            // Obtener resultados con detalles
            var results = await orderRepo.GetResultsByOrderAsync(orderId);
            ViewData["Results"] = results;

            // Obtener resumen de la orden
            var summary = await orderRepo.GetOrderSummaryAsync(orderId);
            ViewData["Summary"] = summary;

            return View(order);
        }

        // POST: Orders/EnterResults
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole("Admin", "Laboratorio")]
        public async Task<IActionResult> EnterResults(int orderId, Dictionary<int, string> resultValues, Dictionary<int, string> notes)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            int updatedCount = 0;
            int errorCount = 0;

            foreach (var kvp in resultValues)
            {
                int resultId = kvp.Key;
                string valueStr = kvp.Value;

                // Saltar si está vacío
                if (string.IsNullOrWhiteSpace(valueStr))
                    continue;

                // Intentar parsear el valor
                if (decimal.TryParse(valueStr, out decimal resultValue))
                {
                    // Obtener nota si existe
                    string note = notes.ContainsKey(resultId) ? notes[resultId] : null;

                    // Obtener el TestResult para validación
                    var testResult = await catalogRepo.Context.TestResults
                        .Include(tr => tr.Order)
                        .FirstOrDefaultAsync(tr => tr.ResultID == resultId);

                    if (testResult != null)
                    {
                        // Validar y obtener AlertLevel usando HelperRepository
                        var validation = await helperRepo.ValidateResultAsync(
                            testResult.TestID,
                            testResult.Order.PatientID,
                            resultValue
                        );

                        string alertLevel = validation.Status;

                        // Actualizar con auditoría
                        bool success = await orderRepo.UpdateTestResultWithAuditAsync(
                            resultId, resultValue, alertLevel, userId.Value, note
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

            if (updatedCount > 0)
            {
                TempData["SuccessMessage"] = $"{updatedCount} resultado(s) actualizado(s) exitosamente.";
            }

            if (errorCount > 0)
            {
                TempData["ErrorMessage"] = $"{errorCount} resultado(s) no pudieron ser actualizados.";
            }

            return RedirectToAction("Details", new { orderId });
        }
    }
}
