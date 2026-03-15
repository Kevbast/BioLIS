using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BioLIS.Models;
using BioLIS.Filters;
using BioLIS.Repositories;
using System.Security.Claims;

namespace BioLIS.Controllers
{
    [AuthorizeUsers] // CAMBIADO de [AuthorizeSession]
    public class HomeController : Controller
    {
        private readonly CatalogRepository _catalogRepo;
        private readonly OrderRepository _orderRepo;

        // Constructor con repositorios para obtener estadísticas
        public HomeController(CatalogRepository catalogRepo, OrderRepository orderRepo)
        {
            _catalogRepo = catalogRepo;
            _orderRepo = orderRepo;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Obtener datos del usuario desde Claims
                var username = HttpContext.User.FindFirstValue(ClaimTypes.Name);
                var role = HttpContext.User.FindFirstValue(ClaimTypes.Role);

                ViewBag.Username = username;
                ViewBag.Role = role;

                // Estadísticas generales
                var patients = await _catalogRepo.GetPatientsAsync();
                var doctors = await _catalogRepo.GetDoctorsAsync();
                var labTests = await _catalogRepo.GetLabTestsAsync();
                var allOrders = await _orderRepo.GetAllOrdersAsync();

                // Órdenes de hoy
                var todayOrders = allOrders.Where(o => o.OrderDate.Date == DateTime.Today).ToList();

                ViewBag.TodayOrders = todayOrders.Count;
                ViewBag.TotalPatients = patients.Count;
                ViewBag.TotalDoctors = doctors.Count;
                ViewBag.TotalTests = labTests.Count;

                // Si es Doctor, mostrar solo SUS órdenes recientes
                if (role == "Doctor")
                {
                    var doctorIdClaim = HttpContext.User.FindFirstValue("DoctorID");
                    if (int.TryParse(doctorIdClaim, out int doctorId))
                    {
                        var myOrders = await _orderRepo.GetOrdersByDoctorAsync(doctorId);
                        ViewBag.RecentOrders = myOrders.OrderByDescending(o => o.OrderDate).Take(5).ToList();
                        ViewBag.MyTodayOrders = myOrders.Count(o => o.OrderDate.Date == DateTime.Today);
                    }
                }
                else
                {
                    // Admin y Laboratorio ven las últimas 5 órdenes de todos
                    ViewBag.RecentOrders = allOrders.OrderByDescending(o => o.OrderDate).Take(5).ToList();
                }

                return View();
            }
            catch
            {
                // En caso de error, mostrar valores por defecto
                ViewBag.TodayOrders = 0;
                ViewBag.TotalPatients = 0;
                ViewBag.TotalDoctors = 0;
                ViewBag.TotalTests = 0;
                ViewBag.RecentOrders = new List<BioLab.Models.Order>();
                return View();
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}