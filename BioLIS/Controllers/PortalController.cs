using BioLIS.Data;
using BioLIS.Models;
using BioLIS.Repositories;
using BioLIS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioLIS.Controllers
{
    public class PortalController : Controller
    {
        private readonly OrderRepository orderRepo;
        private readonly PdfReportService pdfService;
        private readonly LaboratorioContext context;

        public PortalController(OrderRepository orderRepo, PdfReportService pdfService, LaboratorioContext context)
        {
            this.orderRepo = orderRepo;
            this.pdfService = pdfService;
            this.context = context;
        }

        // 1. Mostrar la pantalla para pedir el PIN
        [HttpGet]
        [Route("Portal/Descargar/{tokenId}")]
        public async Task<IActionResult> Descargar(Guid tokenId)
        {
            var tokenRecord = await this.context.OrderShareTokens
                .FirstOrDefaultAsync(t => t.TokenID == tokenId && t.IsActive);

            if (tokenRecord == null || tokenRecord.ExpiresAt < DateTime.Now)
            {
                return View("TokenExpirado"); // Crearemos esta vista luego
            }

            // Le pasamos el TokenID a la vista para que lo envíe en el formulario
            return View(tokenId);
        }

        // 2. Procesar el PIN y descargar el PDF
        [HttpPost]
        [Route("Portal/Descargar/{tokenId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Descargar(Guid tokenId, string pinCode)
        {
            var tokenRecord = await this.context.OrderShareTokens
                .FirstOrDefaultAsync(t => t.TokenID == tokenId && t.IsActive);

            if (tokenRecord == null || tokenRecord.ExpiresAt < DateTime.Now)
            {
                return View("TokenExpirado");
            }

            if (tokenRecord.PinCode != pinCode)
            {
                TempData["ErrorMessage"] = "PIN incorrecto. Inténtelo de nuevo.";
                return View("Descargar", tokenId);
            }

            // Si el PIN es correcto, traemos la orden y generamos el PDF
            var order = await orderRepo.GetOrderByIdAsync(tokenRecord.OrderID);
            var results = await orderRepo.GetResultsByOrderAsync(tokenRecord.OrderID);

            var pdfBytes = pdfService.GenerateResultsPdf(order, results);

            // Sumamos 1 a las descargas
            tokenRecord.DownloadsCount += 1;
            await this.context.SaveChangesAsync();

            return File(pdfBytes, "application/pdf", $"Resultados_BioLIS_{order.OrderNumber}.pdf");
        }
    }
}