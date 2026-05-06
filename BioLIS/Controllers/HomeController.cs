using BioLIS.Filters;
using BioLIS.Models.Entities;
using BioLIS.Models.Common;
using BioLIS.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using BioLIS.Models;

namespace BioLIS.Controllers
{
    [AuthorizeUsers]
    public class HomeController : Controller
    {
        private readonly ApiService api;
        public HomeController(ApiService api) => this.api = api;

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewBag.Username = HttpContext.User.FindFirstValue(ClaimTypes.Name);
                var role = HttpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
                ViewBag.Role = role;

                int? doctorId = null;
                if (role == BioLIS.Models.Common.UserRoles.Doctor)
                {
                    var doctorIdClaim = HttpContext.User.FindFirstValue("DoctorID");
                    if (int.TryParse(doctorIdClaim, out int parsedDoctorId))
                        doctorId = parsedDoctorId;
                }

                var patientsTask = this.api.GetPatientsAsync();
                var doctorsTask = this.api.GetDoctorsAsync();
                var labTestsTask = this.api.GetLabTestsAsync();
                Task<List<Order>?> ordersTask = role == BioLIS.Models.Common.UserRoles.Doctor && doctorId.HasValue
                    ? this.api.GetOrdersByDoctorAsync(doctorId.Value)
                    : this.api.GetAllOrdersAsync();

                await Task.WhenAll(patientsTask, doctorsTask, labTestsTask, ordersTask);

                var patients = patientsTask.Result ?? new();
                var doctors = doctorsTask.Result ?? new();
                var labTests = labTestsTask.Result ?? new();
                var orders = ordersTask.Result ?? new();

                ViewBag.TodayOrders = orders.Count(o => o.OrderDate.Date == DateTime.Today);
                ViewBag.TotalPatients = patients.Count;
                ViewBag.TotalDoctors = doctors.Count;
                ViewBag.TotalTests = labTests.Count;

                List<Order> chartOrders;
                if (role == BioLIS.Models.Common.UserRoles.Doctor)
                {
                    ViewBag.RecentOrders = orders.OrderByDescending(o => o.OrderDate).Take(5).ToList();
                    ViewBag.MyTodayOrders = orders.Count(o => o.OrderDate.Date == DateTime.Today);
                    chartOrders = orders;
                }
                else
                {
                    ViewBag.RecentOrders = orders.OrderByDescending(o => o.OrderDate).Take(5).ToList();
                    chartOrders = orders;
                }

                var statusCounts = chartOrders
                    .GroupBy(o => string.IsNullOrWhiteSpace(o.Status) ? "Pendiente" : o.Status)
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Label).ToList();

                var last7Days   = Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(-6 + i)).ToList();
                var dailyCounts = last7Days.Select(d => chartOrders.Count(o => o.OrderDate.Date == d)).ToList();

                ViewData["ChartStatusLabels"] = JsonSerializer.Serialize(statusCounts.Select(x => x.Label));
                ViewData["ChartStatusValues"] = JsonSerializer.Serialize(statusCounts.Select(x => x.Count));
                ViewData["ChartDayLabels"]    = JsonSerializer.Serialize(last7Days.Select(d => d.ToString("dd/MM")));
                ViewData["ChartDayValues"]    = JsonSerializer.Serialize(dailyCounts);
            }
            catch
            {
                ViewBag.TodayOrders  = 0; ViewBag.TotalPatients = 0;
                ViewBag.TotalDoctors = 0; ViewBag.TotalTests    = 0;
                ViewBag.RecentOrders = new List<Order>();
            }
            return View();
        }

        public IActionResult Privacy() => View();
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
