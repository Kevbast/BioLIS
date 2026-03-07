using BioLab.Models;
using BioLIS.Filters;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BioLIS.Controllers
{
    [AuthorizeSession] // Todos los usuarios autenticados pueden acceder
    public class OrdersController : Controller
    {
        private readonly OrderRepository orderRepo;
        private readonly CatalogRepository catalogRepo;

        public OrdersController(OrderRepository orderRepo, CatalogRepository catalogRepo)
        {
            this.orderRepo = orderRepo;
            this.catalogRepo = catalogRepo;
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
            ViewBag.Patients = patients.Select(p => new SelectListItem
            {
                Value = p.PatientID.ToString(),
                Text = $"{p.FirstName} {p.LastName} - {p.PatientID}"
            }).ToList();

            // Obtener lista de doctores
            var doctors = await catalogRepo.GetDoctorsAsync();
            ViewBag.Doctors = doctors.Select(d => new SelectListItem
            {
                Value = d.DoctorID.ToString(),
                Text = $"{d.FullName} ({d.LicenseNumber ?? "Sin licencia"})"
            }).ToList();

            // Obtener lista de exámenes disponibles
            var labTests = await catalogRepo.GetLabTestsAsync();
            ViewBag.LabTests = labTests;

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
            ViewBag.Results = results;

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
    }
}
