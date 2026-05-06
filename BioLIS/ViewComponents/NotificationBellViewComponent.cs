using BioLIS.Models.Entities;
using BioLIS.Models.ViewModels;
using BioLIS.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BioLIS.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly ApiService api;

        public NotificationBellViewComponent(ApiService api)
        {
            this.api = api;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (UserClaimsPrincipal?.Identity?.IsAuthenticated != true)
                return View(new NotificationBellViewModel { UnreadCount = 0, LatestUnread = new() });

            // Hacemos SOLO UNA llamada a la API
            var recentPage = await this.api.GetMyNotificationsPagedAsync(1, 5, false);

            return View(new NotificationBellViewModel
            {
                // El TotalItems del PagedResult es nuestro UnreadCount
                UnreadCount = recentPage?.TotalItems ?? 0,
                LatestUnread = recentPage?.Items ?? new List<Notification>()
            });
        }
    }
}