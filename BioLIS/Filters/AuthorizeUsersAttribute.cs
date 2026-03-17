
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

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

            var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();

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
                return;
            }

            // Verificar Policy (si existe)
            if (!string.IsNullOrWhiteSpace(this.Policy))
            {
                var policyResult = authService
                    .AuthorizeAsync(user, null, this.Policy)
                    .GetAwaiter()
                    .GetResult();

                if (!policyResult.Succeeded)
                {
                    context.Result = GetRoute("Auth", "ErrorAcceso");
                    return;
                }
            }

            // Verificar Roles (si existen)
            if (!string.IsNullOrWhiteSpace(this.Roles))
            {
                var roles = this.Roles
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim())
                    .ToList();

                if (roles.Any() && !roles.Any(user.IsInRole))
                {
                    context.Result = GetRoute("Auth", "ErrorAcceso");
                    return;
                }
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