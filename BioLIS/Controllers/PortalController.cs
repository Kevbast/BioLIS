using BioLIS.Services;
using Microsoft.AspNetCore.Mvc;

namespace BioLIS.Controllers
{
    public class PortalController : Controller
    {
        private readonly ApiService api;
        private readonly PdfReportService pdfService;

        public PortalController(ApiService api, PdfReportService pdfService)
        {
            this.api        = api;
            this.pdfService = pdfService;
        }

        [HttpGet]
        [Route("Portal/Descargar/{tokenId}")]
        public async Task<IActionResult> Descargar(Guid tokenId)
        {
            var info = await this.api.GetPortalTokenInfoAsync(tokenId);
            if (info == null || !info.Valid) return View("TokenExpirado");
            return View(tokenId);
        }

        [HttpPost]
        [Route("Portal/Descargar/{tokenId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Descargar(Guid tokenId, string pinCode)
        {
            var validate = await this.api.ValidatePortalPinAsync(tokenId, pinCode);
            if (validate == null || validate.Order == null)
            {
                TempData["ErrorMessage"] = "PIN incorrecto. Inténtelo de nuevo.";
                return View("Descargar", tokenId);
            }

            // Le pasamos directamente los DTOs que vienen en validate
            var pdf = this.pdfService.GenerateResultsPdf(validate.Order, validate.Results ?? new());
            return File(pdf, "application/pdf", $"Resultados_BioLIS_{validate.Order.OrderNumber}.pdf");
        }
    }
}
