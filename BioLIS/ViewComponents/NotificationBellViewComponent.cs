using BioLIS.Repositories;
using BioLIS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BioLIS.ViewComponents
{
    public class NotificationBellViewComponent:ViewComponent
    {
        private CatalogRepository catalogrepo;

        public NotificationBellViewComponent(CatalogRepository catalogrepo)
        {
            this.catalogrepo = catalogrepo;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdClaim = ((ClaimsPrincipal)User).FindFirstValue(ClaimTypes.NameIdentifier);
            var model = new NotificationBellViewModel();

            if (int.TryParse(userIdClaim, out int userId))
            {
                model.UnreadCount = await catalogrepo.GetUnreadCountAsync(userId);
                model.LatestUnread = await catalogrepo.GetLatestUnreadNotificationsAsync(userId, 5);
            }

            return View(model);
        }

    }
}
