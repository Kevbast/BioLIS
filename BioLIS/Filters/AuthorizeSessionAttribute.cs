using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BioLIS.Filters
{
    /// <summary>
    /// Filtro de autorización basado en sesiones
    /// Valida que el usuario esté autenticado antes de acceder a una acción
    /// </summary>
    public class AuthorizeSessionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserID");

            if (!userId.HasValue)
            {
                // No hay sesión activa, redirigir al login
                var returnUrl = context.HttpContext.Request.Path;
                context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl });
            }

            base.OnActionExecuting(context);
        }
    }

    /// <summary>
    /// Filtro de autorización por rol
    /// Valida que el usuario tenga el rol requerido
    /// </summary>
    public class AuthorizeRoleAttribute : ActionFilterAttribute
    {
        private readonly string[] _allowedRoles;

        public AuthorizeRoleAttribute(params string[] roles)
        {
            _allowedRoles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserID");
            var userRole = context.HttpContext.Session.GetString("Role");

            if (!userId.HasValue)
            {
                // No hay sesión activa
                var returnUrl = context.HttpContext.Request.Path;
                context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl });
                return;
            }

            if (!string.IsNullOrEmpty(userRole) && !_allowedRoles.Contains(userRole))
            {
                // Usuario autenticado pero sin permisos
                context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
            }

            base.OnActionExecuting(context);
        }
    }
}
