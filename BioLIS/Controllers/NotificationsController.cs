using BioLIS.Filters;
using BioLIS.Hubs;
using BioLIS.Models;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BioLIS.Controllers
{
    [AuthorizeUsers]
    public class NotificationsController : Controller
    {
        private readonly CatalogRepository repo;
        private readonly IHubContext<NotificationHub> hubContext;

        public NotificationsController(CatalogRepository repo, IHubContext<NotificationHub> hubContext)
        {
            this.repo = repo;
            this.hubContext = hubContext;
        }

        public async Task<IActionResult> Index(string? typeFilter = null, string? userFilter = null)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var role = User.FindFirstValue(ClaimTypes.Role);
            bool isAdmin = role == "Admin";

            ViewData["IsAdmin"] = isAdmin;
            ViewData["TypeFilter"] = typeFilter ?? "all";
            ViewData["UserFilter"] = userFilter ?? "all";

            if (isAdmin)
            {
                var allNotifications = await repo.GetAllNotificationsAsync();

                ViewData["AllNotificationUsers"] = allNotifications
                    .Where(n => n.User != null && !string.IsNullOrWhiteSpace(n.User.Username))
                    .Select(n => n.User!.Username)
                    .Distinct()
                    .OrderBy(u => u)
                    .ToList();

                IEnumerable<Notification> query = allNotifications;

                if (!string.IsNullOrWhiteSpace(typeFilter) && typeFilter != "all")
                {
                    query = typeFilter switch
                    {
                        "critica" => query.Where(n =>
                            ($"{n.Title} {n.Message}").ToLower().Contains("urgente") ||
                            ($"{n.Title} {n.Message}").ToLower().Contains("crítico") ||
                            ($"{n.Title} {n.Message}").ToLower().Contains("critico")),
                        "laboratorio" => query.Where(n =>
                            ($"{n.Title} {n.Message}").ToLower().Contains("completad") ||
                            ($"{n.Title} {n.Message}").ToLower().Contains("resultado")),
                        "medico" => query.Where(n =>
                            ($"{n.Title} {n.Message}").ToLower().Contains("aprobad")),
                        "general" => query.Where(n =>
                            !($"{n.Title} {n.Message}").ToLower().Contains("urgente") &&
                            !($"{n.Title} {n.Message}").ToLower().Contains("crítico") &&
                            !($"{n.Title} {n.Message}").ToLower().Contains("critico") &&
                            !($"{n.Title} {n.Message}").ToLower().Contains("completad") &&
                            !($"{n.Title} {n.Message}").ToLower().Contains("resultado") &&
                            !($"{n.Title} {n.Message}").ToLower().Contains("aprobad")),
                        _ => query
                    };
                }

                if (!string.IsNullOrWhiteSpace(userFilter) && userFilter != "all")
                {
                    query = query.Where(n => n.User != null && n.User.Username == userFilter);
                }

                return View(query.ToList());
            }

            var list = await repo.GetUserNotificationsAsync(userId);

            await repo.MarkAllAsReadByUserAsync(userId);
            await hubContext.Clients.User(userId.ToString()).SendAsync("NotificationUpdated", 0);

            foreach (var notification in list)
            {
                notification.IsRead = true;
            }

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return Json(new { count = 0 });
            }

            int count = await repo.GetUnreadCountAsync(userId);
            return Json(new { count });
        }
    }
}
