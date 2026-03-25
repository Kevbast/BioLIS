using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BioLIS.Models;
using BioLIS.Filters;
using BioLIS.Repositories;
using System.Security.Claims;
using System.Text.Json;

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
                List<BioLIS.Models.Order> chartOrders;
                if (role == "Doctor")
                {
                    var doctorIdClaim = HttpContext.User.FindFirstValue("DoctorID");
                    if (int.TryParse(doctorIdClaim, out int doctorId))
                    {
                        var myOrders = await _orderRepo.GetOrdersByDoctorAsync(doctorId);
                        ViewBag.RecentOrders = myOrders.OrderByDescending(o => o.OrderDate).Take(5).ToList();
                        ViewBag.MyTodayOrders = myOrders.Count(o => o.OrderDate.Date == DateTime.Today);
                        chartOrders = myOrders;
                    }
                    else
                    {
                        chartOrders = new List<BioLIS.Models.Order>();
                    }
                }
                else
                {
                    // Admin y Laboratorio ven las últimas 5 órdenes de todos
                    ViewBag.RecentOrders = allOrders.OrderByDescending(o => o.OrderDate).Take(5).ToList();
                    chartOrders = allOrders;
                }

                // Datos para gráficos (Chart.js)
                var statusCounts = chartOrders
                    .GroupBy(o => string.IsNullOrWhiteSpace(o.Status) ? "Pendiente" : o.Status)
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Label)
                    .ToList();

                var last7Days = Enumerable.Range(0, 7)
                    .Select(offset => DateTime.Today.AddDays(-6 + offset))
                    .ToList();

                var dailyCounts = last7Days
                    .Select(day => chartOrders.Count(o => o.OrderDate.Date == day))
                    .ToList();

                ViewData["ChartStatusLabels"] = JsonSerializer.Serialize(statusCounts.Select(x => x.Label));
                ViewData["ChartStatusValues"] = JsonSerializer.Serialize(statusCounts.Select(x => x.Count));
                ViewData["ChartDayLabels"] = JsonSerializer.Serialize(last7Days.Select(d => d.ToString("dd/MM")));
                ViewData["ChartDayValues"] = JsonSerializer.Serialize(dailyCounts);

                return View();
            }
            catch
            {
                // En caso de error, mostrar valores por defecto
                ViewBag.TodayOrders = 0;
                ViewBag.TotalPatients = 0;
                ViewBag.TotalDoctors = 0;
                ViewBag.TotalTests = 0;
                ViewBag.RecentOrders = new List<BioLIS.Models.Order>();
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