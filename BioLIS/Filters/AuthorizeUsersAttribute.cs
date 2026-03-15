
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BioLIS.Filters
{
    /// <summary>
    /// Filtro de autorización personalizado para BioLIS
    /// Soporta Policies y guarda ruta para redirect post-login
    /// </summary>
    public class AuthorizeUsersAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Guardar en TempData para redirigir después del login
            ITempDataProvider provider = context.HttpContext.RequestServices.GetService<ITempDataProvider>();
            var tempData = provider.LoadTempData(context.HttpContext);

            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            tempData["returnUrl"] = returnUrl;

            provider.SaveTempData(context.HttpContext, tempData);

            // Verificar autenticación
            if (user.Identity?.IsAuthenticated != true)
            {
                context.Result = GetRoute("Auth", "Login");
            }
        }

        private RedirectToRouteResult GetRoute(string controller, string action)
        {
            RouteValueDictionary ruta = new RouteValueDictionary(new
            {
                controller = controller,
                action = action
            });

            return new RedirectToRouteResult(ruta);
        }
    }
}