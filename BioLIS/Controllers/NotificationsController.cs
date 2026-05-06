using BioLIS.Filters;
using BioLIS.Models.Entities;
using BioLIS.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BioLIS.Controllers
{
    [AuthorizeUsers]
    public class NotificationsController : Controller
    {
        private readonly ApiService api;

        public NotificationsController(ApiService api)
        {
            this.api = api;
        }

        public async Task<IActionResult> Index(int page = 1, string? typeFilter = null, string? userFilter = null)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return RedirectToAction("Login", "Auth");

            // Tamaño de página
            int pageSize = 15;

            var role = User.FindFirstValue(ClaimTypes.Role);
            bool isAdmin = role == BioLIS.Models.Common.UserRoles.Admin;

            ViewData["IsAdmin"] = isAdmin;
            ViewData["TypeFilter"] = typeFilter ?? "all";
            ViewData["UserFilter"] = userFilter ?? "all";

            if (isAdmin)
            {
                var all = await this.api.GetAllNotificationsAsync() ?? new();

                ViewData["AllNotificationUsers"] = all
                    .Where(n => n.User != null && !string.IsNullOrWhiteSpace(n.User.Username))
                    .Select(n => n.User!.Username).Distinct().OrderBy(u => u).ToList();

                IEnumerable<Notification> query = all;
                if (!string.IsNullOrWhiteSpace(typeFilter) && typeFilter != "all")
                {
                    query = typeFilter switch
                    {
                        "critica" => query.Where(n => ($"{n.Title} {n.Message}").ToLower().Contains("urgente") || ($"{n.Title} {n.Message}").ToLower().Contains("critico")),
                        "laboratorio" => query.Where(n => ($"{n.Title} {n.Message}").ToLower().Contains("completad") || ($"{n.Title} {n.Message}").ToLower().Contains("resultado")),
                        "medico" => query.Where(n => ($"{n.Title} {n.Message}").ToLower().Contains("aprobad")),
                        _ => query
                    };
                }
                if (!string.IsNullOrWhiteSpace(userFilter) && userFilter != "all")
                    query = query.Where(n => n.User != null && n.User.Username == userFilter);

                return View(query.ToList());
            }

            // PARA EL USUARIO NORMAL: Usamos Paginación
            var pagedResult = await this.api.GetMyNotificationsPagedAsync(page, pageSize);

            // Marcamos todas como leídas en la API
            await this.api.MarkAllNotificationsAsReadAsync();

            // Pasamos .Items a la vista
            return View(pagedResult?.Items ?? new List<Notification>());
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out _))
                return Json(new { count = 0 });

            return Json(new { count = await this.api.GetUnreadCountAsync() });
        }

        [HttpGet]
        public async Task<IActionResult> LatestUnread()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out _))
                return Json(new { notifications = Array.Empty<object>() });

            // Usamos el método paginado para pedir solo las primeras 5 "No leídas"
            var pagedResult = await this.api.GetMyNotificationsPagedAsync(1, 5, false);

            var latest = pagedResult?.Items?
                .Select(n => (object)new // <--- AÑADIMOS (object) AQUÍ
                {
                    n.Title,
                    n.Message,
                    n.CreatedAt
                })
                .ToList() ?? new List<object>();

            return Json(new { notifications = latest });
        }

    }
}