using BioLIS.Filters;
using BioLIS.Models.Entities;
using BioLIS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BioLIS.Controllers
{
    [AuthorizeUsers(Policy = "AdminOnly")]
    public class LabTestsController : Controller
    {
        private readonly ApiService api;
        public LabTestsController(ApiService api) => this.api = api;

        public async Task<IActionResult> Index()
            => View(await this.api.GetLabTestsAsync() ?? new());

        public async Task<IActionResult> Create()
        { await LoadSampleTypes(); return View(); }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string testName, string? units, int sampleId)
        {
            if (string.IsNullOrWhiteSpace(testName))
            { TempData["ErrorMessage"] = "El nombre del examen es obligatorio."; return RedirectToAction("Create"); }
            var r = await this.api.CreateLabTestAsync(testName, units, sampleId);
            TempData["SwalType"]    = r.Success ? "success" : "error";
            TempData["SwalTitle"]   = r.Success ? "Examen creado" : "Error";
            TempData["SwalMessage"] = r.Success ? $"'{testName}' creado." : r.Body;
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Update(int testId)
        {
            var lt = await this.api.GetLabTestByIdAsync(testId);
            if (lt == null) return NotFound();
            await LoadSampleTypes(lt.SampleID);
            return View(lt);
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int testId, string testName, string? units, int sampleId)
        {
            var r = await this.api.UpdateLabTestAsync(testId, testName.Trim(), units?.Trim(), sampleId);
            TempData["SwalType"]    = r.Success ? "success" : "error";
            TempData["SwalTitle"]   = r.Success ? "Actualizado" : "Error";
            TempData["SwalMessage"] = r.Body;
            return RedirectToAction(r.Success ? "Index" : "Update", r.Success ? null : new { testId });
        }

        public async Task<IActionResult> Delete(int testId)
        {
            var lt = await this.api.GetLabTestByIdAsync(testId);
            if (lt == null) return NotFound();
            return View(lt);
        }

        [HttpPost, ActionName("Delete")][ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int testId)
        {
            var r = await this.api.DeleteLabTestAsync(testId);
            TempData["SwalType"]    = r.Success ? "success" : "error";
            TempData["SwalTitle"]   = r.Success ? "Eliminado" : "No se pudo eliminar";
            TempData["SwalMessage"] = r.Body;
            return RedirectToAction("Index");
        }

        private async Task LoadSampleTypes(int? selectedId = null)
        {
            var st = await this.api.GetSampleTypesAsync() ?? new();
            ViewData["SampleTypes"] = st.Select(s => new SelectListItem
            {
                Value    = s.SampleID.ToString(),
                Text     = $"{s.SampleName} (Tubo {s.ContainerColor})",
                Selected = s.SampleID == selectedId
            }).ToList();
        }
    }
}
